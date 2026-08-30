using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.Celeste3DEngine;

/// <summary> A 3D Camera GameObject that provides view and projection matrices for rendering. </summary>
public class Camera3D : GameObject
{
    
    /// <summary> Field of View in Degrees </summary>
    public float FOV = 60f;
    /// <summary> Near Clipping Plane </summary>
    public float Near = 0.1f;
    /// <summary> Far Clipping Plane </summary>
    public float Far = 500f;
    
    /// <summary> Aspect Ratio (Width / Height) </summary>
    public float AspectRatio => Engine.Viewport.AspectRatio;

    internal Matrix View { get; private set; }
    internal Matrix Projection { get; private set; }
    internal Matrix ViewProjection;
    
    internal Matrix OrthographicProjection { get; private set; }
    
    
    /// <summary> Creates a new Camera3D with default position, rotation, and scale. </summary>
    public Camera3D() { }
    
    /// <summary> Creates a new Camera3D with the specified position and rotation. </summary>
    public Camera3D(Vector3 position, Vector3 rotation, string name = "") : base(position, rotation, Vector3.One, name) { }

    /// <summary> Creates a new Camera3D with the specified position, rotation, and field of view. </summary>
    public Camera3D(Vector3 position, Vector3 rotation, float FOV, string name = "") : this(position, rotation, name)
    {
        this.FOV = FOV;
    }

    /// <summary> Creates a new Camera3D with the specified position, rotation, field of view, near plane, and far plane. </summary>
    public Camera3D(Vector3 position, Vector3 rotationEuler, float FOV = 60f, float Near = 0.1f, float Far = 500f, string name = "")
        : this(position, rotationEuler, name)
    {
        this.FOV = FOV;
        this.Near = Near;
        this.Far = Far;
    }
    

    internal override void Start()
    {
        base.Start();
        UpdateMatrices(Engine.Viewport);
    }

    internal override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        UpdateMatrices(Engine.Viewport);
    }

    internal void UpdateMatrices(Viewport viewport)
    {
        Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(FOV), viewport.AspectRatio, Near, Far);
        View = Matrix.CreateTranslation(-transform.position) *
               Matrix.CreateFromQuaternion(transform.rotation.Conjugated());
        ViewProjection = View * Projection;
        
        OrthographicProjection = Matrix.CreateOrthographicOffCenter(0, viewport.Width, viewport.Height, 0f, 0.01f, 1000);
    }
    
    internal bool WorldToScreen(Vector3 world, Viewport vp, out Vector2 screen, out float depth01)
    {
        Vector4 clip = Vector4.Transform(new Vector4(world, 1f), View * Projection);

        // Derrière la caméra
        if (clip.W <= 0f)
        {
            screen = default;
            depth01 = 1f;
            return false;
        }

        Vector3 ndc = new Vector3(clip.X, clip.Y, clip.Z) / clip.W; // -1..1

        screen = new Vector2(
            (ndc.X * 0.5f + 0.5f) * vp.Width + vp.X,
            (-ndc.Y * 0.5f + 0.5f) * vp.Height + vp.Y
        );

        depth01 = (ndc.Z * 0.5f + 0.5f); // 0..1
        return true;
    }
}