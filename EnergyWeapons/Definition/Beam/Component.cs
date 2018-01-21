using System.Collections.Generic;

namespace Equinox.EnergyWeapons.Definition.Beam
{
    public abstract class Component
    {
        /// <summary>
        /// Dummies bound to an output
        /// </summary>
        public abstract IEnumerable<string> Outputs { get; }

        /// <summary>
        /// Dummies bound to an input
        /// </summary>
        public abstract IEnumerable<string> Inputs { get; }

        /// <summary>
        /// Internal dummies that shall not be used elsewhere
        /// </summary>
        public abstract IEnumerable<string> Internal { get; }
    }
}