
using System.Collections.Generic;
using System.Xml.Serialization;
using Equinox.EnergyWeapons.Definition.Beam;

namespace Equinox.EnergyWeapons.Definition
{
    public class DefinitionSet
    {
        [XmlElement(nameof(Block), typeof(Block))]
        public List<Block> Beams = new List<Block>();
    }
}
