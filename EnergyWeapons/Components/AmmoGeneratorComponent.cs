using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equinox.EnergyWeapons.Misc;
using Equinox.Utils.Logging;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Equinox.EnergyWeapons.Components
{
    public class AmmoGeneratorComponent : MyGameLogicComponent, ICoreRefComponent
    {
        public override string ComponentTypeDebugString
        {
            get { return nameof(AmmoGeneratorComponent); }
        }

        private ILogging _logger;
        private EnergyWeaponsCore _core;
        private MyWeaponDefinition _weapon;
        private MyObjectBuilder_PhysicalObject[] _weaponAmmoMags;

        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            _weapon = WeaponShortcuts.GetWeaponDefinition(Entity);
            if (_weapon != null && _weapon.HasAmmoMagazines())
            {
                _weaponAmmoMags = new MyObjectBuilder_PhysicalObject[_weapon.AmmoMagazinesId.Length];
                for (var i = 0; i < _weaponAmmoMags.Length; i++)
                    _weaponAmmoMags[i] =
                        new MyObjectBuilder_AmmoMagazine() {SubtypeName = _weapon.AmmoMagazinesId[i].SubtypeName};
            }

            CheckUpdate();
        }

        public void OnBeforeRemovedFromCore()
        {
            _core = null;
            _logger = null;
            CheckUpdate();
        }

        public override void UpdateBeforeSimulation10()
        {
            var user = Entity as IMyGunBaseUser;
            var inv = user?.AmmoInventory;
            if (inv == null || _weaponAmmoMags == null)
                return;
            var grid = Entity as IMyCubeBlock;
            if (grid == null || grid.IsWorking)
            {
                foreach (var mag in _weaponAmmoMags)
                {
                    if (inv.GetItemAmount(mag.GetObjectId(), mag.Flags) > 0)
                        continue;
                    inv.AddItems(1, mag);
                }
            }
            else
            {
                foreach (var mag in _weaponAmmoMags)
                {
                    var amount = inv.GetItemAmount(mag.GetObjectId(), mag.Flags);
                    if (amount > 0)
                        inv.RemoveItemsOfType(amount, mag.GetObjectId());
                }

            }
        }

        public void OnAddedToCore(EnergyWeaponsCore core)
        {
            if (Entity == null)
                return;
            _core = core;
            _logger = core.Logger.CreateProxy(GetType());
            CheckUpdate();
        }

        private void CheckUpdate()
        {
            var def = _core?.Definitions?.EnergyOf(Entity);
            if (def == null || !def.GenerateAmmo)
                NeedsUpdate = MyEntityUpdateEnum.NONE;
            else
                NeedsUpdate = MyEntityUpdateEnum.EACH_10TH_FRAME;
        }
    }
}