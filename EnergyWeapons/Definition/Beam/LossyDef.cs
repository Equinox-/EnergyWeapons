﻿using System.Collections.Generic;
using VRageMath;

namespace Equinox.EnergyWeapons.Definition.Beam
{
    public abstract class Lossy : Component
    {
        /// <summary>
        /// Heat dissipation in KW/K.  If null compute from material properties and block dimensions.
        /// </summary>
        public float? CoolingPower { get; set; }

        public struct LossyDummy
        {
            public readonly string Dummy;

            /// <summary>
            /// Heat loss for this dummy.  HeatLoss*CurrentEnergyThroughput is generated as heat.
            /// </summary>
            public readonly float HeatLoss;

            public LossyDummy(string dummy, float heatLoss)
            {
                Dummy = dummy;
                HeatLoss = MathHelper.Clamp(0, 1, heatLoss);
            }
        }

        public abstract IReadOnlyList<LossyDummy> LossyDummies { get; }
    }
}