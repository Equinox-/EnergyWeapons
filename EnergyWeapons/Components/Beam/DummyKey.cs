using System;
using VRage.ModAPI;

namespace Equinox.EnergyWeapons.Components.Beam
{
    internal struct DummyKey : IEquatable<DummyKey>
    {
        public readonly IMyEntity Entity;
        public readonly string DummyPath;

        public DummyKey(IMyEntity e, string dummy)
        {
            Entity = e;
            DummyPath = dummy;
        }

        public bool Equals(DummyKey other)
        {
            return Equals(Entity, other.Entity) && String.Equals(DummyPath, other.DummyPath);
        }

        public override bool Equals(object obj)
        {
            return obj is DummyKey && Equals((DummyKey) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Entity != null ? Entity.GetHashCode() : 0) * 397) ^
                       (DummyPath != null ? DummyPath.GetHashCode() : 0);
            }
        }
    }
}