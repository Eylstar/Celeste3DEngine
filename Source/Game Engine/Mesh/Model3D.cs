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
    Vector3 initialCamPos;
    
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
    
    bool ignoreFog;
    float hudDepth = 1f;
    
    
    internal Model3D(GameObject go)
    {
        gameObject = go;
        
        modelTransform = go.transform;
        initialModelTransform = modelTransform.Copy();
        initialCamPos = renderingCamera.transform.position;

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
        string gltfVirt = EnginePaths.customModelsPath + $"/{m}.glb";
        
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
    internal void BeforeRender(Matrix lvpnear, Matrix lvpfar, Texture2D shadowTextureNear, Texture2D shadowTextureFar)
    {
        if (objMesh == null || renderingCamera == null) return;

        if (meshRenderer.layer != ModelLayer.World)
            LockTransform();
        
        GraphicsDevice device = Engine.Graphics.GraphicsDevice;
        
        SetupMatrices(out Matrix proj, out Matrix world, out Matrix view);
        SetupDeviceStates(device);
        
        Effect shader = modelMaterial.isLit ? litShader : unlitShader;
        
        // Set common shader parameters
        shader.Parameters["World"].SetValue(world);
        shader.Parameters["View"].SetValue(view);
        shader.Parameters["Projection"].SetValue(proj);

        // Set lit shader parameters
        if (modelMaterial.isLit)
        {
            SetLitParameters(shader, world, lvpnear, lvpfar, shadowTextureNear, shadowTextureFar);
            bool isSkinned = SetupSkinnedParameters(shader);
            shader.CurrentTechnique = isSkinned ? shader.Techniques["SkinnedMesh"] : shader.Techniques["BaseLighting"];
        }
        
        // Set unlit shader parameters
        else if (!modelMaterial.isLit)
        {
            shader.CurrentTechnique = shader.Techniques["UnlitMesh"];
            shader.Parameters["TintColor"]?.SetValue(modelMaterial.tint);
        }
        
        
        objMesh.DrawWithSetup(shader, tex =>
            {
                device.Textures[0] = tex ?? modelTexture?.Texture;
                shader.Parameters["DiffuseTexture"]?.SetValue(tex ?? modelTexture?.Texture);
            });
    }

    void LockTransform()
    {
        // Lock the model position for skybox and HUD objects
        if (meshRenderer.layer == ModelLayer.Skybox)
            modelTransform.position = renderingCamera.transform.position;
        
        else if (meshRenderer.layer == ModelLayer.HUD)
            modelTransform.position = (initialModelTransform.position - initialCamPos) + renderingCamera.transform.position;
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

    void SetLitParameters(Effect shader, Matrix world, Matrix lvpnear, Matrix lvpfar, Texture2D shadowTextureNear, Texture2D shadowTextureFar)
    {
        //shader.CurrentTechnique = shader.Techniques["BaseLighting"];
            
        // Calculate and set World Inverse Transpose matrix for correct normal transformation
        Matrix inverseTranspose = Matrix.Transpose(Matrix.Invert(world));
        shader.Parameters["WorldInverseTranspose"]?.SetValue(inverseTranspose);
    
        shader.Parameters["CameraPos"]?.SetValue(renderingCamera.transform.Position);
    
        shader.Parameters["Shininess"]?.SetValue(modelMaterial.Shininess);
        
        shader.Parameters["LightDirection"]?.SetValue(lightSettings.DirectionalLightDirection);
        
        shader.Parameters["LightViewProjectionNear"]?.SetValue(lvpnear);
        shader.Parameters["LightViewProjectionFar"]?.SetValue(lvpfar);
        
        shader.Parameters["ShadowMapNear"]?.SetValue(shadowTextureNear);
        shader.Parameters["ShadowMapFar"]?.SetValue(shadowTextureFar);
        shader.Parameters["CascadeSplitDistance"]?.SetValue(lightSettings.shadowCascadeSplitDistance);
        
        shader.Parameters["ShadowTexelSizeNear"]?.SetValue(new Vector2(1f / (lightSettings.shadowMapResolution * 2), 1f / (lightSettings.shadowMapResolution * 2)));
        shader.Parameters["ShadowTexelSizeFar"]?.SetValue(new Vector2(1f / (lightSettings.shadowMapResolution / 2), 1f / (lightSettings.shadowMapResolution / 2)));
        
        
        shader.Parameters["ShadowBias"]?.SetValue(lightSettings.shadowBias);
        shader.Parameters["ShadowStrength"]?.SetValue(lightSettings.shadowStrength);
        
        shader.Parameters["ReceivesShadows"]?.SetValue(meshRenderer.receivesShadows ? 1f : 0f);
        shader.Parameters["ShadowSoftness"]?.SetValue(1f);
        
        // Set parameters for ignoring fog objects (skybox and HUD)
        if (ignoreFog)
        {
            shader.Parameters["FogDensity"]?.SetValue(0f);
            shader.Parameters["HeightFogDensity"]?.SetValue(0f);
            shader.Parameters["FogColor"]?.SetValue(Vector3.One);
            shader.Parameters["FogHeightStart"]?.SetValue(0f);
            
            // Special handling for skybox objects to ensure proper lighting look
            if (meshRenderer.layer == ModelLayer.Skybox)
            {
                shader.Parameters["AmbientColor"]?.SetValue(new Vector3(1f));
                shader.Parameters["LightColor"]?.SetValue(new Vector3(0f));
                shader.Parameters["SpecularColor"]?.SetValue(new Vector3(0f));
                shader.Parameters["DiffuseColor"]?.SetValue(new Vector3(1f));
            }
        }
        // Normal fog and lighting settings for world objects
        else
        {
            shader.Parameters["FogColor"]?.SetValue(lightSettings.fogColor);
            shader.Parameters["FogDensity"]?.SetValue(lightSettings.fogDensity);
            shader.Parameters["HeightFogDensity"]?.SetValue(lightSettings.heightFogDensity);
            shader.Parameters["FogHeightStart"]?.SetValue(lightSettings.heightFogStart);
            
            shader.Parameters["AmbientColor"]?.SetValue(lightSettings.AmbientLightColor);
            shader.Parameters["LightColor"]?.SetValue(lightSettings.DirectionalLightColor);
            
            shader.Parameters["SpecularColor"]?.SetValue(modelMaterial.SpecularColor);
            shader.Parameters["DiffuseColor"]?.SetValue(modelMaterial.DiffuseColor);
            
            shader.Parameters["EmissiveColor"]?.SetValue(modelMaterial.EmissiveColor);
            shader.Parameters["EmissiveIntensity"]?.SetValue(modelMaterial.EmissiveIntensity);
            
            shader.Parameters["NearPlane"]?.SetValue(lightSettings.shadowNearPlane);
            shader.Parameters["FarPlane"]?.SetValue(lightSettings.shadowFarPlane);
        }
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
            
            shader.Parameters["BoneMatrices"]?.SetValue(final);
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
        
        
        
        lightPassShader.Parameters["World"]?.SetValue(world);
        lightPassShader.Parameters["View"]?.SetValue(renderingCamera.View);
        lightPassShader.Parameters["Projection"]?.SetValue(renderingCamera.Projection);
        lightPassShader.Parameters["WorldInverseTranspose"]?.SetValue(Matrix.Transpose(Matrix.Invert(world)));
        
        // Set light properties for the shader
        lightPassShader.Parameters["LightPos"]?.SetValue(light.transform.Position);
        lightPassShader.Parameters["LightColor"]?.SetValue(light.color.ToVector3());
        lightPassShader.Parameters["LightIntensity"]?.SetValue(light.intensity);
        lightPassShader.Parameters["LightRange"]?.SetValue(light.range);
        
        // Determine if the light casts shadows and set shadow parameters
        bool castsShadows = (renderer != null && light is ConeLight c && renderer.spotLightsCastingShadows.Contains(c) && c.CastsShadows);
        lightPassShader.Parameters["UseShadows"]?.SetValue(castsShadows ? 1f : 0f);
        
        // Set shadow map parameters if the light casts shadows
        if (castsShadows && renderer != null)
        {
            ConeLight cl = light as ConeLight;

            if (renderer.TryGetSpotlightMatrix(cl, out Matrix spotlightMatrix))
            {
                lightPassShader.Parameters["LightViewProjection"]?.SetValue(spotlightMatrix);
                lightPassShader.Parameters["ShadowMap"]?.SetValue(renderer.spotLightShadowMaps[cl]);
                lightPassShader.Parameters["ShadowBias"]?.SetValue(lightSettings.shadowBias);
                
                lightPassShader.Parameters["ShadowTexelSize"]?.SetValue(new Vector2(1f / lightSettings.spotLightShadowMapResolution, 1f / lightSettings.spotLightShadowMapResolution));
                lightPassShader.Parameters["ShadowSoftness"]?.SetValue(1.5f); 
                
                lightPassShader.Parameters["NearPlane"]?.SetValue(lightSettings.shadowNearPlane);
                lightPassShader.Parameters["FarPlane"]?.SetValue(light.range);
            }
            else
                lightPassShader.Parameters["UseShadows"]?.SetValue(0f);
        }

        // Set spotlight-specific parameters if the light is a ConeLight
        if (light is ConeLight cone)
        {
            lightPassShader.Parameters["UseSpot"]?.SetValue(1f);
            
            float halfAngle = MathHelper.ToRadians(cone.Angle) * 0.5f;
            Vector3 dir = cone.transform.Forward;
            dir.Normalize();
            
            lightPassShader.Parameters["InnerCos"]?.SetValue(MathF.Cos(halfAngle * 0.5f));
            lightPassShader.Parameters["OuterCos"]?.SetValue(MathF.Cos(halfAngle));
            lightPassShader.Parameters["LightDir"]?.SetValue(dir);
        }
        else
            lightPassShader.Parameters["UseSpot"]?.SetValue(0f);
        
        bool isSkinned = SetupSkinnedParameters(lightPassShader);
        lightPassShader.CurrentTechnique = isSkinned ? lightPassShader.Techniques["AddLightSkinned"] : lightPassShader.Techniques["AddLight"];
        
        objMesh.DrawWithSetup(lightPassShader, tex =>
            {
                lightPassShader.Parameters["DiffuseTexture"]?.SetValue(tex ?? modelTexture?.Texture);
            });
    }
    
    // Render the model's shadow map from the light's perspective for shadow casting
    internal void RenderShadowMap(Matrix lvp, ConeLight light = null)
    {
        if (meshRenderer.layer != ModelLayer.World || objMesh == null) return;
        
        // If no ConeLight, it's directional light shadow mapping, so use directional shadow shader. Otherwise, use spotlight shadow shader.
        Effect shadowMap = light != null ? shadowDepthSpot : shadowDepthDirectional;
        
        Matrix world = Matrix.CreateScale(modelTransform.scale) * Matrix.CreateFromQuaternion(modelTransform.rotation) * Matrix.CreateTranslation(modelTransform.position);
        
        shadowMap.Parameters["World"]?.SetValue(world);
        shadowMap.Parameters["LightViewProjection"]?.SetValue(lvp);
        shadowMap.Parameters["NearPlane"]?.SetValue(lightSettings.shadowNearPlane);
        shadowMap.Parameters["FarPlane"]?.SetValue(light?.range ?? lightSettings.shadowFarPlane);

        bool isSkinned = SetupSkinnedParameters(shadowMap);
        shadowMap.CurrentTechnique = isSkinned ? shadowMap.Techniques["ShadowDepthSkinned"] : shadowMap.Techniques["ShadowDepth"];

        objMesh.DrawWithSetup(shadowMap, _ => { });
    }
    
    public void Dispose()
    {
        objMesh?.Dispose();
        objMesh = null;
    }
}