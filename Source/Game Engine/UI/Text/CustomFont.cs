using System;
using System.IO;
using Celeste.Mod;
using Celeste.Mod.Celeste3DEngine;

using FontStashSharp;

namespace Celeste.Mod.Celeste3DEngine;

internal class CustomFont : IDisposable
{
    internal DynamicSpriteFont GetFont(int size) => fontSystem.GetFont(size);
    
    internal FontSystem fontSystem;
    internal string fontName;
    
    internal CustomFont(string nameWithExtension) : this(nameWithExtension, false) { }
    
    internal CustomFont(string nameWithExtension, bool engine)
    {
        fontSystem = new FontSystem();
        
        string fullPath = engine ? EnginePaths.defaultFontsPath : EnginePaths.customFontsPath;
        fullPath += "/" + nameWithExtension;
        
        if ((fullPath.EndsWith(".ttf") || fullPath.EndsWith(".otf")) && Everest.Content.TryGet(fullPath, out ModAsset asset))
        {
            using (var stream = asset.Stream)
            {
                fontSystem.AddFont(stream);
            }
            fontName = Path.GetFileNameWithoutExtension(nameWithExtension);
            Logger.Info("Celeste3DEngine", $"Loaded font '{fontName}' at path '{fullPath}'");
        }
        else
            Logger.Error("Celeste3DEngine", $"Failed to load font '{nameWithExtension}' at path '{fullPath}'");
    }
    
    public void Dispose()
    {
        fontSystem?.Dispose();
        fontSystem = null;
    }
}