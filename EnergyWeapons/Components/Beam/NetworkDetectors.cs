using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equinox.Utils.Logging;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Equinox.EnergyWeapons.Components.Beam
{
    public class NetworkDetectors
    {
        private readonly EnergyWeaponsCore _core;
        private readonly NetworkController _network;
        private readonly ILogging _log;

        public NetworkDetectors(EnergyWeaponsCore core, NetworkController controller)
        {
            _core = core;
            _network = controller;
            _log = core.Logger.CreateProxy(GetType());
        }

        private readonly MyDynamicAABBTree _detectorTree = new MyDynamicAABBTree(Vector3.Zero);
        private readonly Queue<DetectorData> _waitingInsert = new Queue<DetectorData>();
        private readonly Dictionary<DummyKey, DetectorData> _detectorData = new Dictionary<DummyKey, DetectorData>();

        private class DetectorData
        {
            public readonly IMyEntity Entity;
            public readonly string Path;
            public readonly bool Input, Output;
            public BoundingBox BoundingBox { get; private set; }
            public int ProxyId { get; private set; } = -1;

            public DetectorData(IMyEntity ent, string path, bool input, bool output)
            {
                Entity = ent;
                Path = path;
                Input = input;
                Output = output;
            }

            public void Update(int id, BoundingBox box)
            {
                if (ProxyId != -1)
                    EnergyWeaponsCore.LoggerStatic?.Warning("Assigning proxy twice");
                ProxyId = id;
                BoundingBox = box;
            }
        }

        private void CheckWaiting()
        {
            Dictionary<string, IMyModelDummy> tmp = null;
            var limit = _waitingInsert.Count;
            while (_waitingInsert.Count > 0 && (limit-- > 0))
            {
                var ins = _waitingInsert.Dequeue();
                if (tmp == null)
                    tmp = new Dictionary<string, IMyModelDummy>();
                if (ins.Entity.Model != null)
                {
                    ins.Entity.Model.GetDummies(tmp);
                    IMyModelDummy dummy;
                    if (tmp.TryGetValue(ins.Path, out dummy))
                    {
                        var bb = new BoundingBox(-Vector3.Half, Vector3.Half).Transform(
                            dummy.Matrix * ins.Entity.LocalMatrix);
                        var proxyId = _detectorTree.AddProxy(ref bb, ins, 0);
                        ins.Update(proxyId, bb);
                        DoConnect(ins);
                        continue;
                    }
                }

                _waitingInsert.Enqueue(ins);
            }
        }

        private readonly List<DetectorData> _overlapResults = new List<DetectorData>();

        private void DoConnect(DetectorData data)
        {
            var bb = data.BoundingBox;
            _detectorTree.OverlapAllBoundingBox(ref bb, _overlapResults);
            foreach (var k in _overlapResults)
                if (k != data)
                {
                    _log.Debug(
                        $"Detectors overlap {data.Entity}:{data.Path}:{data.Input}:{data.Output} and {k.Entity}:{k.Path}:{k.Input}:{k.Output}");
                    var forward = k.Input && data.Output;
                    var reverse = k.Output && data.Input;
                    if (forward)
                        _network.Link(data.Entity, data.Path, k.Entity, k.Path, reverse, 1, Vector4.One);
                    else if (reverse)
                        _network.Link(k.Entity, k.Path, data.Entity, data.Path, false, 1, Vector4.One);
                }
        }

        public void AddDetector(IMyEntity ent, string dummy, bool input, bool output)
        {
            var key = new DummyKey(ent, dummy);
            var data = new DetectorData(ent, dummy, input, output);
            _detectorData.Add(key, data);
            _waitingInsert.Enqueue(data);

            CheckWaiting();
        }

        public void Remove(IMyEntity ent, string dummy)
        {
            var key = new DummyKey(ent, dummy);
            DetectorData res;
            if (_detectorData.TryGetValue(key, out res))
            {
                _detectorData.Remove(key);
                if (res.ProxyId != -1)
                    _detectorTree.RemoveProxy(res.ProxyId);
                else
                {
                    var c = _waitingInsert.Count;
                    while (c-- > 0)
                    {
                        var k = _waitingInsert.Dequeue();
                        if (k != res)
                            _waitingInsert.Enqueue(k);
                    }
                }

                CheckWaiting();
            }
        }


        private static readonly MyStringId _laserMaterial = MyStringId.GetOrCompute("WeaponLaser");

        public void DebugDraw()
        {
            foreach (var d in _detectorData.Values)
            {
                if (d.ProxyId == -1)
                    continue;
                var parent = d.Entity.Parent;
                if (parent == null)
                    continue;
                var mat = parent.WorldMatrix;
                var box = (BoundingBoxD) d.BoundingBox;
                var c = d.Input ? (d.Output ? Color.Orange : Color.Green) : Color.Red;
                MySimpleObjectDraw.DrawTransparentBox(ref mat, ref box, ref c, MySimpleObjectRasterizer.Wireframe, 0,
                    0.01f, null, _laserMaterial);
            }
        }
    }
}