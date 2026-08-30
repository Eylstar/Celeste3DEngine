using Microsoft.Xna.Framework;

namespace Celeste.Mod.Celeste3DEngine;

/// <summary> A light that emits in a cone shape. </summary>
public class ConeLight : Light
{
    /// <summary> Angle of the cone in degrees </summary>
    public float Angle;
    
    /// <summary> Controls how gradually the light fades toward the edge of the cone. 0 = no falloff, 1 = full falloff </summary>
    public float SpotFalloff = 0.5f;

    /// <summary> Whether this light casts shadows. </summary>
    public bool CastsShadows = false;
    
    /// <summary> Creates a ConeLight at the given position, all parameters are required. </summary>
    public ConeLight(Vector3 position, Color color, float intensity, float range, float angle) : base(position, color, intensity, range)
    {
        Angle = angle;
    }
}