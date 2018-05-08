using System.Linq;
using Equinox.EnergyWeapons.Misc;
using Equinox.EnergyWeapons.Session;
using Equinox.Utils.Logging;
using Sandbox.Definitions;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Equinox.EnergyWeapons.Components
{
    public class AmmoGeneratorComponent : MyGameLogicComponent
    {
        public override string ComponentTypeDebugString => nameof(AmmoGeneratorComponent);

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

            NeedsUpdate = MyEntityUpdateEnum.EACH_10TH_FRAME;
        }

        private static readonly MyFixedPoint _addAmount = 50;
        private static readonly MyFixedPoint _thresholdAmount = 25;

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
                    if (inv.GetItemAmount(mag.GetObjectId(), mag.Flags) > _thresholdAmount)
                        continue;
                    inv.AddItems(_addAmount, mag);
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
    }
}