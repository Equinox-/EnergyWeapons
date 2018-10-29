using System;
using System.Text;
using Equinox.EnergyWeapons.Physics;
using Equinox.Utils;
using Equinox.Utils.Components;
using Sandbox.Definitions;
using Sandbox.Game.Components;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Equinox.EnergyWeapons.Components.Thermal
{
    public class ThermalPhysicsSlim : IThermalPhysicsProvider, IDebugComponent
    {
        private const float TOLERANCE = 1e-5f;
        private static readonly MyStringHash _overheatingHash = MyStringHash.GetOrCompute("Overheating");

        /// <summary>
        /// SpecificHeat * Mass kJ/K
        /// </summary>
        private float _heatCapacity;

        /// <summary>
        /// kg
        /// </summary>
        public float Mass { get; private set; }

        /// <summary>
        /// The material properties
        /// </summary>
        public MaterialProperties Material { get; private set; }

        private float _temperature;

        /// <summary>
        /// Kelvin
        /// </summary>
        public float Temperature
        {
            get { return _temperature; }
            private set
            {
                var old = _temperature;
                _temperature = value;
                TemperatureChanged?.Invoke(old, _temperature);
            }
        }


        // kW/K
        private float _radiateIntoSpaceConductivity;
        private float _overheatDamageMultiplier;
        private float? _overheatTemperature;

        /// <summary>
        /// Radiation in kW/K
        /// </summary>
        public float RadiateIntoSpaceConductivity
        {
            get { return _radiateIntoSpaceConductivity; }
            set
            {
                var upOld = NeedsUpdate;
                _radiateIntoSpaceConductivity = value;
                if (NeedsUpdate != upOld)
                    NeedsUpdateChanged?.Invoke(upOld, NeedsUpdate);
            }
        }

        /// <summary>
        /// Temperature to overheat at, or null to use the melting point of the material.
        /// </summary>
        public float? OverheatTemperature
        {
            get { return _overheatTemperature; }
            set
            {
                var upOld = NeedsUpdate;
                _overheatTemperature = value;
                if (NeedsUpdate != upOld)
                    NeedsUpdateChanged?.Invoke(upOld, NeedsUpdate);
            }
        }

        /// <summary>
        /// Multiplier on overheat.  If this is non-positive it won't overheat
        /// </summary>
        public float OverheatDamageMultiplier
        {
            get { return _overheatDamageMultiplier; }
            set
            {
                var upOld = NeedsUpdate;
                _overheatDamageMultiplier = value;
                if (NeedsUpdate != upOld)
                    NeedsUpdateChanged?.Invoke(upOld, NeedsUpdate);
            }
        }

        /// <summary>
        /// Does this slim thermal physics require incremental updates
        /// </summary>
        public bool NeedsUpdate => _radiateIntoSpaceConductivity >= TOLERANCE || _overheatDamageMultiplier > 0;

        /// <summary>
        /// Raised when the temperature changes.  (old, new)
        /// </summary>
        public event Action<float, float> TemperatureChanged;

        /// <summary>
        /// Raised when the material changes.  (old, new)
        /// </summary>
        public event Action<MaterialProperties, MaterialProperties> MaterialChanged;

        /// <summary>
        /// Raised when <see cref="NeedsUpdate"/> changes.  (old, new)
        /// </summary>
        public event Action<bool, bool> NeedsUpdateChanged;

        public ThermalPhysicsSlim(MaterialProperties props, float mass, float temperature)
        {
            Init(props, mass, temperature);
        }

        public ThermalPhysicsSlim() : this(MaterialPropertyDatabase.IronMaterial, 1, PhysicalConstants.TemperatureSpace)
        {
        }

        public void Init(MaterialProperties props, float mass, float temperature)
        {
            var oldMtl = Material;
            Material = props;
            Mass = mass;
            _heatCapacity = props.SpecificHeat * mass;
            Temperature = temperature;
            _lastUpdate = null;
            MaterialChanged?.Invoke(oldMtl, Material);
            OverheatDamageMultiplier = 1;
        }

        public bool Init(MaterialPropertyDatabase db, IMyEntity e)
        {
            var block = e as IMyCubeBlock;
            if (block != null)
            {
                Init(db.PropertiesOf(block.BlockDefinition), block.Mass,
                    PhysicalConstants.TemperatureAtPoint(e.WorldMatrix.Translation));
                return true;
            }

            var floating = e as MyFloatingObject;
            if (floating != null)
            {
                var def = MyDefinitionManager.Static.GetPhysicalItemDefinition(floating.Item.Content);
                Init(db.PropertiesOf(floating.Item.Content.GetObjectId()), def.Mass * (float) floating.Item.Amount,
                    PhysicalConstants.TemperatureAtPoint(e.WorldMatrix.Translation));
                return true;
            }

            if (e is IMyDestroyableObject)
            {
                Init(MaterialPropertyDatabase.IronMaterial, e.Physics?.Mass ?? 1,
                    PhysicalConstants.TemperatureAtPoint(e.WorldMatrix.Translation));
                return true;
            }

            return false;
        }

        public bool Init(MaterialPropertyDatabase db, IMySlimBlock block)
        {
            if (block.FatBlock != null)
                return Init(db, block.FatBlock);
            Vector3D center;
            block.ComputeWorldCenter(out center);
            Init(db.PropertiesOf(block.BlockDefinition.Id), block.Mass, PhysicalConstants.TemperatureAtPoint(center));
            return true;
        }

        /// <summary>
        /// Adds energy to this object.
        /// </summary>
        /// <param name="kj">kJ</param>
        public void AddEnergy(float kj)
        {
            Temperature += kj / _heatCapacity;
        }

        /// <summary>
        /// Adds energy to this object at the given rate.
        /// </summary>
        /// <param name="kw">kW</param>
        /// <param name="dt">delta time</param>
        public void AddPower(float kw, float dt = MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS)
        {
            AddEnergy(kw * dt);
        }

        /// <summary>
        /// Radiates heat into a sink with fixed temperature
        /// </summary>
        /// <param name="otherTemperature">K</param>
        /// <param name="thermalConductivity">kW/K</param>
        /// <param name="dt">delta time</param>
        public void RadiateHeat(float otherTemperature, float thermalConductivity,
            float dt = MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS)
        {
            var transferExp =
                (float) Math.Exp(-thermalConductivity * dt / _heatCapacity);

            // dTemp/dt = heatScale * thermalConductivity * (temp - tSurround)
            // temp = tSurround + (t0 - tSurround) * exp(-heatScale * thermalConductivity * StepSize)
            Temperature = otherTemperature + (Temperature - otherTemperature) * transferExp;
        }

        /// <summary>
        /// Transfers heat between two physics objects
        /// </summary>
        /// <param name="other">Other physics object</param>
        /// <param name="thermalConductivity">kW/K</param>
        /// <param name="dt">delta time</param>
        public void TransferHeat(ThermalPhysicsSlim other, float thermalConductivity,
            float dt = MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS)
        {
            // T1' = k1*(T2 - T1)
            // T2' = k2*(T1 - T2)
            // T1 = e ^ ((k1 + k2) * t)
            // T2 = (-k2 / k1) * e ^ ((k1 + k2) * t)

            var k1 = -thermalConductivity / _heatCapacity;
            var k2 = -thermalConductivity / other._heatCapacity;

            var exp = (float) Math.Exp((k1 + k2) * dt);

            var ct = Temperature;
            var ot = other.Temperature;

            // BC:
            // T1(0) = _currentTemperature
            // T2(0) = other._currentTemperature
            // c1 = T1(infty) = T2(infty) = total energy / total heat capacity
            var c1 = ((ct * _heatCapacity) + (ot * other._heatCapacity)) / (_heatCapacity + other._heatCapacity);
            Temperature = c1 + (Temperature - c1) * exp;
            other.Temperature = c1 + (ot - c1) * exp;
        }

        ThermalPhysicsSlim IThermalPhysicsProvider.Physics => this;

        private TimeSpan? _lastUpdate;

        /// <summary>
        /// Updates this thermal physics entity.
        /// </summary>
        /// <param name="entity">Entity to apply damage to</param>
        public void Update(IMyDestroyableObject entity = null)
        {
            if (!_lastUpdate.HasValue)
            {
                _lastUpdate = MyAPIGateway.Session.ElapsedPlayTime;
                return;
            }

            var dt = (float) (MyAPIGateway.Session.ElapsedPlayTime - _lastUpdate.Value).TotalSeconds;
            if (dt < MyEngineConstants.UPDATE_STEP_SIZE_IN_SECONDS / 2)
                return;
            _lastUpdate = MyAPIGateway.Session.ElapsedPlayTime;
            Update(dt, entity);
        }

        /// <summary>
        /// Applies overheating mechanics
        /// </summary>
        /// <param name="energy"></param>
        /// <param name="entity"></param>
        public void ApplyOverheating(float energy, IMyDestroyableObject entity, bool removeEnergy,
            MyStringHash? damageName = null)
        {
            var slim = entity as IMySlimBlock;
            var massToDestroy = energy / Material.EnthalpyOfFusion;

            var maxIntegrity = Math.Max(entity?.Integrity ?? 1, 1);

            if (slim != null)
                maxIntegrity = slim.MaxIntegrity;
            else
            {
                MyEntityStat healthStat = null;
                (entity as IMyEntity)?.Components.Get<MyEntityStatComponent>()
                    ?.TryGetStat(MyCharacterStatComponent.HealthId, out healthStat);
                var floating = entity as MyFloatingObject;
                if (healthStat != null)
                    maxIntegrity = healthStat.MaxValue;
                else if (floating != null)
                    maxIntegrity = MyDefinitionManager.Static.GetPhysicalItemDefinition(floating.Item.Content)
                        .Health;
            }

            var integrityPerKg = maxIntegrity / Mass;

            var damageToDo = Math.Min(integrityPerKg * massToDestroy, (entity?.Integrity + 0.1f) ?? 1.1f);

            if (removeEnergy)
            {
                var massToDestroyReal = damageToDo / integrityPerKg;
                AddEnergy(-massToDestroyReal * Material.EnthalpyOfFusion);
            }

            if (!MyAPIGateway.Session.IsServerDecider()) return;
            
            if ((slim?.CubeGrid != null && !slim.IsDestroyed && !slim.CubeGrid.Closed &&
                 slim.CubeGrid.GetCubeBlock(slim.Position) == slim)
                || !((entity as IMyEntity)?.Closed ?? true))
                entity?.DoDamage(damageToDo, damageName ?? _overheatingHash, true);
        }

        /// <summary>
        /// Updates this thermal physics entity with a time delta
        /// </summary>
        /// <param name="entity">Entity to apply damage to</param>
        /// <param name="dt">Delta time, in seconds</param>
        public void Update(float dt, IMyDestroyableObject entity = null)
        {
            var slim = entity as IMySlimBlock;
            if (RadiateIntoSpaceConductivity > TOLERANCE)
            {
                var bkgTemp = PhysicalConstants.TemperatureSpace;
                var ent = entity as IMyEntity;
                if (slim != null)
                {
                    Vector3D pos;
                    slim.ComputeWorldCenter(out pos);
                    bkgTemp = PhysicalConstants.TemperatureAtPoint(pos);
                }
                else if (ent != null)
                    bkgTemp = PhysicalConstants.TemperatureAtPoint(ent.WorldMatrix.Translation);

                RadiateHeat(bkgTemp, RadiateIntoSpaceConductivity, dt);
            }

            if (OverheatDamageMultiplier > 0)
            {
                var temp = OverheatTemperature ?? Material.MeltingPoint;
                var energy = (Temperature - temp) * Material.SpecificHeat * Mass;
                if (energy > 0)
                    ApplyOverheating(energy, entity, true);
            }
        }

        public void Debug(StringBuilder sb)
        {
            sb.Append("Temperature: ").Append(Temperature.ToString("F2")).AppendLine(" K");
            if (RadiateIntoSpaceConductivity > 0)
                sb.Append("Radiating ").Append(RadiateIntoSpaceConductivity.ToString("F2")).AppendLine("kW/K");
            sb.Remove(sb.Length - 1, 1); // remove nl
        }
    }
}