using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.Celeste3DEngine;

public abstract class Light : GameObject
{
    public Color color = Color.White;
    public float intensity = 1f;
    public float range = 10f;
    
    protected Light(Vector3 position, Color color = default, float intensity = 1f, float range = 10f) : base(new Transform(position))
    {
        this.color = color;
        this.intensity = intensity;
        this.range = range;
    }

    internal override void Start()
    {
        scene?.GetRenderer()?.AddLight(this);
        base.Start();
    }

    internal override void Removed()
    {
        scene?.GetRenderer()?.RemoveLight(this);
        base.Removed();
    }

    internal virtual void DrawLightAreaDebug() { }
}