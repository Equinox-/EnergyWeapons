using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equinox.EnergyWeapons.Components.Network;
using Equinox.Utils.Misc;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Input;
using VRage.Utils;
using VRageMath;

namespace Equinox.EnergyWeapons.Components.Beam
{
    public class BeamController : NetworkController<Segment, BeamConnectionData>
    {
        public BeamController(EnergyWeaponsCore core) : base(core)
        {
        }

        public override string ComponentTypeDebugString => nameof(BeamController);

        public override BeamConnectionData DetectorConnectionData => new BeamConnectionData(Vector4.One);
        public override BeamConnectionData DissolveableConnectionData => new BeamConnectionData(Vector4.One);

        public override Segment AllocateSegment(bool bidirectional, params DummyData<Segment, BeamConnectionData>[] path)
        {
            return new Segment(this, bidirectional, path);
        }
//
//        public override void PredictionBegin(Segment segment, float dt, ref Segment.BeamSegmentData next)
//        {
//            // copies
//            next = segment.Current;
//        }
//
//        public override void PredictionAddConnection(Segment segment, float dt,
//            Connection<Segment.BeamSegmentData, BeamConnectionData> con, ref Segment.BeamSegmentData next)
//        {
//            var opponent = con.From.Segment == segment ? con.To.Segment : con.From.Segment;
//            if (opponent == segment)
//                return;
//            var dE = (opponent.Current.Energy - segment.Current.Energy) / 2f;
//            if (!float.IsPositiveInfinity(con.Data.MaxThroughput))
//                dE = Math.Sign(dE) * Math.Min(Math.Abs(dE), con.Data.MaxThroughput * dt);
//            if (con.From.Segment == segment && !con.Bidirectional)
//                dE = Math.Min(0, dE);
//            else if (con.To.Segment == segment && !con.Bidirectional)
//                dE = Math.Max(0, dE);
//
//            var dColor = dE * con.Data.Filter * (dE > 0 ? opponent.CurrentColor : segment.CurrentColor);
//            next = new Segment.BeamSegmentData(next.Energy + dE, next.WeightedColor + dColor, next.Output + Math.Max(-dE, 0),
//                0);
//        }
//


        #region Render

        public static float BeamWidth(float power)
        {
            // at half the max power the width is 70%
            const float keyMaxPower = 1e6f; // 1 GW
            const float keyMaxWidth = 1f; // 1 m

            var powerFrac = MathHelper.Clamp(power / keyMaxPower, 0f, keyMaxWidth * keyMaxWidth);
            return (float)Math.Sqrt(powerFrac);
        }

        public static Vector4 BeamColor(Vector4 tint, float power)
        {
            tint.W = 1;
            return tint;
        }

        public override void Draw()
        {
            if (MyAPIGateway.Input.IsNewKeyPressed(MyKeys.OemQuotes))
                base.DumpData();
            ;
            for (var i = 0; i < Segments.Count; i++)
            {
                Segment segment = Segments.GetInternalArray()[i];
                if (segment == null)
                    continue;
                var state = segment.Current;
                if (state.Energy <= 0)
                    continue;
                var c = BeamColor(state.Color, state.OutputEma);
                var width = BeamWidth(state.OutputEma);

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
    }
}