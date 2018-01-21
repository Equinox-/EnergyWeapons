﻿using System.Collections.Generic;
using System.Linq;
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
            public string Dummy { get; set; }

            /// <summary>
            /// Color mask applied to the output beam.  If <see cref="Vector4.One"/> the color will be unchanged.
            /// </summary>
            public Vector4 Color { get; set; }= Vector4.One;

            /// <summary>
            /// Multiplier applied to total input power to determine output power.
            /// For realism the sum of all power factors should be <see cref="Component.Efficiency"/> so no power is lost (or gained)
            /// </summary>
            public float PowerFactor { get; set; }
        }

        /// <summary>
        /// Dummies that a beam can enter along.
        /// </summary>
        public string[] IncomingBeams { get; set; }

        /// <summary>
        /// All outgoing beams.
        /// </summary>
        public OutgoingBeam[] OutgoingBeams { get; set; }

        /// <summary>
        /// Dummy where all the beams intersect.
        /// </summary>
        public string IntersectionPoint { get; set; }

        public override IEnumerable<string> Outputs
        {
            get { return OutgoingBeams?.Select(x => x.Dummy) ?? Enumerable.Empty<string>(); }
        }

        public override IEnumerable<string> Inputs
        {
            get { return IncomingBeams ?? Enumerable.Empty<string>(); }
        }

        public override IEnumerable<string> Internal
        {
            get { return new[] {IntersectionPoint}; }
        }
    }
}