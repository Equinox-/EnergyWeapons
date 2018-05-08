using Equinox.EnergyWeapons.Components.Network;
using Equinox.EnergyWeapons.Session;
using Equinox.Utils.Logging;
using Equinox.Utils.Misc;
using VRageMath;

namespace Equinox.EnergyWeapons.Components.Beam
{
    public struct BeamConnectionData : IConnectionData
    {
        public readonly Vector4 Filter;
        public readonly float MaxThroughput;
        public bool Bidirectional { get; }

        public BeamConnectionData(Vector4 filter, float maxThroughput = float.PositiveInfinity, bool bidirectional = true)
        {
            Filter = filter;
            MaxThroughput = maxThroughput;
            Bidirectional = bidirectional;
        }

        public bool CanDissolve => float.IsPositiveInfinity(MaxThroughput) && Filter.EqualsEps(Vector4.One) && Bidirectional;

        public override string ToString()
        {
            var mode = Bidirectional ? "bidi" : "unidir";
            return $"BeamConn[M={mode}, Max={MaxThroughput} kW, Filter={Filter.X:F2},{Filter.Y:F2},{Filter.Z:F2},{Filter.W:F2}]";
        }
    }
}
