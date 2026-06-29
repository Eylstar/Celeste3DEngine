using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Celeste.Mod.Celeste3DEngine;

/// <summary> Represents a 2D heightmap for simple floor collision and sampling heights </summary>
public class FloorHeightMap
{
    internal float[,] heights;
    internal float cellSize;
    internal Vector2 origin;
    internal int width;
    internal int height;

    /// <summary> Samples the heightmap at the given world coordinates (x, y). Returns NegativeInfinity if out of bounds. </summary>
    public float Sample(float x, float y)
    {
        float fx = (x - origin.X) / cellSize;
        float fy = (y - origin.Y) / cellSize;
        
        int ix = (int)Math.Floor(fx);
        int iy = (int)Math.Floor(fy);

        if (ix < 0 || iy < 0 || ix >= width - 1 || iy >= height - 1)
        {
            return float.NegativeInfinity;
        }
        
        float tx = fx - ix;
        float ty = fy - iy;
        
        float h00 = heights[ix, iy];
        float h10 = heights[ix + 1, iy];
        float h01 = heights[ix, iy + 1];
        float h11 = heights[ix + 1, iy + 1];
        
        float h0 = MathHelper.Lerp(h00, h10, tx);
        float h1 = MathHelper.Lerp(h01, h11, tx);
        return MathHelper.Lerp(h0, h1, ty);
    }
    
    /// <summary> Builds a heightmap from the given mesh. </summary>
    public static FloorHeightMap BuildHeightmapFromMesh(GameObject obj, int resolutionX = 256, int resolutionZ = 256) 
    {
        MeshData objMesh = obj?.GetComponent<MeshRenderer>()?.GetModel()?.GetMesh();
        
        if (objMesh == null) return null;
        
        VertexPositionNormalTexture[] verts = objMesh.verts;

        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float minZ = float.PositiveInfinity;
        float maxZ = float.NegativeInfinity;

        foreach (VertexPositionNormalTexture v in verts)
        {
            Vector3 p = v.Position;
            if (p.X < minX) minX = p.X;
            if (p.X > maxX) maxX = p.X;
            if (p.Z < minZ) minZ = p.Z;
            if (p.Z > maxZ) maxZ = p.Z;
        }

        float sizeX = maxX - minX;
        float sizeZ = maxZ - minZ;
        
        int w = resolutionX;
        int h = resolutionZ;

        float cellSizeX = sizeX / (w - 1);
        float cellSizeZ = sizeZ / (h - 1);
        float cellSize  = Math.Max(cellSizeX, cellSizeZ);
        
        float[,] heights = new float[w, h];

        for (int ix = 0; ix < w; ix++)
            for (int iz = 0; iz < h; iz++)
                heights[ix, iz] = float.NegativeInfinity;

        for (int i = 0; i < verts.Length; i += 3) 
        {
            Vector3 v0 = verts[i].Position;
            Vector3 v1 = verts[i + 1].Position;
            Vector3 v2 = verts[i + 2].Position;
            
            float triMaxY = Math.Max(v0.Y, Math.Max(v1.Y, v2.Y));

            float minTX = Math.Min(v0.X, Math.Min(v1.X, v2.X));
            float maxTX = Math.Max(v0.X, Math.Max(v1.X, v2.X));
            float minTZ = Math.Min(v0.Z, Math.Min(v1.Z, v2.Z));
            float maxTZ = Math.Max(v0.Z, Math.Max(v1.Z, v2.Z));

            int minIx = (int)((minTX - minX) / cellSize);
            int maxIx = (int)((maxTX - minX) / cellSize);
            int minIz = (int)((minTZ - minZ) / cellSize);
            int maxIz = (int)((maxTZ - minZ) / cellSize);

            minIx = Math.Clamp(minIx, 0, w - 1);
            maxIx = Math.Clamp(maxIx, 0, w - 1);
            minIz = Math.Clamp(minIz, 0, h - 1);
            maxIz = Math.Clamp(maxIz, 0, h - 1);

            for (int ix = minIx; ix <= maxIx; ix++) 
            {
                for (int iz = minIz; iz <= maxIz; iz++) 
                {
                    float x = minX + ix * cellSize;
                    float z = minZ + iz * cellSize;
                    
                    if (TryProjectionOnTriangle(x, z, v0, v1, v2, out float y)) 
                    {
                        if (y > heights[ix, iz]) 
                        {
                            heights[ix, iz] = y;
                        }
                    }
                }
            }
        }
        return new FloorHeightMap(heights, cellSize, new Vector2(minX, minZ));
    }
    
    internal FloorHeightMap(float[,] h, float size, Vector2 orig)
    {
        heights = h;
        cellSize = size;
        origin = orig;
        width = heights.GetLength(0);
        height = heights.GetLength(1);
    }

    static bool TryProjectionOnTriangle(float x, float z, Vector3 v0, Vector3 v1, Vector3 v2, out float y)
    {
        Vector2 p = new Vector2(x, z);
        Vector2 a = new Vector2(v0.X, v0.Z);
        Vector2 b = new Vector2(v1.X, v1.Z);
        Vector2 c = new Vector2(v2.X, v2.Z);
        
        Vector2 v0v1 = b - a;
        Vector2 v0v2 = c - a;
        Vector2 v0p = p - a;
        
        float d00 = Vector2.Dot(v0v1, v0v1);
        float d01 = Vector2.Dot(v0v1, v0v2);
        float d11 = Vector2.Dot(v0v2, v0v2);
        float d20 = Vector2.Dot(v0p, v0v1);
        float d21 = Vector2.Dot(v0p, v0v2);
        
        float denom = d00 * d11 - d01 * d01;
        if (Math.Abs((denom) ) < 1e-6f)
        {
            y = 0f;
            return false;
        }
        float v = (d11 * d20 - d01 * d21) / denom;
        float w = (d00 * d21 - d01 * d20) / denom;
        float u = 1.0f - v - w;
        
        if (u < 0f || v < 0f || w < 0f) 
        {
            y = 0f;
            return false;
        }
        y = u * v0.Y + v * v1.Y + w * v2.Y;
        return true;
    }
}


