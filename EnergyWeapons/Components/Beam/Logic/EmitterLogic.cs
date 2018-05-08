using System;
using System.Text;
using Equinox.EnergyWeapons.Misc;
using Equinox.EnergyWeapons.Session;
using Equinox.Utils.Session;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;
using DummyData =
    Equinox.EnergyWeapons.Components.Network.DummyData<Equinox.EnergyWeapons.Components.Beam.Segment,
        Equinox.EnergyWeapons.Components.Beam.BeamConnectionData>;

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
        private AdvancedResourceSink.SinkData _sink;

        public override void OnAddedToScene()
        {
            _sink = Network.ResourceSink.Value.AllocateSink(ConstantDefs.ElectricityId);
            _sink.MaxPower = Definition.MaxPowerOutput / 1e3f;
            _sink.RequiredPowerFunc = () => DesiredEmitterPower / 1e3f;

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
            Network.ResourceSink.Value.FreeSink(_sink);
            _sink = null;
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
                MyAPIGateway.Session.GetComponent<SchedulerAfter>().RepeatingUpdate(Update, UPDATE_INTERVAL);
            else if (_scheduled && !required)
                MyAPIGateway.Session.GetComponent<SchedulerAfter>().RemoveUpdate(Update);

            _scheduled = required;
        }

        private void StateChanged(IMyCubeBlock e)
        {
            var func = Block as IMyFunctionalBlock;
            NeedsUpdate = Block.IsWorking && (func == null || func.Enabled);
        }

        #endregion

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
                var on = Block.IsWorking;
                if (!on || IsOverheated)
                    return 0;

                float maxPower = Definition.MaxPowerOutput;
                if (!Definition.AutomaticTurnOff)
                    return maxPower;

                var turnOffThreshold = maxPower * EmitterTargetSupply;
                // desiredOutput is enough to hit target in the next update.
                var desiredOutput = (turnOffThreshold - (_dummy.Segment?.Current.Energy ?? turnOffThreshold)) /
                                    (Efficiency(Definition.Efficiency) *
                                     MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS *
                                     UPDATE_INTERVAL);
                return MathHelper.Clamp(desiredOutput, 0, maxPower);
            }
        }

        private float EmitterPower
        {
            get
            {
                var desiredOutput = DesiredEmitterPower;
                if (_sink != null && Network.ResourceSink.Value.Attached)
                    return _sink.CurrentPower * 1e3f;
                return desiredOutput;
            }
        }


        public override void Debug(StringBuilder sb)
        {
            base.Debug(sb);
            sb.Append("Power=").Append(EmitterPower.ToString("F0")).Append("/")
                .Append(DesiredEmitterPower.ToString("F0")).Append("  ");
            sb.Append("Segment=").Append((_dummy.Segment?.GetHashCode() ?? 0).ToString("X8")).Append(" ");
            sb.Append("Sink=").Append(_sink.GetHashCode().ToString("X8"));
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