using System;
using Equinox.Utils.Render;
using Equinox.Utils.Session;
using VRage.Game.Components;

namespace Equinox.EnergyWeapons.Session
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class Renderer : RendererBase
    {
        public override Type[] Dependencies { get; } = {typeof(EnergyWeaponsCore)};

        private EnergyWeaponsCore _core;

        public override void LoadData()
        {
            base.LoadData();
            _core = Session.GetComponent<EnergyWeaponsCore>();
            if (_core == null)
                throw new Exception("No core component!");
        }

        protected override void UnloadData()
        {
            base.UnloadData();
            _core = null;
        }

        public override void Draw()
        {
            if (_core == null || !_core.Master )
                return;
            base.Draw();
        }
    }
}