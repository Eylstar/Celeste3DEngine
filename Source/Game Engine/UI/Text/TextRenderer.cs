using System;
using System.Collections.Generic;
using FontStashSharp;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.Celeste3DEngine;

public class TextRenderer : IDisposable
{
    public TextData textData = new TextData();
    
    // OVERLAY RENDERING
    
    internal bool isDirty = true;
    
    internal CustomFont font;
    SpriteFontBase dynFont;
    
    SpriteBatchRenderer spriteBatchRenderer = new SpriteBatchRenderer();
    
    
    // WORLD RENDERING
    
    VertexBuffer vertexBuffer;
    VertexBuffer shadowVertexBuffer;
    IndexBuffer indexBuffer;
    IndexBuffer shadowIndexBuffer;
    internal object texture;
    int quadCount;
    int shadowQuadCount;
    
    
    public TextRenderer() : this("Renogare") { }
    
    public TextRenderer(TextData data) : this("Renogare", data) { }
    
    
    public TextRenderer(string fontName)
    {
        font = EngineFonts.Get(fontName);
        dynFont = font?.GetFont(textData.fontSize) ?? EngineFonts.GetDefault().GetFont(textData.fontSize);
        textData.onDirty += () => isDirty = true;
        textData.onFontSizeChanged = size => dynFont = font?.GetFont(size) ?? EngineFonts.GetDefault().GetFont(size);
    }
    
    public TextRenderer(string fontName, TextData data) : this(fontName)
    {
        SetData(data);
    }
    

    public void SetFont(string fontName)
    {
        CustomFont temp = EngineFonts.Get(fontName);
        if (temp == null)
            Logger.Error("Celeste3DEngine", $"Font '{fontName}' not found, keeping previous font");
        else
        {
            font = temp;
            dynFont = font.GetFont(textData.fontSize);
            isDirty = true;
        }
    }
        
    public void SetData(TextData data)
    { 
        textData = data;
        textData.onDirty += () => isDirty = true;
        textData.onFontSizeChanged = size => dynFont = font?.GetFont(size) ?? EngineFonts.GetDefault().GetFont(size);
        dynFont = font?.GetFont(this.textData.fontSize) ?? EngineFonts.GetDefault().GetFont(this.textData.fontSize);
    }



    List<LineDrawData> ComputeLinePositions(TextData data, System.Numerics.Vector2 blockCenter, float rad)
    {
        List<LineDrawData> result = new List<LineDrawData>();
        
        var size = dynFont.MeasureString(data.content, XNAHelper.ToNumerics(new Vector2(data.scale)), data.spacing);
        float cos = (float)Math.Cos(rad);
        float sin = (float)Math.Sin(rad);
        
        string[] lines = data.content.Split('\n');
        float lineHeight = (size.Y / lines.Length) * data.lineHeightMult;
        
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            var lineSize = dynFont.MeasureString(line, XNAHelper.ToNumerics(new Vector2(data.scale)), data.spacing);
            var linePos = blockCenter;
            linePos.Y += (i - (lines.Length - 1) / 2f) * lineHeight;

            switch (data.align)
            {
                case TextAlignment.Left:
                    break;
                case TextAlignment.Center:
                    linePos.X -= lineSize.X / 2f;
                    break;
                case TextAlignment.Right:
                    linePos.X -= lineSize.X;
                    break;
            }

            float dx = linePos.X - blockCenter.X;
            float dy = linePos.Y - blockCenter.Y;
            linePos.X = blockCenter.X + dx * cos - dy * sin;
            linePos.Y = blockCenter.Y + dx * sin + dy * cos;

            result.Add(new LineDrawData { line = line, pos = linePos });
        }
        return result;
    }

    
    
    internal void PrintOverlayText()
    {
        TextData d = new TextData(textData);
    
        ViewportScalingHelper.GetUiTransform(out float uiScale, out Vector2 uiOffset);
    
        Vector2 pos = uiOffset + d.position * uiScale;
        d.position = pos;
        d.scale *= uiScale;
        d.shadowOffset *= uiScale;
        
        var pivot = new System.Numerics.Vector2(0.5f, 0.5f);
        float rad = MathHelper.ToRadians(d.rotation);
        List<LineDrawData> lines = ComputeLinePositions(d, XNAHelper.ToNumerics(d.position), rad);
        
        foreach (LineDrawData lineData in lines)
        {
            if (d.shadow && d.shadowColor.A > 0 && d.shadowOffset != 0)
            {
                var shadowPos = lineData.pos + System.Numerics.Vector2.One * d.shadowOffset;
            
                dynFont.DrawText(spriteBatchRenderer, lineData.line,
                    shadowPos, XNAHelper.ToFSColor(d.shadowColor, d.alpha),
                    rad, pivot, XNAHelper.ToNumerics(new Vector2(d.scale)), 0f, d.spacing, d.lineHeightMult);
            }
    
            if (textData.mainColor != Color.Transparent)
            {
                dynFont.DrawText(spriteBatchRenderer, lineData.line,
                    lineData.pos, XNAHelper.ToFSColor(d.mainColor, d.alpha),
                    rad, pivot, XNAHelper.ToNumerics(new Vector2(d.scale)), 0f, d.spacing, d.lineHeightMult);
            }
        }
    }

    
    internal void BuildWorldQuads(Transform canvasTransform)
    {
        MeshWorldRenderer rend = new MeshWorldRenderer();
        MeshWorldRenderer shadowRend = new MeshWorldRenderer();
        rend.Clear();
        shadowRend.Clear();
        
        TextData d = new TextData(textData);
        if (string.IsNullOrEmpty(d.content) || dynFont == null) return;
        
        var pivot = new System.Numerics.Vector2(0.5f, 0.5f);
        float rad = MathHelper.ToRadians(d.rotation);
        var lines = ComputeLinePositions(d, XNAHelper.ToNumerics(d.position), rad);
        
        foreach (var ld in lines)
        {
            if (d.shadow && d.shadowOffset != 0 && d.shadowColor.A > 0)
            {
                dynFont.DrawText(shadowRend, ld.line,
                    ld.pos + System.Numerics.Vector2.One * d.shadowOffset,
                    XNAHelper.ToFSColor(d.shadowColor, d.alpha),
                    rad, pivot, XNAHelper.ToNumerics(new Vector2(d.scale)), 0f, d.spacing, d.lineHeightMult);
            }

            if (d.mainColor != Color.Transparent)
            {
                dynFont.DrawText(rend, ld.line,
                    ld.pos,
                    XNAHelper.ToFSColor(d.mainColor, d.alpha),
                    rad, pivot, XNAHelper.ToNumerics(new Vector2(d.scale)), 0f, d.spacing, d.lineHeightMult);
            }
        }
        
        if (rend.vertices.Count == 0) return;
        
        float pixelsPerUnit = d.pixelsPerUnit;
        
        float uniformScale = Math.Max(canvasTransform.Scale.X, Math.Max(canvasTransform.Scale.Y, canvasTransform.Scale.Z));
        
        Vector3 right = canvasTransform.Right;
        Vector3 up = canvasTransform.Up;
        Vector3 origin = canvasTransform.Position + right * d.position.X * uniformScale - up * d.position.Y * uniformScale;
        
        float unit = 1f / pixelsPerUnit * uniformScale;
        
        GraphicsDevice gd = Engine.Graphics.GraphicsDevice;
        
        // TEXT
        
        var vertices = rend.vertices;
        VertexPositionColorTexture[] worldVerts = new VertexPositionColorTexture[rend.vertices.Count];
        
        for (int i = 0; i < vertices.Count; i++)
        {
            var v = vertices[i];
            float x2d = v.Position.X;
            float y2d = v.Position.Y;
            
            Vector3 pos = origin + right * (x2d * unit) - up * (y2d * unit);
            worldVerts[i] = new VertexPositionColorTexture(pos, XNAHelper.ToXNAColor(v.Color), XNAHelper.ToXNA(v.TextureCoordinate));
        }
        
        var idxArray = rend.indices.ToArray();
        quadCount = vertices.Count / 4;
        texture = rend.lastTexture;
            
        vertexBuffer?.Dispose();
        indexBuffer?.Dispose();
        vertexBuffer = new VertexBuffer(gd, typeof(VertexPositionColorTexture), worldVerts.Length, BufferUsage.WriteOnly);
        vertexBuffer.SetData(worldVerts);
        indexBuffer = new IndexBuffer(gd, typeof(short), idxArray.Length, BufferUsage.WriteOnly);
        indexBuffer.SetData(idxArray);
        
        // SHADOW

        if (shadowRend.vertices.Count == 0) return;
        
        var shadowVertices = shadowRend.vertices;
        VertexPositionColorTexture[] shadowWorldVerts = new VertexPositionColorTexture[shadowVertices.Count];
        
        for (int i = 0; i < shadowVertices.Count; i++)
        {
            var v = shadowVertices[i];
            float x2d = v.Position.X;
            float y2d = v.Position.Y;
            
            Vector3 pos = origin + right * (x2d * unit) - up * (y2d * unit);
            shadowWorldVerts[i] = new VertexPositionColorTexture(pos, XNAHelper.ToXNAColor(v.Color), XNAHelper.ToXNA(v.TextureCoordinate));
        }
        
        var shadowIdxArray = shadowRend.indices.ToArray();
        shadowQuadCount = shadowVertices.Count / 4;
        
        shadowVertexBuffer?.Dispose();
        shadowIndexBuffer?.Dispose();
        shadowVertexBuffer = new VertexBuffer(gd, typeof(VertexPositionColorTexture), shadowWorldVerts.Length, BufferUsage.WriteOnly);
        shadowVertexBuffer.SetData(shadowWorldVerts);
        shadowIndexBuffer = new IndexBuffer(gd, typeof(short), shadowIdxArray.Length, BufferUsage.WriteOnly);
        shadowIndexBuffer.SetData(shadowIdxArray);
    }
    

    internal void DrawWorldText(Effect fx)
    {
        GraphicsDevice gd = Engine.Graphics.GraphicsDevice;
        
        if (quadCount == 0 || vertexBuffer == null || indexBuffer == null) return;

        if (shadowQuadCount > 0 && shadowVertexBuffer != null && shadowIndexBuffer != null)
        {
            gd.SetVertexBuffer(shadowVertexBuffer);
            gd.Indices = shadowIndexBuffer;
            
            foreach (EffectPass pass in fx.CurrentTechnique.Passes)
            {
                pass.Apply();
                gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, shadowQuadCount * 4, 0, shadowQuadCount * 2);
            }
        }
        
        gd.SetVertexBuffer(vertexBuffer);
        gd.Indices = indexBuffer;
            
        foreach (EffectPass pass in fx.CurrentTechnique.Passes)
        {
            pass.Apply();
            gd.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, quadCount * 4, 0, quadCount * 2);
        }
    }
    
    public void Dispose()
    {
        vertexBuffer?.Dispose();
        indexBuffer?.Dispose();
        shadowVertexBuffer?.Dispose();
        shadowIndexBuffer?.Dispose();
        vertexBuffer = null;
        indexBuffer = null;
        shadowIndexBuffer = null;
        shadowVertexBuffer = null;
        texture = null;
    }
    
    struct LineDrawData
    {
        public string line;
        public System.Numerics.Vector2 pos;
    }
}