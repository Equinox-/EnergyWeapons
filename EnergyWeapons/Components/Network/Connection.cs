using System;

namespace Equinox.EnergyWeapons.Components.Network
{
    public struct Connection<TSegmentType, TConnData> : IEquatable<Connection<TSegmentType,TConnData>>
        where TConnData : IConnectionData where TSegmentType : Segment<TSegmentType, TConnData>
    {
        public readonly DummyData<TSegmentType, TConnData> From;
        public readonly DummyData<TSegmentType, TConnData> To;
        public readonly TConnData Data;
        public readonly bool Bidirectional;

        public Connection(DummyData<TSegmentType, TConnData> from, DummyData<TSegmentType, TConnData> to,
            bool bidirectional, TConnData data)
        {
            Bidirectional = bidirectional;
            Data = data;
            From = from;
            To = to;
        }

        public bool Equals(Connection<TSegmentType, TConnData> other)
        {
            return Equals(From, other.From) && Equals(To, other.To);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Connection<TSegmentType, TConnData> && Equals((Connection<TSegmentType, TConnData>) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((From != null ? From.GetHashCode() : 0) * 397) ^ (To != null ? To.GetHashCode() : 0);
            }
        }
    }
}