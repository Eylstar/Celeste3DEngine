using Microsoft.Xna.Framework;

namespace Celeste.Mod.Celeste3DEngine;

/// <summary> Provides default GameObjects for common shapes like Cube, Plane, and Sphere. </summary>
public partial class GameObject
{
    /// <summary> Creates a default cube GameObject with a MeshRenderer component. </summary>
    public static GameObject DefaultCube
    {
        get
        {
            GameObject cube = new GameObject(Vector3.Zero, Vector3.Zero, Vector3.One, "Cube");
            MeshRenderer mr = new MeshRenderer("Cube", Material.DefaultMaterial);
            mr.useEngineModelPath = true;
            cube.AddComponent(mr);
            return cube;
        }
    }
    
    /// <summary> Creates a default plane GameObject with a MeshRenderer component. </summary>
    public static GameObject DefaultPlane
    {
        get
        {
            GameObject plane = new GameObject(Vector3.Zero, Vector3.Zero, Vector3.One, "Plane");
            MeshRenderer mr = new MeshRenderer("Plane", Material.DefaultMaterial);
            mr.useEngineModelPath = true;
            plane.AddComponent(mr);
            return plane;
        }
    }
    
    /// <summary> Creates a default sphere GameObject with a MeshRenderer component. </summary>
    public static GameObject DefaultSphere
    {
        get
        {
            GameObject sphere = new GameObject(Vector3.Zero, Vector3.Zero, Vector3.One, "Sphere");
            MeshRenderer mr = new MeshRenderer("Sphere", Material.DefaultMaterial);
            mr.useEngineModelPath = true;
            sphere.AddComponent(mr);
            return sphere;
        }
    }
}