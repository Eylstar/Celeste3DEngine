using System;
using System.Collections.Generic;
using Monocle;
using Celeste.Mod.Entities;
using FontStashSharp;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Celeste3DEngine;

/// <summary> The Celeste Entity that represent the Core Engine. Manages Scene Loading and Core logic </summary>
[Tracked(true)]
[CustomEntity("Celeste3DEngine/EngineEntity")]
public sealed class EngineEntity : Entity
{
    static EngineEntity instance = null;
    static bool initialized;
    
    static bool persistent;
    static string modelsPath, texturesPath, exportsPath, fontsPath, audioPath;
    
    internal static Scene3D Current3DScene;
    
    
    /// <summary> Event invoked when the EngineEntity is added to a scene </summary>
    public delegate void EngineLoadDelegate(EngineEntity engine, Scene scene);
    /// <summary> Event invoked when the EngineEntity is added to a scene </summary>
    public static event EngineLoadDelegate OnEngineLoad;
    

    public EngineEntity(EntityData data, Vector2 offset) : base(data.Position + offset)
    {
        modelsPath = data.Attr("modelsPath");
        texturesPath = data.Attr("texturesPath");
        exportsPath = data.Attr("exportsPath");
        fontsPath = data.Attr("fontsPath");
        audioPath = data.Attr("audioPath");
        
        EnginePaths.SetAllPathsCtr(modelsPath, texturesPath, exportsPath, fontsPath, audioPath);
        persistent = data.Bool("persistent", true);
        Visible = true;
    }
    
    
    internal EngineEntity(string m, string t, string e, string f, string a, bool p) : base(Vector2.Zero)
    {
        EnginePaths.SetAllPathsCtr(m, t, e, f, a);
        persistent = p;
        Visible = true;
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
        
        instance = this;
        
        if (initialized) return;
        initialized = true;
        
        SetPersistence(persistent);
        
        EngineFonts.LoadDefault();
        
        if (!String.IsNullOrEmpty(EnginePaths.customFontsPath))
            EngineFonts.LoadAllFonts();
        
        ShadersLoader.LoadShaders();
        
        OnEngineLoad?.Invoke(this, scene);
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
                instance.Tag |= Tags.Persistent;
            else
                instance.Tag &= ~Tags.Persistent;
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
        if (instance == this)
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
        
        Logger.Warn("SAVE FLOW", "SAVE ENGINE STATE. Room :" + roomName + " Map: " + mapName);
    }
    
    internal static void CleanEngine()
    {
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