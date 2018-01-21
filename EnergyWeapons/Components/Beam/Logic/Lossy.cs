using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equinox.EnergyWeapons.Definition.Beam;
using VRage.Game.ModAPI;

namespace Equinox.EnergyWeapons.Components.Beam.Logic
{
    public abstract class Lossy<T> : Component<T> where T:LossyComponent
    {
        protected Lossy(NetworkComponent block, T definition) : base(block, definition)
        {
        }

        public override void OnAddedToScene()
        {
            Block.OnUpgradeValuesChanged += UpgradeValuesChanged;
        }

        public override void OnRemovedFromScene()
        {
            Block.OnUpgradeValuesChanged -= UpgradeValuesChanged;
        }

        private void UpgradeValuesChanged()
        {
        }
    }
}
