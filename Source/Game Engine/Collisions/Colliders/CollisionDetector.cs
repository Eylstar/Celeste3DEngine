using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Celeste3DEngine;

/// <summary> Component that can be attached to a GameObject to detect collisions with other Colliders in the scene. </summary>
public class CollisionDetector
{
    /// <summary> Called when this detector collides with a Collider3D </summary>
    public Action<Collider3D> onCollision;
    /// <summary> Called when this detector enters a trigger Collider3D </summary>
    public Action<Collider3D> onTriggerEnter;
    /// <summary> Called when this detector stays in a trigger Collider3D </summary>
    public Action<Collider3D> onTriggerStay;
    /// <summary> Called when this detector exits a trigger Collider3D </summary>
    public Action<Collider3D> onTriggerExit;
    
    internal Dictionary<Collider3D, bool> colliderTriggerStateLastFrame = new Dictionary<Collider3D, bool>();
    
    /// <summary> The GameObject this detector is attached to </summary>
    public GameObject gameObject { get; internal set; }

    /// <summary> How far this detector reaches to find Colliders, both for the broadphase query and the actual check </summary>
    public float distanceToCheck;
    
    /// <summary> Offset from the GameObject's position to the center of this detector </summary>
    public Vector3 offset;

    /// <summary> Whether to draw the center of this detector in debug draw mode </summary>
    public bool debugDrawCenter = true;
    
    /// <summary> Whether to draw the distance of this detector in debug draw mode </summary>
    public bool debugDrawDistance = false;
    
    /// <summary> Whether this detector is enabled and participates in collision detection </summary>
    public bool enabled = true;
    
    internal Vector3 position => gameObject!= null ? gameObject.transform.position + Vector3.Transform(offset * gameObject.transform.scale, gameObject.transform.rotation) : Vector3.Zero;
    
    /// <summary> Creates a new CollisionDetector with a given distance to check and offset </summary>
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