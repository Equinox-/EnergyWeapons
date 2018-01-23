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

using DummyData =
    Equinox.EnergyWeapons.Components.Network.DummyData<Equinox.EnergyWeapons.Components.Beam.Segment, Equinox.EnergyWeapons.Components.Beam.BeamConnectionData>;

namespace Equinox.EnergyWeapons.Components.Beam.Logic
{
    public class Emitter : Lossy<Definition.Beam.Emitter>
    {
        /// <summary>
        /// Auto-supply gives enough energy for this long.
        /// </summary>
        public const float EmitterTargetSupply = 10f;

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

            NeedsUpdate = false;
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
                Network.Core.Scheduler.RepeatingUpdate(Update, UPDATE_INTERVAL);
            else if (_scheduled && !required)
                Network.Core.Scheduler.RemoveUpdate(Update);

            _scheduled = required;
        }

        private void StateChanged(IMyCubeBlock e)
        {
            var func = Block as IMyFunctionalBlock;
            NeedsUpdate = Block.IsWorking && (func == null || func.Enabled);
        }

        #endregion

        public static readonly MyDefinitionId ElectricityId =
            new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Electricity");

        private const int UPDATE_INTERVAL = 10;

        private bool _wasOverheated;

        private bool IsOverheated
        {
            get
            {
                if (!Definition.ThermalFuseMax.HasValue)
                    return false;
                var fuseTemp = _wasOverheated
                    ? (Definition.ThermalFuseMin ?? Definition.ThermalFuseMax.Value)
                    : Definition.ThermalFuseMax;
                return _wasOverheated = (CurrentTemperature) > fuseTemp;
            }
        }

        private float DesiredEmitterPower
        {
            get
            {
                var func = Block as IMyFunctionalBlock;
                var on = Block.IsWorking && (func == null || func.Enabled);
                if (!on || IsOverheated)
                    return 0;

                float maxPower = Definition.MaxPowerOutput;
                if (!Definition.AutomaticTurnOff)
                    return maxPower;

                var turnOffThreshold = maxPower * EmitterTargetSupply;
                // desiredOutput is enough to hit target in the next update.
                var desiredOutput = (turnOffThreshold - (_dummy.Segment?.Current.Energy ?? turnOffThreshold)) /
                                    (Efficiency(Definition.Efficiency) * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS *
                                     UPDATE_INTERVAL);
                return MathHelper.Clamp(desiredOutput, 0, maxPower);
            }
        }

        private float EmitterPower
        {
            get
            {
                var desiredOutput = DesiredEmitterPower;
                if (Block.ResourceSink != null)
                {
                    Block.ResourceSink.SetMaxRequiredInputByType(ElectricityId, desiredOutput / 1e3f); //kW to MW
                    return Block.ResourceSink.CurrentInputByType(ElectricityId) * 1e3f;
                }

                return desiredOutput;
            }
        }


        public override void Debug(StringBuilder sb)
        {
            base.Debug(sb);
            sb.Append("Power=").Append(EmitterPower.ToString("F0")).Append("/")
                .Append(DesiredEmitterPower.ToString("F0")).Append("  ");
            sb.Append("Segment=").Append((_dummy.Segment?.GetHashCode() ?? 0).ToString("X8"));
        }

        private void Update(ulong dticks)
        {
            float power = EmitterPower;

            if (_dummy.Segment == null || Math.Abs(power) < 1e-6f)
                return;
            float dt = MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS * dticks;
            float maxPower = Definition.MaxPowerOutput;

            var fractional = power / maxPower;
            var color = Vector4.Lerp(Definition.ColorMin, Definition.ColorMax, fractional);

            var energy = power * dt;
            _dummy.Segment.Inject(energy * Efficiency(Definition.Efficiency), color);
        }
    }
}