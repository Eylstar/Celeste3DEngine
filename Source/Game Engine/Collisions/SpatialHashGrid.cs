using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Celeste3DEngine;

internal sealed class SpatialHashGrid<T>
{
    public readonly float cellSize;
    public readonly Dictionary<(int x, int y, int z), HashSet<T>> cells = new();
    
    public SpatialHashGrid(float cellSize)
    {
        this.cellSize = cellSize;
    }
    
    // Clears all cells from the spatial hash grid 
    internal void Clear() => cells.Clear();
    
    
    // Inserts an item into the cell at the specified key
    internal void InsertAtCell((int x,int y,int z) key, T item)
    {
        // If the cell does not exist yet, create it first
        if (!cells.TryGetValue(key, out var set))
        {
            set = new HashSet<T>();
            cells[key] = set;
        }
        // Add the item to the cell's set of items
        set.Add(item);
    }
    
    // Removes an item from the cell at the specified key
    internal void RemoveFromCell((int x, int y, int z) key, T item)
    {
        if (!cells.TryGetValue(key, out var set)) return;

        set.Remove(item);

        // If the cell is now empty, remove it from the dictionary to save memory
        if (set.Count == 0) cells.Remove(key);
    }
    
    
    // Queries all cells that intersect with the sphere defined by position and radius and adds all found items to results
    internal void Query(Vector3 position, float radius, HashSet<T> results, bool additive = false)
    {
        if (!additive) results.Clear();
        
        // Determine the range of cells to check based on the position and radius of the sphere
        GetCellRangeBySphere(position, radius, out int minX, out int maxX, out int minY, out int maxY, out int minZ, out int maxZ);

        // Iterate through the relevant cells and collect items found in them
        for (int x = minX; x <= maxX; x++)
        for (int y = minY; y <= maxY; y++)
        for (int z = minZ; z <= maxZ; z++)
        {
            // Check if the cell exists and add its items to the results
            AddCellContents(x, y, z, results);
        }
    }
    
    
    
    
    // Calculates the range of cell coordinates that a sphere intersects with
    internal void GetCellRangeBySphere(Vector3 position, float radius, out int minX, out int maxX, out int minY, out int maxY, out int minZ, out int maxZ)
    {
        // Use inverse multiplication for efficiency instead of division
        float inv = 1f / cellSize;

        // Calculate the min and max cell coordinates in each dimension that the sphere intersects with
        minX = (int)MathF.Floor((position.X - radius) * inv);
        maxX = (int)MathF.Floor((position.X + radius) * inv);
        minY = (int)MathF.Floor((position.Y - radius) * inv);
        maxY = (int)MathF.Floor((position.Y + radius) * inv);
        minZ = (int)MathF.Floor((position.Z - radius) * inv);
        maxZ = (int)MathF.Floor((position.Z + radius) * inv);
    }
    
    
    internal void QueryRay(Vector3 pos, Vector3 dir, float maxDistance, out HashSet<T> results)
    {
        results = new HashSet<T>();
        
        if(maxDistance <= 0f || dir == Vector3.Zero) return;
        dir = Vector3.Normalize(dir);
        
        (int cellX, int cellY, int cellZ) = GetCellCoords(pos);
        AddCellContents(cellX, cellY, cellZ, results);
       
        int stepX = (dir.X > 0f) ? 1 : ((dir.X < 0f) ? -1 : 0);
        int stepY = (dir.Y > 0f) ? 1 : ((dir.Y < 0f) ? -1 : 0);
        int stepZ = (dir.Z > 0f) ? 1 : ((dir.Z < 0f) ? -1 : 0);
       
        float nextBoundaryX = stepX > 0 ? (cellX + 1) * cellSize : cellX * cellSize;
        float nextBoundaryY = stepY > 0 ? (cellY + 1) * cellSize : cellY * cellSize;
        float nextBoundaryZ = stepZ > 0 ? (cellZ + 1) * cellSize : cellZ * cellSize;
        
        float tMaxX = stepX != 0 ? (nextBoundaryX - pos.X) / dir.X : float.PositiveInfinity;
        float tMaxY = stepY != 0 ? (nextBoundaryY - pos.Y) / dir.Y : float.PositiveInfinity;
        float tMaxZ = stepZ != 0 ? (nextBoundaryZ - pos.Z) / dir.Z : float.PositiveInfinity;
        
        float tDeltaX = stepX != 0 ? cellSize / Math.Abs(dir.X) : float.PositiveInfinity;
        float tDeltaY = stepY != 0 ? cellSize / Math.Abs(dir.Y) : float.PositiveInfinity;
        float tDeltaZ = stepZ != 0 ? cellSize / Math.Abs(dir.Z) : float.PositiveInfinity;
        
        if (tMaxX < 0f) tMaxX = 0f;
        if (tMaxY < 0f) tMaxY = 0f;
        if (tMaxZ < 0f) tMaxZ = 0f;
        
        const float epsilon = 0.00001f;
        
        while (true)
        {
            float tNext = MathF.Min(tMaxX, MathF.Min(tMaxY, tMaxZ));
            if (tNext > maxDistance + epsilon) break;
            
            bool stepAlongX = MathF.Abs(tMaxX - tNext) < epsilon;
            bool stepAlongY = MathF.Abs(tMaxY - tNext) < epsilon;
            bool stepAlongZ = MathF.Abs(tMaxZ - tNext) < epsilon;
            
            if (stepAlongX) 
            {
                cellX += stepX;
                tMaxX += tDeltaX;
            }
            if (stepAlongY) 
            {
                cellY += stepY;
                tMaxY += tDeltaY;
            }
            if (stepAlongZ) 
            {
                cellZ += stepZ;
                tMaxZ += tDeltaZ;
            }

            AddCellContents(cellX, cellY, cellZ, results);
        }
    }
    

    void AddCellContents(int x, int y, int z, HashSet<T> results)
    {
        var key = (x, y, z);
        if (!cells.TryGetValue(key, out var list))
            return;

        foreach (T item in list)
            results.Add(item);
    }
    
    (int x, int y, int z) GetCellCoords(Vector3 pos)
    {
        return (
            WorldToCell(pos.X),
            WorldToCell(pos.Y),
            WorldToCell(pos.Z)
        );
    }
    
    int WorldToCell(float value)
    {
        return (int)MathF.Floor(value / cellSize);
    }
}