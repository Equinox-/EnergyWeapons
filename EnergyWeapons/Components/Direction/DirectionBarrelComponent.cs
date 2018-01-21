using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equinox.EnergyWeapons.Misc;
using Equinox.Utils.Logging;
using Sandbox.ModAPI;
using SpaceEngineers.Game.ModAPI;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Equinox.EnergyWeapons.Components.Direction
{
    public class DirectionBarrelComponent : DirectionComponent, ICoreRefComponent
    {
        private readonly string[] _barrelPath;

        private IMyModel _cachedModel;
        private IMyEntity _cachedBarrelSubpart;
        private Matrix _muzzleMatrix;

        public static DirectionBarrelComponent CreateAuto(IMyEntity ent)
        {
            if (ent is IMyLargeGatlingTurret)
                return new DirectionBarrelComponent("GatlingTurretBase1", "GatlingTurretBase2", "GatlingBarrel");
            if (ent is IMyLargeInteriorTurret)
                return new DirectionBarrelComponent("InteriorTurretBase1", "InteriorTurretBase2");
            if (ent is IMyLargeMissileTurret)
                return new DirectionBarrelComponent("MissileTurretBase1", "MissileTurretBarrels");
            if (ent is IMySmallGatlingGun)
                return new DirectionBarrelComponent("Barrel");
            if (ent is IMySmallMissileLauncher)
                return new DirectionBarrelComponent();
            return null;
        }

        public DirectionBarrelComponent(params string[] barrelPath)
        {
            _barrelPath = barrelPath;
        }

        public override string ComponentTypeDebugString
        {
            get { return GetType().Name; }
        }

        private Vector3D _shotOrigin, _shotDirection;

        public override Vector3D ShotOrigin
        {
            get
            {
                CheckCache();
                return _shotOrigin;
            }
        }

        public override Vector3D ShotDirection
        {
            get
            {
                CheckCache();
                return _shotDirection;
            }
        }

        private void CheckCache()
        {
            if (_cachedModel != Entity.Model)
            {
                _cachedModel = Entity.Model;
                var subpart = Entity;
                for (var i = 0; i < _barrelPath.Length; i++)
                {
                    MyEntitySubpart next;
                    if (!subpart.TryGetSubpart(_barrelPath[i], out next))
                    {
                        _logger?.Warning(
                            $"Couldn't find subpart {string.Join("/", _barrelPath, 0, i + 1)} in {subpart.Model?.AssetName}");
                        subpart = null;
                        break;
                    }
                    else
                        subpart = next;
                }

                _cachedBarrelSubpart = subpart;

                _muzzleMatrix = Matrix.Identity;

                var dummies = new Dictionary<string, IMyModelDummy>();
                (_cachedBarrelSubpart?.Model ?? Entity.Model)?.GetDummies(dummies);
                foreach (var kv in dummies)
                    if (kv.Key.IndexOf("muzzle_projectile", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        kv.Key.IndexOf("muzzle_missile", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        kv.Key.IndexOf("barrel", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _muzzleMatrix = kv.Value.Matrix;
                        break;
                    }
            }

            var muzzleWorldMatrix = (MatrixD) _muzzleMatrix * (_cachedBarrelSubpart?.WorldMatrix ?? Entity.WorldMatrix);
            _shotOrigin = muzzleWorldMatrix.Translation;
            _shotDirection = muzzleWorldMatrix.Forward;
        }

        private ILogging _logger;

        public void OnAddedToCore(EnergyWeaponsCore core)
        {
            _logger = core.Logger?.CreateProxy(GetType());
        }

        public void OnBeforeRemovedFromCore()
        {
            _logger = null;
        }
    }
}