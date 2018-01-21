
using System.Collections.Generic;
using System.Xml.Serialization;
using Equinox.EnergyWeapons.Definition.Beam;
using Equinox.EnergyWeapons.Definition.Weapon;

namespace Equinox.EnergyWeapons.Definition
{
    public class DefinitionSet
    {
        [XmlElement(nameof(LaserWeaponDefinition), typeof(LaserWeaponDefinition))]
        public List<EnergyWeaponDefinition> Definitions = new List<EnergyWeaponDefinition>();

        [XmlElement(nameof(Block), typeof(Block))]
        public List<Block> Beams = new List<Block>();
    }
}
