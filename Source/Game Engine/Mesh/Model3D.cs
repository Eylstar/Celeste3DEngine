using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using SharpGLTF.Schema2;
using SharpGLTF.Runtime;
using Image = SharpGLTF.Schema2.Image;
using Texture = SharpGLTF.Schema2.Texture;

namespace Celeste.Mod.Celeste3DEngine;

/// <summary> A 3D model component attached to a GraphicGameObject </summary>
internal sealed class Model3D : IDisposable
{
    // The mesh data for this model
    MeshData objMesh;

    internal MeshData GetMesh() => objMesh;
    
    // The GraphicGameObject this model is attached to
    internal GameObject gameObject;
    
    MeshRenderer meshRenderer => gameObject?.GetComponent<MeshRenderer>();
    Material modelMaterial => meshRenderer?.material ?? Material.DefaultMaterial;
    
    Transform modelTransform; 
    Transform initialModelTransform;

    Vector3 HUDOffset;
    bool hudOffsetInitialized = false;
    Camera3D lastHUDCamera;
    
    Renderer3D renderer => EngineEntity.Current3DScene?.GetRenderer();
    
    VirtualTexture modelTexture;
    VirtualTexture nullTexture => TexturesCache.Get(EnginePaths.defaultTexturesPath + "/nullTexture");
    
    Effect litShader => ShadersLoader.BaseLit;
    Effect unlitShader => ShadersLoader.Unlit;
    Effect shadowDepthSpot => ShadersLoader.ShadowDepthSpot;
    Effect shadowDepthDirectional => ShadersLoader.ShadowDepthDirectional;
    Effect addLight => ShadersLoader.AddLight;
    
    
    Camera3D renderingCamera => EngineEntity.Current3DScene?.GetRenderingCamera();

    LightingSettings lightSettings => EngineEntity.Current3DScene?.GetLightingSettings();
    
    WindSettings windSettings => EngineEntity.Current3DScene?.GetWindSettings();
    
    bool ignoreFog;
    float hudDepth = 1f;
    
    
    internal Model3D(GameObject go)
    {
        gameObject = go;
        
        modelTransform = go.transform;
        initialModelTransform = modelTransform.Copy();

        switch (meshRenderer.layer)
        {
            case ModelLayer.World:
                ignoreFog = false;
                break;
            case ModelLayer.Skybox:
                ignoreFog = true;
                break;
            case ModelLayer.HUD:
                ignoreFog = true;
                hudDepth = go.transform.position.Z;
                break;
        }
        
        LoadCustomModel(meshRenderer.modelName, meshRenderer.useEngineModelPath);
        
        if (meshRenderer.format == ModelFormat.OBJ)
            LoadCustomTexture(meshRenderer.textureName, meshRenderer.useEngineTexturePath);
        else
            modelTexture = nullTexture;
    }
    


    // Loads a custom texture for the model from the specified path
    void LoadCustomTexture(string t, bool enginePaths = false)
    {
        string path = enginePaths ? $"{EnginePaths.defaultTexturesPath}/{t}" : $"{EnginePaths.customTexturesPath}/{t}";
        modelTexture = TexturesCache.Get(path);
        
        if (modelTexture == null)
            modelTexture = nullTexture;
    }
    
    // Loads a custom model for the model from the specified path, first trying to load an export and then falling back to an OBJ if no export is found
    void LoadCustomModel(string m, bool enginePaths = false)
    {
        //GLTF
        ModAsset GLTFAsset;
        string gltfVirt = (enginePaths ? EnginePaths.defaultModelsPath : EnginePaths.customModelsPath) + $"/{m}.glb";
        
        if (meshRenderer.format != ModelFormat.OBJ && Everest.Content.TryGet(gltfVirt, out GLTFAsset))
        {
            Logger.Info("Model3D", $"Loading model GLTF '{m}'");
            using (Stream stream = GLTFAsset.Stream)
            {
                objMesh = GLBMeshData.CreateFromStream(stream, gltfVirt);
            }
            meshRenderer.format = ModelFormat.GLTF;
            return;
        }
        
        // EXPORT 
        ModAsset exportAsset;
        string exportVirt = (enginePaths ? EnginePaths.defaultExportsPath : EnginePaths.customExportsPath) + $"/{m}.export.dat";

        if (meshRenderer.format != ModelFormat.GLTF && Everest.Content.TryGet(exportVirt, out exportAsset))
        {
            using (Stream stream = exportAsset.Stream)
            {
                objMesh = OBJMeshData.CreateFromStream(stream, exportVirt);
            }
            meshRenderer.format = ModelFormat.OBJ;
            return;
        }
        
        //OBJ
        ModAsset objAsset;
        string objVirt = EnginePaths.customModelsPath + $"/{m}" ;

        if (meshRenderer.format != ModelFormat.GLTF && Everest.Content.TryGet(objVirt, out objAsset))
        {
            ExportGenerator.ConvertObjToExport(objAsset);
            using (Stream stream = objAsset.Stream)
            {
                objMesh = OBJMeshData.CreateFromStream(stream, m);
            }
            meshRenderer.format = ModelFormat.OBJ;
            return;
        }
        
        Logger.Error("Celeste3DEngine", $"Could not find model asset at {gltfVirt} or {objVirt} or export at {exportVirt}");
    }
    
    internal void ChangeTexture(string t) => LoadCustomTexture(t);
    
    internal void ChangeModel(string m) => LoadCustomModel(m);
    
    internal void UpdateLayer()
    {
        switch (meshRenderer.layer)
        {
            case ModelLayer.World:
                ignoreFog = false;
                break;
            case ModelLayer.Foreground:
                ignoreFog = false;
                break;
            case ModelLayer.Skybox:
                ignoreFog = true;
                break;
            case ModelLayer.HUD:
                ignoreFog = true;
                hudDepth = gameObject.transform.position.Z;
                break;
        }
    }
    
    
    
    // BEFORE RENDER

    
    // Prepare and render the model before the main scene render pass
    //internal void BeforeRender(Matrix lvpnear, Matrix lvpfar, Texture2D shadowTextureNear, Texture2D shadowTextureFar)
    internal void BeforeRender(Matrix lvp, Texture2D shadowTexture)
    {
        if (objMesh == null || renderingCamera == null) return;

        if (meshRenderer.layer != ModelLayer.World)
            LockTransform();
        
        GraphicsDevice device = Engine.Graphics.GraphicsDevice;
        
        SetupMatrices(out Matrix proj, out Matrix world, out Matrix view);
        SetupDeviceStates(device);
        
        Effect shader = modelMaterial.isLit ? litShader : unlitShader;
        
        // Set common shader parameters
        shader.Param("World").SetValue(world);
        shader.Param("View").SetValue(view);
        shader.Param("Projection").SetValue(proj);

        // Set lit shader parameters
        if (modelMaterial.isLit)
        {
            SetLitParameters(shader, world, lvp, shadowTexture);
            bool isSkinned = SetupSkinnedParameters(shader);
            shader.CurrentTechnique = isSkinned ? shader.Techniques["SkinnedMesh"] : shader.Techniques["BaseLighting"];
        }
        
        // Set unlit shader parameters
        else if (!modelMaterial.isLit)
        {
            shader.CurrentTechnique = shader.Techniques["UnlitMesh"];
            shader.Param("TintColor")?.SetValue(modelMaterial.Color);
        }
        
        
        objMesh.DrawWithSetup(shader, tex =>
            {
                device.Textures[0] = tex ?? modelTexture?.Texture;
                shader.Param("DiffuseTexture")?.SetValue(tex ?? modelTexture?.Texture);
            });
    }

    void LockTransform()
    {
        // Lock the model position for skybox and HUD objects
        if (meshRenderer.layer == ModelLayer.Skybox)
            modelTransform.position = renderingCamera.transform.position;
        
        else if (meshRenderer.layer == ModelLayer.HUD)
        {
            if (!hudOffsetInitialized || lastHUDCamera != renderingCamera)
            {
                HUDOffset = initialModelTransform.position - renderingCamera.transform.position;
                hudOffsetInitialized = true;
                lastHUDCamera = renderingCamera;
            }

            modelTransform.position = HUDOffset + renderingCamera.transform.position;
        }
    }
    
    // Calculate matrices based on layer type
    void SetupMatrices(out Matrix proj, out Matrix world, out Matrix view)
    {
        if (meshRenderer.layer != ModelLayer.HUD)
        {
            proj = renderingCamera.Projection;
            view = renderingCamera.View;
            world = Matrix.CreateScale(modelTransform.scale) * Matrix.CreateFromQuaternion(modelTransform.rotation) * Matrix.CreateTranslation(modelTransform.position);
        }
        else
        {
            proj = renderingCamera.OrthographicProjection;
            view = Matrix.Identity;
            
            ViewportScalingHelper.GetUiTransform(out float uiScale, out Vector2 uiOffset);
            Vector2 uiPos = new Vector2(modelTransform.position.X, modelTransform.position.Y);
            Vector2 screenPos = uiOffset + uiPos * uiScale;
            Vector3 hudScale = modelTransform.scale * uiScale;
                
            world = Matrix.CreateScale(hudScale) * Matrix.CreateFromQuaternion(modelTransform.rotation) * 
                    Matrix.CreateTranslation(screenPos.X, screenPos.Y, -hudDepth);
        }
    }

    void SetupDeviceStates(GraphicsDevice device)
    {
        device.RasterizerState = RasterizerState.CullNone;

        // Set depth and blend states based on layer type
        switch (meshRenderer.layer)
        {
            case ModelLayer.World:
                device.DepthStencilState = DepthStencilState.Default;
                device.BlendState = BlendState.AlphaBlend;
                break;
            case ModelLayer.Foreground:
                device.DepthStencilState = DepthStencilState.Default;
                device.BlendState = BlendState.AlphaBlend;
                break;
            case ModelLayer.Skybox:
                device.DepthStencilState = DepthStencilState.None;
                device.BlendState = BlendState.Opaque;
                break;
            case ModelLayer.HUD:
                device.DepthStencilState = DepthStencilState.Default;
                device.BlendState = BlendState.Opaque;
                break;
        }
        
        device.SamplerStates[0] = SamplerState.LinearClamp;
    }

    void SetLitParameters(Effect shader, Matrix world, Matrix lvp, Texture2D shadowTexture)
    {
        //shader.CurrentTechnique = shader.Techniques["BaseLighting"];
            
        // Calculate and set World Inverse Transpose matrix for correct normal transformation
        Matrix inverseTranspose = Matrix.Transpose(Matrix.Invert(world));
        shader.Param("WorldInverseTranspose")?.SetValue(inverseTranspose);
    
        shader.Param("CameraPos")?.SetValue(renderingCamera.transform.Position);
    
        shader.Param("Shininess")?.SetValue(modelMaterial.Shininess);
        
        shader.Param("LightDirection")?.SetValue(lightSettings.DirectionalLightDirection);
        
        //shader.Param("LightViewProjectionNear"]?.SetValue(lvpnear);
        //shader.Param("LightViewProjectionFar"]?.SetValue(lvpfar);
        shader.Param("LightViewProjection")?.SetValue(lvp);
        
        //shader.Param("ShadowMapNear"]?.SetValue(shadowTextureNear);
        //shader.Param("ShadowMapFar"]?.SetValue(shadowTextureFar);
        shader.Param("ShadowMap")?.SetValue(shadowTexture);
        
        //shader.Param("CascadeSplitDistance"]?.SetValue(lightSettings.shadowCascadeSplitDistance);
        
        //shader.Param("ShadowTexelSizeNear"]?.SetValue(new Vector2(1f / (lightSettings.shadowMapResolution * 2), 1f / (lightSettings.shadowMapResolution * 2)));
        //shader.Param("ShadowTexelSizeFar"]?.SetValue(new Vector2(1f / (lightSettings.shadowMapResolution / 2), 1f / (lightSettings.shadowMapResolution / 2)));
        shader.Param("ShadowTexelSize")?.SetValue(new Vector2(1f / (lightSettings.shadowMapResolution), 1f / (lightSettings.shadowMapResolution)));
        
        
        shader.Param("ShadowBias")?.SetValue(lightSettings.shadowBias);
        shader.Param("ShadowStrength")?.SetValue(lightSettings.shadowStrength);
        
        shader.Param("ReceivesShadows")?.SetValue(meshRenderer.receivesShadows ? 1f : 0f);
        shader.Param("ShadowSoftness")?.SetValue(lightSettings.shadowSoftness);
        
        
        ApplyWindParameters(shader);
        
        // Set parameters for ignoring fog objects (skybox and HUD)
        if (ignoreFog)
        {
            shader.Param("FogDensity")?.SetValue(0f);
            shader.Param("HeightFogDensity")?.SetValue(0f);
            shader.Param("FogColor")?.SetValue(Vector3.One);
            shader.Param("FogHeightStart")?.SetValue(0f);
            
            // Special handling for skybox objects to ensure proper lighting look
            if (meshRenderer.layer == ModelLayer.Skybox)
            {
                shader.Param("AmbientColor")?.SetValue(new Vector3(1f));
                shader.Param("LightColor")?.SetValue(new Vector3(0f));
                shader.Param("SpecularColor")?.SetValue(new Vector3(0f));
                shader.Param("DiffuseColor")?.SetValue(new Vector3(1f));
            }
        }
        // Normal fog and lighting settings for world objects
        else
        {
            shader.Param("FogColor")?.SetValue(lightSettings.fogColor);
            shader.Param("FogDensity")?.SetValue(lightSettings.fogDensity);
            shader.Param("HeightFogDensity")?.SetValue(lightSettings.heightFogDensity);
            shader.Param("FogHeightStart")?.SetValue(lightSettings.heightFogStart);
            
            shader.Param("AmbientColor")?.SetValue(lightSettings.AmbientLightColor);
            shader.Param("LightColor")?.SetValue(lightSettings.DirectionalLightColor);
            
            shader.Param("SpecularColor")?.SetValue(modelMaterial.SpecularColor);
            shader.Param("DiffuseColor")?.SetValue(modelMaterial.DiffuseColor);
            
            shader.Param("EmissiveColor")?.SetValue(modelMaterial.EmissiveColor);
            shader.Param("EmissiveIntensity")?.SetValue(modelMaterial.EmissiveIntensity);
            
            shader.Param("NearPlane")?.SetValue(lightSettings.shadowNearPlane);
            shader.Param("FarPlane")?.SetValue(lightSettings.shadowFarPlane);
            
            shader.Param("TintColor")?.SetValue(modelMaterial.Color);
        }
    }

    void ApplyWindParameters(Effect shader)
    {
        shader.Param("ObjectWorldPos")?.SetValue(modelTransform.position);
        
        shader.Param("UseWind")?.SetValue(meshRenderer.useWind ? 1f : 0f);
        shader.Param("WindDirection")?.SetValue(windSettings.direction);
        shader.Param("WindStrength")?.SetValue(windSettings.strength * meshRenderer.windMultiplier);
        shader.Param("WindFrequency")?.SetValue(windSettings.frequency);
        shader.Param("WindTime")?.SetValue(EngineEntity.Current3DScene?.ElapsedTime ?? 0f);

        float windHeightRange = objMesh != null ? objMesh.boundingSphereRadius * 2f : 0f;
        shader.Param("WindHeightRange")?.SetValue(windHeightRange);
    }
    
    bool SetupSkinnedParameters(Effect shader)
    {
        if (objMesh is not GLBMeshData skinnedMesh) return false;
        
        AnimationPlayer player = gameObject.GetComponent<AnimationPlayer>();
        if (player == null || player.boneMatrices == null || !player.isPlaying) return false;
        
        foreach (CustomPrimitive prim in skinnedMesh.primitives)
        {
            if (prim is not SkinnedPrimitive skprim) continue;
            
            int count = skprim.inverseBindMatrices.Length;
            
            Matrix[] final = new Matrix[count];
            for (int i = 0; i < count; i++)
            {
                int nodeIndex = skprim.nodeIndices[i];
                final[i] = skprim.inverseBindMatrices[i] * player.boneMatrices[nodeIndex];
            }
            
            shader.Param("BoneMatrices")?.SetValue(final);
            
            Logger.Warn("Model3D", $"Set up skinned parameters for {count} bones in model '{meshRenderer.modelName}'");
            return true;
        }
        return false;
    }

    
    // LIGHT AND SHADOW RENDER
    
    // Render an additional light pass for the model (scene lighting)
    internal void RenderLightPass(Light light)
    {
        Effect lightPassShader = addLight;
        if (lightPassShader == null || objMesh == null) return;

        Matrix world = Matrix.CreateScale(modelTransform.scale) * Matrix.CreateFromQuaternion(modelTransform.rotation) * Matrix.CreateTranslation(modelTransform.position);
        
        lightPassShader.Param("World")?.SetValue(world);
        lightPassShader.Param("View")?.SetValue(renderingCamera.View);
        lightPassShader.Param("Projection")?.SetValue(renderingCamera.Projection);
        lightPassShader.Param("WorldInverseTranspose")?.SetValue(Matrix.Transpose(Matrix.Invert(world)));
        
        // Set light properties for the shader
        lightPassShader.Param("LightPos")?.SetValue(light.transform.Position);
        lightPassShader.Param("LightColor")?.SetValue(light.color.ToVector3());
        lightPassShader.Param("LightIntensity")?.SetValue(light.intensity);
        lightPassShader.Param("LightRange")?.SetValue(light.range);
        
        lightPassShader.Param("CameraPos")?.SetValue(renderingCamera.transform.Position);
        lightPassShader.Param("Shininess")?.SetValue(modelMaterial.Shininess);
        lightPassShader.Param("SpecularColor")?.SetValue(modelMaterial.SpecularColor);
        
        // Determine if the light casts shadows and set shadow parameters
        bool castsShadows = (renderer != null && light is ConeLight c && renderer.spotLightsCastingShadows.Contains(c) && c.CastsShadows);
        lightPassShader.Param("UseShadows")?.SetValue(castsShadows ? 1f : 0f);
        
        // Set shadow map parameters if the light casts shadows
        if (castsShadows && renderer != null)
        {
            ConeLight cl = light as ConeLight;

            if (renderer.TryGetSpotlightMatrix(cl, out Matrix spotlightMatrix))
            {
                lightPassShader.Param("LightViewProjection")?.SetValue(spotlightMatrix);
                lightPassShader.Param("ShadowMap")?.SetValue(renderer.spotLightShadowMaps[cl]);
                
                lightPassShader.Param("ShadowBias")?.SetValue(lightSettings.shadowBias);
                lightPassShader.Param("ShadowStrength")?.SetValue(lightSettings.shadowStrength);
                
                lightPassShader.Param("ShadowTexelSize")?.SetValue(new Vector2(1f / lightSettings.spotLightShadowMapResolution, 1f / lightSettings.spotLightShadowMapResolution));
                lightPassShader.Param("ShadowSoftness")?.SetValue(lightSettings.shadowSoftness); 
                
                lightPassShader.Param("NearPlane")?.SetValue(lightSettings.shadowNearPlane);
                lightPassShader.Param("FarPlane")?.SetValue(light.range);
                
                lightPassShader.Param("DistanceAttenuationFactor")?.SetValue(cl.ShadowDistanceAttenuation);
            }
            else
                lightPassShader.Param("UseShadows")?.SetValue(0f);
        }

        // Set spotlight-specific parameters if the light is a ConeLight
        if (light is ConeLight cone)
        {
            lightPassShader.Param("UseSpot")?.SetValue(1f);
            
            float halfAngle = MathHelper.ToRadians(cone.Angle) * 0.5f;
            float fallOff = MathHelper.Clamp(cone.SpotFalloff, 0f, 1f);
            float innerAngle = halfAngle * (1f - fallOff);
            
            Vector3 dir = cone.transform.Forward;
            dir.Normalize();
            
            lightPassShader.Param("InnerCos")?.SetValue(MathF.Cos(innerAngle));
            lightPassShader.Param("OuterCos")?.SetValue(MathF.Cos(halfAngle));
            lightPassShader.Param("LightDir")?.SetValue(dir);
        }
        else
            lightPassShader.Param("UseSpot")?.SetValue(0f);
        
        ApplyWindParameters(lightPassShader);
        
        bool isSkinned = SetupSkinnedParameters(lightPassShader);
        lightPassShader.CurrentTechnique = isSkinned ? lightPassShader.Techniques["AddLightSkinned"] : lightPassShader.Techniques["AddLight"];
        
        objMesh.DrawWithSetup(lightPassShader, tex =>
            {
                lightPassShader.Param("DiffuseTexture")?.SetValue(tex ?? modelTexture?.Texture);
            });
    }
    
    // Render the model's shadow map from the light's perspective for shadow casting
    internal void RenderShadowMap(Matrix lvp, ConeLight light = null)
    {
        if (meshRenderer.layer != ModelLayer.World || objMesh == null) return;
        
        // If no ConeLight, it's directional light shadow mapping, so use directional shadow shader. Otherwise, use spotlight shadow shader.
        Effect shadowMap = light != null ? shadowDepthSpot : shadowDepthDirectional;
        
        Matrix world = Matrix.CreateScale(modelTransform.scale) * Matrix.CreateFromQuaternion(modelTransform.rotation) * Matrix.CreateTranslation(modelTransform.position);
        
        shadowMap.Param("World")?.SetValue(world);
        shadowMap.Param("LightViewProjection")?.SetValue(lvp);
        shadowMap.Param("NearPlane")?.SetValue(lightSettings.shadowNearPlane);
        shadowMap.Param("FarPlane")?.SetValue(light?.range ?? lightSettings.shadowFarPlane);

        bool isSkinned = SetupSkinnedParameters(shadowMap);
        shadowMap.CurrentTechnique = isSkinned ? shadowMap.Techniques["ShadowDepthSkinned"] : shadowMap.Techniques["ShadowDepth"];
        
        shadowMap.Param("UseWind")?.SetValue(1f);
        
        ApplyWindParameters(shadowMap);

        objMesh.DrawWithSetup(shadowMap, tex =>
        {
            Texture2D t = tex ?? modelTexture?.Texture;
            Engine.Graphics.GraphicsDevice.Textures[0] = t;
            shadowMap.Param("DiffuseTexture")?.SetValue(t);
        });
    }
    
    public void Dispose()
    {
        objMesh?.Dispose();
        objMesh = null;
    }
}