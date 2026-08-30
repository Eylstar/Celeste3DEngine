using System;
using System.Collections.Generic;
using Monocle;
using Celeste.Mod.Entities;
using FontStashSharp;
using Microsoft.Xna.Framework;
using MonoMod.ModInterop;

namespace Celeste.Mod.Celeste3DEngine;

[ModImportName("SpeedrunTool.SaveLoad")]
internal static class SpeedrunToolImports
{
    public static Action<Entity, bool> IgnoreSaveState;
}

/// <summary> The Celeste Entity that represent the Core Engine. Manages Scene Loading and Core logic </summary>
[Tracked(true)]
[CustomEntity("Celeste3DEngine/EngineEntity")]
public sealed class EngineEntity : Entity
{
    static EngineEntity instance = null;
    static bool initialized;
    
    static bool persistent = false;
    static string modelsPath, texturesPath, exportsPath, fontsPath, audioPath;
    
    internal static Scene3D Current3DScene;
    
    
    /// <summary> Event invoked when the EngineEntity is added to a scene </summary>
    public delegate void EngineLoadDelegate(EngineEntity engine, Scene scene);
    /// <summary> Event invoked when the EngineEntity is added to a scene </summary>
    public static event EngineLoadDelegate OnEngineLoad;

    /// <summary> (Celeste Lonn Entity Constructor) Creates a new EngineEntity with the specified paths and persistence </summary>
    public EngineEntity(EntityData data, Vector2 offset) : base(data.Position + offset)
    {
        modelsPath = data.Attr("modelsPath");
        texturesPath = data.Attr("texturesPath");
        exportsPath = data.Attr("exportsPath");
        fontsPath = data.Attr("fontsPath");
        audioPath = data.Attr("audioPath");

        EnginePaths.SetAllPathsCtr(modelsPath, texturesPath, exportsPath, fontsPath, audioPath);
        persistent = data.Bool("persistent", false);
        Visible = true;
        
        SpeedrunToolImports.IgnoreSaveState?.Invoke(this, true);
    }
    
    
    
    /// <summary> Creates a new EngineEntity with the specified paths and persistence </summary>
    public EngineEntity(string m, string t, string e, string f, string a, bool p)
    {
        EnginePaths.SetAllPathsCtr(m, t, e, f, a);
        persistent = p;
        Visible = true;
        
        SpeedrunToolImports.IgnoreSaveState?.Invoke(this, true);
    }
    
    public override void Added(Scene scene)
    {
        base.Added(scene);

        if (instance != null && instance != this)
        {
            Logger.Info("EngineEntity", "An instance of EngineEntity already exists in the scene, destroying the new one.");
            RemoveSelf();
            return;
        }
        IndempotentLoad();
        OnEngineLoad?.Invoke(this, scene);
    }

    internal void IndempotentLoad()
    {
        instance = this;
        if (initialized) return;
        initialized = true;
        
        SetPersistence(persistent);
        
        EngineFonts.LoadDefault();
        
        if (!String.IsNullOrEmpty(EnginePaths.customFontsPath))
            EngineFonts.LoadAllFonts();
        
        ShadersLoader.LoadShaders();
    }
    
    /// <summary> Loads a new 3D scene, unloading the previous one if it exists </summary>
    public static void LoadScene(Scene3D newScene)
    {
        if (Current3DScene != null)
            UnloadCurrentScene();
        
        Current3DScene = newScene;
        Current3DScene.LoadScene();
    }
    
    /// <summary> Unloads the current active 3D scene </summary>
    public static void UnloadCurrentScene()
    {
        if (Current3DScene != null)
        {
            Current3DScene.UnloadScene();
            Current3DScene = null;
        }
    }
    
    /// <summary> Gets the current active 3D scene </summary>
    public static Scene3D GetCurrentScene() => Current3DScene;
    
    
    /// <summary> Sets whether the EngineEntity should persist across rooms </summary>
    public static void SetPersistence(bool persist)
    {
        if (instance != null)
        {
            if (persist)
                instance.Tag |= Tags.Global;
            else
                instance.Tag &= ~Tags.Global;
        }
    }

    
    
    public override void Update()
    {
        base.Update();
        Current3DScene?.SceneUpdate();
    }

    public override void Render()
    {
        base.Render();
        Current3DScene?.SceneRender();
    }

    public override void Removed(Scene scene)
    {
        base.Removed(scene);
        if (instance == this && !persistent)
        {
            SaveEngineState(scene);
            CleanEngine();
        }
    }

    public override void SceneEnd(Scene scene)
    {
        base.SceneEnd(scene);
        if (instance == this)
        {
            SaveEngineState(scene);
            CleanEngine();
        }  
    }

    // Saves the engine state to the module save data to reload the engine later
    void SaveEngineState(Scene s)
    {
        if (!persistent) return;

        Celeste3DEngineModuleSaveData saveData = Celeste3DEngineModule.SaveData;
        
        Level currentLevel = s as Level;
        if (currentLevel == null) return;
        
        string roomName = currentLevel.Session.Level ?? "";
        string mapName = currentLevel.Session.Area.SID ?? "";
        
        Celeste3DEngineModuleSaveData.MapSaveData mapData = saveData.mapSaveDataList.Find(m => m.mapName == mapName);
        
        if (mapData == null)
        {
            mapData = new Celeste3DEngineModuleSaveData.MapSaveData
            {
                mapName = mapName, leftRoom = roomName, needToRespawnEngine = true, 
                modelPath = modelsPath, texturePath = texturesPath, exportPath = exportsPath, fontPath = fontsPath, audioPath = audioPath,
                persistentEngine = persistent
            };
            saveData.mapSaveDataList.Add(mapData);
        }
        else
        {
            mapData.leftRoom = roomName;
            mapData.needToRespawnEngine = true;
            mapData.modelPath = modelsPath;
            mapData.texturePath = texturesPath;
            mapData.exportPath = exportsPath;
            mapData.fontPath = fontsPath;
            mapData.audioPath = audioPath;
            mapData.persistentEngine = persistent;
        }
        
        //Logger.Warn("SAVE FLOW", "SAVE ENGINE STATE. Room :" + roomName + " Map: " + mapName);
    }
    
    internal static void CleanEngine()
    {
        Logger.Info("EngineEntity", "Cleaning");
        initialized = false;
        OnEngineLoad = null;
        instance = null;
        
        UnloadCurrentScene();
        Material.ResetDefaultMaterial();
        ShadersLoader.UnloadEffects();
        EnginePaths.ResetPaths();
        EngineFonts.Unload();
        TexturesCache.Reset();
        AudioCache.UnloadAll();
        GLBMeshData.ClearCache();
    }
}