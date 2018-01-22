using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Equinox.EnergyWeapons.Components.Direction;
using Equinox.EnergyWeapons.Components.Thermal;
using Equinox.EnergyWeapons.Definition;
using Equinox.EnergyWeapons.Definition.Weapon;
using Equinox.EnergyWeapons.Misc;
using Equinox.EnergyWeapons.Physics;
using Equinox.Utils.Components;
using Equinox.Utils.Logging;
using Equinox.Utils.Misc;
using ParallelTasks;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.Lights;
using Sandbox.Game.Weapons;
using Sandbox.Game.Weapons.Guns.Barrels;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.Input;
using VRage.ModAPI;
using VRage.Utils;
using VRage.Voxels;
using VRageMath;

namespace Equinox.EnergyWeapons.Components.Weapon
{
    public class LaserWeaponComponent : WeaponComponent<LaserWeaponDefinition>, IRenderableComponent
    {
        private const float MaxVoxelRadius = 30;
        private const float MinPower = 0.02f;
        private static readonly MyStringId _laserMaterial = MyStringId.GetOrCompute("WeaponLaser");

        private readonly ComponentDependency<DirectionComponent> _directionComp;
        private readonly ComponentDependency<MyResourceSinkComponent> _resourceSinkComp;
        private readonly ComponentDependency<ThermalPhysicsComponent> _thermalPhysics;
        private new IMyUserControllableGun Entity => base.Entity as IMyUserControllableGun;

        public LaserWeaponComponent(EnergyWeaponsCore core) : base(core)
        {
            _directionComp =
                new ComponentDependency<DirectionComponent>(this, DirectionBarrelComponent.CreateAuto);
            _thermalPhysics =
                ComponentDependency<ThermalPhysicsComponent>.DependencyWithFactory(this, core,
                    (ent, lcore) => new ThermalPhysicsComponent(lcore));
            _thermalPhysics.ValueChanged += ThermalPhysicsChanged;
            _resourceSinkComp = new ComponentDependency<MyResourceSinkComponent>(this);
            _resourceSinkComp.ValueChanged += ResourceSinkChanged;
            base.DefinitionChanged += OnDefinitionChanged;
        }


        private void OnDefinitionChanged(LaserWeaponDefinition old, LaserWeaponDefinition @new)
        {
            CheckElectricalConfig();
            CheckThermalConfig();
            if (old != @new)
                UpdateInternalBeamPaths();
        }

        #region Electrical Input

        private void CheckElectricalConfig()
        {
            var @new = _resourceSinkComp.Value;
            if (@new == null)
                return;
            @new?.SetRequiredInputFuncByType(ElectricityId, RequiredInput);
            if (Definition != null)
                @new?.SetMaxRequiredInputByType(ElectricityId, Definition.MaxPowerOutput);
        }

        private void ResourceSinkChanged(MyResourceSinkComponent old, MyResourceSinkComponent @new)
        {
            CheckElectricalConfig();
        }

        private float RequiredInput()
        {
            if (!ValidLaser)
                return 0;
            return ShouldShoot ? Definition.MaxPowerOutput : MinPower;
        }

        #endregion

        #region Thermal Physics

        private void CheckThermalConfig()
        {
            var @new = _thermalPhysics.Value;
            if (@new == null)
                return;
            if (Definition != null)
                @new.Physics.RadiateIntoSpaceConductivity = CoolingPower;
            else
                @new.Physics.RadiateIntoSpaceConductivity = 0;
            @new.Physics.OverheatTemperature = Definition?.MeltdownTemperature;
            @new.Physics.OverheatDamageMultiplier = 1;
        }

        private void ThermalPhysicsChanged(ThermalPhysicsComponent arg1, ThermalPhysicsComponent @new)
        {
            CheckThermalConfig();
        }

        private float CurrentTemperature
        {
            get { return _thermalPhysics.Value?.Physics.Temperature ?? PhysicalConstants.TemperatureSpace; }
        }

        private bool _wasOverheated;

        public bool IsOverheated
        {
            get
            {
                if (!ValidLaser || !Definition.ThermalFuseMax.HasValue)
                    return false;
                var fuseTemp = _wasOverheated
                    ? (Definition.ThermalFuseMin ?? Definition.ThermalFuseMax.Value)
                    : Definition.ThermalFuseMax;
                return _wasOverheated = CurrentTemperature > fuseTemp;
            }
        }

        #endregion

        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            _directionComp.OnAddedToContainer();
            _resourceSinkComp.OnAddedToContainer();
            _thermalPhysics.OnAddedToContainer();
            NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME | MyEntityUpdateEnum.EACH_10TH_FRAME;

            var tBlock = Entity as IMyTerminalBlock;
            if (tBlock != null)
            {
                tBlock.AppendingCustomInfo += AppendingCustomInfo;
                _blockShootProperty = tBlock.GetProperty("Shoot").Cast<bool>();
            }

            var block = Entity as IMyCubeBlock;
            if (block != null)
            {
                block.AddUpgradeValue(UpgradeValueEfficiency, 1);
                block.AddUpgradeValue(UpgradeValueCooling, 1);
                block.OnUpgradeValuesChanged += UpgradeValuesChanged;
            }

            _recursiveSubparts.Entity = base.Entity;

            UpgradeValuesChanged();
            UpdateInternalBeamPaths();
        }

        /// <summary>
        /// Cooling power in kW/K
        /// </summary>
        public float CoolingPower => ((Definition?.CoolingPower ?? 0) + _upgradeCoolingMw) * 1e3f;

        /// <summary>
        /// Efficiency of laser emitter, scalar 0-1
        /// </summary>
        public float Efficiency => Math.Min(1, (Definition?.Efficiency ?? 1) * _upgradeEfficiency);

        public override void OnBeforeRemovedFromContainer()
        {
            var tBlock = Entity as IMyTerminalBlock;
            if (tBlock != null)
                tBlock.AppendingCustomInfo -= AppendingCustomInfo;

            var block = Entity as IMyCubeBlock;
            if (block != null)
            {
                block.OnUpgradeValuesChanged -= UpgradeValuesChanged;
            }

            _directionComp.OnBeforeRemovedFromContainer();
            _resourceSinkComp.OnBeforeRemovedFromContainer();
            _thermalPhysics.OnBeforeRemovedFromContainer();
            DestroyFxObjects();

            _recursiveSubparts.Entity = null;
            base.OnBeforeRemovedFromContainer();
        }

        private void AppendingCustomInfo(IMyTerminalBlock myTerminalBlock, StringBuilder sb)
        {
            sb.Append("Valid Laser: ").Append(ValidLaser).AppendLine();
            sb.Append("Shooting: ").Append(IsShooting).AppendLine();
            sb.Append("Overheated: ").Append(IsOverheated).AppendLine();
            sb.Append("Laze Power: ").Append(_currentLasePower).AppendLine(" MW");
            sb.Append("Temperature: ").Append(CurrentTemperature).AppendLine(" K");
            sb.Append("Input: ")
                .Append(_resourceSinkComp.Value?.CurrentInputByType(ElectricityId)
                            .ToString(CultureInfo.InvariantCulture) ?? "no sink")
                .AppendLine(" MW");
        }

        public bool ValidLaser
        {
            get { return Definition != null && Entity != null && Entity.IsFunctional && Entity.Enabled; }
        }

        public bool ShouldShoot
        {
            get { return ValidLaser && !IsOverheated && IsShooting; }
        }

        // MW of power
        private float _currentLasePower;
        private Task? _lazeTask;

        public override void UpdateBeforeSimulation()
        {
            var sink = _resourceSinkComp.Value;
            if (!ValidLaser)
                return;

            IncrementTargetBurn();

            sink?.Update();
            if (sink == null || !ShouldShoot)
            {
                _lazeImpact = null;
                _currentLasePower = 0;
                return;
            }

            var power = sink.CurrentInputByType(ElectricityId);
            float eff = Efficiency;
            _currentLasePower = power * Efficiency;
            _thermalPhysics.Value?.Physics?.AddPower(power * 1e3f * (1 - eff));
            if (_currentLasePower <= 0)
            {
                _lazeImpact = null;
                return;
            }

            UpdateEmissives();

            // TODO reuse lasers
            //            if (_lazeTask?.IsComplete ?? true)
            //                MyAPIGateway.Parallel.Start(ReuseLazeParallel);
        }

        public override void UpdateBeforeSimulation10()
        {
            if (!ValidLaser)
                return;
            BurnTarget();
            if (!ShouldShoot)
                return;
            if (_lazeTask?.IsComplete ?? true)
                _lazeTask = MyAPIGateway.Parallel.Start(CastLazeParallel);
        }

        public override void UpdateAfterSimulation10()
        {
            Entity?.RefreshCustomInfo();
        }


        private ITerminalProperty<bool> _blockShootProperty;

        private bool IsShooting
        {
            get
            {
                var gun = Entity;
                if (gun == null)
                    return false;
                if (gun.IsShooting)
                    return true;
                var gunBase = gun as IMyGunObject<MyGunBase>;
                if (gunBase != null && gunBase.IsShooting)
                    return true;
                return _blockShootProperty?.GetValue(gun) ?? false;
            }
        }

        private IHitInfo _lazeImpact = null;
        private IMySlimBlock _lazeBlockImpact = null;
        private Vector3D _lazeDirection;
        private double _lazeDistance = 100;

        #region Upgrade Values

        public const string UpgradeValueCooling = "ThermalCoolingMWpK";
        public const float UpgradeValueCoolingDefault = 0f;
        public const string UpgradeValueEfficiency = "EnergyWeaponEfficiency";
        public const float UpgradeValueEfficiencyDefault = 1f;

        private float _upgradeEfficiency = UpgradeValueEfficiencyDefault;
        private float _upgradeCoolingMw = UpgradeValueCoolingDefault;

        private void UpgradeValuesChanged()
        {
            _upgradeEfficiency = UpgradeValueEfficiencyDefault;
            _upgradeCoolingMw = UpgradeValueCoolingDefault;
            var block = Entity as IMyCubeBlock;
            if (block != null)
            {
                _upgradeEfficiency =
                    Entity.UpgradeValues.GetValueOrDefault(UpgradeValueEfficiency, UpgradeValueEfficiencyDefault);
                _upgradeCoolingMw =
                    Entity.UpgradeValues.GetValueOrDefault(UpgradeValueEfficiency, UpgradeValueCoolingDefault);
            }

            CheckThermalConfig();
        }

        #endregion

        #region FX

        private float PowerFactor
        {
            get
            {
                if (!IsShooting)
                    return 0;
                return MathHelper.Clamp(_currentLasePower / (Definition.MaxPowerOutput * Efficiency), 0f, 1f);
            }
        }

        private Vector4 BeamColor
        {
            get { return Vector4.Lerp(Definition.LaserColorMin, Definition.LaserColorMax, PowerFactor); }
        }

        private float _lastBeamEmissiveUpdate = float.NegativeInfinity;
        private float _lastHeatEmissiveUpdate = float.NegativeInfinity;

        private void UpdateEmissives()
        {
            if (Definition == null)
                return;
            if (!string.IsNullOrWhiteSpace(Definition.LaserBeamEmissives) &&
                Math.Abs(_lastBeamEmissiveUpdate - PowerFactor) > 0.01f)
            {
                _lastBeamEmissiveUpdate = PowerFactor;
                var col = BeamColor;
                col.W = 1;
                _recursiveSubparts.SetEmissiveParts(Definition.LaserBeamEmissives, col, PowerFactor);
            }

            var tempFactor =
                MathHelper.Clamp(CurrentTemperature / (_thermalPhysics.Value?.Physics?.OverheatTemperature ?? 10e3f), 0,
                    1f);
            if (!string.IsNullOrWhiteSpace(Definition.LaserHeatEmissives) &&
                Math.Abs(_lastHeatEmissiveUpdate - tempFactor) > 0.01f)
            {
                _lastHeatEmissiveUpdate = tempFactor;
                var col = Vector4.Lerp(Vector4.Zero, new Vector4(1, 0, 0, 1), tempFactor);
                _recursiveSubparts.SetEmissiveParts(Definition.LaserHeatEmissives, col, tempFactor);
            }
        }

        private readonly List<DummyPathRef[]> _dummiesToConnect = new List<DummyPathRef[]>();

        private void UpdateInternalBeamPaths()
        {
            _dummiesToConnect.Clear();
            if (Definition?.InternalBeams == null || Entity == null)
                return;
            foreach (var path in Definition.InternalBeams)
                if (path != null && path.Length > 0)
                {
                    var tmp = new DummyPathRef[path.Length];
                    for (var i = 0; i < tmp.Length; i++)
                        tmp[i] = new DummyPathRef(Entity, path[i].Split('/'));
                    _dummiesToConnect.Add(tmp);
                }
        }

        public void Draw()
        {
            if (!ValidLaser)
                return;
            var isFiring = _currentLasePower > 0;
            var dir = _directionComp.Value;
            if (dir == null)
                return;


            // if hitting a grid go an extra quarter block.  Otherwise go an extra eith of the bounding box
            float extraDist =
                (float) Math.Abs((_lazeImpact?.HitEntity as IMyCubeGrid)?.GridSize ??
                                 (_lazeImpact?.HitEntity as IMyCubeBlock)?.CubeGrid.GridSize ??
                                 _lazeImpact?.HitEntity?.PositionComp?.WorldAABB.HalfExtents.Dot(dir.ShotDirection) ??
                                 0f) / 4f;
            if (_lazeImpact?.HitEntity is IMyVoxelBase)
                extraDist = 0.1f;

            if (isFiring)
            {
                var from = dir.ShotOrigin + dir.ShotDirection * Definition.LaserBeamEmitterOffset;
                var to = from + dir.ShotDirection * (_lazeDistance + extraDist - Definition.LaserBeamEmitterOffset);
                var powerFactor = PowerFactor;
                var color = BeamColor;
                var thickness = powerFactor * powerFactor * Definition.MaxLazeThickness;
                MySimpleObjectDraw.DrawLine(from, to, _laserMaterial, ref color, thickness);

                // Draw internal beams:
                foreach (var path in _dummiesToConnect)
                {
                    for (var i = 0; i < path.Length - 1; i++)
                    {
                        var a = path[i];
                        var b = path[i + 1];
                        if (a.Valid && b.Valid)
                            MySimpleObjectDraw.DrawLine(a.WorldMatrix.Translation, b.WorldMatrix.Translation,
                                _laserMaterial, ref color, thickness);
                    }
                }
            }

            if (Definition?.FxImpactName != null)
            {
                _fxImpactCount = Math.Max(_fxImpactCount - 1, 0);
                if (_lazeImpact != null && isFiring)
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
                        if (_lazeImpact != null)
                        {
                            var up = (Vector3) dir.ShotDirection;
                            var fwd = Vector3.Normalize(Vector3.Cross(up, new Vector3(up.Y, up.Z, up.X)));
                            _fxImpactParticles.WorldMatrix = MatrixD.CreateWorld(_lazeImpact.Position, fwd, up);
                            _fxImpactParticles.UserScale = Definition.FxImpactScale;
                            _fxImpactParticles.Velocity =
                                _lazeImpact.HitEntity?.Physics?.LinearVelocity ?? Vector3.Zero;
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

        private readonly RecursiveSubparts _recursiveSubparts = new RecursiveSubparts();

        #endregion

        #region Laze Raycasts

        private void CastLazeParallel()
        {
            if (!ValidLaser)
                return;
            var dir = _directionComp.Value;
            if (dir == null)
                return;
            var from = dir.ShotOrigin + dir.ShotDirection * Definition.RayEmitterOffset;
            var to = from + dir.ShotDirection * Definition.MaxLazeDistance;
            MyAPIGateway.Physics.CastVoxelStorageRay(from, to, out _lazeImpact);
            if (MyAPIGateway.Players.GetPlayerControllingEntity(Entity) != null)
                MyAPIGateway.Utilities.ShowNotification(
                    (_lazeImpact?.Fraction.ToString() ?? "nohit") + "  " +
                    (_lazeImpact?.HitEntity?.ToString() ?? "none"),
                    10 * MyEngineConstants.UPDATE_STEP_SIZE_IN_MILLISECONDS);
            _lazeDirection = dir.ShotDirection;
            _lazeDistance = Definition.RayEmitterOffset + (_lazeImpact?.Fraction ?? 1) * Definition.MaxLazeDistance;
            _lazeBlockImpact = HitBlock();
        }

        private IMySlimBlock HitBlock()
        {
            if (_lazeImpact == null)
                return null;
            var fat = _lazeImpact.HitEntity as IMyCubeBlock;
            if (fat != null)
                return fat.SlimBlock;
            var grid = _lazeImpact.HitEntity as IMyCubeGrid;
            if (grid == null)
                return null;
            var local = Vector3.Transform(_lazeImpact.Position, grid.WorldMatrixNormalizedInv);
            var test = grid.GetCubeBlock(Vector3I.Round(local / grid.GridSize));
            if (test != null)
                return test;
            //            var pt = grid.RayCastBlocks(_lazeImpact.Position,
            //                _lazeImpact.Position + grid.GridSize * 10 * _lazeDirection);
            //            if (pt.HasValue)
            //                return grid.GetCubeBlock(pt.Value);
            //            return null;
            return grid.FirstBlock(_lazeImpact.Position,
                _lazeImpact.Position + grid.GridSize * 10 * _lazeDirection);
        }


        private void ReuseLazeParallel()
        {
            if (!ValidLaser)
                return;
            var dir = _directionComp.Value;
            if (dir == null)
                return;

            var from = dir.ShotOrigin + dir.ShotDirection * Definition.RayEmitterOffset;
            var to = from + dir.ShotDirection * Definition.MaxLazeDistance;

            _lazeDirection = dir.ShotDirection;
            var target = _lazeImpact?.HitEntity;
            if (target != null)
            {
                var line = new LineD(from, to);
                var grid = (target as IMyCubeGrid) ?? (target as IMyCubeBlock)?.CubeGrid;
                if (grid != null)
                {
                    _lazeBlockImpact = grid.FirstBlock(from, to);
                    if (_lazeBlockImpact != null)
                    {
                        BoundingBoxD aabb;
                        _lazeBlockImpact.GetWorldBoundingBox(out aabb);
                        double tmin, tmax;
                        if (aabb.Intersect(ref line, out tmin, out tmax))
                            _lazeDistance = Definition.RayEmitterOffset + tmin * Definition.MaxLazeDistance;
                        else
                            _lazeDistance = Definition.RayEmitterOffset +
                                            Vector3D.Dot(aabb.Center - from, dir.ShotDirection);

                        return;
                    }
                }

                {
                    BoundingBoxD aabb = target.WorldAABB;
                    double tmin, tmax;
                    if (aabb.Intersect(ref line, out tmin, out tmax))
                        _lazeDistance = Definition.RayEmitterOffset + tmin * Definition.MaxLazeDistance;
                    else
                        _lazeDistance = Definition.RayEmitterOffset +
                                        Vector3D.Dot(aabb.Center - from, dir.ShotDirection);

                    return;
                }
            }

            _lazeDistance = Definition.RayEmitterOffset + Definition.MaxLazeDistance;
            _lazeBlockImpact = null;
        }

        #endregion

        #region Target Destroy

        // kJ of energy
        private float _lazeAccumulatedEnergy = 0;

        /// <summary>
        /// Called every tick to increase amount of damage to the target entity.
        /// </summary>
        private void IncrementTargetBurn()
        {
            // if target changed clear + reset accumulated energy
            if (_lazeImpact?.HitEntity != null && !_lazeImpact.HitEntity.Closed)
            {
                _lazeAccumulatedEnergy += Definition.LasePowerMultiplier * _currentLasePower * 1e3f *
                                          MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS;
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
            if (_lazeImpact?.HitEntity == null || _lazeImpact.HitEntity.Closed)
                return;

            var phys = _lazeBlockImpact != null
                ? Core.Physics.PhysicsFor(_lazeBlockImpact)
                : Core.Physics.PhysicsFor(_lazeImpact.HitEntity);
            var voxel = _lazeImpact.HitEntity as IMyVoxelBase;
            if (phys != null)
            {
                phys.AddEnergy(_lazeAccumulatedEnergy);
                _lazeAccumulatedEnergy = 0;
            }
            else if (voxel != null && (_lazeVoxelTask?.IsComplete ?? true))
            {
                var captureEnergy = _lazeAccumulatedEnergy;
                var capturePosition = _lazeImpact.Position;
                var captureDirection = _lazeDirection;

                double maxRadius, maxRate;
                AmountToVaporize(MaterialPropertyDatabase.StoneMaterial, _lazeAccumulatedEnergy, out maxRadius,
                    out maxRate);
                if (maxRadius > 1.5f)
                    _lazeVoxelTask = MyAPIGateway.Parallel.Start(() =>
                    {
                        try
                        {
                            var queryMaterial =
                                voxel.VoxelMaterialAt(capturePosition, captureDirection, ref _storageCache);
                            MaterialProperties targetMaterial = MaterialPropertyDatabase.StoneMaterial;
                            if (queryMaterial.HasValue)
                                targetMaterial = Core.Materials.PropertiesOf(queryMaterial.Value,
                                    MaterialPropertyDatabase.StoneMaterial);

                            double vaporizeRate, vaporizeRadius;
                            AmountToVaporize(targetMaterial, captureEnergy, out vaporizeRadius, out vaporizeRate);

                            var vaporizedVolume = voxel.Laze(new BoundingSphereD(capturePosition, vaporizeRadius),
                                (float) vaporizeRate,
                                ref _storageCache);
                            _lazeAccumulatedEnergy =
                                Math.Max(_lazeAccumulatedEnergy - vaporizedVolume * targetMaterial.DensitySolid *
                                         targetMaterial.EnthalpyOfFusion, 0);
                        }
                        catch (Exception e)
                        {
                            Logger.Error(e.ToString());
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

            if (radius > MaxVoxelRadius)
            {
                rate = Math.Min(radius / MaxVoxelRadius, 1);
                radius = MaxVoxelRadius;
            }
        }

        #endregion
    }
}