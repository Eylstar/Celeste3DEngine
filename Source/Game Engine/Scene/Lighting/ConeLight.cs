using Microsoft.Xna.Framework;

namespace Celeste.Mod.Celeste3DEngine;

public class ConeLight : Light
{
    public float Angle;

    public bool CastsShadows = false;
    
    public ConeLight(Vector3 position, Color color, float intensity, float range, float angle) : base(position, color, intensity, range)
    {
        Angle = angle;
    }
}