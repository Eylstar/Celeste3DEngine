using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Celeste3DEngine;

internal class CollisionSystem
{
    float hashCellSize = 10f;
    internal SpatialHashGrid<Collider3D> colliderSpatialHashGrid;

    // Lists of colliders and collision objects in the scene
    HashSet<Collider3D> colliders = new HashSet<Collider3D>();
    HashSet<GameObject> collisionObjects = new HashSet<GameObject>();

    // Set of active colliders for debug drawing and collision checks
    HashSet<Collider3D> activeColliders = new HashSet<Collider3D>();
    
    // Temporary set to hold colliders to check for each collision object
    HashSet<Collider3D> collidersToCheck = new(256);
    
    // Set of dirty colliders that need to be updated in the spatial hash grid
    HashSet<Collider3D> dirtyColliders = new HashSet<Collider3D>();
    
    // Mapping of colliders to the spatial hash grid cells they occupy
    Dictionary<Collider3D, List<(int x, int y, int z)>> colliderToCells = new();
    
    // Temporary set to track colliders seen this frame (for trigger exit handling)
    HashSet<Collider3D> seenThisFrame = new HashSet<Collider3D>();
    
    //List of stored raycasts for debug drawing
    List<Ray> storedRayCasts = new List<Ray>();
    List<RaycastHit> storedRayCastHits = new List<RaycastHit>();


    // Initialize the collision system with the given scene
    internal void Initialize(Scene3D s)
    {
        // Initialize spatial hash grid for colliders
        colliderSpatialHashGrid = new SpatialHashGrid<Collider3D>(hashCellSize);

        InitColliderList(s);
        InitCollidingList(s);
        BuildSpatialHashGrid();
    }

    // Populate the collider list from all GameObjects in the scene that have colliders attached
    internal void InitColliderList(Scene3D s)
    {
        colliders.Clear();
        foreach (GameObject go in s.GetGameObjectsList())
        {
            if (go.colliders != null && go.colliders.Count > 0)
            {
                foreach (Collider3D col in go.colliders)
                {
                    colliders.Add(col);
                    if (col is BoxCollider3D)
                    {
                        ((BoxCollider3D)col).BuildCache();
                    }
                }
            }
        }
    }

    // Populate the colliding objects list from all GameObjects in the scene that have collision detectors attached
    internal void InitCollidingList(Scene3D s)
    {
        collisionObjects.Clear();
        foreach (GameObject go in s.GetGameObjectsList())
        {
            if (go.collisionDetectors != null && go.collisionDetectors.Count > 0)
                collisionObjects.Add(go);
        }
    }

    // Build the spatial hash grid with the current colliders in the scene
    void BuildSpatialHashGrid()
    {
        colliderSpatialHashGrid.Clear();
        colliderToCells.Clear();

        foreach (Collider3D col in colliders)
        {
            if (col == null || !col.enabled) continue;
            
            if (col is BoxCollider3D box) box.BuildCache();
            UpdateColliderCells(col);
        }
    }
    
    
    // Update the spatial hash grid cells occupied by a given collider
    void UpdateColliderCells(Collider3D col)
    {
        if (!colliderToCells.TryGetValue(col, out var cells))
        {
            cells = new List<(int, int, int)>(8);
            colliderToCells[col] = cells;
        }
        else
            RemoveFromGrid(col, cells);

        // Get the broadphase radius of the collider for cell calculation
        float r = col.GetBroadphaseRadius();
        
        // Compute new cells for the collider
        colliderSpatialHashGrid.GetCellRangeBySphere(col.WorldCenter, r, out int minX, out int maxX, out int minY, 
            out int maxY, out int minZ, out int maxZ);

        // Insert collider into new cells 
        for (int x = minX; x <= maxX; x++)
        for (int y = minY; y <= maxY; y++)
        for (int z = minZ; z <= maxZ; z++)
        {
            var key = (x, y, z);
            colliderSpatialHashGrid.InsertAtCell(key, col);
            
            // Keep track of which cells the collider occupies for future removal
            cells.Add(key);
        }
    }
    
    // Called each frame before the collision resolve, remove all dirty colliders from the spatial hash grid and reinsert them based on their current position
    internal void FlushDirtyColliders()
    {
        foreach (Collider3D col in dirtyColliders)
        {
            if (col == null) continue;

            if (!col.enabled)
            {
                // If collider is disabled, remove it from the grid
                RemoveFromGrid(col);
                continue;
            }

            // Rebuild data cache for BoxColliders if needed 
            if (col is BoxCollider3D box) box.BuildCache();
            
            // Update collider's cells in the spatial hash grid
            UpdateColliderCells(col);
        }

        dirtyColliders.Clear();
    }
    
    // Remove a collider from the spatial hash grid 
    void RemoveFromGrid(Collider3D col)
    {
        // If collider is not in any cells, nothing to remove
        if (!colliderToCells.TryGetValue(col, out var cells)) return;

        // Remove collider from all occupied cells in the spatial hash grid
        foreach (var cell in cells)
            colliderSpatialHashGrid.RemoveFromCell(cell, col);

        // Clear the list of cells for this collider
        cells.Clear();
    }
    
    // Remove a collider from the spatial hash grid given its occupied cells
    void RemoveFromGrid(Collider3D col, List<(int,int,int)> cells)
    {
        foreach (var cell in cells)
            colliderSpatialHashGrid.RemoveFromCell(cell, col);
        cells.Clear();
    }
    

    // Methods to add/remove colliders and collision objects
    internal void AddCollider(Collider3D col)
    {
        if (col == null) return;
        colliders.Add(col);
        MarkColliderDirty(col);
    }

    internal void RemoveCollider(Collider3D col)
    {
        if (col == null) return;

        // If removing a trigger collider, notify all collision detectors that were inside it last frame
        if (col.isTrigger)
        {
            foreach (GameObject obj in collisionObjects)
            {
                // Skip destroyed objects and self-collisions
                if (obj == null || obj.destroyed || col.gameObject == obj) continue;
                
                foreach (CollisionDetector detector in obj.collisionDetectors)
                {
                    // If the detector was inside the trigger last frame, invoke exit events
                    if (detector.WasTriggerLastFrame(col))
                    {
                        col.onTriggerExit?.Invoke(detector);
                        detector.onTriggerExit?.Invoke(col);
                        detector.SetTriggerStateLastFrame(col, false);
                    }
                }
            }
        }

        RemoveFromGrid(col);
        colliders.Remove(col);
        dirtyColliders.Remove(col);
        colliderToCells.Remove(col);
    }
    

    internal void AddCollisionObject(GameObject obj)
    {
        if (obj == null) return;
        collisionObjects.Add(obj);
    }

    internal void RemoveCollisionObject(GameObject obj)
    {
        if (obj == null) return;

        // If removing a collision object, notify all trigger colliders that were inside its detectors last frames
        foreach (CollisionDetector det in obj.collisionDetectors)
        {
            // For each trigger detector, notify all colliders that were inside it last frame
            foreach (var kv in det.colliderTriggerStateLastFrame)
            {
                Collider3D col = kv.Key;
                bool wasInside = kv.Value;

                if (wasInside && col != null)
                {
                    col.onTriggerExit?.Invoke(det);
                    det.onTriggerExit?.Invoke(col);
                    det.SetTriggerStateLastFrame(col, false);
                }
            }
        }

        collisionObjects.Remove(obj);
    }

    // Mark a collider as dirty so it will be updated in the spatial hash grid
    internal void MarkColliderDirty(Collider3D col)
    {
        if (col == null) return;
        dirtyColliders.Add(col);
    }
    
    
    // Resolve collisions between colliders and collision detectors each frame
    internal void ResolveCollisions()
    {
        // Clear active colliders from the previous frame to draw debug shapes only for currently active ones
        foreach (Collider3D col in activeColliders) 
            col.isDistanceActive = false;
        
        activeColliders.Clear();
        
        // Loop through each GameObject with collision detectors and check for collisions
        foreach (GameObject obj in collisionObjects.ToList())
        {
            if (obj == null || obj.destroyed) continue;
            
            // Clear the list of colliders to check for this object
            collidersToCheck.Clear();

            // Populate collidersToCheck with colliders within range of the detector position
            foreach (CollisionDetector detector in obj.collisionDetectors.Where(det => det.enabled))
            {
                if (obj.destroyed) break;
                colliderSpatialHashGrid.Query(detector.position, detector.distanceToCheck, collidersToCheck, true);
            }

            // Check collisions for each detector against the colliders found in range
            foreach (CollisionDetector detector in obj.collisionDetectors.Where(detector => detector.enabled))
            {
                if (obj.destroyed) break;
                float distanceToCheck = detector.distanceToCheck;
                float distanceToCheckCollisionSquared = distanceToCheck * distanceToCheck;
                
                seenThisFrame.Clear();
                
                foreach (Collider3D col in collidersToCheck)
                {
                    if (obj.destroyed) break;
                    if (col == null || (col.gameObject != null && col.gameObject.destroyed)) continue;
                    
                    // Don't check collision with itself
                    if (col.gameObject == obj) continue;
                    
                    // Check if within distance to check
                    if (col.GetPointDistanceSquared(detector.position) > distanceToCheckCollisionSquared) continue;
                    
                    // Mark collider as active for this frame and add to active colliders list for debug drawing
                    col.isDistanceActive = true;
                    activeColliders.Add(col);

                    // Perform collision evaluation
                    if (!col.isTrigger)
                    {
                        // Move the object out of collision depending on his penetration with the collider
                        Vector3 probePos = detector.position;
                        Vector3 prevPos = detector.gameObject.positionLastFrame + Vector3.Transform(detector.offset * detector.gameObject.transform.scale, detector.gameObject.transform.rotation);
                        
                        Vector3 originalProbePos = probePos;
                        col.CheckCollision(ref probePos, prevPos);
                        Vector3 correction = probePos - originalProbePos;
                        
                        detector.gameObject.transform.MoveBy(correction);

                        if (correction.LengthSquared() > 0f)
                        {
                            col.onCollision?.Invoke(detector);
                            detector.onCollision?.Invoke(col);
                        }

                    }
                    
                    // Trigger collision handling
                    else
                    {
                        seenThisFrame.Add(col);
                        if (col.CheckOverlap(detector.position))
                        {
                            // Inside trigger
                            if (detector.WasTriggerLastFrame(col))
                            {
                                col.onTriggerStay?.Invoke(detector);
                                detector.onTriggerStay?.Invoke(col);
                            }
                            // Entered trigger
                            else
                            {
                                col.onTriggerEnter?.Invoke(detector);
                                detector.onTriggerEnter?.Invoke(col);
                                detector.SetTriggerStateLastFrame(col, true);
                            }
                        }
                        // Outside trigger
                        else
                        {
                            if (detector.WasTriggerLastFrame(col))
                            {
                                col.onTriggerExit?.Invoke(detector);
                                detector.onTriggerExit?.Invoke(col);
                                detector.SetTriggerStateLastFrame(col, false);
                            }
                        }
                    }
                }
                
                if (obj.destroyed) break;
                
                // Handle trigger exits for colliders not seen this frame
                List<Collider3D> toExit = null;
                foreach (var kv in detector.colliderTriggerStateLastFrame)
                {
                    Collider3D col = kv.Key;
                    bool wasInside = kv.Value;
                    
                    if (wasInside && col != null && !seenThisFrame.Contains(col))
                    {
                        // Mark for exit after the loop to avoid modifying the dictionary while iterating
                        toExit ??= new List<Collider3D>();
                        toExit.Add(col);
                    }
                }

                if (toExit == null) continue;
                
                foreach (Collider3D col in toExit)
                {
                    col.onTriggerExit?.Invoke(detector);
                    detector.onTriggerExit?.Invoke(col);
                    detector.SetTriggerStateLastFrame(col, false);
                }
            }
        }
    }
    
    // Draw debug shapes for active colliders in the scene
    internal void DrawDebugColliders(Camera3D cam, bool onlyActive)
    {
        if(onlyActive)
        {
            foreach (Collider3D col in activeColliders)
            {
                if (col == null || col.gameObject == null || col.gameObject.destroyed) continue;
                if (col.isDistanceActive)
                    col.DrawShapeDebug(cam);
            }
        }
        else
        {
            foreach (Collider3D col in colliders)
            {
                if (col == null || col.gameObject == null || col.gameObject.destroyed) continue;
                col.DrawShapeDebug(cam);
            }
        }

        foreach (GameObject colliding in collisionObjects)
        {
            foreach (CollisionDetector detector in colliding.collisionDetectors)
            {
                if (detector.debugDrawCenter)
                    DebugDrawShapes3D.DrawSphere(detector.position, 0.1f, cam, Color.Green);
                
                if (detector.debugDrawDistance)
                    DebugDrawShapes3D.DrawSphere(detector.position, detector.distanceToCheck, cam, Color.DarkSeaGreen);
            }
        }
    }

    internal void ClearRayCasts()
    { 
        storedRayCasts.Clear();
        storedRayCastHits.Clear();
    }
    
    internal void DrawDebugRayCasts(Camera3D cam)
    {
        foreach (Ray ray in storedRayCasts)
        {
            DebugDrawShapes3D.DrawLine(ray.Origin, ray.Origin + ray.Direction * ray.MaxDistance, cam, Color.Red);
            DebugDrawShapes3D.DrawSphere(ray.Origin, 0.05f, cam, Color.Red);
        }
        
        foreach (RaycastHit hit in storedRayCastHits)
        {
            DebugDrawShapes3D.DrawSphere(hit.Point, 0.1f, cam, Color.Yellow);
            DebugDrawShapes3D.DrawLine(hit.Point, hit.Point + hit.Normal, cam, Color.Orange);
        }
    }
    
    
    internal bool FindCastHit(Vector3 origin, Vector3 direction, float maxDistance, out RaycastHit hitInfo, bool ignoreTriggers = true)
    {
        hitInfo = null;

        // Query colliders along the ray path using the spatial hash grid
        colliderSpatialHashGrid.QueryRay(origin, direction, maxDistance, out HashSet<Collider3D> potentialColliders);
        
        storedRayCasts.Add(new Ray(origin, direction, maxDistance));

        foreach (Collider3D col in potentialColliders)
        {
            if (col == null || (col.gameObject != null && col.gameObject.destroyed) || !col.enabled) continue;
            if (ignoreTriggers && col.isTrigger) continue;

            if (col.RayCast(origin, direction, maxDistance, out RaycastHit hit) && hit != null)
            {
                if (hit.Distance < maxDistance)
                {
                    maxDistance = hit.Distance;
                    hitInfo = hit;
                }
            }
        }
        
        if (hitInfo != null)
        {
            // Store the raycast hit for debug drawing
            storedRayCastHits.Add(hitInfo);
        }
        
        return hitInfo != null;
    }


    // Unload the collision system and clear all references to colliders and collision objects in the scene
    internal void UnloadCollisionSystem()
    {
        foreach (Collider3D col in colliders)
        {
            col.onTriggerEnter = null;
            col.onTriggerStay = null;
            col.onTriggerExit = null;
            col.onCollision = null;
        }
        colliders.Clear();

        foreach (GameObject colliding in collisionObjects)
        {
            foreach (CollisionDetector detector in colliding.collisionDetectors)
            {
                detector.onTriggerEnter = null;
                detector.onTriggerStay = null;
                detector.onTriggerExit = null;
                detector.onCollision = null;
            }
        }
        collisionObjects.Clear();
        
        activeColliders.Clear();
        collidersToCheck.Clear();
        colliderSpatialHashGrid.Clear();
        colliderToCells.Clear();
    }
}