 
 

using System.Xml.Serialization;
using System.Collections.Generic;
namespace Equinox.Utils.Components
{
    public class GameLogicTypes
    {
        public static readonly IReadOnlyList<string> Types = new[] {
            typeof(Equinox.EnergyWeapons.Components.AmmoGeneratorComponent).FullName, 
            typeof(Equinox.EnergyWeapons.Components.Thermal.ThermalPhysicsComponent).FullName, 
        };
    }
}