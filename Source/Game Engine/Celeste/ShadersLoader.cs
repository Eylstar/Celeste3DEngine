using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.Celeste3DEngine;

internal sealed class ShadersLoader
{
    static List<Effect> shaders = new List<Effect>();
    
    internal static Effect BaseLit;
    internal static Effect Unlit;
    internal static Effect WorldText;
    internal static Effect ShadowDepthSpot;
    internal static Effect ShadowDepthDirectional;
    internal static Effect AddLight;
    
    static Dictionary<Effect, Dictionary<string, EffectParameter>> cachedParameters = new();
    
    internal static void LoadShaders()
    {
        Load("BaseLit.fxb", ref BaseLit);
        Load("Unlit.fxb", ref Unlit);
        Load("WorldText.fxb", ref WorldText);
        Load("AddLight.fxb", ref AddLight);
        Load("ShadowDepthSpot.fxb", ref ShadowDepthSpot);
        Load("ShadowDepthDirectional.fxb", ref ShadowDepthDirectional);
    }

    static void Load(string name, ref Effect effect)
    {
        string path = EnginePaths.defaultShaderPath;
        path += name;
        
        if (Everest.Content.TryGet(path, out ModAsset asset))
        {
            using (Stream stream = asset.Stream)
            {
                byte[] data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);
                effect = new Effect(Engine.Graphics.GraphicsDevice, data);
            }
            
            shaders.Add(effect);
            CacheParameters(effect);
        }
        else
            Logger.Error("Celeste3DEngine", $"Failed to load {effect.Name} at path '{path}'");
    }
    
    internal static EffectParameter GetParam(Effect effect, string name)
    {
        if (cachedParameters.TryGetValue(effect, out var dict) && dict.TryGetValue(name, out EffectParameter parameter))
            return parameter;
        return null;
    }
    
    static void CacheParameters(Effect effect)
    {
        Dictionary<string, EffectParameter> dict = new();
        foreach (EffectParameter p in effect.Parameters)
            dict[p.Name] = p;
        cachedParameters[effect] = dict;
    }
    
    internal static void UnloadEffects()
    {
        foreach (Effect effect in shaders)
            effect?.Dispose();
        shaders.Clear();
        cachedParameters.Clear();
    }
}

internal static class EffectExtensions
{
    internal static EffectParameter Param(this Effect effect, string name) => ShadersLoader.GetParam(effect, name);
}