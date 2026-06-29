using System;
using System.Collections.Generic;
using Monocle;

namespace Celeste.Mod.Celeste3DEngine;

internal static class TexturesCache
{
    static Dictionary<string, VirtualTexture> cache = new();

    internal static VirtualTexture Get(string path)
    {
        if (cache.TryGetValue(path, out VirtualTexture vt))
            return vt;

        if (!Everest.Content.TryGet(path, out ModAsset asset) || asset == null)
        {
            Logger.Error("Celeste3DEngine", $"Texture not found at '{path}'");
            return null;
        }

        VirtualTexture tex = VirtualContent.CreateTexture(path);
        cache[path] = tex;
        return tex;
    }
    
    internal static void Reset()
    {
        foreach (var vt in cache.Values)
            vt.Dispose();
        cache.Clear();
    }
}