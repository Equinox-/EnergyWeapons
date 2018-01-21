using System.Linq;
using Equinox.Utils.Misc;

namespace Equinox.EnergyWeapons.Components.Beam
{
    public class DummyData
    {
        public readonly DummyPathRef Dummy;
        public Segment Segment { get; internal set; }

        public DummyData(DummyPathRef dummy)
        {
            Dummy = dummy;
        }
    }
}