using System;
using VRageMath;

namespace Equinox.EnergyWeapons.Components.Beam
{
    public struct Connection : IEquatable<Connection>
    {
        public readonly DummyData From;
        public readonly DummyData To;
        public readonly Vector4 Filter;
        public readonly float PowerFactor;
        public readonly bool Bidirectional;

        public Connection(DummyData from, DummyData to, bool bidirectional, Vector4 filter, float factor)
        {
            Bidirectional = bidirectional;
            Filter = filter;
            PowerFactor = factor;
            From = from;
            To = to;
        }

        public Connection(DummyData from, DummyData to, bool bidirectional) : this(@from, to, bidirectional,
            Vector4.One, 1)
        {
        }

        public bool Equals(Connection other)
        {
            return Equals(From, other.From) && Equals(To, other.To);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Connection && Equals((Connection) obj);
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