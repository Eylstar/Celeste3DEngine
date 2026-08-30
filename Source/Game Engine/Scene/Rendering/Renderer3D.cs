using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace Celeste.Mod.Celeste3DEngine;

//One per scene, handles 3D rendering of models and HUD elements
public sealed class Renderer3D : IDisposable
{
    public bool enabled = true;
    
    internal bool isHDMode = false;
    public bool IsHDMode => isHDMode;
    
    Model3D skyboxModel;
    List<Model3D> ModelsList = new();
    List<Model3D> ForegroundModels = new();
    List<Model3D> HUDModelsList = new();
    List<UICanvas> HUDCanvases = new();
    
    List<Light> LightsList = new();
    
    internal List<ConeLight> spotLightsCastingShadows = new();
    Dictionary<ConeLight, Matrix> spotLightMatrices = new();
    internal Dictionary<ConeLight, RenderTarget2D> spotLightShadowMaps = new();
    
    RenderTarget2D sceneRenderTarget;
    RenderTarget2D foregroundRenderTarget;
    
    //RenderTarget2D shadowRenderTargetNear;
    //RenderTarget2D shadowRenderTargetFar;
    RenderTarget2D shadowRenderTarget;
    
    //internal Texture2D shadowTextureNear => shadowRenderTargetNear;
    //internal Texture2D shadowTextureFar => shadowRenderTargetFar;
    
    internal Texture2D shadowTexture => shadowRenderTarget;
    
    //internal Matrix lightViewProjMatrixNear;
    //internal Matrix lightViewProjMatrixFar;
    internal Matrix lightViewProjMatrix;
    
    LightingSettings lightingSettings => EngineEntity.Current3DScene?.GetLightingSettings();
    
    
    internal Renderer3D()
    {
        CreateRenderTarget();
        CreateShadowMapsRenderTarget();
    }

#region Render Targets Creation
    void CreateRenderTarget()
    {
        sceneRenderTarget?.Dispose();
        foregroundRenderTarget?.Dispose();
        Viewport vp = Engine.Instance.GraphicsDevice.Viewport;
        sceneRenderTarget = new RenderTarget2D(Engine.Graphics.GraphicsDevice, vp.Width, vp.Height,false, SurfaceFormat.Color, DepthFormat.Depth24);
        foregroundRenderTarget = new RenderTarget2D(Engine.Graphics.GraphicsDevice, vp.Width, vp.Height,false, SurfaceFormat.Color, DepthFormat.Depth24);
    }
    
    
    void CreateShadowMapsRenderTarget()
    {
        //shadowRenderTargetNear?.Dispose();
        //shadowRenderTargetFar?.Dispose();
        shadowRenderTarget?.Dispose();
        
        int size = lightingSettings != null ? lightingSettings.shadowMapResolution : 4096;
        
        //shadowRenderTargetNear = new RenderTarget2D(Engine.Graphics.GraphicsDevice,size * 2,size * 2,false, SurfaceFormat.Single, DepthFormat.Depth24);
        //shadowRenderTargetFar = new RenderTarget2D(Engine.Graphics.GraphicsDevice,size / 2,size / 2,false, SurfaceFormat.Single, DepthFormat.Depth24);
        shadowRenderTarget = new RenderTarget2D(Engine.Graphics.GraphicsDevice,size,size,false, SurfaceFormat.Single, DepthFormat.Depth24);
    }
    
    
    // Ensure the render target matches the current viewport size and shadow map sizes
    void EnsureRenderTragetSize()
    {
        Viewport vp = Engine.Viewport;
        if (sceneRenderTarget == null || sceneRenderTarget.Width != vp.Width || sceneRenderTarget.Height != vp.Height)
            CreateRenderTarget();

        //if (shadowRenderTargetNear == null || shadowRenderTargetFar == null || (lightingSettings != null && (shadowRenderTargetFar.Width != lightingSettings.shadowMapResolution / 2 || shadowRenderTargetNear.Width != lightingSettings.shadowMapResolution * 2)))
        if (shadowRenderTarget == null || (lightingSettings != null && (shadowRenderTarget.Width != lightingSettings.shadowMapResolution )))
            CreateShadowMapsRenderTarget();
    }

#endregion
    
    

    /// <summary> Sets whether the Renderer should render in HD mode or not </summary>
    public void SetHDMode(bool hdMode) => isHDMode = hdMode;
    

#region Rendering Lists Updates

    internal void AddModel(GameObject go) => CreateModel(go);
        
    internal void AddModels(List<GameObject> objs)
    {
        foreach (GameObject go in objs)
            CreateModel(go);
    }

    internal void UpdateLayer(MeshRenderer go)
    {
        if (go == null) return;
        ModelsList.Remove(go.GetModel());
        HUDModelsList.Remove(go.GetModel());
        ForegroundModels.Remove(go.GetModel());
        
        AddModelToList(go.GetModel());
    }
        
    internal void RemoveModelFromLists(GameObject go)
    {
        Model3D model = go?.GetComponent<MeshRenderer>()?.GetModel();
        if (model == null) return;
            
        ModelsList.Remove(model);
        HUDModelsList.Remove(model);
        ForegroundModels.Remove(model);
    }
        
    internal void AddCanvas(UICanvas uiCanvas)
    {
        if (!HUDCanvases.Contains(uiCanvas))
            HUDCanvases.Add(uiCanvas);
    }
        
    internal void RemoveCanvas(UICanvas uiCanvas)
    {
        if (HUDCanvases.Contains(uiCanvas))
            HUDCanvases.Remove(uiCanvas);
    }
        
    internal void AddLight(Light light)
    {
        if (!LightsList.Contains(light))
            LightsList.Add(light);
    }
        
    internal void RemoveLight(Light light)
    {
        if (LightsList.Contains(light))
            LightsList.Remove(light);
    }

#endregion



    
    // Creates and assigns a Model3D to the given GraphicGameObject based on its ModelInfo data
    void CreateModel(GameObject go)
    {
        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr == null || String.IsNullOrEmpty(mr.modelName))
        {
            Logger.Error("Renderer3D", $"GameObject '{go.name}' is missing a meshrenderer or model path and not Model3D wil be created.");
            return;
        }
        
        Model3D model = new Model3D(go);
        mr.SetModel(model);
        AddModelToList(model);
    }

    void AddModelToList(Model3D model)
    {
        switch (model.gameObject.GetComponent<MeshRenderer>().layer)
        {
            case ModelLayer.World:
                ModelsList.Add(model);
                break;
            case ModelLayer.Foreground:
                ForegroundModels.Add(model);
                break;
            case ModelLayer.HUD:
                HUDModelsList.Add(model);
                break;
            case ModelLayer.Skybox:
                skyboxModel = model;
                break;
        }
    }
    
    
#region Before Render
    
// Prepares the scene for rendering by updating matrices and rendering shadow maps and models to the scene render target
    internal void BeforeRender()
    {
        if(!enabled) return;
        
        EnsureRenderTragetSize();
        
        // Directional Light Shadow Map Rendering
        BRDirectionalLightShadowMap();
        
        // Spot Light Shadow Map Rendering
        BrSceneLightsShadowMap();
        
        // World Models Rendering
        BRWorldModels();
        

        // UI Canvases and UI Models Rendering
        BRHUDModels();
        
        ResetDeviceStates();
    }
    
    
    // Renders the shadow map for the current scene from the directional light's perspective
    void BRDirectionalLightShadowMap()
    {
        if (lightingSettings == null || !lightingSettings.DirectionalLightCastsShadows) return;
        
        UpdateLightMatrices();
        
        GraphicsDevice device = Engine.Graphics.GraphicsDevice;
        Viewport old = device.Viewport;
        
        device.DepthStencilState = DepthStencilState.Default;
        device.RasterizerState = RasterizerState.CullNone;
        device.BlendState = BlendState.Opaque;
        
        /*device.SetRenderTarget(shadowRenderTargetNear);
        device.Viewport = new Viewport(0, 0, shadowRenderTargetNear.Width, shadowRenderTargetNear.Height);
        device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.White, 1f, 0);
        
        foreach (Model3D m in ModelsList)
        {
            MeshRenderer mr = m.gameObject.GetComponent<MeshRenderer>();
            if (mr != null && m.gameObject.enabled && mr.castsShadows)
                m.RenderShadowMap(lightViewProjMatrixNear);
        }
        
        device.SetRenderTarget(shadowRenderTargetFar);
        device.Viewport = new Viewport(0, 0, shadowRenderTargetFar.Width, shadowRenderTargetFar.Height);
        device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.White, 1f, 0);
        
        foreach (Model3D m in ModelsList)
        {
            MeshRenderer mr = m.gameObject.GetComponent<MeshRenderer>();
            if (mr != null && m.gameObject.enabled && mr.castsShadows)
                m.RenderShadowMap(lightViewProjMatrixFar);
        }*/
        
        device.SetRenderTarget(shadowRenderTarget);
        device.Viewport = new Viewport(0, 0, shadowRenderTarget.Width, shadowRenderTarget.Height);
        device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.White, 1f, 0);
        
        foreach (Model3D m in ModelsList)
        {
            MeshRenderer mr = m.gameObject.GetComponent<MeshRenderer>();
            if (mr != null && m.gameObject.enabled && mr.castsShadows)
                m.RenderShadowMap(lightViewProjMatrix);
        }
        
        device.SetRenderTarget(null);
        device.Viewport = old;
    }
    
    // Updates the directional light view and projection matrices based on the current camera and lighting settings
    void UpdateLightMatrices()
    {
        if (lightingSettings == null || !lightingSettings.DirectionalLightCastsShadows) return;
        
        Scene3D scene = EngineEntity.Current3DScene;
        if (scene == null) return;

        Camera3D cam = scene.GetRenderingCamera();
        if (cam == null) return;
        
        // Determine light direction
        Vector3 dir = lightingSettings.DirectionalLightDirection;
        if (dir.LengthSquared() < 1e-6f) dir = Vector3.Down;
        dir.Normalize();
        
        float camNear = Math.Max(0.01f, lightingSettings.shadowNearPlane);
        //float split = lightingSettings.shadowCascadeSplitDistance;
        float camFar = lightingSettings.shadowFarPlane;
        
        // Build matrices for near and far splits of the shadow map
        //lightViewProjMatrixNear = BuildDirectionalCascadeMatrix(cam, dir, camNear, split, shadowRenderTargetNear.Width);
        //lightViewProjMatrixFar = BuildDirectionalCascadeMatrix(cam, dir, split, camFar, shadowRenderTargetFar.Width);
        lightViewProjMatrix = BuildDirectionalShadowMatrix(cam, dir, camNear, camFar, shadowRenderTarget.Width);
    }


    // Build the matrixs for the directional light (near and far)
    Matrix BuildDirectionalShadowMatrix(Camera3D cam, Vector3 lightDir, float cascadeNear, float cascadeFar, float mapSize)
    {
        Vector3[] frustumCornersWS = FrustumHelper.GetFrsutumCorners(cam, cascadeNear, cascadeFar);
        
        Vector3 frustumCenter = Vector3.Zero;
        foreach (Vector3 corner in frustumCornersWS)
            frustumCenter += corner;
        frustumCenter /= frustumCornersWS.Length;
        
        // Choose an up vector that is not parallel to the light direction
        Vector3 up = Vector3.Up;
        if (Math.Abs(Vector3.Dot(up, lightDir)) > 0.95f)
            up = Vector3.Forward;
        
        float radius = 0f;
        foreach (Vector3 corner in frustumCornersWS)
        {
            float dist = Vector3.Distance(frustumCenter, corner);
            if (dist > radius) radius = dist;
        }
        
        radius = (float)Math.Ceiling(radius * 16f) / 16f; // Round up to reduce swimming
        
        // Create light view and projection matrices for orthographic shadow mapping
        Vector3 lightPos = frustumCenter - lightDir * (radius + 100f);
        Matrix lightView = Matrix.CreateLookAt(lightPos, frustumCenter, up);
        
        Vector3 min = new Vector3(float.MaxValue);
        Vector3 max = new Vector3(float.MinValue);
        
        foreach (Vector3 corner in frustumCornersWS)
        {
            Vector3 cornerLS = Vector3.Transform(corner, lightView);
            min = Vector3.Min(min, cornerLS);
            max = Vector3.Max(max, cornerLS);
        }

        float padding = 50f;
        min.Z -= padding;
        max.Z += padding;
        
        min.X = -radius;
        max.X = radius;
        min.Y = -radius;
        max.Y = radius;
        
        float extentX = max.X - min.X;
        float extentY = max.Y - min.Y;
        
        // Snap the light's orthographic projection to texel-sized increments to reduce shadow swimming
        float worldUnitsPerTexelX = extentX / mapSize;
        float worldUnitsPerTexelY = extentY / mapSize;
        
        min.X = (float)Math.Floor(min.X / worldUnitsPerTexelX) * worldUnitsPerTexelX;
        max.X = (float)Math.Ceiling(max.X / worldUnitsPerTexelX) * worldUnitsPerTexelX;
        min.Y = (float)Math.Floor(min.Y / worldUnitsPerTexelY) * worldUnitsPerTexelY;
        max.Y = (float)Math.Ceiling(max.Y / worldUnitsPerTexelY) * worldUnitsPerTexelY;
        
        Matrix lightProj = Matrix.CreateOrthographicOffCenter(min.X, max.X, min.Y, max.Y, -max.Z, -min.Z);
        return lightView * lightProj;
    }
    
    
    // Renders the shadow map for the spot lights that cast shadows in the scene
    void BrSceneLightsShadowMap()
    {
        spotLightsCastingShadows = FindShadowCastingLights();
        if (lightingSettings == null || spotLightsCastingShadows.Count == 0) return;
        
        GraphicsDevice device = Engine.Graphics.GraphicsDevice;
        Viewport old = device.Viewport;
        
        device.DepthStencilState = DepthStencilState.Default;
        device.RasterizerState = RasterizerState.CullNone;
        device.BlendState = BlendState.Opaque;
        
        spotLightMatrices.Clear();

        foreach (ConeLight spotShadowCaster in spotLightsCastingShadows)
        {
            device.SetRenderTarget(spotLightShadowMaps[spotShadowCaster]);
            device.Viewport = new Viewport(0, 0, spotLightShadowMaps[spotShadowCaster].Width, spotLightShadowMaps[spotShadowCaster].Height);
            
            device.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.White, 1f, 0);
            
            ComputeConeLightMatrix(spotShadowCaster, out Matrix spotlightVPM);
            spotLightMatrices[spotShadowCaster] = spotlightVPM;
            
            Vector3 lightPos = spotShadowCaster.transform.Position;
            float range = spotShadowCaster.range;
            
            foreach (Model3D m in ModelsList)
            {
                MeshRenderer mr = m.gameObject.GetComponent<MeshRenderer>();
                if (mr == null || !m.gameObject.enabled || !mr.castsShadows || m.GetMesh() == null) 
                    continue;
                
                Vector3 modelPos = GetWorldBoundsCenter(m);
                float modelRadius = m.GetMesh().boundingSphereRadius * Math.Max(m.gameObject.transform.scale.X, Math.Max(m.gameObject.transform.scale.Y, m.gameObject.transform.scale.Z));
                float cullDistance = range + modelRadius;
                
                if (Vector3.DistanceSquared(lightPos, modelPos) > cullDistance * cullDistance) continue;
                
                m.RenderShadowMap(spotlightVPM, spotShadowCaster);
            }

            foreach (Model3D m in ForegroundModels)
            {
                MeshRenderer mr = m.gameObject.GetComponent<MeshRenderer>();
                if (mr == null || !m.gameObject.enabled || !mr.castsShadows || m.GetMesh() == null) 
                    continue;
                
                Vector3 modelPos = GetWorldBoundsCenter(m);
                float modelRadius = m.GetMesh().boundingSphereRadius * Math.Max(m.gameObject.transform.scale.X, Math.Max(m.gameObject.transform.scale.Y, m.gameObject.transform.scale.Z));
                float cullDistance = range + modelRadius;
                
                if (Vector3.DistanceSquared(lightPos, modelPos) > cullDistance * cullDistance) continue;
                
                m.RenderShadowMap(spotlightVPM, spotShadowCaster);
            }
        }
        
        device.SetRenderTarget(null);
    }
    
    
    // Returns the view-projection matrix for a given cone light
    internal void ComputeConeLightMatrix(ConeLight spotLight, out Matrix spotlightVPM)
    {
        Vector3 pos = spotLight.transform.Position;
        Vector3 dir = spotLight.transform.Forward;
        if (dir.LengthSquared() < 1e-6f) dir = Vector3.Forward;
        dir.Normalize();
        
        Vector3 up = spotLight.transform.Up;
        if (up.LengthSquared() < 1e-6f) up = Vector3.Up;
        up.Normalize();
        
        if (Math.Abs(Vector3.Dot(up, dir)) > 0.95f)
            up = Math.Abs(Vector3.Dot(Vector3.Right, dir)) > 0.95f ? Vector3.Up : Vector3.Right;
        
        Matrix view = Matrix.CreateLookAt(pos, pos + dir, up);
        
        float near = lightingSettings != null ? lightingSettings.shadowNearPlane : 0.1f;
        float far = Math.Max(near + 0.1f, spotLight.range);
        float fov = MathHelper.ToRadians(spotLight.Angle);
        
        Matrix proj = Matrix.CreatePerspectiveFieldOfView(fov * 1.05f, 1f, near, far);
        spotlightVPM = view * proj;
    }
    
    
    // Renders all world models to the scene render target
    void BRWorldModels()
    {
        Camera3D cam = EngineEntity.Current3DScene.GetRenderingCamera();
        BoundingFrustum frustum = new BoundingFrustum(cam.View * cam.Projection);
        
        Skybox sky = skyboxModel.gameObject as Skybox;
        
        //Classic RenderTarget
        
        Engine.Graphics.GraphicsDevice.SetRenderTarget(sceneRenderTarget);
        Engine.Graphics.GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, new Color(0,0,0,0), 1f, 0);

        if(sky != null && !sky.foregroundRendererEnabled && skyboxModel.gameObject.enabled)
            skyboxModel?.BeforeRender(lightViewProjMatrix, shadowTexture);
            //skyboxModel?.BeforeRender(lightViewProjMatrixNear, lightViewProjMatrixFar, shadowTextureNear, shadowTextureFar);
        
        foreach (Model3D m in ModelsList)
        {
            if (!m.gameObject.enabled || !m.gameObject.GetComponent<MeshRenderer>().isVisible) continue;

            if (m.gameObject.GetComponent<MeshRenderer>().isFrustumCulled)
            {
                Vector3 s = m.gameObject.transform.scale;
                float max = Math.Max(s.X, Math.Max(s.Y, s.Z));
                BoundingSphere sphere = new BoundingSphere(GetWorldBoundsCenter(m), m.GetMesh().boundingSphereRadius * max);
                if (frustum.Contains(sphere) == ContainmentType.Disjoint)
                {
                    continue;
                }
            }
            
            //m.BeforeRender(lightViewProjMatrixNear, lightViewProjMatrixFar, shadowTextureNear, shadowTextureFar);
            m.BeforeRender(lightViewProjMatrix, shadowTexture);
        }
        
        ApplyLightToModels(ModelsList);
        
        BRWorldCanvases(false);
        
        
        //Foreground RenderTarget

        Engine.Graphics.GraphicsDevice.SetRenderTarget(foregroundRenderTarget);
        Engine.Graphics.GraphicsDevice.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, new Color(0,0,0,0), 1f, 0);
        
        if(sky != null && sky.foregroundRendererEnabled && skyboxModel.gameObject.enabled)
            skyboxModel?.BeforeRender(lightViewProjMatrix, shadowTexture);
            //skyboxModel?.BeforeRender(lightViewProjMatrixNear, lightViewProjMatrixFar, shadowTextureNear, shadowTextureFar);

        foreach (Model3D m in ForegroundModels)
        {
            if (!m.gameObject.enabled || !m.gameObject.GetComponent<MeshRenderer>().isVisible) continue;

            if (m.gameObject.GetComponent<MeshRenderer>().isFrustumCulled)
            {
                Vector3 s = m.gameObject.transform.scale;
                float max = Math.Max(s.X, Math.Max(s.Y, s.Z));
                BoundingSphere sphere = new BoundingSphere(GetWorldBoundsCenter(m), m.GetMesh().boundingSphereRadius * max);
                if (frustum.Contains(sphere) == ContainmentType.Disjoint)
                {
                    continue;
                }
            }
            
            //m.BeforeRender(lightViewProjMatrixNear, lightViewProjMatrixFar, shadowTextureNear, shadowTextureFar);
            m.BeforeRender(lightViewProjMatrix, shadowTexture);
        }
        
        ApplyLightToModels(ForegroundModels);
        
        BRWorldCanvases(true);
    }
    

    void ApplyLightToModels(List<Model3D> models)
    {
        if (models.Count == 0) return;
        
        GraphicsDevice device = Engine.Graphics.GraphicsDevice;
        
        device.DepthStencilState = DepthStencilState.DepthRead;
        device.RasterizerState = RasterizerState.CullNone;
        device.BlendState = BlendState.Additive;
        
        List<Model3D> litModels = models.FindAll(m => 
            m.gameObject.enabled && 
            m.gameObject.GetComponent<MeshRenderer>() != null &&
            m.gameObject.GetComponent<MeshRenderer>().material.isLit && 
            m.gameObject.GetComponent<MeshRenderer>().isVisible);

        foreach (Light light in LightsList)
        {
            if (!light.enabled) continue;
            foreach (Model3D m in litModels)
            {
                if (m.GetMesh() == null) continue;
                
                Vector3 modelPos = GetWorldBoundsCenter(m);
                float modelRadius = m.GetMesh().boundingSphereRadius * Math.Max(m.gameObject.transform.scale.X, Math.Max(m.gameObject.transform.scale.Y, m.gameObject.transform.scale.Z));
                float cullDistance = light.range + modelRadius;

                if (Vector3.DistanceSquared(light.transform.Position, modelPos) > cullDistance * cullDistance) continue;
                
                m.RenderLightPass(light);
            }
        }
        
        device.BlendState = BlendState.Opaque;
        device.DepthStencilState = DepthStencilState.Default;
    }
    
    
    // Renders all world space UI canvases to the scene render target
    void BRWorldCanvases(bool isForeground)
    {
        foreach (UICanvas canvas in HUDCanvases)
        {
            if (canvas != null && canvas.canvasType == CanvasType.WorldSpace && canvas.isRendererInForeground == isForeground)
                canvas.RenderTexts();
        }
    }
    
    
    // Renders all HUD models to the scene render target
    void BRHUDModels()
    {
        Engine.Graphics.GraphicsDevice.Clear(ClearOptions.DepthBuffer, new Color(0,0,0,0), 1f, 0);

        foreach (Model3D c in HUDModelsList)
            if (c.gameObject.enabled)
                //c.BeforeRender(lightViewProjMatrixNear, lightViewProjMatrixFar, shadowTextureNear, shadowTextureFar);
                c.BeforeRender(lightViewProjMatrix, shadowTexture);
    }
    
    
    // Resets the graphics device states to default values after rendering operations
    void ResetDeviceStates()
    {
        GraphicsDevice d = Engine.Graphics.GraphicsDevice;
        d.SetRenderTarget(null);
        d.BlendState = BlendState.Opaque;
        d.DepthStencilState = DepthStencilState.Default;
        d.RasterizerState = RasterizerState.CullNone;
        
        for (int i = 0; i < 4; i++)
        {
            d.Textures[i] = null;
            d.SamplerStates[i] = SamplerState.LinearClamp;
        }
    }

#endregion
    

    // RENDER

    internal void RenderHDBG()
    {
        if (!enabled || !isHDMode) return;
        RenderScene(sceneRenderTarget, new Rectangle(0, 0, Engine.Viewport.Width, Engine.Viewport.Height));
    }
    
    internal void RenderWorld()
    {
        if (!enabled || isHDMode) return;
        RenderScene(sceneRenderTarget, GameplayBuffers.Level.Bounds);
    }
    
    internal void RenderForeground()
    {
        if (enabled && isHDMode)
            RenderScene(foregroundRenderTarget, new Rectangle(0, 0, Engine.Viewport.Width, Engine.Viewport.Height));
        else if (enabled && !isHDMode)
            RenderScene(foregroundRenderTarget, GameplayBuffers.Level.Bounds);
    }
    

    // Renders the scene render target to the screen
    void RenderScene(RenderTarget2D target, Rectangle rect)
    {
        EnsureRenderTragetSize();
        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.LinearClamp, DepthStencilState.Default, RasterizerState.CullNone);
        Draw.SpriteBatch.Draw(target, rect, Color.White);
        Draw.SpriteBatch.End();
    }
    
    // Renders all overlay HUD texts on top of the scene rendering (called after RenderScene)
    internal void RenderOverlayCanvases()
    {
        if (HUDCanvases == null || HUDCanvases.Count == 0) return;

        foreach (UICanvas canvas in HUDCanvases)
        {
            if (canvas != null && (canvas.GetTextRenderers().Count != 0 || canvas.GetUISprites().Count != 0))
            {
                if (canvas.canvasType == CanvasType.Overlay)
                    canvas.RenderTexts();
            }
        }
    }
    
    
    // Finds and returns a list of cone lights that cast shadows, limited by the maximum allowed in lighting settings
    List<ConeLight> FindShadowCastingLights()
    {
        int maxLights = lightingSettings.maxShadowedLights;
        int foundCount = 0;
        int overhead = 0;
        List<ConeLight> shadowLights = new();
        
        foreach (Light light in LightsList)
        {
            if (light is ConeLight cone && cone.enabled && cone.CastsShadows)
            {
                if (foundCount >= maxLights)
                {
                    overhead++;
                    continue;
                }
                foundCount++;
                shadowLights.Add(cone);
            }
        }

        if (overhead > 0)
            Logger.Info("Celeste3DEngine", $"Renderer3D: There are {overhead} overhead shadow-casting lights that exceed the max of {maxLights} casting ConeLight set in LightingSettings.");
        
        ProcessLightsRenderTargets(shadowLights);
        
        return shadowLights;
    }

    void ProcessLightsRenderTargets(List<ConeLight> shadowLights)
    {
        foreach (ConeLight cone in shadowLights)
        {
            if (!spotLightShadowMaps.ContainsKey(cone))
            {
                int size = lightingSettings != null ? lightingSettings.spotLightShadowMapResolution : 1024;
                RenderTarget2D map = new RenderTarget2D(Engine.Graphics.GraphicsDevice, size, size, false, SurfaceFormat.Single, DepthFormat.Depth24);
                spotLightShadowMaps[cone] = map;
            }
        }
        
        List<ConeLight> toRemove = new();

        foreach (var kvp in spotLightShadowMaps)
            if (!shadowLights.Contains(kvp.Key))
                toRemove.Add(kvp.Key);

        foreach (ConeLight light in toRemove)
        {
            spotLightShadowMaps[light].Dispose();
            spotLightShadowMaps.Remove(light);
        }
    }


    static Vector3 GetWorldBoundsCenter(Model3D m)
    {
        var t = m.gameObject.transform;
        Vector3 localCenter = m.GetMesh().boundingSphereCenter;
        
        Matrix worldMatrix = Matrix.CreateScale(t.scale) * Matrix.CreateFromQuaternion(t.rotation) * Matrix.CreateTranslation(t.Position);
        return Vector3.Transform(localCenter, worldMatrix);
    }
    
    
    public void Dispose()
    {
        foreach (Model3D m in ModelsList) m.Dispose();
        foreach (Model3D c in HUDModelsList) c.Dispose();
        foreach (UICanvas ca in HUDCanvases) ca.ClearCanvas();
        
        ModelsList.Clear();
        HUDModelsList.Clear();
        HUDCanvases.Clear();
        LightsList.Clear();
        
        sceneRenderTarget?.Dispose();
        foregroundRenderTarget?.Dispose();
        //shadowRenderTargetNear?.Dispose();
        //shadowRenderTargetFar?.Dispose();
        shadowRenderTarget?.Dispose();
        
        foreach (var kvp in spotLightShadowMaps)
            kvp.Value.Dispose();
    }
    
    internal bool TryGetSpotlightMatrix(ConeLight spotlight, out Matrix matrix)
    {
        return spotLightMatrices.TryGetValue(spotlight, out matrix);
    }
}