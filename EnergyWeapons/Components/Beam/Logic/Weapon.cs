using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Equinox.EnergyWeapons.Misc;
using Equinox.EnergyWeapons.Physics;
using Equinox.Utils.Logging;
using Equinox.Utils.Misc;
using ParallelTasks;
using Sandbox.Game.Entities;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Voxels;
using VRageMath;

using DummyData =
    Equinox.EnergyWeapons.Components.Network.DummyData<Equinox.EnergyWeapons.Components.Beam.Segment,
        Equinox.EnergyWeapons.Components.Beam.BeamConnectionData>;

namespace Equinox.EnergyWeapons.Components.Beam.Logic
{
    public class Weapon : Lossy<Definition.Beam.Weapon>, IRenderableComponent
    {
        private static readonly TimeSpan _shootDebounceTime = TimeSpan.FromMilliseconds(10);

        public Weapon(NetworkComponent block, Definition.Beam.Weapon definition) : base(block, definition)
        {
        }

        private DummyData _dummy;

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();
            bool tmp;
            _dummy = Network.Controller.GetOrCreate(Block, Definition.Dummy, out tmp);
            Block.IsWorkingChanged += IsWorkingChanged;
            IsWorkingChanged(Block);
        }

        public override void OnRemovedFromScene()
        {
            base.OnRemovedFromScene();
            Block.IsWorkingChanged -= IsWorkingChanged;
            NeedsUpdate = false;
            DestroyFxObjects();
        }

        public override void Debug(StringBuilder sb)
        {
            base.Debug(sb);
            sb.Append("Lazing=").Append(IsShooting).Append(" ");
            sb.Append("BeamPower=").Append(_energyThroughput.ToString("F2")).Append("kW ");
            sb.Append("BeamColor=").AppendFormat("[{0:F2} {1:F2} {2:F2} {3:F2}] ", _beamColor.X, _beamColor.Y,
                _beamColor.Z, _beamColor.W);
            sb.Append("BeamThickness=")
                .Append(BeamController.BeamWidth(_energyThroughput)
                    .ToString("F2")).Append("m ");
            sb.Append("Distance=")
                .Append(((_raycastResult?.Fraction ?? float.PositiveInfinity) * Definition.MaxLazeDistance).ToString("F1")).Append(" ");
        }

        #region Update Logic

        private bool _needsUpdate;

        private bool NeedsUpdate
        {
            get { return _needsUpdate; }
            set
            {
                _needsUpdate = value;
                CheckScheduled();
            }
        }

        private bool _scheduled;

        private void CheckScheduled()
        {
            var required = _needsUpdate && Block != null && Block.InScene;

            if (required && !_scheduled)
            {
                Network.Core.Scheduler.RepeatingUpdate(UpdateSlow, 10);
                Network.Core.Scheduler.RepeatingUpdate(UpdateFast, 2);
            }
            else if (_scheduled && !required)
            {
                Network.Core.Scheduler.RemoveUpdate(UpdateSlow);
                Network.Core.Scheduler.RemoveUpdate(UpdateFast);
            }

            _scheduled = required;
        }

        private void IsWorkingChanged(IMyCubeBlock obj)
        {
            NeedsUpdate = obj.IsWorking;
        }

        #endregion

        private bool IsShooting
        {
            get
            {
                var gun = Block as IMyUserControllableGun;
                if (gun != null && gun.IsShooting)
                    return true;
                var gunBase = Block as IMyGunObject<MyGunBase>;
                return gunBase?.GunBase != null &&
                       (gunBase.IsShooting || (DateTime.UtcNow - gunBase.GunBase.LastShootTime) < _shootDebounceTime);
            }
        }

        private void UpdateSlow(ulong dticks)
        {
            if (!IsShooting || !Block.IsWorking)
                return;
            var dt = dticks * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;

            CheckRaycast();
            BurnTarget();
        }

        private void UpdateFast(ulong dticks)
        {
            var dt = dticks * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;

            if (!IsShooting || !Block.IsWorking || _dummy?.Segment == null)
            {
                _raycastResult = null;
                _raycastResultBlock = null;
                _lazeAccumulatedEnergy = 0;
                return;
            }

            float energyToAdd = _dummy.Segment.Current.Energy / 20f;
            _energyThroughput = energyToAdd / dt;
            _dummy.Segment.Inject(-energyToAdd, _beamColor = _dummy.Segment.Current.Color);

            float eff = Efficiency(Definition.Efficiency);
            IncrementTargetBurn(eff * energyToAdd);
        }

        #region Raycast

        private IHitInfo _raycastResult;
        private IMySlimBlock _raycastResultBlock;
        private Task? _raycastTask;

        /// <summary>
        /// Energy throughput in kW
        /// </summary>
        private float _energyThroughput;

        private void CheckRaycast()
        {
            if (_raycastTask?.IsComplete ?? true)
                _raycastTask = MyAPIGateway.Parallel.Start(CastLazeWorker);
        }

        private void CastLazeWorker()
        {
            if (!IsShooting || !Block.IsWorking)
                return;
            var matrix = _dummy.Dummy.WorldMatrix;
            var origin = matrix.Translation;
            var dir = matrix.Forward;

            var from = origin;
            var to = from + dir * Definition.MaxLazeDistance;
            MyAPIGateway.Physics.CastVoxelStorageRay(from, to, out _raycastResult);
            _raycastResultBlock = _raycastResult?.Block(dir);
        }

        #endregion
        
        #region Target Destroy

        // kJ of energy to transfer into target
        private float _lazeAccumulatedEnergy = 0;

        /// <summary>
        /// Called every tick to increase amount of damage to the target entity.
        /// </summary>
        /// <param name="energy">Energy in kJ</param>
        private void IncrementTargetBurn(float energy)
        {
            var target = (object) _raycastResultBlock ?? _raycastResult?.HitEntity;

            // if target changed clear + reset accumulated energy
            if (_raycastResult?.HitEntity != null && !_raycastResult.HitEntity.Closed)
            {
                _lazeAccumulatedEnergy += Definition.WeaponDamageMultiplier * energy;
            }
            else
            {
                _lazeAccumulatedEnergy = 0;
            }
        }

        private Task? _lazeVoxelTask;
        private MyStorageData _storageCache;

        /// <summary>
        /// Actually damage the target entity
        /// </summary>
        private void BurnTarget()
        {
            var block = _raycastResultBlock;
            var entity = (block?.CubeGrid) ?? _raycastResult?.HitEntity;
            if (entity == null || entity.Closed)
                return;

            var phys = block != null
                ? Network.Core.Physics.PhysicsFor(block)
                : Network.Core.Physics.PhysicsFor(entity);
            var voxel = entity as MyVoxelBase;
            if (phys != null)
            {
                phys.AddEnergy(_lazeAccumulatedEnergy);
                _lazeAccumulatedEnergy = 0;
            }
            else if (voxel != null && (_lazeVoxelTask?.IsComplete ?? true))
            {
                var captureEnergy = _lazeAccumulatedEnergy;
                var capturePosition = _raycastResult.Position;
                var hitNormal = _raycastResult.Normal;

                double maxRadius, maxRate;
                AmountToVaporize(MaterialPropertyDatabase.StoneMaterial, _lazeAccumulatedEnergy, out maxRadius,
                    out maxRate);
                if (maxRadius > 1.5f)
                    _lazeVoxelTask = MyAPIGateway.Parallel.Start(() =>
                    {
                        try
                        {
                            var queryMaterial =
                                voxel.VoxelMaterialAt(capturePosition, -hitNormal, ref _storageCache);
                            MaterialProperties targetMaterial = MaterialPropertyDatabase.StoneMaterial;
                            if (queryMaterial.HasValue)
                                targetMaterial = Network.Core.Materials.PropertiesOf(queryMaterial.Value,
                                    MaterialPropertyDatabase.StoneMaterial);

                            double vaporizeRate, vaporizeRadius;
                            AmountToVaporize(targetMaterial, captureEnergy, out vaporizeRadius, out vaporizeRate);

                            var vaporizedVolume = voxel.Laze(new BoundingSphereD(capturePosition, vaporizeRadius),
                                (float) vaporizeRate,
                                ref _storageCache);
                            _lazeAccumulatedEnergy = Math.Max(_lazeAccumulatedEnergy - vaporizedVolume *
                                                              targetMaterial.DensitySolid *
                                                              targetMaterial.EnthalpyOfFusion, 0);
                        }
                        catch (Exception e)
                        {
                            Log.Error(e.ToString());
                        }
                    });
            }
        }

        /// <summary>
        /// Compute amount of a voxel material to vaporize
        /// </summary>
        /// <param name="properties">Material</param>
        /// <param name="energy">kJ</param>
        /// <param name="radius">m</param>
        /// <param name="rate">scalar 0-1</param>
        private static void AmountToVaporize(MaterialProperties properties, float energy, out double radius,
            out double rate)
        {
            var kgVaporized = energy / properties.EnthalpyOfFusion;
            var cubicMetersVaporized = kgVaporized / properties.DensitySolid;
            rate = MathHelper.Clamp(cubicMetersVaporized / 2f, 0.25f, 1f);
            // Vaporized = pi^2 * r / 2
            radius = (cubicMetersVaporized / (rate * Math.PI * Math.PI / 2));

            if (radius > Settings.MaxVoxelRadius)
            {
                rate = Math.Min(radius / Settings.MaxVoxelRadius, 1);
                radius = Settings.MaxVoxelRadius;
            }
        }

        #endregion

        #region FX

        private Vector4 _beamColor;

        public void Draw()
        {
            var fire = IsShooting;

            var result = _raycastResult;
            var matrix = _dummy.Dummy.WorldMatrix;
            var origin = matrix.Translation;
            var dir = matrix.Forward;

            // if hitting a grid go an extra quarter block.  Otherwise go an extra fourth of the bounding box
            float extraDist = (float) Math.Abs((result?.HitEntity as IMyCubeGrid)?.GridSize ??
                                               (result?.HitEntity as IMyCubeBlock)?.CubeGrid.GridSize ??
                                               result?.HitEntity?.PositionComp?.WorldAABB.HalfExtents.Dot(dir) ??
                                               0f) / 4f;
            if (result?.HitEntity is IMyVoxelBase)
                extraDist = 0.1f;

            if (fire)
            {
                var from = origin;
                var to = origin + ((result?.Fraction ?? 1) * Definition.MaxLazeDistance + extraDist) * dir;
                var thickness = BeamController.BeamWidth(_energyThroughput);
                var color = BeamController.BeamColor(_beamColor, _energyThroughput);
                MySimpleObjectDraw.DrawLine(from, to, BeamController.LaserMaterial, ref color, thickness);
            }

            if (Definition?.FxImpactName != null)
            {
                _fxImpactCount = Math.Max(_fxImpactCount - 1, 0);
                if (result != null && fire)
                    _fxImpactCount = Math.Min(_fxImpactCount + Definition.FxImpactBirthRate,
                        Definition.FxImpactMaxCount);
                if (_fxImpactCount > 0)
                {
                    if (_fxImpactParticles == null)
                    {
                        MyParticlesManager.TryCreateParticleEffect(Definition?.FxImpactName, out _fxImpactParticles);
                    }

                    if (_fxImpactParticles != null)
                    {
                        if (result != null)
                        {
                            var up = (Vector3) dir;
                            var fwd = Vector3.Normalize(Vector3.Cross(up, new Vector3(up.Y, up.Z, up.X)));
                            _fxImpactParticles.WorldMatrix = MatrixD.CreateWorld(result.Position, fwd, up);
                            _fxImpactParticles.UserScale = Definition.FxImpactScale;
                            _fxImpactParticles.Velocity =
                                result.HitEntity?.Physics?.GetVelocityAtPoint(result.Position) ?? Vector3.Zero;
                        }

                        _fxImpactParticles.UserBirthMultiplier = _fxImpactCount;
                    }
                }
                else
                {
                    _fxImpactParticles?.StopEmitting();
                    if (_fxImpactParticles != null)
                        MyParticlesManager.RemoveParticleEffect(_fxImpactParticles);
                    _fxImpactParticles = null;
                }
            }
        }

        public void DebugDraw()
        {
        }

        private int _fxImpactCount;
        private MyParticleEffect _fxImpactParticles;

        private void DestroyFxObjects()
        {
            if (_fxImpactParticles != null)
            {
                MyParticlesManager.RemoveParticleEffect(_fxImpactParticles);
                _fxImpactParticles = null;
            }
        }

        #endregion
    }
}