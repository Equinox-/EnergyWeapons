using Equinox.Utils.Render;
using Equinox.Utils.Session;
using Sandbox.ModAPI;

namespace Equinox.Utils.Components
{
    public interface IRenderableComponent
    {
        void Draw();
        void DebugDraw();
    }

    public static class RenderableComponentExt
    {
        public static void RegisterRenderable(this IRenderableComponent r)
        {
            MyAPIGateway.Session.GetComponent<RendererBase>().Register(r);
        }

        public static void UnregisterRenderable(this IRenderableComponent r)
        {
            MyAPIGateway.Session.GetComponent<RendererBase>().Unregister(r);
        }
    }
}
