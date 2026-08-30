using Microsoft.Xna.Framework;

namespace Celeste.Mod.Celeste3DEngine;

/// <summary> Settings for lighting in the 3D scene </summary>
public class LightingSettings
{
    /// <summary> The color of the ambient light in the scene </summary>
    public Vector3 AmbientLightColor = new Vector3(0.2f);
    /// <summary> The color of the directional light in the scene </summary>
    public Vector3 DirectionalLightColor = new Vector3(1f);
    /// <summary> The direction of the directional light in the scene </summary>
    public Vector3 DirectionalLightDirection = new Vector3(0.5f, -0.8f, -0.1f);
    /// <summary> Whether the directional light casts shadows </summary>
    public bool DirectionalLightCastsShadows = true;
    
    
    /// <summary> The maximum number of lights that can cast shadows at once. </summary>
    public int maxShadowedLights = 4;
    
    /// <summary> The resolution of the shadow map for directional lights</summary>
    public int shadowMapResolution = 4096;
    
    /// <summary> The resolution of the shadow map for spot lights. </summary>
    public int spotLightShadowMapResolution = 1024;
    
    /// <summary> The near plane distance for shadow mapping. </summary>
    public float shadowNearPlane = 0.1f;
    /// <summary> The far plane distance for shadow mapping. </summary>
    public float shadowFarPlane = 1.5f;
    
    /// <summary> The strength of the shadows (0 = no shadow, 1 = full shadow) </summary>
    public float shadowStrength = 1f;
    /// <summary> The bias to apply to the shadow map to prevent shadow acne </summary>
    public float shadowBias = 0.0002f;
    
    /// <summary> How far the shadow's soft edge samples spread, in shadow map texels. 0 gives a hard edge, higher values blur it further </summary>
    public float shadowSoftness = 2f;

    // /// <summary> The distance at which the shadow cascades split (for directional lights) </summary>
    //public float shadowCascadeSplitDistance = 15f;
    
    /// <summary> The density of the fog in the scene </summary>
    public float fogDensity = 0.035f;
    
    /// <summary> The density of the height-based fog in the scene </summary>
    public float heightFogDensity = 0.06f;
    
    /// <summary> The height at which the height-based fog starts in the scene </summary>
    public float heightFogStart = 0f;
    
    /// <summary> The color of the fog in the scene </summary>
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