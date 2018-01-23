using System;
using System.Collections.Generic;
using System.Linq;
using Equinox.EnergyWeapons.Misc;
using Equinox.Utils.Logging;
using Equinox.Utils.Misc;
using ParallelTasks;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Input;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Equinox.EnergyWeapons.Components.Network
{
    public abstract class NetworkController<TSegmentType, TConnData> : MyEntityComponentBase, IRenderableComponent
        where TConnData : IConnectionData where TSegmentType:Segment<TSegmentType, TConnData>
    {
        public EnergyWeaponsCore Core { get; }
        public ILogging Logger { get; }

        private readonly NetworkDetectors<TSegmentType, TConnData> _detectors;

        protected NetworkController(EnergyWeaponsCore core)
        {
            Core = core;
            Logger = core.Logger.CreateProxy(GetType());
            _detectors = new NetworkDetectors<TSegmentType, TConnData>(core, this);
        }

        #region Per-Network Config

        // :/ consider redesign
        public abstract TConnData DetectorConnectionData { get; }

        public abstract TConnData DissolveableConnectionData { get; }

        public abstract TSegmentType AllocateSegment(bool bidirectional, params DummyData<TSegmentType, TConnData>[] path);
        #endregion

        #region Network Storage

        private readonly Dictionary<DummyKey, DummyData<TSegmentType, TConnData>> _pathByDummy =
            new Dictionary<DummyKey, DummyData<TSegmentType, TConnData>>();

        public List<TSegmentType> Segments { get; } = new List<TSegmentType>();

        public DummyData<TSegmentType, TConnData> GetOrCreate(IMyEntity entity, string path, out bool created)
        {
            created = false;
            var key = new DummyKey(entity, path);
            DummyData< TSegmentType, TConnData > res;
            if (!_pathByDummy.TryGetValue(key, out res))
            {
                res = new DummyData<TSegmentType, TConnData>(new DummyPathRef(entity, path.Split('/')));
                res.Segment = AllocateSegment(true, res);
                _pathByDummy.Add(key, res);
                created = true;
            }

            return res;
        }

        public void MakeLink(DummyData<TSegmentType, TConnData> from, DummyData<TSegmentType, TConnData> to,
            bool bidirectional, TConnData data)
        {
            Segment<TSegmentType, TConnData>.MakeLink(from, to, bidirectional, data);
        }

        public void BreakLink(DummyData<TSegmentType, TConnData> from, DummyData<TSegmentType, TConnData> to)
        {
            Segment<TSegmentType, TConnData>.BreakLink(from, to);
        }

        public void Remove(IMyEntity entity, string path)
        {
            _detectors?.Remove(entity, path);

            var key = new DummyKey(entity, path);
            DummyData<TSegmentType, TConnData> data;
            if (!_pathByDummy.TryGetValue(key, out data))
                return;
            _pathByDummy.Remove(key);
            Segment<TSegmentType, TConnData>.Remove(data);
        }


        public void Link(IMyEntity fromEntity, string fromPath, IMyEntity toEntity, string toPath, bool bidirectional,
            TConnData data)
        {
            bool tmp;

            Logger.Debug(
                $"Linking {fromEntity.ToStringSmart()}: {fromPath} with {toEntity.ToStringSmart()}:{toPath} ({bidirectional})");
            var from = GetOrCreate(fromEntity, fromPath, out tmp);
            var to = GetOrCreate(toEntity, toPath, out tmp);
            MakeLink(from, to, bidirectional, data);
        }

        public void Unlink(IMyEntity fromEntity, string fromPath, IMyEntity toEntity, string toPath)
        {
            Logger.Debug(
                $"Unlinking {fromEntity.ToStringSmart()}: {fromPath} with {toEntity.ToStringSmart()}:{toPath}");
            bool tmp;
            var from = GetOrCreate(fromEntity, fromPath, out tmp);
            var to = GetOrCreate(toEntity, toPath, out tmp);
            BreakLink(from, to);
        }

        public void AddDetector(IMyEntity ent, string path, bool input, bool output)
        {
            Logger.Debug($"Creating detector {ent.ToStringSmart()}:{path}:{input}?{output}");
            _detectors.AddDetector(ent, path, input, output);
        }

        #endregion

        #region Updating

        private Task? _updateTask;
        private float _dtCapture;
        public void Update(ulong deltaTicks)
        {
            if (_updateTask.HasValue && !_updateTask.Value.IsComplete)
                _updateTask.Value.Wait();
            var dt = deltaTicks * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;
            foreach (var k in Segments)
                k.Commit(dt);
            _dtCapture = dt;
            _updateTask = MyAPIGateway.Parallel.ForNonBlocking(0, Segments.Count, (i) =>
            {
                if (i >= Segments.Count)
                    return;
                Segments.GetInternalArray()[i]?.Predict(_dtCapture);
            });
        }

        public override void OnAddedToScene()
        {
            Core.Scheduler.RepeatingUpdate(Update, 5);
        }

        public override void OnRemovedFromScene()
        {
            Core.Scheduler.RemoveUpdate(Update);
        }

        public override void OnAddedToContainer()
        {
            if (Entity.InScene)
                OnAddedToScene();
        }

        public override void OnBeforeRemovedFromContainer()
        {
            if (Entity.InScene)
                OnRemovedFromScene();
        }

        #endregion

        public virtual void Draw()
        {
        }

        #region Debugging

        public void DumpData()
        {
            Logger.Info("-- begin network dump --");
            Logger.Info($"Segment count {Segments.Count}");
            foreach (var segment in Segments)
            {
                Logger.Info($"segment {segment.GetHashCode():X8}:");
                var f = segment.Forwards ? "F" : "";
                var r = segment.Backwards ? "R" : "";
                Logger.Info(
                    $"  Path ({segment.Path.Count} {f}{r}): {string.Join(", ", segment.Path.Select(x => x.Dummy))}");
                if (segment.Connections.Count > 0)
                {
                    Logger.Info(
                        $"  Connections ({segment.Connections.Count}): {string.Join(", ", segment.Connections.Select(x => (x.From.Segment != segment ? x.From : x.To).Segment.GetHashCode().ToString("X8")))}");
                    Logger.Info(
                        $"  Connections (explicit): {string.Join(", ", segment.Connections.Select(x => x.From.Dummy + (x.Bidirectional ? " with " : " to ") + x.To.Dummy + " " + x.Data))}");
                }
            }

            Logger.Info("-- end network dump --");
        }

        public void DebugDraw()
        {
            for (var i = 0; i < Segments.Count; i++)
            {
                Segment<TSegmentType, TConnData> segment = Segments.GetInternalArray()[i];
                if (segment == null)
                    continue;
                var c = (Vector4) Utils.Misc.ColorExtensions.SeededColor(i);
                for (var j = 1; j < segment.Path.Count; j++)
                    DrawArrow(segment.Path[j - 1].Dummy.WorldMatrix.Translation,
                        segment.Path[j].Dummy.WorldMatrix.Translation, c, c, segment.Forwards, segment.Backwards);

                foreach (var conn in segment.Connections)
                    if (conn.From.Segment == segment)
                    {
                        var c2 = (Vector4) Utils.Misc.ColorExtensions.SeededColor(
                            Segments.IndexOf((TSegmentType) conn.To.Segment));
                        DrawArrow(conn.From.Dummy.WorldMatrix.Translation, conn.To.Dummy.WorldMatrix.Translation,
                            c, c2, true, conn.Bidirectional);
                    }
            }

            _detectors.DebugDraw();
        }


        private static readonly MyStringId _laserMaterial = MyStringId.GetOrCompute("WeaponLaser");
        private static void DrawArrow(Vector3D from, Vector3D to, Vector4 fromColor, Vector4 toColor, bool forwards,
            bool backwards)
        {
            var dir = (from - to);
            var lineLen = dir.Normalize();
            if (lineLen < 0.05f)
            {
                var m = MatrixD.CreateTranslation((from + to) / 2);
                Color c = (fromColor + toColor) / 2;
                var box = new BoundingBoxD(Vector3D.One / -8, Vector3D.One / 8);
                MySimpleObjectDraw.DrawTransparentBox(ref m, ref box, ref c, MySimpleObjectRasterizer.Wireframe, 0,
                    0.01f, null, _laserMaterial);
                return;
            }

            var a = Vector3D.Cross(dir, MyAPIGateway.Session.Camera.Position - ((from + to) / 2));
            a.Normalize();


            var len = Math.Min(lineLen / 10, 0.2f);

            var ds0 = to + Vector3D.Normalize(dir + a) * len;
            var ds1 = to + Vector3D.Normalize(dir - a) * len;

            var dt0 = from + Vector3D.Normalize(-dir + a) * len;
            var dt1 = from + Vector3D.Normalize(-dir - a) * len;


            MySimpleObjectDraw.DrawLine(ds0, dt0, _laserMaterial, ref fromColor, 0.05f);
            MySimpleObjectDraw.DrawLine(ds1, dt1, _laserMaterial, ref toColor, 0.05f);

            if (forwards)
            {
                MySimpleObjectDraw.DrawLine(to, ds0, _laserMaterial, ref toColor, 0.05f);
                MySimpleObjectDraw.DrawLine(to, ds1, _laserMaterial, ref toColor, 0.05f);
            }

            if (backwards)
            {
                MySimpleObjectDraw.DrawLine(from, dt0, _laserMaterial, ref fromColor, 0.05f);
                MySimpleObjectDraw.DrawLine(from, dt1, _laserMaterial, ref fromColor, 0.05f);
            }
        }

        #endregion
    }
}