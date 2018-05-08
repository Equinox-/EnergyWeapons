using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using Equinox.Utils.Misc;
using VRageMath;

namespace Equinox.EnergyWeapons.Definition.Beam
{
    public class Emitter : Lossy
    {
        /// <summary>
        /// Dummy this emitter outputs on.
        /// </summary>
        public string Dummy { get; set; }

        /// <summary>
        /// Maximum power this emitter can support, in kW.
        /// </summary>
        public float MaxPowerOutput { get; set; }

        /// <summary>
        /// Color at minimum power.
        /// </summary>
        [XmlIgnore]
        public Vector4 ColorMin { get; set; }

        [XmlElement(nameof(ColorMin))]
        public SerializableVector4 ColorMinSerial
        {
            get { return ColorMin; }
            set { ColorMin = value; }
        }

        /// <summary>
        /// Color at maximum power.
        /// </summary>
        [XmlIgnore]
        public Vector4 ColorMax { get; set; }

        [XmlElement(nameof(ColorMax))]
        public SerializableVector4 ColorMaxSerial
        {
            get { return ColorMax; }
            set { ColorMax = value; }
        }

        /// <summary>
        /// The laser must drop below this temperature after overheating before it will start firing again.
        /// If null it's assumed to be <see cref="ThermalFuseMax"/>
        /// </summary>
        public float? ThermalFuseMin { get; set; } = 1000;

        /// <summary>
        /// Temperature in K at which the laser automatically stops firing, or null if this isn't supported.
        /// </summary>
        public float? ThermalFuseMax { get; set; } = 1500;

        /// <summary>
        /// Will this emitter automatically turn off if the beam isn't loaded.
        /// </summary>
        [DefaultValue(true)]
        public bool AutomaticTurnOff { get; set; } = true;

        /// <summary>
        /// Efficiency of this emitter
        /// </summary>
        [DefaultValue(1f)]
        public float Efficiency { get; set; } = 1f;

        [XmlIgnore]
        public override IEnumerable<string> Outputs => new[] {Dummy};

        [XmlIgnore]
        public override IEnumerable<string> Inputs => Enumerable.Empty<string>();

        [XmlIgnore]
        public override IEnumerable<string> Internal => Enumerable.Empty<string>();


        private LossyDummy[] _lossy;

        [XmlIgnore]
        public override IReadOnlyList<LossyDummy> LossyDummies => _lossy ?? (_lossy = new[] {new LossyDummy(Dummy, 1-Efficiency)});
    }
}
