using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equinox.EnergyWeapons.Components.Network;
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
            public readonly float Output, OutputEma;

            public BeamSegmentData(float energy, Vector4 weightedColor, float output, float outputEma)
            {
                Energy = energy;
                WeightedColor = weightedColor;
                Output = output;
                OutputEma = outputEma;
            }

            public static BeamSegmentData operator +(BeamSegmentData lhs, BeamSegmentData rhs)
            {
                return new BeamSegmentData(lhs.Energy + rhs.Energy, lhs.WeightedColor + rhs.WeightedColor,
                    lhs.Output + rhs.Output, lhs.OutputEma + rhs.OutputEma);
            }
        }

        public Segment(BeamController network, bool bidirectional,
            params DummyData<Segment, BeamConnectionData>[] path) : base(network, bidirectional, path)
        {
        }

        public BeamSegmentData Current { get; private set; }

        private BeamSegmentData _next;
        private BeamSegmentData _nextInjected;

        public override void Predict(float dt)
        {
            _next = new BeamSegmentData(Current.Energy, Current.WeightedColor, 0, 0);
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

            var dColor = dE * con.Data.Filter * (dE > 0 ? opponent.Current.Color : Current.Color);
            _next += new BeamSegmentData(dE, dColor, Math.Max(-dE, 0), 0);
        }

        public void Inject(float energy, Vector4 color)
        {
            lock (this)
            {
                _nextInjected += new BeamSegmentData(energy, energy * color, Math.Max(0, -energy), 0);
            }
        }

        public override void Commit(float dt)
        {
            lock (this)
            {
                _next += _nextInjected;
                _nextInjected = new BeamSegmentData(0, Vector4.Zero, 0, 0);
            }

            var output = _next.Output / Math.Max(1e-6f, dt);
            Current = new BeamSegmentData(_next.Energy, _next.WeightedColor, output, Current.OutputEma * 0.95f + output * 0.05f);

            RaiseStateChanged();
        }

        public override void Debug(StringBuilder sb)
        {
            base.Debug(sb);
            sb.Append(" Power=").Append(Current.Energy.ToString("F2")).Append("kJ");
            sb.Append(" Output=").Append(Current.OutputEma.ToString("F2")).Append("kW");
            var cc = Current.Color;
            sb.Append(" Color=").AppendFormat("[{0:F2} {1:F2} {2:F2} {3:F2}]", cc.X, cc.Y,
                cc.Z, cc.W);
        }
    }
}