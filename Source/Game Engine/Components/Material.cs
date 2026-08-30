using System;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.Celeste3DEngine;

/// <summary> Material properties for 3D models </summary>
public class Material
{
    /// <summary> Color of the material under diffuse lighting </summary>
    public Vector3 DiffuseColor = Vector3.One;
    
    /// <summary> Color of the material under specular lighting </summary>
    public Vector3 SpecularColor = Vector3.Zero;
    
    /// <summary> Shininess of the material (higher values result in smaller, sharper specular highlights) </summary>
    public float Shininess = 32f;
    
    /// <summary> Whether the material is affected by lighting </summary>
    public bool isLit = true;
    
    /// <summary> Tint color applied to the material </summary>
    public Vector3 Color = Vector3.One;
    
    /// <summary> Emissive color of the material (the color it appears to emit, unaffected by lighting) </summary>
    public Vector3 EmissiveColor = Vector3.Zero;
    
    /// <summary> Emissive intensity of the material (multiplies the emissive color) </summary>
    public float EmissiveIntensity = 0f;

    
    /// <summary> Fallback value here for backwards compatibility </summary>
    [Obsolete("Retro compatibility value only. Use Material.Color instead", false)]
    public Vector3 tint
    {
        get => Color;
        set => Color = value;
    }
    
    
    internal static readonly Material DefaultMaterial = new Material();
    
    /// <summary> Copies the values from another Material into this one </summary>
    public void CopyFrom(Material mat)
    {
        DiffuseColor = mat.DiffuseColor;
        SpecularColor = mat.SpecularColor;
        Shininess = mat.Shininess;
        isLit = mat.isLit;
        Color = mat.Color;
        EmissiveColor = mat.EmissiveColor;
        EmissiveIntensity = mat.EmissiveIntensity;
    }

    /// <summary> Creates a copy of this Material to prevent changes to the original when modifying the copy </summary>
    public Material Clone()
    {
        return new Material
        {
            DiffuseColor = this.DiffuseColor,
            SpecularColor = this.SpecularColor,
            Shininess = this.Shininess,
            isLit = this.isLit,
            Color = this.Color,
            EmissiveColor = this.EmissiveColor,
            EmissiveIntensity = this.EmissiveIntensity
        };
    }

    /// <summary> Sets a new DefaultMaterial for the Engine (efective in all Scenes) </summary>
    public static void SetDefaultMaterial(Material mat)
    {
        DefaultMaterial.CopyFrom(mat);
    }
    
    /// <summary> Sets the diffuse color of the material using a MonoGame Color </summary>
    public void SetDiffuseColor(Color color) => DiffuseColor = color.ToVector3();
    
    /// <summary> Sets the specular color of the material using a MonoGame Color </summary>
    public void SetSpecularColor(Color color) => SpecularColor = color.ToVector3();
    
    internal static void ResetDefaultMaterial()
    {
        DefaultMaterial.DiffuseColor = Vector3.One;
        DefaultMaterial.SpecularColor = Vector3.Zero;
        DefaultMaterial.Shininess = 32f;
        DefaultMaterial.isLit = true;
        DefaultMaterial.Color = Vector3.One;
        DefaultMaterial.EmissiveColor = Vector3.Zero;
        DefaultMaterial.EmissiveIntensity = 0f;
    }
}