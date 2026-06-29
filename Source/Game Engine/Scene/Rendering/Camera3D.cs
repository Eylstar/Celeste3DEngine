using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.Celeste3DEngine;

public class Camera3D : GameObject
{
    
    /// <summary> Field of View in Degrees </summary>
    public float FOV = MathHelper.ToRadians(60f);
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
    
    
    public Camera3D() { }
    
    public Camera3D(Vector3 position, Vector3 rotation, string name = "") : base(position, rotation, Vector3.One, name) { }

    public Camera3D(Vector3 position, Vector3 rotationEuler, float FOV = 60f, float Near = 0.1f, float Far = 500f, string name = "")
        : this(position, rotationEuler, name)
    {
        this.FOV = MathHelper.ToRadians(FOV);
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
        Projection = Matrix.CreatePerspectiveFieldOfView(FOV, viewport.AspectRatio, Near, Far);
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