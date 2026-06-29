using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Celeste3DEngine;

public partial class GameObject
{
    internal Scene3D scene;
    public Scene3D GetScene => scene;
    
    public bool enabled = true;
    internal bool destroyed;
    
    internal bool started = false;
    
    /// <summary> The name of this GameObject </summary>
    public string name;
    
    /// <summary> The tag of this GameObject </summary>
    public string tag;
    
    public Transform transform { get; internal set; }
    public Transform initialTransform { get; internal set; }
    
    internal Vector3 positionLastFrame;
    public Vector3 GetPositionLastFrame() => positionLastFrame;
    
    MeshRenderer meshRenderer;
    internal List<Behaviour> behaviours = new();
    internal List<Collider3D> colliders = new();
    internal List<CollisionDetector> collisionDetectors = new();
    
    
    public GameObject()
    {
        transform = new Transform(Vector3.Zero, Vector3.Zero, Vector3.One);
        initialTransform = transform.Copy();
        name = "";
        transform.SetOwner(this);
    }

    public GameObject(string name)
    {
        transform = new Transform(Vector3.Zero, Vector3.Zero, Vector3.One);
        initialTransform = transform.Copy();
        this.name = name;
        transform.SetOwner(this);
    }

    public GameObject(Transform transform, string name = "") 
    {
        this.transform = transform;
        initialTransform = transform.Copy();
        this.name = name;
        this.transform.SetOwner(this);
    }
    
    
    public GameObject (Vector3 position, Vector3 rotation, Vector3 scale, string name = "") : this(new Transform(position, rotation, scale), name){}
    
    // Called when this GameObject is added to the scene
    internal virtual void Added(Scene3D s)
    {
        this.scene = s;
        foreach (Behaviour b in behaviours) 
            b.Added();
    }
    
    // Called once when the scene starts or before the first Update of this GameObject
    internal virtual void Start()
    {
        started = true;
        
        if(meshRenderer != null)
            scene.GetRenderer()?.AddModel(this);
        
        // Ensure colliders are registered in the collision system
        foreach (Collider3D col in colliders)
        {
            scene.collisionSystem?.AddCollider(col);
        }

        // Ensure the object is registered if it has collision detectors
        if (collisionDetectors.Count > 0)
            scene.collisionSystem?.AddCollisionObject(this);
        
        List<Behaviour> it = new List<Behaviour>(behaviours);
        foreach (Behaviour b in it)
        {
            if (b.enabled && !b.started)
            {
                b.Start();
                b.started = true;
            }
        }
    }

    // Called every frame to update this GameObject and its components
    internal virtual void Update(float deltaTime)
    {
        List<Behaviour> it = new List<Behaviour>(behaviours);
        foreach (Behaviour b in it)
        {
            if (b.enabled)
            {
                if (!b.started)
                {
                    b.Start();
                    b.started = true;
                }
                b.Update(deltaTime);
            }
        }
        
        positionLastFrame = transform.position;
    }
    
    /// <summary> Destroys this GameObject and removes it from the scene </summary>
    public virtual void Destroy()
    {
        if (destroyed)
        {
            Logger.Warn("GameObject", $"Attempted to destroy GameObject '{name}' which is already destroyed.");
            return;
        }
        
        Removed();
        scene?.RemoveGameObject(this);
    }

    // Called when this GameObject is removed from the scene
    internal virtual void Removed()
    {
        if (meshRenderer != null)
            scene?.GetRenderer()?.RemoveModelFromLists(this);
        
        destroyed = true;
        RemoveGameObjectReferences();
    }
    
    /// <summary> Set the Transform of this GameObject </summary>
    public void SetTransform(Transform t)
    { 
        transform = t;
        t.SetOwner(this);
    }
    
    /// <summary> Set the name of this GameObject </summary>
    public void SetName(string newName) => name = newName;
    
    /// <summary> Set the tag of this GameObject </summary>
    public void SetTag(string newTag) => tag = newTag;
    
    
    /// <summary> Add a component of type T to this GameObject </summary>
    public void AddComponent<T>(T t)
    {
        if (t is MeshRenderer mr)
        {
            if (meshRenderer != null)
                scene?.GetRenderer()?.RemoveModelFromLists(this);
            meshRenderer = mr;
            meshRenderer.gameObject = this;
            if (started)
                scene?.GetRenderer()?.AddModel(this);
        }
        else if (t is Behaviour b)
        {
            behaviours.Add(b);
            b.gameObject = this;
        }
        else if (t is Collider3D c)
        {
            colliders.Add(c);
            c.SetGameObject(this);
            scene?.collisionSystem?.AddCollider(c);
        }
        else if (t is CollisionDetector d)
        {
            collisionDetectors.Add(d);
            d.SetParent(this);
            scene?.collisionSystem?.AddCollisionObject(this);
        }
    }
    
    /// <summary> Remove passed component from this gameobject if found </summary>
    public void RemoveComponent<T>(T t)
    {
        if (t is MeshRenderer mr && meshRenderer == mr)
        {
            scene?.GetRenderer()?.RemoveModelFromLists(this);
            meshRenderer = null;
        }
        if (t is Behaviour b) behaviours.Remove(b);
        
        else if (t is Collider3D c) 
        {
            colliders.Remove(c);
            scene?.collisionSystem?.RemoveCollider(c);
        }
        else if (t is CollisionDetector d)
        {
            collisionDetectors.Remove(d);

            if (collisionDetectors.Count == 0)
                scene?.collisionSystem?.RemoveCollisionObject(this);
        }
    }
    
    /// <summary> Remove the first component of type T from this GameObject </summary>
    public void RemoveFirstFoundComponent<T>() where T : class
    {
        if (typeof(T) == typeof(MeshRenderer))
        {
            if (meshRenderer != null)
            {
                scene?.GetRenderer()?.RemoveModelFromLists(this);
                meshRenderer = null;
            }
            return;
        }
        if (typeof(T) == typeof(Collider3D))
        {
            if (colliders.Count > 0)
            {
                scene?.collisionSystem?.RemoveCollider(colliders[0]);
                colliders.RemoveAt(0);
            }
            return;
        }
        
        if (typeof(T) == typeof(CollisionDetector))
        {
            if (collisionDetectors.Count > 0)
            {
                if (collisionDetectors.Count == 1)
                {
                    scene?.collisionSystem?.RemoveCollisionObject(this);
                }
                collisionDetectors.RemoveAt(0);
            }
            return;
        }

        if (behaviours != null)
        {
            Behaviour toRemove = null;
            foreach (Behaviour b in behaviours) 
                if (b is T)
                {
                    toRemove = b;
                    break;
                }

            if (toRemove != null)
                behaviours.Remove(toRemove);
        }
            
    }
    
    /// <summary> Remove all components of type T from this GameObject </summary>
    public void RemoveAllComponents<T>()
    {
        if (typeof(T) == typeof(MeshRenderer))
        {
            if (meshRenderer != null)
            {
                scene?.GetRenderer()?.RemoveModelFromLists(this);
                meshRenderer = null;
            }
            return;
        }
        if (typeof(T) == typeof(Collider3D))
        {
            foreach (Collider3D c in colliders)
                scene?.collisionSystem?.RemoveCollider(c);
            colliders.Clear();
        }
        
        else if (typeof(T) == typeof(CollisionDetector))
        {
            scene?.collisionSystem?.RemoveCollisionObject(this);
            collisionDetectors.Clear();
        }
        
        List<Behaviour> toRemove = new();
        if (behaviours != null)  
            foreach (Behaviour b in behaviours) 
                if (b is T t) toRemove.Add(t as Behaviour);
        
        if (toRemove.Count > 0)
            foreach (Behaviour b in toRemove)
                behaviours?.Remove(b);
        
    }
    
    /// <summary> Get the first component of type T attached to this GameObject or null </summary>
    public virtual T GetComponent<T>() where T : class
    {
        if (typeof(T) == typeof(MeshRenderer)) 
            return meshRenderer as T;
        
        if (typeof(T) == typeof(Collider3D)) 
            return (colliders != null && colliders.Count > 0) ? colliders[0] as T : null;
        
        if (typeof(T) == typeof(CollisionDetector)) 
            return (collisionDetectors != null && collisionDetectors.Count > 0) ? collisionDetectors[0] as T : null;
        
        if (behaviours != null) 
            foreach (Behaviour b in behaviours) 
                if (b is T t) return t;

        return null;
    }
    
    /// <summary> Get all components of type T attached to this GameObject </summary>
    public virtual List<T> GetAllComponents<T> () where T : class
    {
        List<T> results = new();

        if (typeof(T) == typeof(MeshRenderer))
            if (meshRenderer != null)
                results.Add(meshRenderer as T);
        
        if (colliders != null && typeof(Collider3D).IsAssignableFrom(typeof(T)))
            foreach (Collider3D c in colliders)
                results.Add(c as T);
        
        if (collisionDetectors != null && typeof(T) == typeof(CollisionDetector))
            foreach (CollisionDetector d in collisionDetectors)
                results.Add(d as T);
        
        if (behaviours != null)  
            foreach (Behaviour b in behaviours) 
                if (b is T t) results.Add(t);
        
        return results;
    }
    
    /// <summary> Check if this GameObject has a component of type T attached </summary>
    public virtual bool HasComponent<T>() where T : class
    {
        if (typeof(T) == typeof(MeshRenderer)) 
            return meshRenderer != null;
        
        if (typeof(Collider3D).IsAssignableFrom(typeof(T))) 
            foreach (Collider3D c in colliders) 
                if (c is T) return true;
        
        if (typeof(T) == typeof(CollisionDetector)) 
            return collisionDetectors != null && collisionDetectors.Count > 0;
        
        if (behaviours != null) 
            foreach (Behaviour b in behaviours) 
                if (b is T) return true;

        return false;
    }
    
    
    // Mark all colliders as dirty so they will be reprocessed in the next collision update (only for BoxColliders)
    internal void MarkCollidersDirty()
    {
        foreach (Collider3D c in colliders)
        {
            scene?.collisionSystem?.MarkColliderDirty(c);
        }
    }

    internal virtual void RemoveGameObjectReferences()
    {
        foreach (Collider3D c in colliders)
        {
            scene?.collisionSystem?.RemoveCollider(c);
            c.SetGameObject(null);
        }
        colliders.Clear();

        foreach (CollisionDetector d in collisionDetectors)
            d.SetParent(null);
        
        scene?.collisionSystem?.RemoveCollisionObject(this);
        collisionDetectors.Clear();
        
        foreach (Behaviour b in behaviours) 
            b.Removed();
        
        behaviours?.Clear();
        
        scene = null;
        if (meshRenderer != null)
        {
            meshRenderer.model?.Dispose();
            meshRenderer.model = null;
            meshRenderer.material = null;
            meshRenderer = null;
        }
    }
}