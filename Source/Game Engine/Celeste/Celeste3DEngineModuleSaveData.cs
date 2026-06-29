using System;
using System.Collections.Generic;

namespace Celeste.Mod.Celeste3DEngine;

public class Celeste3DEngineModuleSaveData : EverestModuleSaveData
{
    public List<MapSaveData> mapSaveDataList { get; set; } = new List<MapSaveData>();
    
    [Serializable]
    public class MapSaveData
    {
        public string mapName { get; set; } = "";
        public string leftRoom { get; set; } = "";
        
        public bool needToRespawnEngine { get; set; } = false;
        public bool needToCallBack { get; set; } = false;
        public string callBackID { get; set; } = "";
        
        public string modelPath { get; set; } = "";
        public string texturePath { get; set; } = "";
        public string exportPath { get; set; } = "";
        public string fontPath { get; set; } = "";
        public string audioPath { get; set; } = "";
        public bool persistentEngine { get; set; } = false;
    }
}