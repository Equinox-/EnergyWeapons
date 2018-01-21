using System;
using VRage.Game.ModAPI;
using VRageMath;

namespace Equinox.Utils.Misc
{
    public static class GridIntersection
    {
        public static IMySlimBlock FirstBlock(this IMyCubeGrid grid, Vector3D worldStart, Vector3D worldEnd,
            Func<IMySlimBlock, bool> pred = null, Vector3I? gridSizeInflate = null)
        {
            for (var itr = CellEnumerator.EnumerateGridCells(grid, worldStart, worldEnd, gridSizeInflate);
                itr.IsValid;
                itr.MoveNext())
            {
                var block = grid.GetCubeBlock(itr.Current);
                if (block != null && (pred == null || pred.Invoke(block)))
                    return block;
            }
            return null;
        }
    }
}