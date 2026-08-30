using System.Collections.Generic;

namespace Celeste.Mod.Celeste3DEngine;

internal static class EngineFonts
{
    static Dictionary<string, CustomFont> fonts = new();
    
    static void Load(string nameWithExtension)
    {
        string nameWithoutExtension = nameWithExtension.Substring(0, nameWithExtension.LastIndexOf('.'));
        
        if (fonts.ContainsKey(nameWithoutExtension))
            return;
        
        CustomFont font = new CustomFont(nameWithExtension);
        fonts[font.fontName] = font;
    }
    
    internal static void LoadDefault()
    {
        if (fonts.ContainsKey("Renogare"))
            return;
        
        CustomFont font = new CustomFont("Renogare.otf", true);
        fonts[font.fontName] = font;
    }
    
    internal static void LoadAllFonts()
    {
        string fontsPath = EnginePaths.CustomFontsPath;
        if (fontsPath == null) return;
        if (Everest.Content.TryGet(fontsPath, out ModAsset _ , true))
        {
            foreach (var entry in Everest.Content.Map)
            {
                if (entry.Key.StartsWith(fontsPath) && (entry.Key.EndsWith(".ttf") || entry.Key.EndsWith(".otf")))
                {
                    string fontFile = entry.Key.Substring(fontsPath.Length + 1);
                    Load(fontFile);
                }
            }
        }
    }
    
    internal static CustomFont Get(string name)
    {
        if (fonts.TryGetValue(name, out CustomFont font))
            return font;
        else
        {
            Logger.Error("Celeste3DEngine", $"Font '{name}' not found, returning Default font Renogare");
            return GetDefault();
        }
    }
    
    internal static CustomFont GetDefault()
    {
        if (fonts.TryGetValue("Renogare", out CustomFont font))
            return font;
        else
        {
            Logger.Error("Celeste3DEngine", $"Default font 'Renogare' not found, returning null");
            return null;
        }
    }
    
    internal static void Unload()
    {
        foreach (CustomFont font in fonts.Values)
            font.Dispose();
        fonts.Clear();
    }
}