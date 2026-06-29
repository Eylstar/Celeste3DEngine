using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.Celeste3DEngine;

public abstract class Collider3D
{
    public bool isTrigger;
    
    public Action<CollisionDetector> onCollision;
    public Action<CollisionDetector> onTriggerEnter;
    public Action<CollisionDetector> onTriggerStay;
    public Action<CollisionDetector> onTriggerExit;
    
    public GameObject gameObject;
    
    internal Vector3 center;
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
    
    public Collider3D(bool isTrigger = false, bool enabled = true)
    {
        this.isTrigger = isTrigger;
        this.enabled = enabled;
    }
    
    internal void SetGameObject(GameObject go)
    {
        this.gameObject = go;
    }

    internal virtual bool CheckOverlap(Vector3 camPos)
    {
        return false;
    }
    
    internal virtual void CheckCollision(ref Vector3 objPos, Vector3 prevPos){}

    internal virtual void DrawShapeDebug(Camera3D cam){}

    internal virtual float GetBroadphaseRadius()
    {
        return 0f;
    }

    internal virtual bool RayCast(Vector3 rayOrigin, Vector3 rayDirection, float maxDistance, out RaycastHit hit)
    {
        hit = null;
        return false;
    }
    
    internal virtual float GetPointDistanceSquared(Vector3 p)
    {
        Vector3 d = p - WorldCenter;
        return d.LengthSquared();
    }
}