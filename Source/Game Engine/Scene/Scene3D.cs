using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Monocle;

namespace Celeste.Mod.Celeste3DEngine;

/// <summary> Represents a 3D scene in the game engine. Manages GameObjects and Start/Update logic loops. </summary>
public sealed class Scene3D
{
    // Events for scene lifecycle
    public delegate void SceneLoadDelegate(Scene3D scene3D);
    public event SceneLoadDelegate OnSceneLoad;
    
    public delegate void SceneUnloadDelegate(Scene3D scene3D);
    public event SceneUnloadDelegate OnSceneUnload;
    
    // The main rendering camera for the scene 
    Camera3D RenderingCamera;
    
    /// <summary> Gets the main rendering Camera for the scene </summary>
    public Camera3D GetRenderingCamera() => RenderingCamera;

    // The renderer responsible for rendering the 3D scene
    Renderer3D renderer = new();
    
    /// <summary> Gets the Renderer3D object for the scene </summary>
    public Renderer3D GetRenderer() => renderer;
    
    // Lighting settings for the scene
    LightingSettings lightingSettings = new();
    
    /// <summary> Gets the LightingSettings used in the scene </summary>
    public LightingSettings GetLightingSettings() => lightingSettings;
    
    //The list of all GameObjects currently in the scene
    HashSet<GameObject> gameObjects = new();
    HashSet<GameObject> pendingStart = new();
    HashSet<GameObject> startNextFrame = new();
    List<GameObject> iterationList = new();
    
    Skybox skybox = new Skybox();
    
    /// <summary> Gets the Skybox object used in the scene </summary>
    public Skybox GetSkybox => skybox;
    
    internal static AudioListener audioListener = new AudioListener();
    bool customAudioListenerObject;
    GameObject audioListenerObject;
    
    /// <summary> Returns a readOnly list with all GameObjects in the scene </summary>
    public IReadOnlyCollection<GameObject> GetGameObjectsList() => gameObjects;
    
    
    internal CollisionSystem collisionSystem = new();
    
    bool isPaused;
    bool sceneWiped;
    
    /// <summary> Decides if the Colliders and Collision Detectors are shown in the scene (Wireframes) </summary>
    public bool debugShowColliders = false;
    
    /// <summary> Decides if only the Colliders in range of the Collision Detectors are shown in the scene (Wireframes) </summary>
    public bool debugShowOnlyCollidersInRange = false;
    
    /// <summary> Decides if the RayCasts performed in the CollisionSystem are shown in the scene (Lines) </summary>
    public bool debugShowRayCasts = false;
    
    
    //Called once by the EngineEntity when the scene is created/loaded
    internal void LoadScene()
    {
        // If no main camera was set before loading the scene, create a default one
        if (RenderingCamera == null)
        {
            RenderingCamera = new Camera3D();
            gameObjects.Add(RenderingCamera);
        }
        
        //Add the default skybox to the scene
        AddGameObject(skybox);

        
        // Initialize the Collision System for this scene
        collisionSystem.Initialize(this);
        
        RenderingHooks.AddRenderer(renderer);
        
        OnSceneLoad?.Invoke(this);
    }
    
    
    

#region Object Adders

    /// <summary> Adds a GameObject to the scene. </summary>
    public void AddGameObject(GameObject go)
    {
        gameObjects.Add(go);
        go.Added(this);
        startNextFrame.Add(go);
    }
        
    /// <summary> Adds multiple GameObjects to the scene. </summary>
    public void AddGameObjects(IEnumerable<GameObject> gos)
    {
        foreach (GameObject go in gos)
            AddGameObject(go);
    }
    
    
    internal void RemoveGameObject(GameObject go)
    {
        if (gameObjects.Contains(go))
            gameObjects.Remove(go);
    }
    

    /// <summary> Adds a static HUD orthographic GameObject </summary>
    public void AddHUDObject(GameObject go)
    {
        if (go.GetComponent<MeshRenderer>() == null)
        {
            Logger.Error("Celeste3DEngine", $"Trying to add a HUD GameObject without a MeshRenderer. Please add a MeshRenderer component to the GameObject before adding it as a HUD object.");
            return;
        }
        go.GetComponent<MeshRenderer>().layer = ModelLayer.HUD;
        AddGameObject(go);
    }

    

#endregion

#region Object & Values Setters

    /// <summary> Sets the main rendering Camera for the scene </summary>
    public void SetMainCamera(Camera3D camera)
    {
        if (gameObjects.Contains(camera))
            RenderingCamera = camera;
        else
            Logger.Warn("Celeste3DEngine", $"Trying to set a main camera that is not in the scene. Please add the camera to the scene as GameObject before setting it as main camera.");
    }
    
    /// <summary> Sets the lighting settings for the scene </summary>
    public void SetLightingSettings(LightingSettings settings) => lightingSettings = settings;
    
    /// <summary> Pauses or unpauses the scene update and render </summary>
    public void SetPause(bool pause) => isPaused = pause;

    /// <summary> Enables or disables the skybox rendering </summary>
    public void EnableSkybox(bool enabled)
    {
        skybox.enabled = enabled;
    }
    
    /// <summary> Change the Skybox Texture </summary>
    public void ChangeSkyBox(string texture)
    {
        skybox.GetComponent<MeshRenderer>().useEngineTexturePath = false;
        skybox.GetComponent<MeshRenderer>().textureName = texture;
        skybox.GetComponent<MeshRenderer>().GetModel()?.ChangeTexture(texture);
    }

    /// <summary> Changes the GameObject that acts as the AudioListener for the scene. </summary>
    public void ChangeAudioListenerObject(GameObject obj)
    {
        customAudioListenerObject = true;
        audioListenerObject = obj;
    }
    
    /// <summary> Resets the AudioListener to follow the main rendering camera. </summary>
    public void ResetAudioListenerToCamera()
    {
        customAudioListenerObject = false;
        audioListenerObject = null;
    }

#endregion
    
#region Object Finders

    /// <summary> Finds the first GameObject in the scene with given name </summary>
    public GameObject FindFirstByName(string name)
    {
        foreach (GameObject go in gameObjects)
            if (go.name == name)
                return go;
        
        return null;
    }
    
    /// <summary> Finds all GameObjects in the scene with given name </summary>
    public List<GameObject> FindAllByName(string name)
    {
        List<GameObject> gos = new List<GameObject>();
        
        foreach (GameObject go in gameObjects)
            if (go.name == name)
                gos.Add(go);
        
        return gos;
    }
    
    /// <summary> Finds the first GameObject in the scene with given tag </summary>
    public GameObject FindFirstByTag(string tag)
    {
        foreach (GameObject go in gameObjects)
            if (go.tag == tag)
                return go;
        
        return null;
    }
    
    /// <summary> Finds all GameObjects in the scene with given tag </summary>
    public List<GameObject> FindAllByTag(string tag)
    {
        List<GameObject> gos = new List<GameObject>();
        
        foreach (GameObject go in gameObjects)
            if (go.tag == tag)
                gos.Add(go);
        
        return gos;
    }
    
    /// <summary> Finds the first GameObject in the scene that has a Behaviour of given Type (Ressource heavy, use in Update with caution)  </summary>
    public GameObject FindFirstByType<T>() where T : Behaviour
    {
        foreach (GameObject g in gameObjects)
            foreach (Behaviour be in g.behaviours)
                if (be is T)
                    return g;
        
        return null;
    }
    
    /// <summary> Finds all GameObjects in the scene with a Behaviour of given Type (Ressource heavy, use in Update with caution)  </summary>
    public List<GameObject> FindAllByType<T>() where T : Behaviour
    {
        List<GameObject> gos = new List<GameObject>();
        foreach (GameObject go in gameObjects)
            foreach (Behaviour be in go.behaviours)
                if (be is T)
                {
                    gos.Add(go);
                    break;
                }
                    
        return gos;
    }

#endregion
    


    // Called every frame when Celeste updates the scene
    internal void SceneUpdate()
    {
        if (sceneWiped || isPaused) return;

        // Start all pending GameObjects that were added during the last update cycle
        pendingStart.UnionWith(startNextFrame);
        startNextFrame.Clear();
        foreach (GameObject go in pendingStart)
        {
            if (go != null && !go.destroyed && !go.started)
                go.Start();
        }
        pendingStart.Clear();
        
        collisionSystem.ClearRayCasts();
        
        // Update all GameObjects in the scene
        List<GameObject> it = new List<GameObject>(gameObjects);
        foreach (GameObject go in it)
        {
            if (go != null && !go.destroyed)
                go.Update(Engine.DeltaTime);
        }
        
        // Update the AudioListener position
        UpdateAudioListenerPosition();

        
        // Prepare the renderer for this frame
        renderer.BeforeRender();
        
        // Update collision system and resolve collisions
        collisionSystem.FlushDirtyColliders();
        collisionSystem.ResolveCollisions();
        
        // Clean up destroyed GameObjects from the scene
        CleanupDestroyedObjects();
    }

    void UpdateAudioListenerPosition()
    {
        if (!customAudioListenerObject && RenderingCamera != null)
        {
            audioListener.Position = RenderingCamera.transform.Position;
            audioListener.Forward = -RenderingCamera.transform.Forward;
            audioListener.Up = RenderingCamera.transform.Up;
        }
        else if (customAudioListenerObject)
        {
            if (audioListenerObject != null)
            {
                audioListener.Position = audioListenerObject.transform.Position;
                audioListener.Forward = -audioListenerObject.transform.Forward;
                audioListener.Up = audioListenerObject.transform.Up;
            }
            else
            {
                Logger.Warn("Scene3D", "Audio listener object is null. Please set a valid audio listener object using ChangeAudioListenerObject() or reset it to RenderingCamera with ResetAudioListenerToCamera().");
            }
        }
    }
    
    
    // Called every frame when Celeste renders the scene
    internal void SceneRender()
    {
        if (sceneWiped || isPaused) return;
        
        iterationList.AddRange(gameObjects);
        foreach (GameObject go in iterationList)
        {
            if (go != null && !go.destroyed)
            {
                foreach (Behaviour b in go.behaviours)
                    if (b.enabled)
                    {
                        /*if (!b.started)
                        {
                            b.Start();
                            b.started = true;
                        }*/
                        b.Render();
                    }
            }
        }
        iterationList.Clear();
        
        // Draw debug colliders if enabled
        if (debugShowColliders)
            collisionSystem.DrawDebugColliders(RenderingCamera, debugShowOnlyCollidersInRange);
        
        if (debugShowRayCasts)
            collisionSystem.DrawDebugRayCasts(RenderingCamera);
    }
    
    
    // Cleans up destroyed GameObjects from the scene list
    void CleanupDestroyedObjects()
    {
        List<GameObject> toRemove = new();
        
        // Find all destroyed GameObjects to remove
        foreach (GameObject go in gameObjects)
            if (go.destroyed) 
                toRemove.Add(go);

        foreach (GameObject go in toRemove)
            gameObjects.Remove(go);
    }
    
    // Called once by the EngineEntity when the scene is unloaded/ended
    internal void UnloadScene()
    {
        OnSceneUnload?.Invoke(this);
        
        // Destroy all GameObjects in the scene
        foreach (GameObject go in gameObjects)
        {
            if (go is not Skybox)
                go.Destroy();
            else
                (go as Skybox).ForceDestroy();
        }
        
        pendingStart.Clear();
        startNextFrame.Clear();
        
        CleanupDestroyedObjects();
        
        // Unload all references and systems
        
        collisionSystem.UnloadCollisionSystem();
        RenderingHooks.activeRenderer = null;
        
        renderer?.Dispose();
        
        // Mark the scene as wiped to prevent further updates/renders
        sceneWiped = true;
    }
}