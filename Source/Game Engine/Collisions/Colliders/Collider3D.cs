using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.Celeste3DEngine;

/// <summary> Base class for 3D colliders </summary>
public abstract class Collider3D
{
    /// <summary> Whether this collider is a trigger (does not block movement) </summary>
    public bool isTrigger;
    
    /// <summary> Called when a collision detector collides with this Collider, with the detector as parameter </summary>
    public Action<CollisionDetector> onCollision;
    /// <summary> Called when a collision detector enters this trigger collider, with the detector as parameter </summary>
    public Action<CollisionDetector> onTriggerEnter;
    /// <summary> Called when a collision detector stays in this trigger collider, with the detector as parameter </summary>
    public Action<CollisionDetector> onTriggerStay;
    /// <summary> Called when a collision detector exits this trigger collider, with the detector as parameter </summary>
    public Action<CollisionDetector> onTriggerExit;
    
    /// <summary> The GameObject this collider is attached to </summary>
    public GameObject gameObject;
    
    internal Vector3 center;
    
    /// <summary> Offset from the GameObject's position to the center of this collider </summary>
    public Vector3 CenterOffset 
    {
        get => center;
        set
        {
            center = value;
            if (gameObject != null && gameObject.scene != null)
                gameObject.scene.collisionSystem?.MarkColliderDirty(this);
        }
    }
    
    internal bool isDistanceActive = false;
    
    bool _enabled = true;
    
    /// <summary> Whether this collider is enabled and participates in collision detection </summary>
    public bool enabled 
    {
        get => _enabled;
        set
        {
            if (_enabled == value) return;
            _enabled = value;
            
            if (gameObject != null && gameObject.scene != null)
                gameObject.scene.collisionSystem?.MarkColliderDirty(this);
        }
    }
    
    /// <summary> Returns the world position of the center of this collider </summary>
    public Vector3 WorldCenter 
    {
        get {
            if (gameObject == null) return center;

            Transform t = gameObject.transform;
            Vector3 worldCenter = t.position;

            if (center != Vector3.Zero) 
            {
                Vector3 localOffset = center * t.scale;
                worldCenter += Vector3.Transform(localOffset, t.rotation);
            }

            return worldCenter;
        }
    }
    
    /// <summary> Creates a new Collider3D </summary>
    public Collider3D() : this(false, true) { }
    
    /// <summary> Creates a new Collider3D </summary>
    public Collider3D(bool isTrigger = false, bool enabled = true)
    {
        this.isTrigger = isTrigger;
        this.enabled = enabled;
    }
    
    internal void SetGameObject(GameObject go)
    {
        this.gameObject = go;
    }
    
    /// <summary> Checks if the given position is overlapping with this collider </summary>
    public virtual bool CheckOverlap(Vector3 camPos)
    {
        return false;
    }
    
    /// <summary> Checks if the given position is colliding with this collider, and if so, modifies the position to resolve the collision </summary>
    public virtual void CheckCollision(ref Vector3 objPos, Vector3 prevPos){}

    /// <summary> Draws the collider in debug draw mode </summary>
    public virtual void DrawShapeDebug(Camera3D cam){}

    /// <summary> Returns the radius of the collider for broadphase collision detection </summary>
    public virtual float GetBroadphaseRadius()
    {
        return 0f;
    }

    /// <summary> Checks if a ray intersects with this collider, and if so, returns the hit information </summary>
    public virtual bool RayCast(Vector3 rayOrigin, Vector3 rayDirection, float maxDistance, out RaycastHit hit)
    {
        hit = null;
        return false;
    }
    
    internal virtual float GetPointDistanceSquared(Vector3 p)
    {
        float dist = Vector3.Distance(p, WorldCenter) - GetBroadphaseRadius();
        if (dist < 0f) return 0f;
        return dist * dist;
    }
}