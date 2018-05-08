using System.Collections.Generic;
using Equinox.Utils.Misc;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Collections;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

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
        /// <param name="ignoreVoxels">Ignore voxel entities</param>
        /// <param name="info">Hit result</param>
        /// <param name="any">Select any result</param>
        /// <param name="prefetch">Prefetch voxel physics if needed</param>
        /// <returns>true if a hit was returned</returns>
        public static bool CastVoxelStorageRay(this IMyPhysics physics, Vector3D from, Vector3D to,
            bool ignoreVoxels, out IHitInfo info, bool any = false, bool prefetch = true)
        {
            if (prefetch && !ignoreVoxels)
            {
                var ray = new LineD(from, to);
                PrefetchRay(ref ray);
            }

            var hasHit = physics.CastRay(from, to, out info, ignoreVoxels ? 9 : 0);
            if (hasHit && info?.HitEntity is IMyVoxelBase)
            {
                if (ignoreVoxels)
                {
                    info = null;
                    return false;
                }

                info = new ExtraHitInfo()
                {
                    Position = info.Position,
                    HitEntity = ((MyVoxelBase) info.HitEntity)?.RootVoxel ?? info.HitEntity,
                    Normal = info.Normal,
                    Fraction = (float) ((info.Position - from).Dot(to - from) / (to - from).LengthSquared())
                };
            }

            return hasHit;
        }

        public struct RaycastPrediction
        {
            private readonly IMyEntity _root;
            private readonly IHitInfo _hit;
            private readonly LineD _ray;
            private readonly float _maxPrediction;
            private IMySlimBlock _block;
            private Vector3D _hitPosition;

            public RaycastPrediction(IHitInfo hit, LineD ray, float maxPrediction)
            {
                _hit = hit;
                _ray = ray;
                _hitPosition = _hit.Position;
                _maxPrediction = maxPrediction;

                var root = hit?.HitEntity;
                while (root?.Parent != null)
                    root = root.Parent;
                _root = root;

                _block = null;
                // ReSharper disable once ExpressionIsAlwaysNull
                UpdateSlimBlock();
            }

            private void UpdateSlimBlock()
            {
                var grid = _root as IMyCubeGrid;
                _block = grid?.FirstBlock(_hit.Position,
                    _hit.Position + _maxPrediction * _ray.Direction);
                if (_block == null)
                    return;

                Vector3 halfExtents;
                _block.ComputeScaledHalfExtents(out halfExtents);
                Vector3D center;
                _block.ComputeScaledCenter(out center);
                var bb = new BoundingBoxD(center - halfExtents, center + halfExtents);
                LineD l;
                var m = grid.WorldMatrixNormalizedInv;
                l.From = Vector3D.Transform(_ray.From, ref m);
                l.To = Vector3D.Transform(_ray.To, ref m);
                l.Direction = Vector3D.TransformNormal(_ray.Direction, ref m);
                l.Length = _ray.Length;
                double t1, t2;
                if (bb.Intersect(ref l, out t1, out t2))
                    _hitPosition = _ray.From + _ray.Direction * t1;
                else
                    _hitPosition = _ray.From + (Vector3.Transform(center, grid.WorldMatrix) - _ray.From).Dot(_ray.Direction) * _ray.Direction;
            }

            private void CheckPrediction()
            {
                if (_block != null)
                {
                    if (_block.IsDestroyed)
                        UpdateSlimBlock();
                }
            }

            public IMySlimBlock Block
            {
                get
                {
                    CheckPrediction();
                    return _block;
                }
            }

            public Vector3D HitPosition
            {
                get
                {
                    CheckPrediction();
                    return _hitPosition;
                }
            }

            public Vector3D HitNormal => _hit.Normal;

            public double Fraction => (HitPosition - _ray.From).Dot(_ray.Direction);
            public IMyEntity Root => _root;
        }

        public static IMySlimBlock Block(this IHitInfo hit, Vector3 rayDirection, float maxPrediction = 10f)
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
            return grid.FirstBlock(hit.Position, hit.Position + maxPrediction * rayDirection);
        }
    }
}