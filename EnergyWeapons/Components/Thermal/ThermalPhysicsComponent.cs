using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equinox.EnergyWeapons.Misc;
using Equinox.EnergyWeapons.Physics;
using Equinox.Utils.Components;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.ModAPI;
using VRage.Utils;

namespace Equinox.EnergyWeapons.Components.Thermal
{
    public class ThermalPhysicsComponent : ComponentSceneCallback, IDebugComponent, IThermalPhysicsProvider
    {
        private const float TOLERANCE = 1e-5f;
        private static readonly MyStringHash _overheatingHash = MyStringHash.GetOrCompute("Overheating");


        private readonly EnergyWeaponsCore _core;

        public ThermalPhysicsSlim Physics { get; }

        public ThermalPhysicsComponent(EnergyWeaponsCore core)
        {
            _core = core;
            Physics = new ThermalPhysicsSlim(MaterialPropertyDatabase.IronMaterial, 1,
                PhysicalConstants.TemperatureSpace);
            Physics.NeedsUpdateChanged += (old, @new) => NeedsUpdate = @new;
        }

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
            var required = _needsUpdate && Entity != null && Entity.InScene;

            if (required && !_scheduled)
                _core.Scheduler.RepeatingUpdate(UpdateAfterSimulation10, 10);
            else if (_scheduled && !required)
                _core.Scheduler.RemoveUpdate(UpdateAfterSimulation10);

            _scheduled = required;
        }

        public override void OnAddedToScene()
        {
            CheckProperties();
            CheckScheduled();
        }

        public override void OnRemovedFromScene()
        {
            NeedsUpdate = false;
            CheckScheduled();
        }


        private void CheckProperties()
        {
            if (Entity == null || _core == null)
                return;
            Physics.Init(_core.Materials, Entity);
        }

        private void UpdateAfterSimulation10(ulong ticks)
        {
            if (Entity != null && Entity.InScene)
                Physics.Update(Entity as IMyDestroyableObject);
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

        public void Debug(StringBuilder sb)
        {
            Physics.Debug(sb);
        }
    }
}