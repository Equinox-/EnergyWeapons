using System;
using Equinox.Utils.Scheduler;
using Equinox.Utils.Session;
using VRage.Game.Components;

namespace Equinox.EnergyWeapons.Session
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class SchedulerAfter : SchedulerBase
    {
        public override Type[] Dependencies { get; } = { typeof(EnergyWeaponsCore) };

        private EnergyWeaponsCore _core;

        public SchedulerAfter() : base(typeof(SchedulerAfter))
        {
        }

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

        public override void UpdateAfterSimulation()
        {
            if (_core == null || !_core.Master)
                return;
            RunUpdate(1);
        }
    }
}
