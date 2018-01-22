using System;
using System.Linq;
using Equinox.Utils.Misc;

namespace Equinox.EnergyWeapons.Components.Beam
{
    public class DummyData
    {
        public readonly DummyPathRef Dummy;
        private Segment _segment;

        public Segment Segment
        {
            get { return _segment; }
            set
            {
                if (_segment == value)
                    return;
                var old = _segment;
                _segment = value;
                SegmentChanged?.Invoke(old, value);
            }
        }

        public event Action<Segment, Segment> SegmentChanged;

        public DummyData(DummyPathRef dummy)
        {
            Dummy = dummy;
        }
    }
}