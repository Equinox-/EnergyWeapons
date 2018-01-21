using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Game.Entities;
using VRage.Game.Components;

namespace Equinox.Utils.Components
{
    public static class ComponentExtensions
    {
        public static T GetAny<T>(this MyEntityComponentContainer container) where T : MyGameLogicComponent
        {
            return container.Get<T>() ??
                   (container.Get<MyGameLogicComponent>() ?? container.Get<MyCompositeGameLogicComponent>())
                   ?.GetAs<T>();
        }
    }
}