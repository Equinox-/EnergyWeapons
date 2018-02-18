using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.ObjectBuilders.Definitions;

namespace Equinox.EnergyWeapons.Misc
{
    public static class ConstantDefs
    {
        public static readonly MyDefinitionId ElectricityId = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Electricity");
    }
}
