using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Equinox.Utils.Components;

namespace Equinox.EnergyWeapons.Components.Network
{
    public abstract class Segment<TSegmentType, TConnData> : IDebugComponent where TConnData : IConnectionData
        where TSegmentType : Segment<TSegmentType, TConnData>
    {
        private readonly List<DummyData<TSegmentType, TConnData>> _path;

        private readonly List<Connection<TSegmentType, TConnData>> _connections =
            new List<Connection<TSegmentType, TConnData>>();

        private readonly NetworkController<TSegmentType, TConnData> _network;

        protected event Action PathUpdated;

        public IReadOnlyList<Connection<TSegmentType, TConnData>> Connections => _connections;
        public IReadOnlyList<DummyData<TSegmentType, TConnData>> Path => _path;

        protected Segment(NetworkController<TSegmentType, TConnData> network, params DummyData<TSegmentType, TConnData>[] path)
        {
            _network = network;
            _network.Segments.Add((TSegmentType) this);
            _path = new List<DummyData<TSegmentType, TConnData>>(path);
            foreach (var k in path)
                k.Segment = (TSegmentType) this;
        }

        private bool AnyConnections(DummyData<TSegmentType, TConnData> data)
        {
            foreach (var k in _connections)
                if (k.To == data || k.From == data)
                    return true;
            return false;
        }

        private void Split(DummyData<TSegmentType, TConnData> at)
        {
            var idx = _path.IndexOf(at);
            if (idx == -1)
                throw new Exception(
                    $"Couldn't find {at.Dummy} in {GetHashCode():X8}: {string.Join(", ", _path.Select(x => x.Dummy))}");

            var from = _path[idx - 1];
            var to = _path[idx];

            var ns = _network.AllocateSegment();
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

            var conn = new Connection<TSegmentType, TConnData>(from, to, _network.DissolveableConnectionData);
            _connections.Add(conn);
            ns._connections.Add(conn);

            PathUpdated?.Invoke();
            ns.PathUpdated?.Invoke();
        }

        public static void MakeLink(DummyData<TSegmentType, TConnData> from, DummyData<TSegmentType, TConnData> to,
            TConnData data)
        {
            {
                if (!from.Endpoint)
                    from.Segment.Split(from);

                if (!to.Endpoint)
                    to.Segment.Split(to);
            }

            if (data.CanDissolve && !from.Segment.AnyConnections(from) && !to.Segment.AnyConnections(to) &&
                from.Segment != to.Segment)
            {
                // attach via path injection
                var moveFromNode = from.Segment.Path.Count < to.Segment.Path.Count;

                var nodeToMove = moveFromNode ? from : to;
                var nodeToPreserve = moveFromNode ? to : from;
                var segmentToRemove = nodeToMove.Segment;
                var segmentToGrow = nodeToPreserve.Segment;

                // Update paths
                {
                    var removeReversed = segmentToRemove._path.Last() == nodeToMove;
                    var modifyReversed = segmentToGrow._path.First() == nodeToPreserve;

                    var modCount = segmentToRemove._path.Count;
                    int modOffset;
                    if (modifyReversed)
                    {
                        modOffset = 0;
                        segmentToGrow._path.InsertRange(0, segmentToRemove._path);
                    }
                    else
                    {
                        modOffset = segmentToGrow._path.Count;
                        segmentToGrow._path.AddRange(segmentToRemove._path);
                    }

                    foreach (var n in segmentToRemove._path)
                        n.Segment = segmentToGrow;

                    if (modifyReversed != removeReversed)
                    {
                        // reverse injected items
                        for (var i = 0; i < (modCount >> 1) + (modCount & 1); i++)
                        {
                            var ii = modOffset + i;
                            var oi = modOffset + modCount - i - 1;
                            var tmp = segmentToGrow._path[oi];
                            segmentToGrow._path[oi] = segmentToGrow._path[ii];
                            segmentToGrow._path[ii] = tmp;
                        }
                    }

                    segmentToRemove._path.Clear();
                }
                // Update connections
                {
                    foreach (var conn in segmentToRemove._connections)
                        segmentToGrow._connections.Add(conn);
                    segmentToRemove._connections.Clear();
                }
                segmentToRemove._network.Segments.Remove(segmentToRemove);

                segmentToGrow.PathUpdated?.Invoke();
            }
            else
            {
                var conn = new Connection<TSegmentType, TConnData>(from, to, data);

                from.Segment._connections.Add(conn);
                if (to.Segment != from.Segment)
                    to.Segment._connections.Add(conn);
            }
        }

        private void RemoveLink(DummyData<TSegmentType, TConnData> from, DummyData<TSegmentType, TConnData> to)
        {
            for (var i = 0; i < _connections.Count; i++)
                if ((_connections[i].From == from && _connections[i].To == to) ||
                    (_connections[i].Data.Bidirectional && _connections[i].From == to && _connections[i].To == from))
                {
                    _connections.RemoveAtFast(i);
                    i--;
                }
        }

        private void BreakAfter(int idx)
        {
            idx++;
            var ns = _network.AllocateSegment();
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
            PathUpdated?.Invoke();
        }

        public static void BreakLink(DummyData<TSegmentType, TConnData> from, DummyData<TSegmentType, TConnData> to)
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
        public static void Remove(DummyData<TSegmentType, TConnData> data)
        {
            // remove any connections using this dummy.
            for (var i = 0; i < data.Segment._connections.Count; i++)
            {
                var c = data.Segment._connections[i];
                DummyData<TSegmentType, TConnData> partner = null;
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
                    }
                }
            }

            var idx = data.Segment._path.IndexOf(data);
            if (idx != 0 && idx != data.Segment._path.Count - 1)
                data.Segment.BreakAfter(idx);
            data.Segment._path.Remove(data);
            data.Segment.PathUpdated?.Invoke();
            data.Segment = null;
        }

        public event Action<TSegmentType> StateUpdated;

        /// <summary>
        /// Fills the next tick information.
        /// </summary>
        public virtual void Predict(float dt)
        {
            // ReSharper disable once ForCanBeConvertedToForeach to make this "thread safe"
            for (var i = 0; i < _connections.Count; i++)
            {
                var con = _connections.GetInternalArray()[i]; // normal indexer can out-of-bounds
                if (con.From == null || con.To == null)
                    continue;
                PredictConnection(con, dt);
            }
        }

        protected abstract void PredictConnection(Connection<TSegmentType, TConnData> connection, float dt);

        /// <summary>
        /// Commits the next tick information into the current tick information
        /// </summary>
        public abstract void Commit(float dt);

        protected void RaiseStateChanged()
        {
            StateUpdated?.Invoke((TSegmentType) this);
        }

        public virtual void Debug(StringBuilder sb)
        {
            sb.Append("C=").Append(_path.Count);
        }
    }
}