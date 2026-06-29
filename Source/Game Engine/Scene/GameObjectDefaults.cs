using Microsoft.Xna.Framework;

namespace Celeste.Mod.Celeste3DEngine;

public partial class GameObject
{
    public static GameObject DefaultCube
    {
        get
        {
            GameObject cube = new GameObject(Vector3.Zero, Vector3.Zero, Vector3.One, "Cube");
            MeshRenderer mr = new MeshRenderer("cube", "defaultTexture", Material.DefaultMaterial);
            mr.useEngineModelPath = true;
            mr.useEngineTexturePath = true;
            cube.AddComponent(mr);
            return cube;
        }
    }
    
    public static GameObject DefaultPlane
    {
        get
        {
            GameObject plane = new GameObject(Vector3.Zero, Vector3.Zero, Vector3.One, "Plane");
            MeshRenderer mr = new MeshRenderer("plane", "defaultTexture", Material.DefaultMaterial);
            mr.useEngineModelPath = true;
            mr.useEngineTexturePath = true;
            plane.AddComponent(mr);
            return plane;
        }
    }
    
    public static GameObject DefaultSphere
    {
        get
        {
            GameObject sphere = new GameObject(Vector3.Zero, Vector3.Zero, Vector3.One, "Sphere");
            MeshRenderer mr = new MeshRenderer("sphere", "defaultTexture", Material.DefaultMaterial);
            mr.useEngineModelPath = true;
            mr.useEngineTexturePath = true;
            sphere.AddComponent(mr);
            return sphere;
        }
    }
}