using System;
using System.IO;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace Celeste.Mod.Celeste3DEngine;

/// <summary> Generates an optimised Export file from an OBJ ModAsset </summary>
public sealed class ExportGenerator
{
    /// <summary> Converts the given OBJ ModAsset into a custom .export.dat file in the mod's exports folder (only works in non zipped mod) </summary>
    public static void ConvertObjToExport(ModAsset asset)
    {
        if (asset == null) return;
        
        string modDir = asset.Source.Mod.PathDirectory;
        
        if (String.IsNullOrEmpty(modDir))
        {
            Logger.Warn("ExportGenerator", $"Could not export asset '{asset.PathVirtual}' because its mod is in a zip file. Exporting only works for unzipped mods.");
            return;
        }

        if (String.IsNullOrEmpty(EnginePaths.CustomExportsPath))
        {
            Logger.Warn("ExportGenerator", $"Could not export asset '{asset.PathVirtual}' because the Custom Exports path is not set. Please set EnginePaths.CustomExportsPath to a valid path relative to the mod folder.");
            return;
        }
        
        Logger.Info("ExportGenerator", $"Exporting OBJ asset '{asset.PathVirtual}' to custom .export.dat format.");
        

        OBJMeshData temp;
        using (Stream s = asset.Stream)
            temp = OBJMeshData.CreateFromStream(s, asset.PathVirtual);
        
        string virtualPath = asset.PathVirtual;
        string fileName = Path.GetFileNameWithoutExtension(virtualPath);
        string exportDir = Path.Combine(modDir, EnginePaths.CustomExportsPath);
        string exportPath = Path.Combine(exportDir, fileName + ".export.dat");
        
        Directory.CreateDirectory(exportDir);
        
        using (FileStream fs = File.Create(exportPath))
        {
            using (BinaryWriter writer = new BinaryWriter(fs, Encoding.UTF8))
            {
                VertexPositionNormalTexture[] verts = temp.verts;
                writer.Write(verts.Length);
                
                for (int i = 0; i < verts.Length; i++)
                {
                    writer.Write(verts[i].Position.X);
                    writer.Write(verts[i].Position.Y);
                    writer.Write(verts[i].Position.Z);
                    writer.Write(verts[i].TextureCoordinate.X);
                    writer.Write(verts[i].TextureCoordinate.Y);
                    writer.Write(verts[i].Normal.X);
                    writer.Write(verts[i].Normal.Y);
                    writer.Write(verts[i].Normal.Z);
                }
            }
        }
        
        temp.Dispose();
    }
}