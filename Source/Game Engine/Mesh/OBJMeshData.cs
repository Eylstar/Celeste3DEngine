using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Celeste.Mod.Celeste3DEngine;

internal sealed class OBJMeshData : MeshData, IDisposable
{
    internal VertexBuffer vertBuffer;
    internal static OBJMeshData ParseMesh(Stream stream, bool isExport, string meshName)
    {
        if (stream.CanSeek) stream.Seek(0, SeekOrigin.Begin);
        
        OBJMeshData objMesh = new OBJMeshData();
        
        List<Vector3> positions = new List<Vector3>();
        List<Vector2> texcoords = new List<Vector2>();
        List<Vector3> normals = new List<Vector3>();
        
        List<VertexPositionNormalTexture> verticesWithNormals = new List<VertexPositionNormalTexture>();

        
        // EXPORT FORMAT
        if (isExport)
        {
            using BinaryReader reader = new BinaryReader(stream, Encoding.UTF8);
            
            int vertexCount = reader.ReadInt32();

            for (int i = 0; i < vertexCount; i++)
            {
                float x = reader.ReadSingle();
                float y = reader.ReadSingle();
                float z = reader.ReadSingle();
                float u = reader.ReadSingle();
                float v = reader.ReadSingle();
                float nx = reader.ReadSingle();
                float ny = reader.ReadSingle();
                float nz = reader.ReadSingle();
                
                verticesWithNormals.Add(new VertexPositionNormalTexture
                {
                    Position = new Vector3(x, y, z),
                    TextureCoordinate = new Vector2(u, v),
                    Normal = new Vector3(nx, ny, nz)
                });
                
            }
        }
        
        // OBJ FORMAT
        else
        {
            using StreamReader r = new StreamReader(stream, Encoding.UTF8, true, 4096);

            string line;

            while ((line = r.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                    
                string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                
                if (parts.Length == 0) continue;

                switch (parts[0])
                {
                    case "o":
                        break;
                            
                    case "v":
                        positions.Add(new Vector3(Float(parts[1]), Float(parts[2]), Float(parts[3])));
                        break;
                            
                    case "vt":
                        float u = Float(parts[1]);
                        float v = Float(parts[2]);
                        texcoords.Add(new Vector2(u, 1f - v));
                        break;
                    
                    case "vn":
                        float nx = Float(parts[1]);
                        float ny = Float(parts[2]);
                        float nz = Float(parts[3]);
                        normals.Add(new Vector3(nx, ny, nz));
                        break;
                            
                    case "f":
                    {
                        int vertexCount = parts.Length - 1;
                        
                        if (vertexCount < 3) break;

                        int[] facePos = new int[vertexCount];
                        int[] faceUv = new int[vertexCount];
                        int[] faceNorm = new int[vertexCount];

                        for (int i = 0; i < vertexCount; i++)
                        {
                            string[] comp = parts[i + 1].Split('/');

                            // v
                            facePos[i] = ParseIndex(comp[0], positions.Count);

                            // vt
                            faceUv[i] = -1;
                            if (comp.Length > 1 && !string.IsNullOrEmpty(comp[1]))
                                faceUv[i] = ParseIndex(comp[1], texcoords.Count);

                            // vn
                            faceNorm[i] = -1;
                            if (comp.Length > 2 && !string.IsNullOrEmpty(comp[2]))
                                faceNorm[i] = ParseIndex(comp[2], normals.Count);
                        }
                        
                        VertexPositionNormalTexture MakeVertex(int idx)
                        {
                            return new VertexPositionNormalTexture
                            {
                                Position = positions[facePos[idx]],
                                TextureCoordinate = (faceUv[idx] >= 0 && faceUv[idx] < texcoords.Count) ? 
                                    texcoords[faceUv[idx]] : Vector2.Zero,
                                Normal = (faceNorm[idx] >= 0 && faceNorm[idx] < normals.Count) ? 
                                    normals[faceNorm[idx]] : Vector3.Zero
                            };
                        }

                        for (int i = 1; i < vertexCount - 1; i++)
                        {
                            int i0 = 0;
                            int i1 = i;
                            int i2 = i + 1;
                            
                            verticesWithNormals.Add(MakeVertex(i0));
                            verticesWithNormals.Add(MakeVertex(i1));
                            verticesWithNormals.Add(MakeVertex(i2));
                        }
                        break;
                    }
                }
            }
        }
        if (verticesWithNormals.Count == 0)
            Logger.Error("3DEngine", $"No vertices generated for '{meshName}'. posCount={positions.Count}, uvCount={texcoords.Count}");
        
        bool anyValidNormal = false;
        for (int i = 0; i < verticesWithNormals.Count; i++)
        {
            if (verticesWithNormals[i].Normal.LengthSquared() > 1e-12f)
            {
                anyValidNormal = true;
                break;
            }
        }

        if (!anyValidNormal)
            ComputeFlatNormals(verticesWithNormals);
        
        // Compute bounding sphere radius
        Vector3 center = Vector3.Zero;
        foreach (var v in verticesWithNormals)
            center += v.Position;
        center /= verticesWithNormals.Count;

        float maxDistSq = 0f;
        foreach (var v in verticesWithNormals)
        {
            float dSq = Vector3.DistanceSquared(v.Position, center);
            if (dSq > maxDistSq) maxDistSq = dSq;
        }
        objMesh.boundingSphereRadius = MathF.Sqrt(maxDistSq);
        
        
        objMesh.verts = verticesWithNormals.ToArray();
        objMesh.ResetVertexBuffer();
        return objMesh;
    }
    
    public static OBJMeshData CreateFromStream(Stream stream, string fileName)
    {
        bool isExport = fileName.EndsWith(".export.dat", StringComparison.OrdinalIgnoreCase);
        string meshName = Path.GetFileNameWithoutExtension(fileName);
        return ParseMesh(stream, isExport, meshName);
    }
    

    internal override void DrawWithSetup(Effect effect, Action<Texture2D> onBeforePrimitive)
    {
        onBeforePrimitive(null);
        if (vertBuffer == null || vertBuffer.IsDisposed || vertBuffer.GraphicsDevice.IsDisposed) return;
        
        Engine.Graphics.GraphicsDevice.SetVertexBuffer(vertBuffer);
        foreach (EffectPass e in effect.CurrentTechnique.Passes)
        {
            e.Apply();
            Engine.Graphics.GraphicsDevice.DrawPrimitives(PrimitiveType.TriangleList, 0, vertBuffer.VertexCount / 3);
        }
    }

    bool ResetVertexBuffer()
    {
        if (vertBuffer != null && !vertBuffer.IsDisposed && !vertBuffer.GraphicsDevice.IsDisposed) return false;
        
        vertBuffer = new VertexBuffer(Engine.Graphics.GraphicsDevice, typeof (VertexPositionNormalTexture), verts.Length, BufferUsage.None);
        vertBuffer?.SetData(verts);
        return true;
    }

    public void ReassignVertices()
    {
        if (ResetVertexBuffer()) return;
        vertBuffer?.SetData(verts);
    }

    public override void Dispose()
    {
        vertBuffer?.Dispose();
    }
    
    static float Float(string data)
    {
        return float.Parse(data, CultureInfo.InvariantCulture);
    }
    
    static int ParseIndex(string s, int count)
    {
        int idx = int.Parse(s);
        if (idx > 0)
            return idx - 1;
        else
            return count + idx;
    }
    
    static void ComputeFlatNormals(List<VertexPositionNormalTexture> verts)
    {
        for (int i = 0; i + 2 < verts.Count; i += 3)
        {
            Vector3 p0 = verts[i + 0].Position;
            Vector3 p1 = verts[i + 1].Position;
            Vector3 p2 = verts[i + 2].Position;

            Vector3 e1 = p1 - p0;
            Vector3 e2 = p2 - p0;

            Vector3 n = Vector3.Cross(e1, e2);

            if (n.LengthSquared() < 1e-12f)
                n = Vector3.Up;
            else
                n.Normalize();

            VertexPositionNormalTexture v0 = verts[i + 0]; 
            v0.Normal = n; 
            verts[i + 0] = v0;
            VertexPositionNormalTexture v1 = verts[i + 1]; 
            v1.Normal = n; 
            verts[i + 1] = v1;
            VertexPositionNormalTexture v2 = verts[i + 2]; 
            v2.Normal = n; 
            verts[i + 2] = v2;
        }
    }
}