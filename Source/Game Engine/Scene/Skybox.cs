using Microsoft.Xna.Framework;

namespace Celeste.Mod.Celeste3DEngine;

/// <summary> A simple GameObject that represents the Skybox of the scene. Uses default sky texture. You can change the texture with Scene3D.ChangeSkybox or disable it with Scene3D.EnableSkybox</summary>
public class Skybox : GameObject
{
    public bool foregroundRendererEnabled = false;
    internal Skybox()
        : base(Vector3.Zero, Vector3.Zero, Vector3.One, "Skybox")
    {
        MeshRenderer rend = new MeshRenderer("skybox", "sky", Material.DefaultMaterial, ModelLayer.Skybox);
        rend.useEngineModelPath = true;
        rend.useEngineTexturePath = true;
        AddComponent(rend);
    }

    public override void Destroy()
    {
        Logger.Warn("Skybox", "Can't Destroy Skybox. Use Scene3D.EnableSkybox(false) instead.");
    }
}