using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Celeste3DEngine;

public class CollisionDetector
{
    public Action<Collider3D> onCollision;
    public Action<Collider3D> onTriggerEnter;
    public Action<Collider3D> onTriggerStay;
    public Action<Collider3D> onTriggerExit;
    
    internal Dictionary<Collider3D, bool> colliderTriggerStateLastFrame = new Dictionary<Collider3D, bool>();
    
    public GameObject gameObject { get; internal set; }

    public float distanceToCheck;
    public Vector3 offset;

    public bool debugDrawCenter = true;
    public bool debugDrawDistance = true;
    
    internal Vector3 position => gameObject.transform.position + Vector3.Transform(offset * gameObject.transform.scale, gameObject.transform.rotation);
    
    public CollisionDetector (float distanceToCheck = 5f, Vector3 offset = default)
    {
        this.distanceToCheck = distanceToCheck;
        this.offset = offset;
    }
    
    internal bool WasTriggerLastFrame(Collider3D col)
    {
        if (!colliderTriggerStateLastFrame.ContainsKey(col))
        {
            colliderTriggerStateLastFrame[col] = false;
            return false;
        }
        return colliderTriggerStateLastFrame[col];
    }
    
    internal void SetTriggerStateLastFrame(Collider3D col, bool isTrigger)
    {
        if (!isTrigger)
            colliderTriggerStateLastFrame.Remove(col);
        else
            colliderTriggerStateLastFrame[col] = true;
    }
    
    internal void SetParent(GameObject go)
    {
        this.gameObject = go;
    }
}