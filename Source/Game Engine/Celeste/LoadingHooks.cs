using System;

namespace Celeste.Mod.Celeste3DEngine;

// Class used to Reload the save Engine state if it was persistent
internal class LoadingHooks
{
    internal static void ON_Level_LoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader)
    {
        orig(self, playerIntro, isFromLoader);
        EnsureEngine(self);
    }
    
    static void EnsureEngine(Level level) 
    {
        EngineEntity engine = level.Tracker.GetEntity<EngineEntity>();
        Celeste3DEngineModuleSaveData saveData = Celeste3DEngineModule.SaveData;
        
        // If an engine exists, don't do anything
        if (engine != null || saveData == null) return;
        
        string mapName = level.Session.Area.SID;
        string roomName = level.Session.Level;
        
        Celeste3DEngineModuleSaveData.MapSaveData saveDataForMap = saveData.mapSaveDataList.Find(m => m.mapName == mapName);

        // If there is no save data for this map, don't do anything
        if (saveDataForMap == null) return;

        if (saveDataForMap.needToRespawnEngine && saveDataForMap.leftRoom == roomName)
        {
            // Respawn the engine with the saved data
            saveDataForMap.needToRespawnEngine = false;
            engine = new EngineEntity(saveDataForMap.modelPath, saveDataForMap.texturePath, saveDataForMap.exportPath, saveDataForMap.fontPath, saveDataForMap.audioPath, saveDataForMap.persistentEngine);
            level.Add(engine);
            
            // If a callback was set by the mapper, call it and reset the flag
            if (saveDataForMap.needToCallBack)
            {
                if (EngineCallbacks.TryGetCallback(saveDataForMap.callBackID, out Action<EngineEntity, Level> callback))
                {
                    saveDataForMap.needToCallBack = false;
                    callback.Invoke(engine, level);
                }
                else
                    Logger.Warn("SAVE FLOW", $"Unknown callbackId: {saveDataForMap.callBackID}");
            }
        }
    }
}