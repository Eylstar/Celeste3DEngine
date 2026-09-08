using Microsoft.Xna.Framework;

namespace Celeste.Mod.Celeste3DEngine;

/// <summary> Settings for wind in the scene. </summary>
public class WindSettings
{
    /// <summary> Direction of the wind. </summary>
    public Vector3 direction = new Vector3(1, 0, 0);
    
    /// <summary> Strength of the wind. </summary>
    public float strength = 0.75f;
    
    /// <summary> Frequency of the wind. </summary>
    public float frequency = 1.5f;
    
    /// <summary> Sets the wind direction. </summary>
    public void SetWindDirectionYaw(float yawDegrees)
    {
        Quaternion rot = Quaternion.CreateFromYawPitchRoll(MathHelper.ToRadians(yawDegrees), 0f, 0f);
        direction = Vector3.Transform(Vector3.UnitX, rot);
    }
}