using Microsoft.Xna.Framework;

namespace Celeste.Mod.Celeste3DEngine;

/// <summary> A light that emits in all directions from a single point. </summary>
public class PointLight : Light
{
    /// <summary> Creates a PointLight at the given position </summary>
    public PointLight(Vector3 position, Color color, float intensity = 1f, float range = 10f) : base(position, color, intensity, range)
    {
    }
}