using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Equinox.EnergyWeapons;
using Equinox.Utils.Logging;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Equinox.Utils.Misc
{
    public class DummyPathRef
    {
        private readonly IMyEntity _entity;

        private IMyModel _cachedModel;
        private IMyModel _cachedSubpartModel;
        private IMyEntity _cachedSubpart;
        private MatrixD? _cachedDummyMatrix;
        private readonly string[] _path;

        public DummyPathRef(IMyEntity e, params string[] path)
        {
            _entity = e;
            _path = path;
        }

        private void Update()
        {
            _cachedModel = _entity.Model;
            _cachedSubpart = _entity;
            for (var i = 0; i < _path.Length - 1; i++)
            {
                MyEntitySubpart part;
                if (_cachedSubpart.TryGetSubpart(_path[i], out part))
                    _cachedSubpart = part;
                else
                {
                    EnergyWeaponsCore.LoggerStatic?.Warning(
                        $"Couldn't find subpart {_path[i]} in {_cachedSubpart.Model?.AssetName}");
                    var tmp2 = new Dictionary<string, IMyModelDummy>();
                    _cachedSubpart.Model?.GetDummies(tmp2);
                    EnergyWeaponsCore.LoggerStatic?.Warning(
                        $"Existing dummies/subparts: {string.Join(", ", tmp2.Keys)}");
                }
            }

            _cachedSubpartModel = _cachedSubpart?.Model;
            _cachedDummyMatrix = null;
            var tmp = new Dictionary<string, IMyModelDummy>();
            _cachedSubpartModel?.GetDummies(tmp);
            IMyModelDummy dummy;
            if (tmp.TryGetValue(_path[_path.Length - 1], out dummy))
                _cachedDummyMatrix = dummy.Matrix;
            else
            {
                EnergyWeaponsCore.LoggerStatic?.Warning(
                    $"Couldn't find dummy {_path[_path.Length - 1]} in {_cachedSubpart?.Model?.AssetName}");
                EnergyWeaponsCore.LoggerStatic?.Warning(
                    $"Existing dummies: {string.Join(", ", tmp.Keys)}");
            }

        }

        private bool CheckUpdate()
        {
            if (_cachedModel == _entity.Model && _cachedSubpartModel == _cachedSubpart?.Model)
                return true;
            Update();
            return false;
        }

        public Vector3D WorldPosition
        {
            get
            {
                CheckUpdate();
                return Vector3D.Transform(_cachedDummyMatrix?.Translation ?? Vector3.Zero,
                    _cachedSubpart?.WorldMatrix ?? _entity.WorldMatrix);
            }
        }

        public MatrixD WorldMatrix
        {
            get
            {
                CheckUpdate();
                return (_cachedDummyMatrix ?? MatrixD.Identity) * (_cachedSubpart?.WorldMatrix ?? _entity.WorldMatrix);
            }
        }

        public bool Valid
        {
            get
            {
                CheckUpdate();
                return _cachedSubpart != null && _cachedDummyMatrix.HasValue;
            }
        }

        public override string ToString()
        {
            return $"{_entity.ToStringSmart()}: {string.Join("/", _path)}";
        }
    }
}