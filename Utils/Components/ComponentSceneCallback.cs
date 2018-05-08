using VRage.Game.Components;

namespace Equinox.Utils.Components
{
    public class ComponentSceneCallback : MyEntityComponentBase
    {
        public override string ComponentTypeDebugString => GetType().Name;

        public override void OnAddedToContainer()
        {
            base.OnAddedToContainer();
            if (Entity.InScene)
                OnAddedToScene();
        }

        public override void OnBeforeRemovedFromContainer()
        {
            if (Entity.InScene)
                OnRemovedFromScene();
            base.OnBeforeRemovedFromContainer();
        }
    }
}
