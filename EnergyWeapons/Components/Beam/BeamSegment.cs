using System;
using System.Text;
using Equinox.EnergyWeapons.Components.Network;
using Equinox.EnergyWeapons.Session;
using Equinox.Utils.Logging;
using Equinox.Utils.Misc;
using VRageMath;

namespace Equinox.EnergyWeapons.Components.Beam
{
    public class Segment : Network.Segment<Segment, BeamConnectionData>
    {
        public struct BeamSegmentData
        {
            /// <summary>
            /// Total energy stored in kJ
            /// </summary>
            public readonly float Energy;

            /// <summary>
            /// Color of the beam, multiplied by total energy in kJ
            /// </summary>
            public readonly Vector4 WeightedColor;

            /// <summary>
            /// Color of the beam
            /// </summary>
            public Vector4 Color => WeightedColor / Math.Max(Energy, 1e-6f);

            /// <summary>
            /// Output of the beam, in kW
            /// </summary>
            public readonly float Output;

            /// <summary>
            /// Output color of the beam, in kW*color
            /// </summary>
            public readonly Vector4 WeightedOutputColor;

            /// <summary>
            /// Output color of the beam, in pure color
            /// </summary>
            public Vector4 OutputColor => WeightedOutputColor / Math.Max(Output, 1e-6f);

            public BeamSegmentData(float energy, Vector4 weightedColor, float output, Vector4 weightedOutputColor)
            {
                Energy = energy;
                WeightedColor = weightedColor;
                Output = output;
                WeightedOutputColor = weightedOutputColor;
            }

            public static BeamSegmentData operator +(BeamSegmentData lhs, BeamSegmentData rhs)
            {
                return new BeamSegmentData(lhs.Energy + rhs.Energy, lhs.WeightedColor + rhs.WeightedColor,
                    lhs.Output + rhs.Output, lhs.WeightedOutputColor + rhs.WeightedOutputColor);
            }

            public static BeamSegmentData operator *(BeamSegmentData lhs, float s)
            {
                return new BeamSegmentData(lhs.Energy * s, lhs.WeightedColor * s,
                    lhs.Output * s, lhs.WeightedOutputColor * s);
            }
        }

        public Segment(BeamController network, params DummyData<Segment, BeamConnectionData>[] path) : base(network,
            path)
        {
        }

        public BeamSegmentData Current { get; private set; }

        public BeamSegmentData CurrentEma { get; private set; }

        private BeamSegmentData _next;
        private BeamSegmentData _nextInjected;

        public override void Predict(float dt)
        {
            _next = new BeamSegmentData(Current.Energy, Current.WeightedColor, 0, Vector4.Zero);
            base.Predict(dt);
        }

        protected override void PredictConnection(Connection<Segment, BeamConnectionData> con, float dt)
        {
            var opponent = con.From.Segment == this ? con.To.Segment : con.From.Segment;
            if (opponent == this)
                return;
            var dE = (opponent.Current.Energy - Current.Energy) / 2f;
            if (!float.IsPositiveInfinity(con.Data.MaxThroughput))
                dE = Math.Sign(dE) * Math.Min(Math.Abs(dE), con.Data.MaxThroughput * dt);
            if (con.From.Segment == this && !con.Data.Bidirectional)
                dE = Math.Min(0, dE);
            else if (con.To.Segment == this && !con.Data.Bidirectional)
                dE = Math.Max(0, dE);

            var sourceColor = dE > 0 ? opponent.Current.Color : Current.Color;
            Vector4 mixColor = sourceColor;
            mixColor *= con.Data.Filter;
            if (Math.Abs(mixColor.X) + Math.Abs(mixColor.Y) + Math.Abs(mixColor.Y) <= 1e-1f)
                mixColor = new Vector4(1e-1f, 1e-1f, 1e-1f, 1) / 3;

            // always subtract the source energy color, add the mixed color.
            var dColor = dE * (dE > 0 ? mixColor : sourceColor);
            var output = Math.Max(-dE, 0);
            _next += new BeamSegmentData(dE, dColor, output, output * mixColor);
        }

        public void Inject(float energy, Vector4 color)
        {
            lock (this)
            {
                var output = Math.Max(0, -energy);
                _nextInjected += new BeamSegmentData(energy, energy * color, output, output * color);
            }
        }

        public override void Commit(float dt)
        {
            lock (this)
            {
                _next += _nextInjected;
                _nextInjected = new BeamSegmentData(0, Vector4.Zero, 0, Vector4.Zero);
            }

            var output = _next.Output / Math.Max(1e-6f, dt);
            var outputColor = _next.WeightedOutputColor / Math.Max(1e-6f, dt);
            Current = new BeamSegmentData(_next.Energy,
                Vector4.Clamp(_next.WeightedColor, Vector4.Zero, _next.Energy * Vector4.One), output, outputColor);
            CurrentEma = CurrentEma * 0.95f + Current * 0.05f;

            RaiseStateChanged();
        }

        public override void Debug(StringBuilder sb)
        {
            base.Debug(sb);
            sb.Append(" Power=").Append(Current.Energy.ToString("F2")).Append("kJ");
            sb.Append(" Output=").Append(CurrentEma.Output.ToString("F2")).Append("kW");
            var cc = CurrentEma.OutputColor;
            sb.Append(" OutColor=").AppendFormat("[{0:F2} {1:F2} {2:F2} {3:F2}]", cc.X, cc.Y,
                cc.Z, cc.W);
        }
    }
}