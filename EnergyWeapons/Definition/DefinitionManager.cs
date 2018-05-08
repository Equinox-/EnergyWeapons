using System.Collections.Generic;
using Equinox.EnergyWeapons.Definition.Beam;
using Equinox.EnergyWeapons.Misc;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace Equinox.EnergyWeapons.Definition
{
    public class DefinitionManager
    {
        private readonly Dictionary<MyDefinitionId, Block> _beamDefs =
            new Dictionary<MyDefinitionId, Block>(MyDefinitionId.Comparer);

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
            if (set.Beams != null)
                foreach (var d in set.Beams)
                    if (d != null)
                        _beamDefs.Add(d.Id, d);
        }
    }
}