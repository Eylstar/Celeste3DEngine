using System;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.ModInterop;

namespace Celeste.Mod.Celeste3DEngine;

public class Celeste3DEngineModule : EverestModule 
{
    public static Celeste3DEngineModule Instance { get; private set; }

    public override Type SaveDataType => typeof(Celeste3DEngineModuleSaveData);
    public static Celeste3DEngineModuleSaveData SaveData => (Celeste3DEngineModuleSaveData) Instance._SaveData;

    public Celeste3DEngineModule() 
    {
        Instance = this;
        
#if DEBUG
        Logger.SetLogLevel(nameof(Celeste3DEngineModule), LogLevel.Verbose);
#else
        Logger.SetLogLevel(nameof(Celeste3DEngineModule), LogLevel.Info);
#endif
    }

    public override void Load()
    {
        typeof(SpeedrunToolImports).ModInterop();
        
        IL.Celeste.Level.Render += RenderingHooks.IL_Level_Render;
        On.Celeste.Level.Render += RenderingHooks.ON_HUD_RenderHD;

        On.Celeste.Level.LoadLevel += LoadingHooks.ON_Level_LoadLevel;
    }
    
    public override void Unload() 
    {
        IL.Celeste.Level.Render -= RenderingHooks.IL_Level_Render;
        On.Celeste.Level.Render -= RenderingHooks.ON_HUD_RenderHD;
        
        On.Celeste.Level.LoadLevel -= LoadingHooks.ON_Level_LoadLevel;
        
        EngineEntity.CleanEngine();
        EngineCallbacks.ClearCallbacks();
    }
}