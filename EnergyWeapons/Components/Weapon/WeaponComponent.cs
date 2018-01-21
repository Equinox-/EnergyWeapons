using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equinox.EnergyWeapons.Definition;
using Equinox.EnergyWeapons.Definition.Weapon;
using Equinox.EnergyWeapons.Misc;
using Equinox.EnergyWeapons.Physics;
using Equinox.Utils.Logging;
using Sandbox.Game.Entities;
using Sandbox.Game.Weapons;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.Definitions;

namespace Equinox.EnergyWeapons.Components.Weapon
{
    public abstract class WeaponComponent<TDef> : MyGameLogicComponent, ICoreRefComponent
        where TDef : EnergyWeaponDefinition
    {
        public static readonly MyDefinitionId ElectricityId =
            new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Electricity");

        public override string ComponentTypeDebugString => GetType().Name;

        protected ILogging Logger { get; private set; }

        protected EnergyWeaponsCore Core { get; private set; }

        protected TDef Definition { get; private set; }

        protected MaterialProperties MaterialProperties { get; private set; }

        protected event Action<TDef, TDef> DefinitionChanged;

        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            CheckDef();
        }

        public void OnAddedToCore(EnergyWeaponsCore core)
        {
            Core = core;
            Logger = core?.Logger.CreateProxy(GetType());
            CheckDef();
        }

        public void OnBeforeRemovedFromCore()
        {
            Core = null;
            Logger = null;
            CheckDef();
        }

        private void CheckDef()
        {
            Definition = null;
            if (Entity != null && Core != null)
            {
                var block = Entity as IMyCubeBlock;
                var gun = Entity as IMyGunObject<MyGunBase>;
                if (block != null)
                    MaterialProperties = Core.Materials.PropertiesOf(block.BlockDefinition);
                if (gun != null)
                    MaterialProperties = Core.Materials.PropertiesOf(gun.DefinitionId);
                if (MaterialProperties == null)
                    MaterialProperties = MaterialPropertyDatabase.IronMaterial;

                var old = Definition;
                Definition = Core.Definitions.EnergyOf(Entity) as TDef;
                if (old != Definition)
                    DefinitionChanged?.Invoke(old, Definition);
            }
        }
    }
}