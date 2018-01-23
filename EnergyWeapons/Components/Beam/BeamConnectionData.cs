using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equinox.EnergyWeapons.Components.Network;
using Equinox.Utils.Misc;
using VRageMath;

namespace Equinox.EnergyWeapons.Components.Beam
{
    public struct BeamConnectionData : IConnectionData
    {
        public readonly Vector4 Filter;
        public readonly float MaxThroughput;

        public BeamConnectionData(Vector4 filter, float maxThroughput = float.PositiveInfinity)
        {
            Filter = filter;
            MaxThroughput = maxThroughput;
        }

        public bool CanDissolve => float.IsPositiveInfinity(MaxThroughput) && Filter.EqualsEps(Vector4.One);

        public override string ToString()
        {
            return $"BeamConn[Max={MaxThroughput} kW, Filter={Filter.X:F2},{Filter.Y:F2},{Filter.Z:F2},{Filter.W:F2}]";
        }
    }
}
