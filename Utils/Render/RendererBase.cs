using System.Collections.Generic;
using Equinox.Utils.Components;
using Equinox.Utils.Session;
using Sandbox.ModAPI;
using VRage.Input;

namespace Equinox.Utils.Render
{
    public abstract class RendererBase : RegisteredSessionComponent
    {
        protected RendererBase() : base(typeof(RendererBase))
        {
        }

        private readonly List<IRenderableComponent> _renderables = new List<IRenderableComponent>();

        public void Register(IRenderableComponent c)
        {
            _renderables.Add(c);
        }

        public void Unregister(IRenderableComponent c)
        {
            _renderables.Remove(c);
        }

        public override void LoadData()
        {
            base.LoadData();
            _renderables.Clear();
        }

        protected override void UnloadData()
        {
            base.UnloadData();
            _renderables.Clear();
        }

        public override void Draw()
        {
            base.Draw();
            if (MyAPIGateway.Utilities.IsDedicated)
                return;
            var debug = MyAPIGateway.Input.IsKeyPress(MyKeys.OemOpenBrackets);
            foreach (IRenderableComponent k in _renderables)
            {
                k.Draw();
                if (debug)
                    k.DebugDraw();
            }
        }
    }
}