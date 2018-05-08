using System.Text;
using Equinox.EnergyWeapons.Physics;
using Equinox.EnergyWeapons.Session;
using Equinox.Utils.Components;
using Equinox.Utils.Session;
using Sandbox.ModAPI;
using VRage.Game.ModAPI.Interfaces;
using VRage.Utils;

namespace Equinox.EnergyWeapons.Components.Thermal
{
    public class ThermalPhysicsComponent : ComponentSceneCallback, IDebugComponent, IThermalPhysicsProvider
    {
        private const float TOLERANCE = 1e-5f;
        private static readonly MyStringHash _overheatingHash = MyStringHash.GetOrCompute("Overheating");

        
        public ThermalPhysicsSlim Physics { get; }

        public ThermalPhysicsComponent()
        {
            Physics = new ThermalPhysicsSlim(MaterialPropertyDatabase.IronMaterial, 1, PhysicalConstants.TemperatureSpace);
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
                MyAPIGateway.Session.GetComponent<SchedulerAfter>().RepeatingUpdate(UpdateAfterSimulation10, 10);
            else if (_scheduled && !required)
                MyAPIGateway.Session.GetComponent<SchedulerAfter>().RemoveUpdate(UpdateAfterSimulation10);

            _scheduled = required;
        }

        private ThermalManager _thermal;

        public override void OnAddedToScene()
        {
            _thermal = MyAPIGateway.Session.GetComponent<ThermalManager>();
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
            if (Entity == null || _thermal == null)
                return;
            Physics.Init(_thermal.Materials, Entity);
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