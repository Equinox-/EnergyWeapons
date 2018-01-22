using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equinox.Utils.Logging;
using Equinox.Utils.Misc;
using Sandbox.Engine.Physics;
using Sandbox.Engine.Utils;
using Sandbox.Engine.Voxels;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Ingame;
using VRage.Collections;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Generics;
using VRage.ModAPI;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;
using IMyStorage = VRage.ModAPI.IMyStorage;

namespace Equinox.EnergyWeapons.Misc
{
    public static class RaycastShortcuts
    {
        private static readonly MyConcurrentPool<List<MyLineSegmentOverlapResult<MyVoxelBase>>> _voxelCache =
            new MyConcurrentPool<List<MyLineSegmentOverlapResult<MyVoxelBase>>>(1);

        private static readonly MyTimedItemCache _raycastPrefetchCache = new MyTimedItemCache(4000);

        public class ExtraHitInfo : IHitInfo
        {
            public Vector3D Position { get; set; }
            public IMyEntity HitEntity { get; set; }
            public Vector3 Normal { get; set; }
            public float Fraction { get; set; }
        }

        /// <summary>
        /// Prefetches planetary voxel physics along a ray
        /// </summary>
        /// <param name="ray">ray to prefetch</param>
        /// <param name="force">force a new prefetch task</param>
        public static void PrefetchRay(ref LineD ray, bool force = false)
        {
            // Force is second so we still update the cache for forced
            if (!_raycastPrefetchCache.IsItemPresent(ray.GetHash(),
                    (int) MyAPIGateway.Session.ElapsedPlayTime.TotalSeconds) || force)
            {
                var voxelHits = _voxelCache.Get();
                try
                {
                    MyGamePruningStructure.GetVoxelMapsOverlappingRay(ref ray, voxelHits);
                    foreach (var e in voxelHits)
                    {
                        var planet = (e.Element?.RootVoxel ?? e.Element) as MyPlanet;
                        // This needs to be done for all voxel maps. To bad we can't.
                        planet?.PrefetchShapeOnRay(ref ray);
                    }
                }
                finally
                {
                    voxelHits.Clear();
                    if (_voxelCache.Count <= 1)
                        _voxelCache.Return(voxelHits);
                }
            }
        }

        /// <summary>
        /// Casts a ray with some voxel related tweaks
        /// </summary>
        /// <param name="physics">Physics data</param>
        /// <param name="from">Ray start</param>
        /// <param name="to">Ray end</param>
        /// <param name="info">Hit result</param>
        /// <param name="any">Select any result</param>
        /// <param name="prefetch">Prefetch voxel physics if needed</param>
        /// <returns>true if a hit was returned</returns>
        public static bool CastVoxelStorageRay(this IMyPhysics physics, Vector3D from, Vector3D to, out IHitInfo info,
            bool any = false, bool prefetch = true)
        {
            if (prefetch)
            {
                var ray = new LineD(from, to);
                PrefetchRay(ref ray);
            }

            var hasHit = physics.CastRay(from, to, out info);
            if (hasHit && info?.HitEntity is IMyVoxelBase)
            {
                info = new ExtraHitInfo()
                {
                    Position = info.Position,
                    HitEntity = ((IMyVoxelBase) info.HitEntity)?.RootVoxel ?? info.HitEntity,
                    Normal = info.Normal,
                    Fraction = (float) ((info.Position - from).Dot(to - from) / (to - from).LengthSquared())
                };
            }

            return hasHit;
        }

        public static IMySlimBlock Block(this IHitInfo hit, Vector3 rayDirection)
        {
            if (hit == null)
                return null;
            var fat = hit.HitEntity as IMyCubeBlock;
            if (fat != null)
                return fat.SlimBlock;
            var grid = hit.HitEntity as IMyCubeGrid;
            if (grid == null)
                return null;
            var local = Vector3.Transform(hit.Position, grid.WorldMatrixNormalizedInv);
            var test = grid.GetCubeBlock(Vector3I.Round(local / grid.GridSize));
            if (test != null)
                return test;
            return grid.FirstBlock(hit.Position, hit.Position + grid.GridSize * 10 * rayDirection);
        }
    }
}