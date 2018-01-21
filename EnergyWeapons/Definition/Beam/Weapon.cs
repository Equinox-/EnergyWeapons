using System.Collections.Generic;
using System.Linq;
using VRageMath;

namespace Equinox.EnergyWeapons.Definition.Beam
{
    public class Weapon : LossyComponent
    {
        /// <summary>
        /// Dummy this emitter recieves a beam on.  Output direction is <see cref="Vector3.Forward"/>
        /// </summary>
        public string Dummy { get; set; }

        /// <summary>
        /// Multiplier applied to the power output of this weapon.
        /// </summary>
        /// <remarks>
        /// Clasically setting this to 1 would is accurate, however because of damage
        /// caused by the vaporizing material exploding outwards we can set it higher
        /// and still be considered "realistic"
        /// </remarks>
        public float WeaponDamageMultiplier { get; set; } = 10;

        public override IEnumerable<string> Outputs => Enumerable.Empty<string>();

        public override IEnumerable<string> Inputs => new[] {Dummy};

        public override IEnumerable<string> Internal => Enumerable.Empty<string>();
    }
}