using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;

namespace Equinox.Utils.Components
{
    public class ComponentSceneCallback : MyEntityComponentBase
    {
        public override string ComponentTypeDebugString => GetType().Name;

        public override void OnAddedToContainer()
        {
            if (Entity.InScene)
                OnAddedToScene();
        }

        public override void OnBeforeRemovedFromContainer()
        {
            if (Entity.InScene)
                OnRemovedFromScene();
        }
    }
}
