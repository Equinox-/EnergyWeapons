using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Equinox.EnergyWeapons.Components.Thermal
{
    public interface IThermalPhysicsProvider
    {
        ThermalPhysicsSlim Physics { get; }
    }
}
