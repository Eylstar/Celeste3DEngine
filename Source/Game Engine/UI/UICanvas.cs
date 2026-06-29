using System;
using System.Collections.Generic;
using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.Celeste3DEngine;

/// <summary> Canvas that Contains a list of TextRenderers or Sprites to display. Can be Overlay or World space</summary>
public class UICanvas : GameObject
{
    /// <summary> Type of Canvas. Overlay will draw the UI in front of everything, WorldSpace will place the UI in the 3D World </summary>
    public CanvasType canvasType = CanvasType.Overlay;
    
    List<TextRenderer> textRenderers = new();
    List<UISprite> uiSprites = new();
    
    Transform lastTransform = new();
    
    /// <summary> Returns a ReadOnly List of all the TextRenderers on this Canvas </summary>
    public IReadOnlyList<TextRenderer> GetTextRenderers() => textRenderers;
    
    Effect textShader;

    internal override void Start()
    {
        base.Start();
        scene?.GetRenderer()?.AddCanvas(this);
        textShader = ShadersLoader.WorldText;
        lastTransform = transform.Copy();
    }

    internal override void Removed()
    {
        scene?.GetRenderer()?.RemoveCanvas(this);
        base.Removed();
    }

    /// <summary> Adds a TextRenderer to the Canvas </summary>
    public void AddTextRenderer(TextRenderer textRenderer) => textRenderers.Add(textRenderer);
    
    /// <summary> Adds a UISprite to the Canvas </summary>
    public void AddUISprite(UISprite sprite) => uiSprites.Add(sprite);
    
    /// <summary> Removes a TextRenderer from the Canvas </summary>
    public void RemoveTextRenderer(TextRenderer textRenderer) => textRenderers.Remove(textRenderer);
    
    /// <summary> Removes a UISprite from the Canvas </summary>
    public void RemoveUISprite(UISprite sprite) => uiSprites.Remove(sprite);
    
    /// <summary> Clears all TextRenderers from the Canvas </summary>
    public void ClearCanvas()
    {
        foreach (TextRenderer tr in textRenderers)
            tr.Dispose();
        
        textRenderers.Clear();
        uiSprites.Clear();
    }
    

    internal void RenderTexts()
    {
        if (TransformChanged())
        {
            foreach (TextRenderer t in textRenderers)
                t.isDirty = true;
            
            foreach (UISprite s in uiSprites)
                s.dirty = true;
        }
        
        if (canvasType == CanvasType.Overlay) 
            RenderOverlay();
        else 
            RenderWorld();
        
        lastTransform = transform.Copy();
    }

    void RenderOverlay()
    {
        Engine.Graphics.GraphicsDevice.SetRenderTarget(null);
            
        Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, NonPremultiplied, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone, null, Matrix.Identity);
        
        foreach (TextRenderer t in textRenderers)
        {
            if (t == null || t.textData == null || !t.textData.visible) continue;
            t.PrintOverlayText();
        }
        
        foreach (UISprite s in uiSprites)
        {
            if (s == null || s.texture == null) continue;
            s.DrawImage();
        }
        
        Draw.SpriteBatch.End();
    }

    void RenderWorld()
    {
        GraphicsDevice device = Engine.Graphics.GraphicsDevice;

        Camera3D cam = EngineEntity.Current3DScene?.GetRenderingCamera();
        if (cam == null) return;
        
        Matrix view = cam.View;
        Matrix proj = cam.Projection;

        device.RasterizerState = RasterizerState.CullNone;
        device.SamplerStates[0] = SamplerState.LinearClamp;
        device.DepthStencilState = DepthStencilState.DepthRead;
        device.BlendState = NonPremultiplied;
        
        textShader.CurrentTechnique = textShader.Techniques["UnlitText"];

        textShader.Parameters["World"]?.SetValue(Matrix.Identity);
        textShader.Parameters["View"]?.SetValue(view);
        textShader.Parameters["Projection"]?.SetValue(proj);
        
        foreach (TextRenderer txtRend in textRenderers)
        {
            if (txtRend == null || txtRend.textData == null) continue;
            TextData textData = txtRend.textData;
            if (!textData.visible || textData.alpha <= 0f) continue;

            if (txtRend.isDirty)
            {
                txtRend.BuildWorldQuads(this.transform);
                txtRend.isDirty = false;
            }

            if (txtRend.texture == null) continue;
            
            textShader.Parameters["DiffuseTexture"]?.SetValue((Texture2D)txtRend.texture);
            device.Textures[0] = (Texture2D)txtRend.texture;

            txtRend.DrawWorldText(textShader);
        }
        
        foreach (UISprite s in uiSprites)
        {
            if (s == null || s.texture == null) continue;

            if (s.dirty)
            {
                s.BuildQuad(this.transform);
                s.dirty = false;
            }
            
            textShader.Parameters["DiffuseTexture"]?.SetValue(s.texture);
            device.Textures[0] = s.texture;
            
            s.DrawQuad(textShader);
        }
    }
    
    
    static readonly BlendState NonPremultiplied = new BlendState
    {
        ColorSourceBlend = Blend.SourceAlpha,
        ColorDestinationBlend = Blend.InverseSourceAlpha,
        AlphaSourceBlend = Blend.SourceAlpha,
        AlphaDestinationBlend = Blend.InverseSourceAlpha
    };
    
    bool TransformChanged() => 
        transform.Position != lastTransform.Position ||
        transform.Scale    != lastTransform.Scale    ||
        transform.rotation != lastTransform.rotation;
}

/// <summary> Types of UICanvas. Overlay is drawn on top of everything, WorldSpace is drawn in the 3D world </summary>
public enum CanvasType
{
    Overlay, 
    WorldSpace
}
