using System;
using Sandbox.Definitions;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using VRage.Game;
using VRage.ModAPI;
using VRage.Voxels;
using VRageMath;

namespace Equinox.Utils.Misc
{
    public static class VoxelExtensions
    {
        public static void ClampVoxelCoord(this IMyStorage self, ref Vector3I voxelCoord, int distance = 1)
        {
            if (self == null)
                return;
            Vector3I max = self.Size - distance;
            Vector3I.Clamp(ref voxelCoord, ref Vector3I.Zero, ref max, out voxelCoord);
        }

        public static MyDefinitionId? VoxelMaterialAt(this MyVoxelBase voxel, Vector3D min, Vector3D grow,
            ref MyStorageData cache)
        {
            if (cache == null)
                cache = new MyStorageData();
            var shape = new BoundingBoxD(Vector3D.Min(min, min + grow), Vector3D.Max(min, min + grow));
            Vector3I voxMin;
            Vector3I voxMax;
            Vector3I voxCells;
            GetVoxelShapeDimensions(voxel, shape, out voxMin, out voxMax, out voxCells);
            Vector3I_RangeIterator cellsItr = new Vector3I_RangeIterator(ref Vector3I.Zero, ref voxCells);
            while (cellsItr.IsValid())
            {
                Vector3I cellMinCorner;
                Vector3I cellMaxCorner;
                GetCellCorners(ref voxMin, ref voxMax, ref cellsItr, out cellMinCorner, out cellMaxCorner);
                Vector3I rangeMin = cellMinCorner - 1;
                Vector3I rangeMax = cellMaxCorner + 1;
                voxel.Storage.ClampVoxelCoord(ref rangeMin);
                voxel.Storage.ClampVoxelCoord(ref rangeMax);
                cache.Resize(rangeMin, rangeMax);
                voxel.Storage.ReadRange(cache, MyStorageDataTypeFlags.Material, 0, rangeMin, rangeMax);

                var mortonCode = -1;
                var maxMortonCode = cache.Size3D.Size;
                while (++mortonCode < maxMortonCode)
                {
                    Vector3I pos;
                    MyMortonCode3D.Decode(mortonCode, out pos);
                    var content = cache.Content(ref pos);
                    if (content <= MyVoxelConstants.VOXEL_CONTENT_EMPTY)
                        continue;
                    var material = cache.Material(ref pos);
                    var def = MyDefinitionManager.Static.GetVoxelMaterialDefinition(material);
                    if (def != null)
                        return def.Id;
                }
                cellsItr.MoveNext();
            }

            return null;
        }

        public static float Laze(this MyVoxelBase voxel, BoundingSphereD area, float amount, ref MyStorageData cache)
        {
            if (cache == null)
                cache = new MyStorageData();
            Vector3I voxMin;
            Vector3I voxMax;
            Vector3I voxCells;
            var shape = new BoundingBoxD(area.Center - area.Radius, area.Center + area.Radius);
            GetVoxelShapeDimensions(voxel, shape, out voxMin, out voxMax, out voxCells);
            ulong totalRemoved = 0uL;
            Vector3I_RangeIterator cellsItr = new Vector3I_RangeIterator(ref Vector3I.Zero, ref voxCells);
            while (cellsItr.IsValid())
            {
                Vector3I cellMinCorner;
                Vector3I cellMaxCorner;
                GetCellCorners(ref voxMin, ref voxMax, ref cellsItr, out cellMinCorner, out cellMaxCorner);
                Vector3I rangeMin = cellMinCorner - 1;
                Vector3I rangeMax = cellMaxCorner + 1;
                voxel.Storage.ClampVoxelCoord(ref rangeMin);
                voxel.Storage.ClampVoxelCoord(ref rangeMax);
                ulong removed = 0uL;
                cache.Resize(rangeMin, rangeMax);
                voxel.Storage.ReadRange(cache, MyStorageDataTypeFlags.Content, 0, rangeMin, rangeMax);
                Vector3I_RangeIterator cellVoxItr = new Vector3I_RangeIterator(ref cellMinCorner, ref cellMaxCorner);
                while (cellVoxItr.IsValid())
                {
                    Vector3I cellVoxIdx = cellVoxItr.Current - rangeMin;
                    byte b = cache.Content(ref cellVoxIdx);
                    if (b != 0)
                    {
                        Vector3I current = cellVoxItr.Current;
                        Vector3D worldPos;
                        MyVoxelCoordSystems.VoxelCoordToWorldPosition(voxel.PositionLeftBottomCorner, ref current,
                            out worldPos);
                        var d = (worldPos - area.Center).Length() / area.Radius;
                        if (d < 1)
                        {
                            int nVal = Math.Max(0, (int) (b - 255f * amount * (1 - d)));
                            cache.Content(ref cellVoxIdx, (byte) nVal);
                            removed += b - (ulong) nVal;
                        }
                    }

                    cellVoxItr.MoveNext();
                }

                if (removed > 0uL)
                {
                    voxel.Storage.WriteRange(cache, MyStorageDataTypeFlags.Content, rangeMin, rangeMax);
                }

                totalRemoved += removed;
                cellsItr.MoveNext();
            }

            return totalRemoved / 255f;
        }

        private static void GetVoxelShapeDimensions(MyVoxelBase voxelMap, BoundingBoxD shape, out Vector3I minCorner,
            out Vector3I maxCorner, out Vector3I numCells)
        {
            ComputeShapeBounds(voxelMap, ref shape, out minCorner, out maxCorner);
            numCells = new Vector3I((maxCorner.X - minCorner.X) / 16, (maxCorner.Y - minCorner.Y) / 16,
                (maxCorner.Z - minCorner.Z) / 16);
        }

        public static void ComputeShapeBounds(MyVoxelBase voxelMap, ref BoundingBoxD shapeAabb,
            out Vector3I voxelMin, out Vector3I voxelMax)
        {
            var storageSize = voxelMap.Storage.Size;
            MyVoxelCoordSystems.WorldPositionToVoxelCoord(voxelMap.PositionLeftBottomCorner, ref shapeAabb.Min,
                out voxelMin);
            MyVoxelCoordSystems.WorldPositionToVoxelCoord(voxelMap.PositionLeftBottomCorner, ref shapeAabb.Max,
                out voxelMax);
            voxelMin += voxelMap.StorageMin;
            voxelMax += voxelMap.StorageMin;
            voxelMax += 1;
            storageSize -= 1;
            Vector3I.Clamp(ref voxelMin, ref Vector3I.Zero, ref storageSize, out voxelMin);
            Vector3I.Clamp(ref voxelMax, ref Vector3I.Zero, ref storageSize, out voxelMax);
        }

        public static Vector3D VoxelFloatToWorldCoord(MyVoxelBase voxelMap, Vector3D voxelCoords)
        {
            Vector3 tmp = voxelCoords - (Vector3D) voxelMap.StorageMin;
            Vector3D result;
            MyVoxelCoordSystems.LocalPositionToWorldPosition(voxelMap.PositionLeftBottomCorner, ref tmp, out result);
            return result;
        }

        public static Vector3D WorldCoordToVoxelFloat(MyVoxelBase voxelMap, Vector3D worldCoords)
        {
            Vector3 tmp;
            MyVoxelCoordSystems.WorldPositionToLocalPosition(voxelMap.PositionLeftBottomCorner, ref worldCoords, out tmp);
            tmp += voxelMap.StorageMin;
            return Vector3D.Clamp(tmp, Vector3D.Zero, voxelMap.StorageMax);
        }

        private static void GetCellCorners(ref Vector3I minCorner, ref Vector3I maxCorner,
            ref Vector3I_RangeIterator it, out Vector3I cellMinCorner, out Vector3I cellMaxCorner)
        {
            cellMinCorner = new Vector3I(minCorner.X + it.Current.X * 16, minCorner.Y + it.Current.Y * 16,
                minCorner.Z + it.Current.Z * 16);
            cellMaxCorner = new Vector3I(Math.Min(maxCorner.X, cellMinCorner.X + 16),
                Math.Min(maxCorner.Y, cellMinCorner.Y + 16), Math.Min(maxCorner.Z, cellMinCorner.Z + 16));
        }
    }
}