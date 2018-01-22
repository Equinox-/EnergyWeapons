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

        /// <summary>
        /// Maximum distance the laser can travel
        /// </summary>
        public double MaxLazeDistance { get; set; } = 1e4f;

        /// <summary>
        /// Efficiency of converting beam network energy into real energy.
        /// </summary>
        public float Efficiency { get; set; } = 1;
        
        public override IEnumerable<string> Outputs => Enumerable.Empty<string>();

        public override IEnumerable<string> Inputs => new[] {Dummy};

        public override IEnumerable<string> Internal => Enumerable.Empty<string>();


        private LossyDummy[] _lossy;

        public override IReadOnlyList<LossyDummy> LossyDummies =>
            _lossy ?? (_lossy = new[] {new LossyDummy(Dummy,1- Efficiency)});

        /// <summary>
        /// Particle effect spawned at impact point: Name
        /// </summary>
        public string FxImpactName { get; set; } = "WelderContactPoint";

        /// <summary>
        /// Particle effect spawned at impact point: Birth rate
        /// </summary>
        public int FxImpactBirthRate { get; set; } = 2;

        /// <summary>
        /// Particle effect spawned at impact point: Scale
        /// </summary>
        public float FxImpactScale { get; set; } = 3f;

        /// <summary>
        /// Particle effect spawned at impact point: Maximum count
        /// </summary>
        public int FxImpactMaxCount { get; set; } = 25;
    }
}