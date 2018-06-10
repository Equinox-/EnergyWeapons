using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using VRage.Utils;
using VRageMath;

namespace Equinox.EnergyWeapons.Definition.Beam
{
    public class Weapon : Lossy
    {
        /// <summary>
        /// Dummy this emitter recieves a beam on.  Output direction is <see cref="Vector3.Forward"/>
        /// </summary>
        public string Dummy { get; set; }

        /// <summary>
        /// Maximum distance the laser can travel
        /// </summary>
        public double MaxLazeDistance { get; set; } = 1e4f;

        /// <summary>
        /// Efficiency of converting beam network energy into real energy.
        /// </summary>
        [DefaultValue(1f)]
        public float Efficiency { get; set; } = 1;
        
        #region Impact Particles
        /// <summary>
        /// Particle effect spawned at impact point: Name
        /// </summary>
        [DefaultValue("WelderContactPoint")]
        public string FxImpactName { get; set; } = "WelderContactPoint";

        /// <summary>
        /// Particle effect spawned at impact point: Birth rate
        /// </summary>
        [DefaultValue(2)]
        public int FxImpactBirthRate { get; set; } = 2;

        /// <summary>
        /// Particle effect spawned at impact point: Scale
        /// </summary>
        [DefaultValue(3f)]
        public float FxImpactScale { get; set; } = 3f;

        /// <summary>
        /// Particle effect spawned at impact point: Maximum count
        /// </summary>
        [DefaultValue(25)]
        public int FxImpactMaxCount { get; set; } = 25;
        #endregion

        #region Damage Controls
        /// <summary>
        /// Multiplier applied to the power output of this weapon.
        /// </summary>
        /// <remarks>
        /// Clasically setting this to 1 would is accurate, however because of damage
        /// caused by the vaporizing material exploding outwards we can set it higher
        /// and still be considered "realistic"
        /// </remarks>
        [DefaultValue(10f)]
        public float WeaponDamageMultiplier { get; set; } = 10;
        
        /// <summary>
        /// Multiplier on voxel damage.
        /// </summary>
        public float VoxelDamageMultiplier { get; set; } = 1f;
        
        /// <summary>
        /// Amount of damage applied directly (without heat mechanics)
        /// </summary>
        public float DirectDamageFactor { get; set; } = 0f;

        /// <summary>
        /// Maximum distance, in meters, of raycast prediction.  Higher = damages faster, but with possible mistakes.
        /// </summary>
        public float RaycastPrediction { get; set; } = 30f;

        /// <summary>
        /// Direct damage type ID.
        /// </summary>
        [XmlIgnore]
        public MyStringHash DamageType { get; set; } = MyStringHash.GetOrCompute("Laser");

        /// <summary>
        /// Direct damage type ID.
        /// </summary>
        [DefaultValue("Laser")]
        public string DamageTypeSerial
        {
            get { return DamageType.String; }
            set { DamageType = MyStringHash.GetOrCompute(value); }
        }
        #endregion

        #region Capacitor Controls
        /// <summary>
        /// The amount of the internal capacitor that can be discharged per tick.
        /// </summary>
        /// <remarks>
        /// Keep this value below 1/20th.
        /// </remarks>
        public float CapacitorDischargePerTick { get; set; } = 1 / 30f;

        /// <summary>
        /// Maximum charge of the capacitor, in kJ
        /// </summary>
        /// <remarks>
        /// If this value is less than or equal to zero the internal capacitor will not charge when the weapon isn't shooting.
        /// </remarks>
        public float CapacitorMaxCharge { get; set; } = 0f;

        /// <summary>
        /// The minimum fill ratio of the capacitor this weapon will fire at.
        /// </summary>
        /// <remarks>
        /// Will not take affect if <see cref="CapacitorMaxCharge"/> is zero.
        /// </remarks>
        public float CapacitorMinRatioToFire { get; set; } = 0.9f;

        /// <summary>
        /// Once the weapon has begun firing with <see cref="CapacitorMinRatioToFire"/>
        /// it can fire until the capacitor drops below this.  Should be more than twice
        /// <see cref="CapacitorDischargePerTick"/>
        /// </summary>
        /// <remarks>
        /// Will not take affect if <see cref="CapacitorMaxCharge"/> is zero.
        /// </remarks>
        public float CapacitorMinRatioToFireHysteresis { get; set; } = 0.1f;
        #endregion
        
        #region Internals
        [XmlIgnore]
        public override IEnumerable<string> Outputs => Enumerable.Empty<string>();

        [XmlIgnore]
        public override IEnumerable<string> Inputs => new[] {Dummy};

        [XmlIgnore]
        public override IEnumerable<string> Internal => Enumerable.Empty<string>();

        private LossyDummy[] _lossy;

        [XmlIgnore]
        public override IReadOnlyList<LossyDummy> LossyDummies =>
            _lossy ?? (_lossy = new[] {new LossyDummy(Dummy, 1 - Efficiency)});

        #endregion
    }
}