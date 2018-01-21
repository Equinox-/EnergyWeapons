using System.Collections.Generic;
using System.Linq;
using VRageMath;

namespace Equinox.EnergyWeapons.Definition.Beam
{
    public class Emitter : LossyComponent
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
        public Vector4 ColorMin { get; set; }

        /// <summary>
        /// Color at maximum power.
        /// </summary>
        public Vector4 ColorMax { get; set; }

        /// <summary>
        /// The laser must drop below this temperature after overheating before it will start firing again.
        /// If null it's assumed to be <see cref="ThermalFuseMax"/>
        /// </summary>
        public float? ThermalFuseMin { get; set; } = 1000;

        /// <summary>
        /// Temperature in K at which the laser automatically stops firing, or null if this isn't supported.
        /// </summary>
        public float? ThermalFuseMax { get; set; } = 1500;

        public override IEnumerable<string> Outputs
        {
            get { return new[] {Dummy}; }
        }

        public override IEnumerable<string> Inputs
        {
            get { return Enumerable.Empty<string>(); }
        }

        public override IEnumerable<string> Internal
        {
            get { return Enumerable.Empty<string>(); }
        }
    }
}
