using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equinox.Utils.Logging;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace Equinox.EnergyWeapons.Components.Beam.Logic
{
    public class Emitter : Lossy<Definition.Beam.Emitter>
    {
        public Emitter(NetworkComponent block, Definition.Beam.Emitter definition) : base(block, definition)
        {
        }

        private DummyData _dummy;

        public override void OnAddedToScene()
        {
            Block.IsWorkingChanged += StateChanged;
            var func = Block as IMyFunctionalBlock;
            if (func != null)
                func.EnabledChanged += StateChanged;

            bool created;
            _dummy = Network.Controller.GetOrCreate(Block, Definition.Dummy, out created);
            StateChanged(Block);
        }

        public override void OnRemovedFromScene()
        {
            Block.IsWorkingChanged -= StateChanged;
            var func = Block as IMyFunctionalBlock;
            if (func != null)
                func.EnabledChanged -= StateChanged;


            if (_scheduled)
            {
                Update(10);
                Network.Core.Scheduler.RemoveUpdate(Update);
            }
        }


        private bool _scheduled = false;

        private void StateChanged(IMyCubeBlock e)
        {
            var func = Block as IMyFunctionalBlock;
            var on = Block.IsWorking && (func == null || func.Enabled);
            if (on && !_scheduled)
                Network.Core.Scheduler.RepeatingUpdate(Update, 10);
            else if (!on && _scheduled)
            {
                Update(10);
                Network.Core.Scheduler.RemoveUpdate(Update);
            }

            _scheduled = on;
        }

        public static readonly MyDefinitionId ElectricityId =
            new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Electricity");

        private void Update(ulong dticks)
        {
            var func = Block as IMyFunctionalBlock;
            var on = Block.IsWorking && (func == null || func.Enabled);
            if (!on)
                return;


            float maxPower = Definition.MaxPowerOutput;
            float power;
            if (Block.ResourceSink != null)
            {
                Block.ResourceSink.SetMaxRequiredInputByType(ElectricityId, maxPower / 1e3f); //kW to MW
                power = Block.ResourceSink.CurrentInputByType(ElectricityId) * 1e3f;
            }
            else
            {
                power = maxPower;
            }

            var fractional = power / maxPower;
            var color = Vector4.Lerp(Definition.ColorMin, Definition.ColorMax, fractional);

            var energy = power * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS * dticks;
            var heatLoss = Math.Max(0, 1 - Definition.Efficiency) * energy;
            _dummy.Segment?.AddEnergy(energy * Definition.Efficiency, color);
        }
    }
}