using System;
using System.Linq;
using Equinox.Utils.Misc;

namespace Equinox.EnergyWeapons.Components.Network
{
    public class DummyData<TSegmentType, TConnData> where TConnData : IConnectionData
        where TSegmentType : Segment<TSegmentType, TConnData>
    {
        public readonly DummyPathRef Dummy;
        private TSegmentType _segment;

        public TSegmentType Segment
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

        public event Action<TSegmentType, TSegmentType> SegmentChanged;

        public DummyData(DummyPathRef dummy)
        {
            Dummy = dummy;
        }

        public bool Endpoint => _segment != null && (_segment.Path.Last() == this || _segment.Path.First() == this);
    }
}