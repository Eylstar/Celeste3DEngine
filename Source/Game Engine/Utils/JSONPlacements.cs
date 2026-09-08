using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;

using System.Text.Json;

namespace Celeste.Mod.Celeste3DEngine;

public static class JSONPlacements
{
    public static List<(string meshName, Transform transform)> GetPlacements(string path)
    {
        if (!Everest.Content.TryGet(path, out ModAsset asset))
        {
            Logger.Error("Celeste3DEngine", $"Failed to load placements from '{path}'");
            return null;
        }

        Logger.Info("Celeste3DEngine", $"Loading placements from '{path}'");

        List<Placement> placements;

        using (Stream stream = asset.Stream)
            placements = JsonSerializer.Deserialize<List<Placement>>(stream);
        
        List<(string meshName, Transform transform)> results = new();

        foreach (Placement p in placements)
        {
            Vector3 position = new Vector3(p.position[0], p.position[1], p.position[2]);
            Quaternion quaternion = new Quaternion(p.rotation[0], p.rotation[1], p.rotation[2], p.rotation[3]);
            Vector3 scale = new Vector3(p.scale[0], p.scale[1], p.scale[2]);
            
            Transform t = new Transform(position, quaternion, scale);
            results.Add((p.model, t));
        }
        
        return results;
    }
}

internal class Placement
{
    public string model { get; set; }
    public float[] position { get; set; }
    public float[] rotation { get; set; }
    public float[] scale { get; set; }
}