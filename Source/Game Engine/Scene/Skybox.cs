using Microsoft.Xna.Framework;

namespace Celeste.Mod.Celeste3DEngine;

/// <summary> A simple GameObject that represents the Skybox of the scene. Uses default sky texture. You can change the texture with Scene3D.ChangeSkybox or disable it with Scene3D.EnableSkybox</summary>
public class Skybox : GameObject
{
    /// <summary> Whether the skybox should be rendered in the foreground layer. This is useful for scenes with a lot of fog or transparency, but can cause issues with depth testing. </summary>
    public bool foregroundRendererEnabled = false;
    internal Skybox()
        : base(Vector3.Zero, Vector3.Zero, Vector3.One, "Skybox")
    {
        MeshRenderer rend = new MeshRenderer("skybox", "sky", Material.DefaultMaterial, ModelLayer.Skybox);
        rend.useEngineModelPath = true;
        rend.useEngineTexturePath = true;
        AddComponent(rend);
    }

    /// <summary> Destroys the Skybox. Use Scene3D.EnableSkybox(false) instead. </summary>
    public override void Destroy()
    {
        Logger.Warn("Skybox", "Can't Destroy Skybox. Use Scene3D.EnableSkybox(false) instead.");
    }
    
    internal void ForceDestroy()
    {
        base.Destroy();
    }
}