﻿using System;
using System.Collections.Generic;
using System.Text;
using Equinox.EnergyWeapons.Components.Thermal;
using Equinox.EnergyWeapons.Definition.Beam;
using Equinox.EnergyWeapons.Physics;
using Equinox.Utils.Components;
using VRageMath;
using DummyData =
    Equinox.EnergyWeapons.Components.Network.DummyData<Equinox.EnergyWeapons.Components.Beam.Segment,
        Equinox.EnergyWeapons.Components.Beam.BeamConnectionData>;

namespace Equinox.EnergyWeapons.Components.Beam.Logic
{
    public abstract class Lossy<T> : Component<T> where T : Lossy
    {
        protected readonly ComponentDependency<ThermalPhysicsComponent> ThermalPhysics;

        private class DummyLossHandler : IDisposable
        {
            private readonly Lossy<T> _controller;
            private readonly DummyData _dummy;
            private readonly Lossy.LossyDummy _data;

            public DummyLossHandler(Lossy<T> ctl, Lossy.LossyDummy d)
            {
                _controller = ctl;
                _data = d;
                bool tmp;
                _dummy = ctl.Network.Controller.GetOrCreate(ctl.Block, d.Dummy, out tmp);
                _dummy.SegmentChanged += SegmentChanged;
                SegmentChanged(null, _dummy.Segment);
            }

            private void SegmentChanged(Segment old, Segment @new)
            {
                if (old == @new)
                    return;
                if (old != null)
                    old.StateUpdated -= StateUpdate;
                if (@new != null)
                    @new.StateUpdated += StateUpdate;
            }

            private void StateUpdate(Segment obj)
            {
                var loss = MathHelper.Clamp(_controller.Loss(_data.HeatLoss), 0, 1);
                _controller.ThermalPhysics.Value.Physics.AddEnergy(obj.Current.Output * loss);
            }

            public void Dispose()
            {
                SegmentChanged(_dummy.Segment, null);
                _dummy.SegmentChanged -= SegmentChanged;
            }
        }

        private readonly List<DummyLossHandler> _handlers = new List<DummyLossHandler>();

        protected Lossy(NetworkComponent block, T definition) : base(block, definition)
        {
            ThermalPhysics = new ComponentDependency<ThermalPhysicsComponent>(block);
        }

        public float CurrentTemperature =>
            ThermalPhysics.Value?.Physics.Temperature ?? PhysicalConstants.TemperatureSpace;

        public override void OnAddedToScene()
        {
            ThermalPhysics.OnAddedToContainer();
            Block.OnUpgradeValuesChanged += UpgradeValuesChanged;
            Block.AddUpgradeValue(UpgradeValueCooling, UpgradeValueCoolingDefault);
            Block.AddUpgradeValue(UpgradeValueEfficiency, UpgradeValueEfficiencyDefault);
            foreach (var k in Definition.LossyDummies)
                if (k.HeatLoss > 0)
                    _handlers.Add(new DummyLossHandler(this, k));
            CommitCoolingPower();
        }

        public override void OnRemovedFromScene()
        {
            Block.OnUpgradeValuesChanged -= UpgradeValuesChanged;
            foreach (var k in _handlers)
                k.Dispose();
            _handlers.Clear();
            CommitCoolingPower(remove: true);
            ThermalPhysics.OnBeforeRemovedFromContainer();
        }

        #region Upgrade Values

        public const string UpgradeValueCooling = "ThermalCoolingKWpK";
        public const float UpgradeValueCoolingDefault = 0f;
        public const string UpgradeValueEfficiency = "EnergyWeaponEfficiency";
        public const float UpgradeValueEfficiencyDefault = 1f;

        private float _upgradeEfficiency = UpgradeValueEfficiencyDefault;
        private float _upgradeCoolingKw = UpgradeValueCoolingDefault;

        private void UpgradeValuesChanged()
        {
            _upgradeEfficiency = UpgradeValueEfficiencyDefault;
            _upgradeCoolingKw = UpgradeValueCoolingDefault;
            if (Block?.UpgradeValues != null)
            {
                _upgradeEfficiency =
                    Block.UpgradeValues.GetValueOrDefault(UpgradeValueEfficiency, UpgradeValueEfficiencyDefault);
                _upgradeCoolingKw =
                    Block.UpgradeValues.GetValueOrDefault(UpgradeValueEfficiency, UpgradeValueCoolingDefault);
            }

            CommitCoolingPower();
        }

        private float _currCoolingPower = 0;

        private void CommitCoolingPower(bool remove = false)
        {
            var phys = ThermalPhysics.Value.Physics;
            var @new = remove ? 0 : CoolingPower;
            phys.RadiateIntoSpaceConductivity += (@new - _currCoolingPower);
            _currCoolingPower = @new;
        }

        /// <summary>
        /// Cooling power in kW/K
        /// </summary>
        private float CoolingPower => ((Definition.CoolingPower ?? 0) + _upgradeCoolingKw);

        protected float Efficiency(float @base)
        {
            return Math.Min(1, @base * _upgradeEfficiency);
        }

        protected float Loss(float @base)
        {
            return Math.Min(1, @base / _upgradeEfficiency);
        }

        public override void Debug(StringBuilder sb)
        {
            if (_currCoolingPower > 0)
                sb.Append("CoolingPower=").Append(_currCoolingPower.ToString("F2")).Append("kW/K ");
            sb.Append("Efficiency=").Append(_upgradeEfficiency).Append(" ");
        }

        #endregion
    }
}