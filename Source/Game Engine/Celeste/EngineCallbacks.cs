using System;
using System.Collections.Generic;
using Monocle;

namespace Celeste.Mod.Celeste3DEngine;

/// <summary> Class that manages callbacks for the EngineEntity. Use it for enabling the engine to load your entity functions even if it's not in the room. Needs to be registered at least once first. </summary>
public static class EngineCallbacks
{
    static Dictionary<string, Action<EngineEntity, Scene>> callbacks = new();

    /// <summary> Maps a callback ID to a function, call this unconditionally every time your mod loads ///</summary>
    public static void RegisterCallback(string id, Action<EngineEntity, Scene> callback) => callbacks[id] = callback;

    internal static bool TryGetCallback(string id, out Action<EngineEntity, Scene> callback) => callbacks.TryGetValue(id, out callback);
    
    internal static void ClearCallbacks() => callbacks.Clear();
    
    /// <summary> Arms the given callback ID to be invoked if this map's Engine needs to respawn later ///</summary>
    public static void CreateCallbackID(Scene scene, string id) 
    {
        Level level = scene as Level;
        if (level == null) return;

        Celeste3DEngineModuleSaveData s = Celeste3DEngineModule.SaveData;
        Celeste3DEngineModuleSaveData.MapSaveData mapData = s.mapSaveDataList.Find(m => m.mapName == level.Session.Area.SID);
        
        if (mapData == null)
        {
            mapData = new Celeste3DEngineModuleSaveData.MapSaveData()
            {
                mapName = level.Session.Area.SID
            };
            s.mapSaveDataList.Add(mapData);
        }
        mapData.needToCallBack = true;
        mapData.callBackID = id;
    }
}