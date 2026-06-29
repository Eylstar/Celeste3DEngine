namespace Celeste.Mod.Celeste3DEngine;

/// <summary> The GameObject component used for rendering 3D models. </summary>
public class MeshRenderer
{
    internal GameObject gameObject;
    
    internal ModelFormat format = ModelFormat.UNKNOWN;
    
    internal Model3D model;
    internal Model3D GetModel() => model;
    internal void SetModel(Model3D m) => model = m;
    
    internal Material material = Material.DefaultMaterial;
    
    internal string modelName;
    internal string textureName;
    
    internal bool useEngineModelPath = false;
    internal bool useEngineTexturePath = false;
    
    internal ModelLayer layer;
    
    /// <summary> Whether this Model casts shadows. Default true </summary>
    public bool castsShadows = true;
    
    /// <summary> Whether this Model receives shadows. Default true </summary>
    public bool receivesShadows = true;

    /// <summary> Wether this model should be automatically culled (not rendered) when not in the frustum of the rendering camera. Default true </summary>
    public bool isFrustumCulled = true;
    
    /// <summary> Whether this Model is visible. Setting this to false will make the Model not render, but it will still cast shadows if castsShadows is true. Default true </summary>
    public bool isVisible = true;
    
    internal Material InstanceMaterial
    {
        get
        {
            if (ReferenceEquals(material, Material.DefaultMaterial))
                material = material.Clone();
            return material;
        }
    }
    
    /// <summary> Creates a new OBJ MeshRenderer with the given model and texture names. </summary>
    public MeshRenderer(string modelName, string textureName, ModelLayer modelLayer = ModelLayer.World)
    {
        this.modelName = modelName;
        this.textureName = textureName;
        layer = modelLayer;
    }

    /// <summary> Creates a new GLTF MeshRenderer with the given model name. </summary>
    public MeshRenderer(string modelName, ModelLayer modelLayer = ModelLayer.World)
    {
        this.modelName = modelName;
        layer = modelLayer;
    }
    
    /// <summary> Creates a new OBJ MeshRenderer with the given model and texture names, and sets the material. </summary>
    public MeshRenderer(string modelName, string textureName, Material mat, ModelLayer modelLayer = ModelLayer.World)
        : this(modelName, textureName, modelLayer)
    {
        SetMaterial(mat);
    }
    
    /// <summary> Creates a new GLTF MeshRenderer with the given model name, and sets the material. </summary>
    public MeshRenderer(string modelName, Material mat, ModelLayer modelLayer = ModelLayer.World)
        : this(modelName, modelLayer)
    {
        SetMaterial(mat);
    }
    
    /// <summary> Changes the texture of this Model at runtime. </summary>
    public void ChangeTexture(string newTexturePath)
    {
        textureName = newTexturePath;
        useEngineTexturePath = false;
        model?.ChangeTexture(newTexturePath);
    }
    
    /// <summary> Changes the model of this Model at runtime. </summary>
    public void ChangeModel(string newModelPath)
    {
        modelName = newModelPath;
        useEngineModelPath = false;
        model?.ChangeModel(newModelPath);
    }
    
    /// <summary> Changes the layer of this Model at runtime. </summary>
    public void ChangeLayer(ModelLayer newLayer)
    {
        layer = newLayer;
        //if (gameObject?.scene == null) return;
        model?.UpdateLayer();
        gameObject?.scene?.GetRenderer().UpdateLayer(this);
    }
    
    /// <summary> Returns the Material of this Object </summary>
    public Material GetMaterial() => InstanceMaterial;
    
    /// <summary> Sets the material for this Model. If null is passed, the DefaultMaterial will be used. </summary>
    public void SetMaterial(Material mat) => material = mat ?? Material.DefaultMaterial;
}

enum ModelFormat
{
    OBJ,
    GLTF,
    UNKNOWN
}


public enum ModelLayer
{
    World,
    Foreground,
    HUD,
    Skybox
}