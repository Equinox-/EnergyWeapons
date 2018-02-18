using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equinox.EnergyWeapons.Misc;
using Equinox.Utils.Components;
using Sandbox.Definitions;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace Equinox.EnergyWeapons.Components
{
    public class AdvancedResourceSink : ComponentSceneCallback, IDebugComponent
    {
        private readonly ComponentDependency<MyResourceSinkComponent> _resourceSink;

        public class SinkData
        {
            public readonly AdvancedResourceSink Sink;
            public readonly MyDefinitionId Id;

            internal SinkData(AdvancedResourceSink sink, MyDefinitionId id)
            {
                Sink = sink;
                Id = id;
            }

            /// <summary>
            /// Function for computing the current power demand, in MW.
            /// </summary>
            public Func<float> RequiredPowerFunc { get; set; } = null;

            /// <summary>
            /// Current power demanded by the sink, in MW
            /// </summary>
            public float RequiredPower { get; set; }

            /// <summary>
            /// Maximum power demanded by the sink, in MW
            /// </summary>
            public float MaxPower { get; set; }

            /// <summary>
            /// Raised when <see cref="CurrentPower"/> changes.
            /// </summary>
            public event Action InputChanged;

            private float _currentPower;

            /// <summary>
            /// Current power delivered to the sink, in MW.
            /// </summary>
            public float CurrentPower
            {
                get { return _currentPower; }
                internal set
                {
                    var old = _currentPower;
                    _currentPower = value;
                    if (Math.Abs(old - _currentPower) > 1e-6f)
                        InputChanged?.Invoke();
                }
            }
        }

        private class PerTypeData
        {
            public readonly List<SinkData> Sinks = new List<SinkData>();

            public float CalculateMaxInput()
            {
                var v = 0f;
                foreach (var l in Sinks)
                    v += l.MaxPower;
                return v;
            }

            public readonly Func<float> DelCalculateRequiredInput;

            public PerTypeData()
            {
                DelCalculateRequiredInput = CalculateRequiredInput;
            }

            private float CalculateRequiredInput()
            {
                var v = 0f;
                foreach (var l in Sinks)
                {
                    if (l.RequiredPowerFunc != null)
                        l.RequiredPower = l.RequiredPowerFunc();
                    v += l.RequiredPower;
                }

                return v;
            }
        }

        private readonly Dictionary<MyDefinitionId, PerTypeData> _sinks =
            new Dictionary<MyDefinitionId, PerTypeData>(MyDefinitionId.Comparer);

        private readonly EnergyWeaponsCore _core;

        public AdvancedResourceSink(EnergyWeaponsCore core)
        {
            _core = core;
            _resourceSink = new ComponentDependency<MyResourceSinkComponent>(this);
            _resourceSink.ValueChanged += SinkChanged;
        }

        private const long UPDATE_RATE = 5;
        public bool Attached => Container != null && _resourceSink.Value != null;
        
        public override void OnAddedToScene()
        {
            _core.Scheduler.RepeatingUpdate(Update, UPDATE_RATE);
        }

        public override void OnRemovedFromScene()
        {
            _core.Scheduler.RemoveUpdate(Update);
        }

        private PerTypeData Type(MyDefinitionId id)
        {
            PerTypeData res;
            if (!_sinks.TryGetValue(id, out res))
            {
                _sinks.Add(id, res = new PerTypeData());
                var root = _resourceSink.Value;
                if (root != null)
                {
                    var info = new MyResourceSinkInfo()
                    {
                        MaxRequiredInput = res.CalculateMaxInput(),
                        RequiredInputFunc = res.DelCalculateRequiredInput,
                        ResourceTypeId = id
                    };
                    root.AddType(ref info);
                }
            }

            return res;
        }

        public SinkData AllocateSink(MyDefinitionId id)
        {
            var td = Type(id);
            var sink = new SinkData(this, id);
            td.Sinks.Add(sink);
            return sink;
        }

        public void FreeSink(SinkData sink)
        {
            var td = Type(sink.Id);
            td.Sinks.Remove(sink);
            if (td.Sinks.Count == 0)
                _sinks.Remove(sink.Id);
        }

        public void Update(ulong dticks = UPDATE_RATE)
        {
            var root = _resourceSink.Value;
            if (root == null)
                return;
            foreach (var kv in _sinks)
            {
                root.SetMaxRequiredInputByType(kv.Key, kv.Value.CalculateMaxInput());
                root.SetRequiredInputFuncByType(kv.Key, kv.Value.DelCalculateRequiredInput);
            }

            root.Update();
        }

        private SinkData _operationalSink;
        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            _resourceSink.OnAddedToContainer();
            if (Entity is IMyFunctionalBlock)
            {
                _operationalSink = AllocateSink(ConstantDefs.ElectricityId);
                _operationalSink.RequiredPower = _operationalSink.MaxPower = 50e-3f; // 50 W
            }
        }

        public override void OnBeforeRemovedFromContainer()
        {
            if (_operationalSink != null)
            {
                FreeSink(_operationalSink);
                _operationalSink = null;
            }
            base.OnBeforeRemovedFromContainer();
            _resourceSink.OnBeforeRemovedFromContainer();
        }

        private void SinkChanged(MyResourceSinkComponent old, MyResourceSinkComponent @new)
        {
            if (old != null)
            {
                foreach (var key in old.AcceptedResources)
                {
                    old.SetMaxRequiredInputByType(key, 0);
                    old.SetRequiredInputFuncByType(key, () => 0);
                }

                RemoveDelegate(ref old.CurrentInputChanged, OnInputChanged);
            }

            if (@new == null)
                return;
            foreach (var kv in _sinks)
            {
                var info = new MyResourceSinkInfo()
                {
                    MaxRequiredInput = kv.Value.CalculateMaxInput(),
                    RequiredInputFunc = kv.Value.DelCalculateRequiredInput,
                    ResourceTypeId = kv.Key
                };
                @new.AddType(ref info);
                @new.SetMaxRequiredInputByType(kv.Key, kv.Value.CalculateMaxInput());
                @new.SetRequiredInputFuncByType(kv.Key, kv.Value.DelCalculateRequiredInput);
            }

            AddDelegate(ref @new.CurrentInputChanged, OnInputChanged);
        }


        private void OnInputChanged(MyDefinitionId resourceTypeId, float oldInput, MyResourceSinkComponent sink)
        {
            PerTypeData tmp;
            if (!_sinks.TryGetValue(resourceTypeId, out tmp))
                return;

            var factor = sink.CurrentInputByType(resourceTypeId) / Math.Max(1e-6f, tmp.CalculateMaxInput());
            foreach (var v in tmp.Sinks)
                v.CurrentPower = v.MaxPower * factor;
        }

        #region I hate my life

        private static void RemoveDelegate(ref MyCurrentResourceInputChangedDelegate evt,
            MyCurrentResourceInputChangedDelegate callback)
        {
            if (evt == null)
                return;
            evt = (MyCurrentResourceInputChangedDelegate) Delegate.Remove(evt, callback);
        }

        private static void AddDelegate(ref MyCurrentResourceInputChangedDelegate evt,
            MyCurrentResourceInputChangedDelegate callback)
        {
            if (evt == null)
            {
                evt = callback;
                return;
            }

            evt = (MyCurrentResourceInputChangedDelegate) Delegate.Combine(evt, callback);
        }

        #endregion

        public void Debug(StringBuilder sb)
        {
            const string indent = "  ";
            sb.Append(_sinks.Count).Append(" ").Append(_resourceSink.Value != null ? "linked" : "unlinked")
                .AppendLine();
            foreach (var x in _sinks)
            {
                sb.Append(indent);
                sb.Append(x.Key.SubtypeName).Append(" -> ").Append(x.Value.Sinks.Count);
                sb.Append(" required: ").Append(_resourceSink.Value?.RequiredInputByType(x.Key).ToString("F3") ?? "nil")
                    .Append("/")
                    .Append(_resourceSink.Value?.MaxRequiredInputByType(x.Key).ToString("F3") ?? "nil")
                    .Append(" MW");
                sb.Append(" curr: ").Append(_resourceSink.Value?.CurrentInputByType(x.Key).ToString("F3") ?? "nil")
                    .Append(" MW").AppendLine();
                foreach (var c in x.Value.Sinks)
                    sb.Append(indent).Append(indent)
                        .Append(
                            $"{c.GetHashCode():X8}  {c.RequiredPower:F3}/{c.MaxPower:F3} MW curr: {c.CurrentPower:F3} MW")
                        .AppendLine();
            }

            sb.Remove(sb.Length - 1, 1); // nl
        }
    }
}