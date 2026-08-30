namespace Celeste.Mod.Celeste3DEngine;

/// <summary> Base class for all components that can be attached to a GameObject </summary>
public abstract class Behaviour
{
    /// <summary> Called when the GameObject is added to the Scene. The Scene and 3DModels may still not be loaded by tne Engine </summary>
    public virtual void Added() { }
    
    /// <summary> Called once before the first update of the Behaviour </summary>
    public virtual void Start() { }
    
    /// <summary> Called every frame at the same time of Celeste.Entity.Update </summary>
    public virtual void Update(float deltaTime) { }
    
    /// <summary> Called every frame at the same time of Celeste.Entity.Render </summary>
    public virtual void Render() { }
    
    /// <summary> Called when the GameObject is removed from the Scene </summary>
    public virtual void Removed() { }
    
    /// <summary> Whether this Behaviour is enabled and should have its Update and Render methods called </summary>
    public bool enabled = true;
    
    internal bool started = false;
    
    /// <summary> The GameObject this Behaviour is attached to </summary>
    public GameObject gameObject;
    
    /// <summary> The Transform of the GameObject this Behaviour is attached to </summary>
    public Transform transform => gameObject.transform;
    
    /// <summary> The Scene this Behaviour's GameObject is in </summary>
    public Scene3D scene => gameObject.scene;
}