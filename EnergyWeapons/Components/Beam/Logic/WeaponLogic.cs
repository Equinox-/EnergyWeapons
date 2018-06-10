using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Equinox.EnergyWeapons.Misc;
using Equinox.EnergyWeapons.Physics;
using Equinox.EnergyWeapons.Session;
using Equinox.Utils.Components;
using Equinox.Utils.Logging;
using Equinox.Utils.Misc;
using Equinox.Utils.Session;
using ParallelTasks;
using Sandbox.Engine.Voxels;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
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
        public Weapon(NetworkComponent block, Definition.Beam.Weapon definition) : base(block, definition)
        {
        }

        private DummyData _dummy;

        public override void OnAddedToScene()
        {
            base.OnAddedToScene();
            _blockShootProperty = (Block as IMyTerminalBlock)?.GetProperty("Shoot").Cast<bool>();
            bool tmp;
            _dummy = Network.Controller.GetOrCreate(Block, Definition.Dummy, out tmp);
            _dummy.SegmentChanged += SegmentChanged;
            SegmentChanged(null, _dummy.Segment);
            Block.IsWorkingChanged += IsWorkingChanged;
            IsWorkingChanged(Block);
        }

        private void SegmentChanged(Segment old, Segment @new)
        {
            if (old != null)
                old.StateUpdated -= SegmentUpdated;
            if (@new != null)
                @new.StateUpdated += SegmentUpdated;
        }

        public override void OnRemovedFromScene()
        {
            base.OnRemovedFromScene();
            Block.IsWorkingChanged -= IsWorkingChanged;
            SegmentChanged(_dummy.Segment, null);
            _dummy.SegmentChanged -= SegmentChanged;
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
            sb.Append("Energy=").Append(_capacitorEnergy.ToString("F0")).Append("kJ ");
            sb.Append("Distance=")
                .Append(
                    ((_raycastResult?.Fraction ?? float.PositiveInfinity) * Definition.MaxLazeDistance).ToString("F1"))
                .Append(" ");
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
                var scheduler = MyAPIGateway.Session.GetComponent<SchedulerAfter>();
                scheduler.RepeatingUpdate(UpdateRaycast, 10);
                scheduler.RepeatingUpdate(UpdateDamage, 1L);
            }
            else if (_scheduled && !required)
            {
                var scheduler = MyAPIGateway.Session.GetComponent<SchedulerAfter>();
                scheduler.RemoveUpdate(UpdateRaycast);
                scheduler.RemoveUpdate(UpdateDamage);
                UpdateRaycast(0);
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
                if (_capacitorEnergy <= 0 && (_dummy.Segment == null || _dummy.Segment.Current.Energy <= 0))
                    return false;

                var gun = Block as IMyUserControllableGun;
                if (gun != null && gun.IsShooting)
                    return true;
                var gunBase = Block as IMyGunObject<MyGunBase>;
                if (gunBase != null && gunBase.IsShooting)
                    return true;
                // ReSharper disable once ConvertIfStatementToReturnStatement
                if (_blockShootProperty != null && _blockShootProperty.GetValue(Block))
                    return true;
                return false;
            }
        }

        private void UpdateDamage(ulong dticks)
        {
            if (_dummy.Segment != null && _dummy.Segment.Current.Energy > 0)
                SegmentUpdated(_dummy.Segment);

            if (!IsShooting || !Block.IsWorking)
            {
                _raycastResult = null;
                _lazeAccumulatedEnergy = 0;
                return;
            }

            if (dticks == 0)
                return;

            var dt = dticks * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;

            float shotPower;
            lock (this)
            {
                shotPower = _capacitorEnergy * Definition.CapacitorDischargePerTick;
                _beamColor = _capacitorColor / Math.Max(1e-6f, _capacitorEnergy);
                _capacitorEnergy -= shotPower;
                _capacitorColor -= _beamColor * shotPower;
            }

            _energyThroughput = shotPower / dt;

            var eff = Efficiency(Definition.Efficiency);
            _lazeAccumulatedEnergy += Definition.WeaponDamageMultiplier * eff * shotPower;
            BurnTarget();
        }

        private void UpdateRaycast(ulong dticks)
        {
            if (!IsShooting || !Block.IsWorking)
                return;
            CheckRaycast();
        }

        private float _capacitorEnergy;
        private Vector4 _capacitorColor;

        private void SegmentUpdated(Segment segment)
        {
            var e = segment.Current.Energy;
            lock (this)
            {
                if (!IsShooting && Definition.CapacitorMaxCharge <= 0)
                    return;

                if (Definition.CapacitorMaxCharge > 0)
                    e = Math.Min(e, Definition.CapacitorMaxCharge - _capacitorEnergy);
                if (e <= 0)
                    return;
                _capacitorEnergy += e;
                _capacitorColor += e * _dummy.Segment.Current.Color;
            }

            _dummy.Segment.Inject(-e, _dummy.Segment.Current.Color);
        }

        #region Raycast

        private RaycastShortcuts.RaycastPrediction? _raycastResult;
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
            IHitInfo hitInfo;
            MyAPIGateway.Physics.CastVoxelStorageRay(from, to, Definition.VoxelDamageMultiplier <= 0, out hitInfo);
            if (hitInfo != null)
                _raycastResult =
                    new RaycastShortcuts.RaycastPrediction(hitInfo, new LineD(from, to), Definition.RaycastPrediction);
            else
                _raycastResult = null;
        }

        #endregion

        #region Target Destroy

        // kJ of energy to transfer into target
        private float _lazeAccumulatedEnergy = 0;


        private Task? _lazeVoxelTask;
        private MyStorageData _storageCache;

        /// <summary>
        /// Actually damage the target entity
        /// </summary>
        private void BurnTarget()
        {
            if (_lazeAccumulatedEnergy <= 0)
                return;
            RaycastShortcuts.RaycastPrediction result;
            {
                var rcap = _raycastResult;
                if (!rcap.HasValue)
                    return;
                result = rcap.Value;
            }

            var block = result.Block;
            var rootEntity = result.Root;
            if (rootEntity == null || rootEntity.Closed)
            {
                _lazeAccumulatedEnergy = 0;
                return;
            }

            var thermalManager = MyAPIGateway.Session.GetComponent<ThermalManager>();

            var phys = block != null
                ? thermalManager.PhysicsFor(block)
                : thermalManager.PhysicsFor(rootEntity);
            var voxel = rootEntity as MyVoxelBase;
            if (phys != null)
            {
                var target = block ?? (rootEntity as IMyDestroyableObject);
                if (Definition.DirectDamageFactor > 0)
                    phys.ApplyOverheating(_lazeAccumulatedEnergy * Definition.DirectDamageFactor, target, false,
                        Definition.DamageType);
                if (Definition.DirectDamageFactor < 1)
                    phys.AddEnergy(_lazeAccumulatedEnergy * (1 - Definition.DirectDamageFactor));
                _lazeAccumulatedEnergy = 0;
            }
            else if (voxel != null && Definition.VoxelDamageMultiplier > 0 && (_lazeVoxelTask?.IsComplete ?? true))
            {
                var captureEnergy = _lazeAccumulatedEnergy;
                var capturePosition = result.HitPosition;
                var hitNormal = result.HitNormal;

                double maxRadius, maxRate;
                AmountToVaporize(MaterialPropertyDatabase.StoneMaterial,
                    _lazeAccumulatedEnergy * Definition.VoxelDamageMultiplier, out maxRadius,
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
                                targetMaterial = thermalManager.Materials.PropertiesOf(queryMaterial.Value,
                                    MaterialPropertyDatabase.StoneMaterial);

                            double vaporizeRate, vaporizeRadius;
                            AmountToVaporize(targetMaterial, captureEnergy, out vaporizeRadius, out vaporizeRate);

                            var vaporizedVolume = voxel.Laze(new BoundingSphereD(capturePosition, vaporizeRadius),
                                (float) vaporizeRate,
                                ref _storageCache);
                            _lazeAccumulatedEnergy = Math.Max(_lazeAccumulatedEnergy - vaporizedVolume *
                                                              targetMaterial.DensitySolid *
                                                              targetMaterial.EnthalpyOfFusion /
                                                              Definition.VoxelDamageMultiplier, 0);
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


            if (fire)
            {
                var from = origin;
                var to = from + dir * Definition.MaxLazeDistance;

                if (result.HasValue)
                {
                    // if hitting a grid go an extra quarter block.  Otherwise go an extra fourth of the bounding box
                    var extraDist = (float) Math.Abs((result.Value.Root as IMyCubeGrid)?.GridSize ??
                                                     result.Value.Root?.PositionComp?.WorldAABB.HalfExtents
                                                         .Dot(dir) ??
                                                     0f) / 4f;
                    if (result.Value.Root is IMyVoxelBase)
                        extraDist = 0.1f;

                    to = result.Value.HitPosition + extraDist * dir;
                }

                var thickness = BeamController.BeamWidth(_energyThroughput);
                var color = BeamController.BeamColor(_beamColor, _energyThroughput);
                MySimpleObjectDraw.DrawLine(from, to, BeamController.LaserMaterial, ref color, thickness);
            }

            if (!string.IsNullOrEmpty(Definition?.FxImpactName))
            {
                _fxImpactCount = Math.Max(_fxImpactCount - 1, 0);
                if (fire)
                    _fxImpactCount = Math.Min(_fxImpactCount + Definition.FxImpactBirthRate,
                        Definition.FxImpactMaxCount);
                if (_fxImpactCount > 0 && result.HasValue)
                {
                    if (_fxImpactParticles == null)
                    {
                        var up = (Vector3) dir;
                        var fwd = Vector3.Normalize(Vector3.Cross(up, new Vector3(up.Y, up.Z, up.X)));
                        var world = MatrixD.CreateWorld(result.Value.HitPosition, fwd, up);
//                        MyParticlesManager.TryCreateParticleEffect(Definition?.FxImpactName, world, out _fxImpactParticles);
                        MyParticlesManager.TryCreateParticleEffect(Definition.FxImpactName, out _fxImpactParticles);
                    }

                    if (_fxImpactParticles != null)
                    {
                        {
                            var up = (Vector3) dir;
                            var fwd = Vector3.Normalize(Vector3.Cross(up, new Vector3(up.Y, up.Z, up.X)));
                            _fxImpactParticles.WorldMatrix = MatrixD.CreateWorld(result.Value.HitPosition, fwd, up);
                            _fxImpactParticles.UserScale = Definition.FxImpactScale;
                            _fxImpactParticles.Velocity =
                                result.Value.Root?.Physics?.GetVelocityAtPoint(result.Value.HitPosition) ??
                                Vector3.Zero;
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

        // because keen
        private ITerminalProperty<bool> _blockShootProperty;

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