using System.Text;
using Equinox.Utils.Components;
using Equinox.Utils.Logging;
using VRage.Game.ModAPI;

namespace Equinox.EnergyWeapons.Components.Beam.Logic
{
    public interface IComponent
    {
        void OnAddedToScene();
        void OnRemovedFromScene();
    }

    public abstract class Component<T> : IComponent, IDebugComponent where T : Definition.Beam.Component
    {
        protected readonly ILogging Log;
        protected readonly T Definition;
        protected readonly IMyCubeBlock Block;
        protected readonly NetworkComponent Network;

        protected Component(NetworkComponent block, T definition)
        {
            Log = block.Core.Logger.CreateProxy(GetType());
            Block = block.Entity as IMyCubeBlock;
            Definition = definition;
            Network = block;
        }

        public abstract void OnAddedToScene();
        public abstract void OnRemovedFromScene();

        public abstract void Debug(StringBuilder sb);
    }
}