using System;
using VRage.Game.ModAPI;
using VRageMath;

namespace Equinox.Utils.Misc
{
    public struct CellEnumerator
    {
        private double invDx, xFrac, invDy, yFrac, invDz, zFrac;
        private Vector3I unsignedCurrent, directionInt;
        private Vector3I unsignedEnd;

        private static Vector3 SignNonZero(Vector3 tmp)
        {
            return new Vector3(tmp.X >= 0f ? 1 : -1, tmp.Y >= 0f ? 1 : -1, tmp.Z >= 0f ? 1 : -1);
        }

        private static Vector3I SignIntNonZero(Vector3 tmp)
        {
            return new Vector3I(tmp.X >= 0f ? 1 : -1, tmp.Y >= 0f ? 1 : -1, tmp.Z >= 0f ? 1 : -1);
        }

        private static Vector3I GetGridPoint(ref Vector3D v, Vector3I min, Vector3I max)
        {
            Vector3I result = default(Vector3I);
            if (v.X < min.X)
            {
                v.X = result.X = min.X;
            }
            else if (v.X >= max.X + 1)
            {
                v.X = max.X + 1;
                result.X = max.X;
            }
            else
            {
                result.X = (int) Math.Floor(v.X);
            }

            if (v.Y < min.Y)
            {
                v.Y = result.Y = min.Y;
            }
            else if (v.Y >= max.Y + 1)
            {
                v.Y = max.Y + 1;
                result.Y = max.Y;
            }
            else
            {
                result.Y = (int) Math.Floor(v.Y);
            }

            if (v.Z < min.Z)
            {
                v.Z = result.Z = min.Z;
            }
            else if (v.Z >= max.Z + 1)
            {
                v.Z = max.Z + 1;
                result.Z = max.Z;
            }
            else
            {
                result.Z = (int) Math.Floor(v.Z);
            }

            return result;
        }


        public static CellEnumerator EnumerateGridCells(IMyCubeGrid grid, Vector3D worldStart, Vector3D worldEnd,
            Vector3I? gridSizeInflate = null)
        {
            MatrixD worldMatrixNormalizedInv = grid.PositionComp.WorldMatrixNormalizedInv;
            Vector3D localStart;
            Vector3D.Transform(ref worldStart, ref worldMatrixNormalizedInv, out localStart);
            Vector3D localEnd;
            Vector3D.Transform(ref worldEnd, ref worldMatrixNormalizedInv, out localEnd);
            Vector3 gridSizeHalfVector = new Vector3(grid.GridSize / 2);
            localStart += gridSizeHalfVector;
            localEnd += gridSizeHalfVector;
            Vector3I minInflate = grid.Min - Vector3I.One;
            Vector3I maxInflate = grid.Max + Vector3I.One;
            if (gridSizeInflate.HasValue)
            {
                minInflate -= gridSizeInflate.Value;
                maxInflate += gridSizeInflate.Value;
            }
            return new CellEnumerator(localStart, localEnd, minInflate, maxInflate, grid.GridSize);
        }
        
        public CellEnumerator(Vector3D localStart, Vector3D localEnd, Vector3I min, Vector3I max, float gridSize)
        {
            Vector3D delta = localEnd - localStart;
            Vector3D blockStart = localStart / gridSize;
            Vector3D blockEnd = localEnd / gridSize;

            Vector3 direction = SignNonZero(delta);
            directionInt = SignIntNonZero(delta);
            unsignedCurrent = GetGridPoint(ref blockStart, min, max) * directionInt;
            unsignedEnd = GetGridPoint(ref blockEnd, min, max) * directionInt;
            delta *= direction;
            blockStart *= direction;

            invDx = 1.0 / delta.X;
            xFrac = invDx * (Math.Floor(blockStart.X + 1.0) - blockStart.X);
            invDy = 1.0 / delta.Y;
            yFrac = invDy * (Math.Floor(blockStart.Y + 1.0) - blockStart.Y);
            invDz = 1.0 / delta.Z;
            zFrac = invDz * (Math.Floor(blockStart.Z + 1.0) - blockStart.Z);
        }


        public void MoveNext()
        {
            if (xFrac < zFrac)
            {
                if (xFrac < yFrac)
                {
                    xFrac += invDx;
                    ++unsignedCurrent.X;
                }
                else
                {
                    yFrac += invDy;
                    ++unsignedCurrent.Y;
                }
            }
            else if (zFrac < yFrac)
            {
                zFrac += invDz;
                ++unsignedCurrent.Z;
            }
            else
            {
                yFrac += invDy;
                ++unsignedCurrent.Y;
            }
        }

        public Vector3I Current => unsignedCurrent * directionInt;

        public bool IsValid
        {
            get
            {
                if (xFrac < zFrac)
                {
                    if (xFrac < yFrac)
                        return unsignedCurrent.X <= unsignedEnd.X;
                    return unsignedCurrent.Y <= unsignedEnd.Y;
                }

                if (zFrac < yFrac)
                    return unsignedCurrent.Z <= unsignedEnd.Z;
                return unsignedCurrent.Y <= unsignedEnd.Y;
            }
        }
    }
}