using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Equinox.EnergyWeapons.Definition.Beam;
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

namespace Equinox.EnergyWeapons.Components.Beam
{
    public class NetworkController : MyEntityComponentBase, IRenderableComponent
    {
        public EnergyWeaponsCore Core { get; }
        public ILogging Logger { get; }

        private readonly NetworkStorage _network = new NetworkStorage();
        private readonly NetworkDetectors _detectors;

        public NetworkController(EnergyWeaponsCore core)
        {
            Core = core;
            Logger = core.Logger.CreateProxy(GetType());
            _detectors = new NetworkDetectors(core, this);
        }

        #region Network

        public DummyData GetOrCreate(IMyEntity ent, string path, out bool created)
        {
            return _network.GetOrCreate(ent, path, out created);
        }

        public void Link(IMyEntity fromEntity, string fromPath, IMyEntity toEntity, string toPath, bool bidirectional,
            float powerFactor, Vector4 colorFilter)
        {
            bool tmp;

            Logger.Debug(
                $"Linking {fromEntity.ToStringSmart()}: {fromPath} with {toEntity.ToStringSmart()}:{toPath} ({bidirectional})");
            var from = _network.GetOrCreate(fromEntity, fromPath, out tmp);
            var to = _network.GetOrCreate(toEntity, toPath, out tmp);
            _network.MakeLink(from, to, bidirectional, powerFactor, colorFilter);
        }

        public void Unlink(IMyEntity fromEntity, string fromPath, IMyEntity toEntity, string toPath)
        {
            Logger.Debug(
                $"Unlinking {fromEntity.ToStringSmart()}: {fromPath} with {toEntity.ToStringSmart()}:{toPath}");
            bool tmp;
            var from = _network.GetOrCreate(fromEntity, fromPath, out tmp);
            var to = _network.GetOrCreate(toEntity, toPath, out tmp);
            _network.BreakLink(from, to);
        }

        public void AddDetector(IMyEntity ent, string path, bool input, bool output)
        {
            Logger.Debug($"Creating detector {ent.ToStringSmart()}:{path}:{input}?{output}");
            _detectors.AddDetector(ent, path, input, output);
        }

        public void Remove(IMyEntity ent, string path)
        {
            _detectors.Remove(ent, path);
            _network.Remove(ent, path);
        }

        #endregion

        #region Updating

        private Task? _updateTask;

        public void Update(ulong deltaTicks)
        {
            if (MyAPIGateway.Input.IsKeyPress(MyKeys.OemCloseBrackets))
                return;
            if (_updateTask.HasValue && !_updateTask.Value.IsComplete)
                _updateTask.Value.Wait();
            var dt = deltaTicks * MyEngineConstants.PHYSICS_STEP_SIZE_IN_SECONDS;
            foreach (var k in _network.Segments)
                k.Commit(dt);
            _updateTask = MyAPIGateway.Parallel.ForNonBlocking(0, _network.Segments.Count, (i) =>
            {
                if (i >= _network.Segments.Count)
                    return;
                _network.Segments.GetInternalArray()[i]?.Predict();
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

        #region Render

        public static float BeamWidth(float power)
        {
            // at half the max power the width is 70%
            const float _keyMaxPower = 1e6f; // 1 GW
            const float _keyMaxWidth = 1f; // 1 m

            var powerFrac = MathHelper.Clamp(power / _keyMaxPower, 0f, _keyMaxWidth * _keyMaxWidth);
            return (float) Math.Sqrt(powerFrac);
        }

        public static Vector4 BeamColor(Vector4 tint, float power)
        {
            tint.W = 1;
            return tint;
        }

        public void Draw()
        {
            for (var i = 0; i < _network.Segments.Count; i++)
            {
                Segment segment = _network.Segments.GetInternalArray()[i];
                if (segment == null || segment.OutputEma <= 0)
                    continue;
                var c = BeamColor(segment.CurrentColor, segment.OutputEma);
                var width = BeamWidth(segment.OutputEma);

                if (segment.Path.Count > 0)
                {
                    Vector3D prev = segment.Path[0].Dummy.WorldMatrix.Translation;
                    bool prevView = MyAPIGateway.Session.Camera.IsInFrustum(ref prev);
                    for (var j = 1; j < segment.Path.Count; j++)
                    {
                        var curr = segment.Path[j].Dummy.WorldMatrix.Translation;
                        var currView = MyAPIGateway.Session.Camera.IsInFrustum(ref curr);
                        if (prevView || currView)
                            MySimpleObjectDraw.DrawLine(prev, curr, LaserMaterial, ref c, width);
                        prev = curr;
                        prevView = currView;
                    }
                }

                foreach (var conn in segment.Connections)
                {
                    var tsi = conn.To.Segment?.Path.Count ?? 0;
                    var fsi = conn.From.Segment?.Path.Count ?? 0;
                    var tci = conn.To.Segment?.Connections.Count ?? 0;
                    var fci = conn.From.Segment?.Connections.Count ?? 0;
                    // choose source segment by first which has more elements, then by which has fewer connections, then just choose from;
                    Segment preferredSegment = conn.From.Segment;
                    if (tsi != fsi)
                        preferredSegment = tsi > fsi ? conn.To.Segment : conn.From.Segment;
                    else if (tci != fci)
                        preferredSegment = tci < fci ? conn.To.Segment : conn.From.Segment;
                    if (segment == preferredSegment)
                    {
                        var a = conn.From.Dummy.WorldMatrix.Translation;
                        var b = conn.To.Dummy.WorldMatrix.Translation;
                        if (MyAPIGateway.Session.Camera.IsInFrustum(ref a) ||
                            MyAPIGateway.Session.Camera.IsInFrustum(ref b))
                            MySimpleObjectDraw.DrawLine(a, b, LaserMaterial, ref c, width);
                    }
                }
            }
        }

        public static readonly MyStringId LaserMaterial = MyStringId.GetOrCompute("WeaponLaser");

        #endregion

        #region Debugging

        public void DumpData()
        {
            Logger.Debug("-- begin network dump --");
            Logger.Debug($"Segment count {_network.Segments.Count}");
            foreach (var segment in _network.Segments)
            {
                Logger.Debug($"segment {segment.GetHashCode():X8}:");
                var f = segment.Forwards ? "F" : "";
                var r = segment.Backwards ? "R" : "";
                Logger.Debug(
                    $"  Path ({segment.Path.Count} {f}{r}): {string.Join(", ", segment.Path.Select(x => x.Dummy))}");
                if (segment.Connections.Count > 0)
                {
                    Logger.Debug(
                        $"  Connections ({segment.Connections.Count}): {string.Join(", ", segment.Connections.Select(x => (x.From.Segment != segment ? x.From : x.To).Segment.GetHashCode().ToString("X8")))}");
                    Logger.Debug(
                        $"  Connections (explicit): {string.Join(", ", segment.Connections.Select(x => x.From.Dummy + (x.Bidirectional ? " with " : " to ") + x.To.Dummy))}");
                }
            }

            Logger.Debug("-- end network dump --");
        }

        public void DebugDraw()
        {
            for (var i = 0; i < _network.Segments.Count; i++)
            {
                Segment segment = _network.Segments.GetInternalArray()[i];
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
                            _network.Segments.IndexOf(conn.To.Segment));
                        DrawArrow(conn.From.Dummy.WorldMatrix.Translation, conn.To.Dummy.WorldMatrix.Translation,
                            c, c2, true, conn.Bidirectional);
                    }
            }

            _detectors.DebugDraw();
        }

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
                    0.01f, null, LaserMaterial);
                return;
            }

            var a = Vector3D.Cross(dir, MyAPIGateway.Session.Camera.Position - ((from + to) / 2));
            a.Normalize();


            var len = Math.Min(lineLen / 10, 0.2f);

            var ds0 = to + Vector3D.Normalize(dir + a) * len;
            var ds1 = to + Vector3D.Normalize(dir - a) * len;

            var dt0 = from + Vector3D.Normalize(-dir + a) * len;
            var dt1 = from + Vector3D.Normalize(-dir - a) * len;


            MySimpleObjectDraw.DrawLine(ds0, dt0, LaserMaterial, ref fromColor, 0.05f);
            MySimpleObjectDraw.DrawLine(ds1, dt1, LaserMaterial, ref toColor, 0.05f);

            if (forwards)
            {
                MySimpleObjectDraw.DrawLine(to, ds0, LaserMaterial, ref toColor, 0.05f);
                MySimpleObjectDraw.DrawLine(to, ds1, LaserMaterial, ref toColor, 0.05f);
            }

            if (backwards)
            {
                MySimpleObjectDraw.DrawLine(from, dt0, LaserMaterial, ref fromColor, 0.05f);
                MySimpleObjectDraw.DrawLine(from, dt1, LaserMaterial, ref fromColor, 0.05f);
            }
        }

        public override string ComponentTypeDebugString => nameof(NetworkController);

        #endregion
    }
}