using Microsoft.Xna.Framework;

namespace Celeste.Mod.Celeste3DEngine;

public class LightingSettings
{
    public Vector3 AmbientLightColor = new Vector3(0.2f, 0.2f, 0.2f);
    public Vector3 DirectionalLightColor = new Vector3(1f, 1f, 1f);
    public Vector3 DirectionalLightDirection = new Vector3(-1f, -1f, -1f);
    public bool DirectionalLightCastsShadows = true;

    
    
    internal int maxShadowedLights = 4;
    public int shadowMapResolution = 2048;
    public int spotLightShadowMapResolution = 1024;
    
    public float directionalShadowOrthographicSize = 30f;
    public float shadowNearPlane = 0.1f;
    public float shadowFarPlane = 60f;
    
    public float shadowStrength = 1f;
    public float shadowBias = 0.001f;

    public float shadowCascadeSplitDistance = 15f;
    
    public float fogDensity = 0.035f;
    public float heightFogDensity = 0.06f;
    public float heightFogStart = 0f;
    public Vector3 fogColor = new Vector3(0.6f, 0.7f, 0.8f);
    
    /// <summary> Sets the directional light's direction based on Euler angles in degrees. X = pitch, Y = yaw, Z = roll. </summary>
    public void SetDirectionalLightRotation(Vector3 eulerDegrees)
    {
        Quaternion rot = Quaternion.CreateFromYawPitchRoll(
            MathHelper.ToRadians(eulerDegrees.Y),
            MathHelper.ToRadians(eulerDegrees.X),
            MathHelper.ToRadians(eulerDegrees.Z));
    
        DirectionalLightDirection = Vector3.Transform(Vector3.Forward, rot);
    }
}