using Microsoft.Xna.Framework;

namespace Celeste.Mod.Celeste3DEngine;

public class PointLight : Light
{
    public PointLight(Vector3 position, Color color = default, float intensity = 1f, float range = 10f) : base(position, color, intensity, range)
    {
    }
}