﻿using System.Collections.Generic;
using System.Xml.Serialization;
using VRageMath;

namespace Equinox.EnergyWeapons.Definition.Beam
{
    public class Path : Component
    {
        /// <summary>
        /// Dummies that are connected with this path.
        /// Laser power is accepted along <see cref="Vector3.Forward"/> of the first entry,
        /// and emitted along <see cref="Vector3.Forward"/> of the last entry.
        /// </summary>
        [XmlElement("Dummy", typeof(string))]
        public string[] Dummies { get; set; }

        [XmlIgnore]
        public override IEnumerable<string> Outputs
        {
            get
            {
                if (Dummies == null)
                    yield break;
                if (Dummies.Length > 1)
                    yield return Dummies[Dummies.Length - 1];
                if (Dummies.Length > 0)
                    yield return Dummies[0];
            }
        }

        [XmlIgnore]
        public override IEnumerable<string> Inputs
        {
            get
            {
                if (Dummies == null)
                    yield break;
                if (Dummies.Length > 0)
                    yield return Dummies[0];
                if (Dummies.Length > 1)
                    yield return Dummies[Dummies.Length - 1];
            }
        }

        [XmlIgnore]
        public override IEnumerable<string> Internal
        {
            get
            {
                if (Dummies != null)
                {
                    for (var i = 1; i < Dummies.Length - 1; i++)
                        yield return Dummies[i];
                }
            }
        }
    }
}