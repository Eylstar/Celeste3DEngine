using Monocle;

namespace Celeste.Mod.Celeste3DEngine;

internal class GlobalSceneRenderer : Renderer
{
    public override void Render(Scene scene)
    {
        Scene3D scene3D = EngineEntity.Current3DScene;
        if (scene3D == null || !scene3D.updatedOnce) return;
        
        Renderer3D renderer = scene3D.GetRenderer();
        if (renderer == null) return;
        
        renderer.BeforeRender();
        renderer.RenderBridge();
    }
}