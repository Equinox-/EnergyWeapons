using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Equinox.Utils.Logging;
using Equinox.Utils.Misc;
using VRage.ModAPI;
using VRageMath;

namespace Equinox.EnergyWeapons.Components.Beam
{
    public class NetworkStorage
    {
        private readonly Dictionary<DummyKey, DummyData> _pathByDummy = new Dictionary<DummyKey, DummyData>();

        public List<Segment> Segments { get; } = new List<Segment>();

        public DummyData GetOrCreate(IMyEntity entity, string path, out bool created)
        {
            created = false;
            var key = new DummyKey(entity, path);
            DummyData res;
            if (!_pathByDummy.TryGetValue(key, out res))
            {
                res = new DummyData(new DummyPathRef(entity, path.Split('/')));
                res.Segment = new Segment(this, true, res);
                _pathByDummy.Add(key, res);
                created = true;
            }

            return res;
        }

        public void MakeLink(DummyData from, DummyData to, bool bidirectional, float factor,
            Vector4 filter)
        {
            Segment.MakeLink(from, to, bidirectional, factor, filter);
        }

        public void BreakLink(DummyData from, DummyData to)
        { 
            Segment.BreakLink(from,to);
        }

        public void Remove(IMyEntity entity, string path)
        {
            var key = new DummyKey(entity, path);
            DummyData data;
            if (!_pathByDummy.TryGetValue(key, out data))
                return;
            _pathByDummy.Remove(key);
            Segment.Remove(data);
        }
    }
}