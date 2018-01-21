using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equinox.EnergyWeapons.Definition.Beam;
using Equinox.EnergyWeapons.Definition.Weapon;
using Equinox.EnergyWeapons.Misc;
using Equinox.Utils.Logging;
using Sandbox.Common.ObjectBuilders;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Equinox.EnergyWeapons.Definition
{
    public class DefinitionManager
    {
        private readonly Dictionary<MyDefinitionId, EnergyWeaponDefinition> _energyDefs =
            new Dictionary<MyDefinitionId, EnergyWeaponDefinition>(MyDefinitionId.Comparer);

        private readonly Dictionary<MyDefinitionId, Block> _beamDefs =
            new Dictionary<MyDefinitionId, Block>(MyDefinitionId.Comparer);

        public EnergyWeaponDefinition EnergyOf(MyDefinitionId id)
        {
            return _energyDefs.GetValueOrDefault(id);
        }

        private static T GetDef<T>(IDictionary<MyDefinitionId, T> dict, object e) where T : class
        {
            T res;
            var entity = e as IMyEntity;
            if (entity != null)
            {
                var weapon = WeaponShortcuts.GetWeaponDefinition(entity);
                if (weapon != null && dict.TryGetValue(weapon.Id, out res))
                    return res;
            }
            var block = e as IMyCubeBlock;
            if (block != null && dict.TryGetValue(block.BlockDefinition, out res))
                return res;
            var slim = e as IMySlimBlock;
            if (slim != null && dict.TryGetValue(slim.BlockDefinition.Id, out res))
                return res;
            return null;
        }

        public EnergyWeaponDefinition EnergyOf(object e)
        {
            return GetDef(_energyDefs, e);
        }

        public Block BeamOf(MyDefinitionId id)
        {
            return _beamDefs.GetValueOrDefault(id);
        }

        public Block BeamOf(object e)
        {
            return GetDef(_beamDefs, e);
        }

        public void Add(DefinitionSet set)
        {
            if (set?.Definitions == null)
                return;
            foreach (var d in set.Definitions)
                if (d != null)
                    _energyDefs.Add(d.Id, d);
            foreach (var d in set.Beams)
                if (d!=null)
                    _beamDefs.Add(d.Id, d);
        }
    }
}