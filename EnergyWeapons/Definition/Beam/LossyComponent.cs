using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Equinox.EnergyWeapons.Definition.Beam
{
    public abstract class LossyComponent : Component
    {
        /// <summary>
        /// Efficiency of this beam component.  (1 - ThisValue) * TotalInputPower is generated heat.
        /// </summary>
        public float Efficiency { get; set; } = 1;

        /// <summary>
        /// Heat dissipation in MW/K.  If null compute from material properties and block dimensions.
        /// </summary>
        public float? CoolingPower { get; set; }
    }
}
