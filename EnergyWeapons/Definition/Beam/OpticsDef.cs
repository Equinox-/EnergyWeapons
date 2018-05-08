using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using Equinox.Utils.Misc;
using VRageMath;

namespace Equinox.EnergyWeapons.Definition.Beam
{
    public class Optics : Component
    {
        public class OutgoingBeam
        {
            /// <summary>
            /// Dummy the beam exits the optical system on.
            /// </summary>
            [XmlAttribute]
            public string Dummy { get; set; }

            /// <summary>
            /// Color mask applied to the output beam.  If <see cref="Vector4.One"/> the color will be unchanged.
            /// </summary>
            [XmlIgnore]
            public Vector4 Color { get; set; }= Vector4.One;

            [XmlElement(nameof(Color))]
            public SerializableVector4 ColorSerial
            {
                get { return Color; }
                set { Color = value; }
            }


            /// <summary>
            /// Maximum throughput of this connection, or infinite
            /// </summary>
            [DefaultValue(float.PositiveInfinity)]
            public float MaxThroughput { get; set; } = float.PositiveInfinity;
        }

        /// <summary>
        /// Dummies that a beam can enter along.
        /// </summary>
        [XmlElement("Incoming", typeof(string))]
        public string[] IncomingBeams { get; set; }

        /// <summary>
        /// All outgoing beams.
        /// </summary>
        [XmlElement("Outgoing", typeof(OutgoingBeam))]
        public OutgoingBeam[] OutgoingBeams { get; set; }

        /// <summary>
        /// Dummy where all the beams intersect.
        /// </summary>
        public string IntersectionPoint { get; set; }

        [XmlIgnore]
        public override IEnumerable<string> Outputs => OutgoingBeams?.Select(x => x.Dummy) ?? Enumerable.Empty<string>();

        [XmlIgnore]
        public override IEnumerable<string> Inputs => IncomingBeams ?? Enumerable.Empty<string>();

        [XmlIgnore]
        public override IEnumerable<string> Internal => new[] {IntersectionPoint};
    }
}