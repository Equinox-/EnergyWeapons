using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Weapons;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Equinox.EnergyWeapons.Misc
{
    public static class WeaponShortcuts
    {
        public static MyWeaponDefinition GetWeaponDefinition(IMyEntity ent)
        {
            try
            {
                var block = ent as IMyCubeBlock;
                if (block != null && ent is IMyGunBaseUser)
                {
                    var def = MyDefinitionManager.Static.GetCubeBlockDefinition(block.BlockDefinition);
                    var wep = def as MyWeaponBlockDefinition;
                    if (wep != null)
                        return MyDefinitionManager.Static.GetWeaponDefinition(wep.WeaponDefinitionId);
                    return MyDefinitionManager.Static.GetWeaponDefinition(
                        GetBackwardCompatibleDefinitionId(def.Id.TypeId));
                }

                var gun = ent as IMyHandheldGunObject<MyToolBase>;
                if (gun != null)
                {
                    var def = gun.PhysicalItemDefinition;
                    var pdef = def as MyWeaponItemDefinition;
                    if (pdef != null)
                        return MyDefinitionManager.Static.GetWeaponDefinition(pdef.WeaponDefinitionId);
                }
            }
            catch
            {
                // ignored
            }
            return null;
        }

        private static MyDefinitionId GetBackwardCompatibleDefinitionId(MyObjectBuilderType typeId)
        {
            if (typeId == typeof(MyObjectBuilder_LargeGatlingTurret))
            {
                return new MyDefinitionId(typeof(MyObjectBuilder_WeaponDefinition), "LargeGatlingTurret");
            }
            if (typeId == typeof(MyObjectBuilder_LargeMissileTurret))
            {
                return new MyDefinitionId(typeof(MyObjectBuilder_WeaponDefinition), "LargeMissileTurret");
            }
            if (typeId == typeof(MyObjectBuilder_InteriorTurret))
            {
                return new MyDefinitionId(typeof(MyObjectBuilder_WeaponDefinition), "LargeInteriorTurret");
            }
            if (typeId == typeof(MyObjectBuilder_SmallMissileLauncher) || typeId == typeof(MyObjectBuilder_SmallMissileLauncherReload))
            {
                return new MyDefinitionId(typeof(MyObjectBuilder_WeaponDefinition), "SmallMissileLauncher");
            }
            if (typeId == typeof(MyObjectBuilder_SmallGatlingGun))
            {
                return new MyDefinitionId(typeof(MyObjectBuilder_WeaponDefinition), "GatlingGun");
            }
            return default(MyDefinitionId);
        }
    }
}
