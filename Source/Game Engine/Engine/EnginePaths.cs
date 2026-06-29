namespace Celeste.Mod.Celeste3DEngine;

public static class EnginePaths
{
    internal static string customModelsPath = "";
    internal static string customTexturesPath = "";
    internal static string customExportsPath = "";
    internal static string customFontsPath = "";
    internal static string customAudioPath = "";

    internal static string defaultTexturesPath = "Graphics/3DEngine/DefaultTextures";
    internal static string defaultExportsPath = "Graphics/3DEngine/DefaultExports";
    internal static string defaultShaderPath = "Graphics/3DEngine/DefaultShaders/";
    internal static string defaultFontsPath = "Graphics/3DEngine/DefaultFonts";
    
    
    /// <summary> The internal default path for Engine Textures </summary>
    public static string DefaultTexturesPath => defaultTexturesPath;
    
    /// <summary> The internal default path for Engine Export Models </summary> 
    public static string DefaultExportsPath => defaultExportsPath;
    
    /// <summary> The internal default path for Engine Fonts </summary>
    public static string DefaultFontsPath => defaultFontsPath;

    
    /// <summary> The user defined path for custom models </summary>
    public static string CustomModelsPath => customModelsPath;
    
    /// <summary> The user defined path for custom exports </summary>
    public static string CustomExportsPath => customExportsPath;

    /// <summary> The user defined path for custom fonts </summary>
    public static string CustomFontsPath => customFontsPath;

    /// <summary> The user defined path for custom textures </summary>
    public static string CustomTexturesPath => customTexturesPath;
    
    /// <summary> The user defined path for custom audio </summary>
    public static string CustomAudioPath => customAudioPath;

    
    /// <summary> Sets the global paths for models, textures, exports, and fonts for the engine to load them </summary>
    public static void SetAllPaths(string mod, string tex, string exp, string fon, string aud)
    {
        SetAllPathsCtr(mod, tex, exp, fon, aud);
        if (fon != "") 
            EngineFonts.LoadAllFonts();
    }
    
    internal static void SetAllPathsCtr(string mod, string tex, string exp, string fon, string aud)
    {
        customModelsPath = mod;
        customTexturesPath = tex;
        customExportsPath = exp;
        customFontsPath = fon;
        customAudioPath = aud;
    }
    
    /// <summary> Resets all paths to empty strings</summary>
    public static void ResetPaths() { customModelsPath = ""; customTexturesPath = ""; customExportsPath = ""; customFontsPath = ""; customAudioPath = ""; }
    
    /// <summary> Sets the user defined path for custom models</summary>
    public static void SetModelsPath(string path) => customModelsPath = path; 
    
    /// <summary> Sets the user defined path for custom textures</summary>
    public static void SetTexturesPath(string path) => customTexturesPath = path; 
    
    /// <summary> Sets the user defined path for custom exports</summary>
    public static void SetExportsPath(string path) => customExportsPath = path; 
    
    /// <summary> Sets the user defined path for custom fonts</summary>
    public static void SetFontsPath(string path)
    {
        customFontsPath = path;
        if (customFontsPath != "")
            EngineFonts.LoadAllFonts();
    }
    
    /// <summary> Sets the user defined path for custom audio</summary>
    public static void SetAudioPath(string path) => customAudioPath = path;
}