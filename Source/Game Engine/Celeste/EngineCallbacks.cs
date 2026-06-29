using System;
using System.Collections.Generic;
using Monocle;

namespace Celeste.Mod.Celeste3DEngine;

/// <summary> Class that manages callbacks for the EngineEntity. Use it for enabling the engine to load your entity functions even if it's not in the room. Needs to be registered at least once first. </summary>
public static class EngineCallbacks
{
    static Dictionary<string, Action<EngineEntity, Level>> callbacks = new();

    public static void RegisterCallback(string id, Action<EngineEntity, Level> callback) => callbacks[id] = callback;

    public static bool TryGetCallback(string id, out Action<EngineEntity, Level> callback) => callbacks.TryGetValue(id, out callback);
    
    internal static void ClearCallbacks() => callbacks.Clear();
    
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