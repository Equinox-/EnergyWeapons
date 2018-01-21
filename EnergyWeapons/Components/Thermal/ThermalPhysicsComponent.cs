using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equinox.EnergyWeapons.Misc;
using Equinox.EnergyWeapons.Physics;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.ModAPI;
using VRage.Utils;

namespace Equinox.EnergyWeapons.Components.Thermal
{
    public class ThermalPhysicsComponent : MyGameLogicComponent, IThermalPhysicsProvider, ICoreRefComponent
    {
        private const float TOLERANCE = 1e-5f;
        private static readonly MyStringHash _overheatingHash = MyStringHash.GetOrCompute("Overheating");

        public override string ComponentTypeDebugString
        {
            get { return nameof(ThermalPhysicsComponent); }
        }

        private EnergyWeaponsCore _core;

        public ThermalPhysicsSlim Physics { get; }

        public ThermalPhysicsComponent()
        {
            Physics = new ThermalPhysicsSlim(MaterialPropertyDatabase.IronMaterial, 1, PhysicalConstants.TemperatureSpace);
            Physics.NeedsUpdateChanged += OnNeedsUpdateChanged;
        }

        private void OnNeedsUpdateChanged(bool old, bool @new)
        {
            if (@new)
                NeedsUpdate |= MyEntityUpdateEnum.EACH_10TH_FRAME;
            else
                NeedsUpdate &= ~MyEntityUpdateEnum.EACH_10TH_FRAME;
        }

        public override void OnAddedToContainer()
        {
            CheckProperties();
        }

        public void OnAddedToCore(EnergyWeaponsCore core)
        {
            _core = core;
            CheckProperties();
        }

        public void OnBeforeRemovedFromCore()
        {
            _core = null;
            CheckProperties();
        }

        public override void UpdateAfterSimulation10()
        {
            Physics.Update(Entity as IMyDestroyableObject);
        }

        private void CheckProperties()
        {
            if (Entity == null || _core == null)
                return;
            Physics.Init(_core.Materials, Entity);
        }


        /// <summary>
        /// Radiates heat into space.
        /// </summary>
        /// <param name="thermalConductivity">kW/K</param>
        public void RadiateIntoSpace(float thermalConductivity)
        {
            var temp = Entity != null
                ? PhysicalConstants.TemperatureAtPoint(Entity.WorldMatrix.Translation)
                : PhysicalConstants.TemperatureSpace;
            Physics.RadiateHeat(temp, thermalConductivity);
        }
    }
}