using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Equinox.Utils.Components;
using Equinox.Utils.Logging;
using Equinox.Utils.Misc;
using VRage;
using VRage.Noise.Modifiers;
using VRageMath;

namespace Equinox.EnergyWeapons.Components.Beam
{
    public class Segment : IDebugComponent
    {
        [Flags]
        private enum Direction
        {
            /// <summary>
            /// Beam can travel from beginning to end, or self to other.
            /// </summary>
            FirstToLast = 0x1,

            /// <summary>
            /// Beam can travel from end to beginning, or other to self.
            /// </summary>
            LastToFirst = 0x2
        }

        private readonly List<DummyData> _path;
        private Direction _pathDirection;

        private readonly List<Connection> _connections = new List<Connection>();

        private bool _activeDirectionInvalid;
        private Direction _activeDirection;

        private Direction GetActiveDirection(bool recurse)
        {
            if (!_activeDirectionInvalid)
                return _activeDirection;

            var dir = _pathDirection;
            foreach (var e in _connections)
            {
                var sink = e.To;
                var source = e.From;
                if (e.Bidirectional)
                {
                    if (!recurse)
                        continue;

                    var other = e.From.Segment != this ? e.From : e.To;
                    var odir = other.Segment.GetActiveDirection(false);
                    var fwd = (odir & Direction.FirstToLast) != 0;
                    var rev = (odir & Direction.LastToFirst) != 0;
                    if (fwd == rev)
                        continue;
                    var inv = e.From.Segment == this ? e.From : e.To;
                    // other is the source (i.e. the output from this connection)
                    var isSink = (fwd && other.Segment._path.First() == other) ||
                                 (rev && other.Segment._path.Last() == other);
                    if (isSink)
                    {
                        sink = inv;
                        source = other;
                    }
                    else
                    {
                        sink = other;
                        source = inv;
                    }
                }

                if ((dir & Direction.FirstToLast) != 0 && _path.First() != sink && _path.Last() != source)
                    dir &= ~Direction.FirstToLast;
                if ((dir & Direction.LastToFirst) != 0 && _path.Last() != sink && _path.First() != source)
                    dir &= ~Direction.LastToFirst;
            }

            if (recurse)
            {
                var changed = _activeDirection != dir;
                if (changed)
                {
                    foreach (var e in _connections)
                        if (e.Bidirectional)
                        {
                            var other = e.From.Segment != this ? e.From : e.To;
                            other.Segment._activeDirectionInvalid = true;
                        }
                }

                _activeDirection = dir;
                _activeDirectionInvalid = false;
            }

            return dir;
        }

        private Direction ActiveDirection
        {
            get
            {
                if (_activeDirectionInvalid)
                    GetActiveDirection(true);

                return _activeDirection;
            }
        }

        private readonly NetworkStorage _network;


        public IReadOnlyList<Connection> Connections => _connections;
        public IReadOnlyList<DummyData> Path => _path;
        public bool Forwards => (ActiveDirection & Direction.FirstToLast) != 0;
        public bool Backwards => (ActiveDirection & Direction.LastToFirst) != 0;

        public Segment(NetworkStorage network, bool bidirectional, params DummyData[] path)
        {
            _network = network;
            _network.Segments.Add(this);
            _path = new List<DummyData>(path);
            foreach (var k in path)
                k.Segment = this;
            _pathDirection = bidirectional ? Direction.FirstToLast | Direction.LastToFirst : Direction.FirstToLast;
            _activeDirectionInvalid = true;
        }

        private bool AnyConnections(DummyData data, bool asSource = true, bool asSink = true)
        {
            foreach (var k in _connections)
                if ((k.To == data && (asSink || k.Bidirectional)) ||
                    (k.From == data && (asSource || k.Bidirectional)))
                    return true;
            return false;
        }

        /// <summary>
        /// Is this dummy on the output of its segment.
        /// </summary>
        private static bool IsPossibleSource(DummyData d)
        {
            var dir = d.Segment.GetActiveDirection(true);
            if ((dir & Direction.FirstToLast) != 0 && d.Segment._path.Last() == d)
                return true;
            return (dir & Direction.LastToFirst) != 0 && d.Segment._path.First() == d;
        }

        /// <summary>
        /// Is this dummy on the input of its segment.
        /// </summary>
        private static bool IsPossibleSink(DummyData d)
        {
            var dir = d.Segment.GetActiveDirection(true);
            if ((dir & Direction.FirstToLast) != 0 && d.Segment._path.First() == d)
                return true;
            return (dir & Direction.LastToFirst) != 0 && d.Segment._path.Last() == d;
        }

        private void Split(DummyData at, bool shouldBeSource)
        {
            var dir = GetActiveDirection(true);
            var idx = _path.IndexOf(at);
            if (idx == -1)
                throw new Exception(
                    $"Couldn't find {at.Dummy} in {GetHashCode():X8}: {string.Join(", ", _path.Select(x => x.Dummy))}");

            if ((shouldBeSource && (dir & Direction.FirstToLast) != 0) ||
                (!shouldBeSource && (dir & Direction.LastToFirst) != 0))
            {
                // should not be moved to the new segment
                idx++;
            }

            var reverse = (dir & Direction.FirstToLast) == 0 ? 1 : 0;
            var from = _path[idx - 1 + reverse];
            var to = _path[idx - reverse];

            var ns = new Segment(_network, true) {_pathDirection = _pathDirection};
            for (var i = idx; i < _path.Count; i++)
            {
                ns._path.Add(_path[i]);
                _path[i].Segment = ns;
                for (var j = 0; j < _connections.Count; j++)
                {
                    if (_connections[j].From == _path[i] || _connections[j].To == _path[i])
                    {
                        ns._connections.Add(_connections[j]);
                        _connections.RemoveAtFast(j);
                        j--;
                    }
                }
            }

            _path.RemoveRange(idx, _path.Count - idx);

            var conn = new Connection(from, to,
                (dir & Direction.FirstToLast) != 0 && (dir & Direction.LastToFirst) != 0);
            _connections.Add(conn);
            ns._connections.Add(conn);

            _activeDirectionInvalid = true;
            ns._activeDirectionInvalid = true;

            if (shouldBeSource)
            {
                if (!IsPossibleSource(at))
                    throw new Exception($"Failed to split {GetHashCode():X8} so {at.Dummy} was a source");
            }
            else
            {
                if (!IsPossibleSink(at))
                    throw new Exception($"Failed to split {GetHashCode():X8} so {at.Dummy} was a source");
            }
        }

        public static void MakeLink(DummyData from, DummyData to, bool bidir, float factor, Vector4 filter)
        {
            bool attachReversed;
            {
                var fromAsFrom = IsPossibleSource(from);
                var fromAsTo = bidir && IsPossibleSink(from);
                var toAsTo = IsPossibleSink(to);
                var toAsFrom = bidir && IsPossibleSource(to);


                // If it can't be connected in forward mode and can be connected in backwards mode... do it.
                if (!fromAsFrom && !toAsTo && fromAsTo && toAsFrom)
                    attachReversed = true;
                else
                {
                    attachReversed = false;
                    if (!fromAsFrom)
                        from.Segment.Split(from, true);

                    if (!toAsTo)
                        to.Segment.Split(to, false);
                }
            }

            var simple = factor.EqualsEps(1f) && filter.EqualsEps(Vector4.One);
            if (simple && !from.Segment.AnyConnections(from, true, bidir) &&
                !to.Segment.AnyConnections(to, bidir, true) &&
                from.Segment != to.Segment)
            {
                // attach via path injection

                var moving = attachReversed ? from : to;
                var removing = moving.Segment;
                var modifying = attachReversed ? to : from;

                // Update paths
                {
                    var removeReversed = removing._path.First() != moving;
                    var modifyReversed = modifying.Segment._path.Last() != modifying;

                    var modCount = removing._path.Count;
                    int modOffset;
                    if (modifyReversed)
                    {
                        modOffset = 0;
                        modifying.Segment._path.InsertRange(0, removing._path);
                    }
                    else
                    {
                        modOffset = modifying.Segment._path.Count;
                        modifying.Segment._path.AddRange(removing._path);
                    }

                    for (var i = 0; i < modCount; i++)
                        modifying.Segment._path[modOffset + i].Segment = modifying.Segment;

                    if (modifyReversed != removeReversed)
                    {
                        // reverse injected items
                        for (var i = 0; i < (modCount >> 1) + (modCount & 1); i++)
                        {
                            var ii = modOffset + i;
                            var oi = modOffset + modCount - i - 1;
                            var tmp = modifying.Segment._path[oi];
                            modifying.Segment._path[oi] = modifying.Segment._path[ii];
                            modifying.Segment._path[ii] = tmp;
                        }

                        var swapped = ((int) removing._pathDirection << 1) |
                                      ((int) removing._pathDirection >> 1);
                        modifying.Segment._pathDirection &= (Direction) swapped;
                    }
                    else
                    {
                        modifying.Segment._pathDirection &= removing._pathDirection;
                    }

                    // Not bidir we definitely aren't attaching reversed.  So modifying is "from".
                    if (!bidir)
                        modifying.Segment._pathDirection &=
                            modifyReversed ? Direction.LastToFirst : Direction.FirstToLast;

                    removing._path.Clear();
                }
                // Update connections
                {
                    foreach (var conn in removing._connections)
                        modifying.Segment._connections.Add(conn);
                    removing._connections.Clear();
                }
                modifying.Segment._activeDirectionInvalid = true;

                removing._network.Segments.Remove(removing);
            }
            else
            {
                var conn = new Connection(from, to, bidir, filter, factor);

                from.Segment._connections.Add(conn);
                from.Segment._activeDirectionInvalid = true;
                if (to.Segment != from.Segment)
                {
                    to.Segment._connections.Add(conn);
                    to.Segment._activeDirectionInvalid = true;
                }
            }
        }

        private void RemoveLink(DummyData from, DummyData to)
        {
            for (var i = 0; i < _connections.Count; i++)
                if ((_connections[i].From == from && _connections[i].To == to) ||
                    (_connections[i].Bidirectional && _connections[i].From == to && _connections[i].To == from))
                {
                    _connections.RemoveAtFast(i);
                    i--;
                    _activeDirectionInvalid = true;
                }
        }

        private void BreakAfter(int idx)
        {
            idx++;
            var ns = new Segment(_network, true);
            ns._pathDirection = _pathDirection;
            for (var i = idx; i < _path.Count; i++)
            {
                ns._path.Add(_path[i]);
                _path[i].Segment = ns;
                for (var j = 0; j < _connections.Count; j++)
                {
                    if (_connections[j].From == _path[i] || _connections[j].To == _path[i])
                    {
                        ns._connections.Add(_connections[j]);
                        _connections.RemoveAtFast(j);
                        j--;
                    }
                }
            }

            _path.RemoveRange(idx, _path.Count - idx);
            _activeDirectionInvalid = true;
            ns._activeDirectionInvalid = true;
        }

        public static void BreakLink(DummyData from, DummyData to)
        {
            if (from.Segment != to.Segment)
            {
                from.Segment.RemoveLink(from, to);
                to.Segment.RemoveLink(from, to);
                return;
            }

            var i = from.Segment._path.IndexOf(from);
            var j = to.Segment._path.IndexOf(to);
            if (i == -1)
                throw new Exception($"Unable to find {from} in {from.Segment}");
            if (j == -1)
                throw new Exception($"Unable to find {to} in {to.Segment}");

            if (Math.Abs(i - j) != 1)
                throw new Exception($"{from} and {to} not adjacent");
            from.Segment.BreakAfter(Math.Min(i, j));
        }

        /// <summary>
        /// Removes the given dummy
        /// </summary>
        /// <param name="data">Data to remove</param>
        public static void Remove(DummyData data)
        {
            // remove any connections using this dummy.
            for (var i = 0; i < data.Segment._connections.Count; i++)
            {
                var c = data.Segment._connections[i];
                DummyData partner = null;
                if (c.From == data)
                    partner = c.To;
                else if (c.To == data)
                    partner = c.From;
                else
                    continue;
                data.Segment._connections.RemoveAtFast(i);
                i--;
                if (partner.Segment == data.Segment)
                    continue;
                var o = partner.Segment._connections;
                for (var j = 0; j < o.Count; j++)
                {
                    var cc = o[j];
                    if (cc.From == data || cc.To == data)
                    {
                        o.RemoveAtFast(j);
                        j--;
                        partner.Segment._activeDirectionInvalid = true;
                    }
                }
            }

            var idx = data.Segment._path.IndexOf(data);
            if (idx != 0 && idx != data.Segment._path.Count - 1)
                data.Segment.BreakAfter(idx);
            data.Segment._path.Remove(data);
            data.Segment._activeDirectionInvalid = true;
            data.Segment = null;
        }

        /// <summary>
        /// Current energy in kJ
        /// </summary>
        public float CurrentEnergy { get; private set; }

        /// <summary>
        /// Current beam color
        /// </summary>
        public Vector4 CurrentColor { get; private set; }

        /// <summary>
        /// Total output of energy in kW.
        /// </summary>
        public float Output { get; private set; }

        /// <summary>
        /// Total output of energy in kW, averaged with an exponential moving average.
        /// </summary>
        public float OutputEma { get; private set; }

        public event Action<Segment> StateUpdated;

        private float _nextEnergy;
        private Vector4 _nextColor;
        private float _nextTotalOutput;

        private float _injectedEnergy, _drainedEnergy;
        private Vector4 _injectColorWeighted;

        /// <summary>
        /// Fills the next tick information.
        /// </summary>
        public void Predict()
        {
            var nextEnergy = CurrentEnergy;
            var nextColorWeighted = CurrentColor * CurrentEnergy;

            var exports = 0f;

            // ReSharper disable once ForCanBeConvertedToForeach to make this "thread safe"
            for (var i = 0; i < _connections.Count; i++)
            {
                var con = _connections.GetInternalArray()[i]; // normal indexer can out-of-bounds
                if (con.From == null || con.To == null)
                    continue;
                var opponent = con.From.Segment == this ? con.To.Segment : con.From.Segment;
                if (opponent == this)
                    continue;
                var dE = (opponent.CurrentEnergy - CurrentEnergy) * con.PowerFactor / 2f;
                if (con.From.Segment == this && !con.Bidirectional)
                    dE = Math.Min(0, dE);
                else if (con.To.Segment == this && !con.Bidirectional)
                    dE = Math.Max(0, dE);

                if (dE < 0)
                    exports -= dE;

                nextEnergy += dE;
                nextColorWeighted += dE * con.Filter * (dE > 0 ? opponent.CurrentColor : CurrentColor);
            }

            lock (this)
            {
                _nextTotalOutput += exports;

                _nextEnergy = Math.Max(0, nextEnergy);
                _nextColor = nextColorWeighted / Math.Max(ENERGY_EPS, nextEnergy);
            }
        }

        /// <summary>
        /// Adds energy to this segment
        /// </summary>
        /// <param name="energy">kJ to add</param>
        /// <param name="color">color of energy</param>
        public void AddEnergy(float energy, Vector4 color)
        {
            lock (this)
            {
                if (energy > 0)
                    _injectedEnergy += energy;
                else
                    _drainedEnergy -= energy;
                _injectColorWeighted += energy * color;
            }
        }

        /// <summary>
        /// Commits the next tick information into the current tick information
        /// </summary>
        public void Commit(float dt)
        {
            float nextOutput;

            lock (this)
            {
                // Throughput is max(sum(all inputs), sum(all outputs)) / dt
                _nextTotalOutput += _drainedEnergy;
                nextOutput = _nextTotalOutput / dt;

                _nextTotalOutput = 0;
                var netInjection = _injectedEnergy - _drainedEnergy;

                var d = Math.Max(ENERGY_EPS, netInjection + _nextEnergy);
                _nextColor = (_nextColor * _nextEnergy + _injectColorWeighted * netInjection) / d;
                _nextEnergy += netInjection;

                _injectedEnergy = 0;
                _drainedEnergy = 0;
                _injectColorWeighted = Vector4.Zero;
            }

            Output = nextOutput;
            OutputEma = OutputEma * 0.95f + Output * .05f;
            CurrentColor = Vector4.Clamp(_nextColor, Vector4.Zero, Vector4.One);
            CurrentEnergy = _nextEnergy;

            StateUpdated?.Invoke(this);
        }

        private const float ENERGY_EPS = 1e-3f;

        public void Debug(StringBuilder sb)
        {
            sb.Append("C=").Append(_path.Count);
            sb.Append(" Power=").Append(CurrentEnergy.ToString("F2")).Append("kJ");
            sb.Append(" Output=").Append(OutputEma.ToString("F2")).Append("kW");
            sb.Append(" Color=").AppendFormat("[{0:F2} {1:F2} {2:F2} {3:F2}]", CurrentColor.X, CurrentColor.Y,
                CurrentColor.Z, CurrentColor.W);
        }
    }
}