using System;

using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.Celeste3DEngine;

/// <summary> A Sprite that can be drawn in an UICanvas</summary>
public class UISprite
{
    string texturePath;
    VirtualTexture vTexture;
    internal Texture2D texture => vTexture?.Texture;
    
    Vector2 position;
    Vector2 scale;
    Color color;
    float alpha;
    float rotation;
    
    /// <summary> The position of the sprite in UI coordinates </summary>
    public Vector2 Position => position;
    /// <summary> The scale of the sprite in UI coordinates </summary>
    public Vector2 Scale => scale;
    /// <summary> The color/tint of the sprite </summary>
    public Color Color => color;
    /// <summary> The transparency of the sprite </summary>
    public float Alpha => alpha;
    /// <summary> The rotation of the sprite in degrees </summary>
    public float Rotation => rotation;
    
    
    VertexBuffer vertexBuffer;
    IndexBuffer indexBuffer;
    int quadCount;

    float _pixelPerUnit = 200f;
    
    /// <summary> The number of pixels that correspond to one unit in world space. Default is 200 pixels per unit. </summary>
    public float pixelPerUnit
    {
        get => _pixelPerUnit;
        set
        {
            if (value <= 0f)
            {
                Logger.Error("UISprite", "pixelPerUnit must be greater than 0. Value not changed.");
                return;
            }
            _pixelPerUnit = value;
            dirty = true;
        }
    }
    
    internal bool dirty = true;
    
    /// <summary> Creates a new UISprite with the specified texture path and default position (0,0) and scale (1,1) </summary>
    public UISprite(string texturePath) : this(texturePath, Vector2.Zero, Vector2.One) { }
    
    /// <summary> Creates a new UISprite with the specified texture path and position </summary>
    public UISprite(string texturePath, Vector2 position, Vector2 scale, Color? color = null, float alpha = 1f, float rotation = 0f)
    {
        this.texturePath = texturePath;
        this.position = position;
        this.scale = scale;
        this.color = color ?? Color.White;
        this.alpha = alpha;
        this.rotation = rotation;
        
        string fullPath = EnginePaths.customTexturesPath + "/" + texturePath;
        vTexture = TexturesCache.Get(fullPath);
    }
    
    /// <summary> Sets the position of the sprite in UI coordinates </summary>
    public void SetPosition(Vector2 newPos)
    {
        position = newPos;
        dirty = true;
    }
    
    /// <summary> Sets the scale of the sprite in UI coordinates </summary>
    public void SetScale(Vector2 newScale)
    {
        scale = newScale;
        dirty = true;
    }
    
    /// <summary> Sets the color/tint of the sprite </summary>
    public void SetColor(Color newColor)
    {
        color = newColor;
        dirty = true;
    }
    
    /// <summary> Sets the transparency of the sprite </summary>
    public void SetAlpha(float newAlpha)
    {
        alpha = newAlpha;
        dirty = true;
    }
    
    /// <summary> Sets the rotation of the sprite in degrees </summary>
    public void SetRotation(float newRotation)
    {
        rotation = newRotation;
        dirty = true;
    }
    
    internal void DrawImage()
    {
        if (texture == null) return;
        
        ViewportScalingHelper.GetUiTransform(out float uiScale, out Vector2 uiOffset);
        
        Vector2 pos = uiOffset + position * uiScale;

        Vector2 s = scale * uiScale;
        Vector2 origin = new Vector2(texture.Width / 2f, texture.Height / 2f);
        float rad = MathHelper.ToRadians(rotation);
        
        Draw.SpriteBatch.Draw(texture, pos, null, new Color(color, alpha), rad, origin, s, SpriteEffects.None, 0f);
    }

    internal void BuildQuad(Transform canvasTransform)
    {
        if (texture == null) return;
        
        float uniformScale = Math.Max(canvasTransform.Scale.X, Math.Max(canvasTransform.Scale.Y, canvasTransform.Scale.Z));
        
        Vector3 right = canvasTransform.Right;
        Vector3 up = canvasTransform.Up;
        Vector3 center = canvasTransform.Position + right * position.X * uniformScale - up * position.Y * uniformScale;
        
        float halfWidth = (texture.Width * scale.X) /  _pixelPerUnit / 2f * uniformScale;
        float halfHeight = (texture.Height * scale.Y) / _pixelPerUnit / 2f * uniformScale;
        
        Vector3[] vertices = new Vector3[4]
        {
            center + (-right * halfWidth) + (-up * halfHeight),
            center + (right * halfWidth) + (-up * halfHeight),
            center + (right * halfWidth) + (up * halfHeight),
            center + (-right * halfWidth) + (up * halfHeight)
        };

        if (rotation != 0f)
        {
            float rad = MathHelper.ToRadians(rotation);
            float cos = (float)Math.Cos(rad);
            float sin = (float)Math.Sin(rad);

            Vector3 rotate(Vector3 p)
            {
                Vector3 dir = p - center;
                float dr = Vector3.Dot(dir, right);
                float du = Vector3.Dot(dir, up);
                return center + right * (dr * cos - du * sin) + up * (dr * sin + du * cos);
            }
            
            for (int i = 0; i < vertices.Length; i++)
                vertices[i] = rotate(vertices[i]);
        }
        
        Color c = new Color(color, alpha);
        
        VertexPositionColorTexture [] verts = new VertexPositionColorTexture[4]
        {
            new VertexPositionColorTexture(vertices[3], c, new Vector2(0f, 0f)),
            new VertexPositionColorTexture(vertices[2], c, new Vector2(1f, 0f)),
            new VertexPositionColorTexture(vertices[0], c, new Vector2(0f, 1f)),
            new VertexPositionColorTexture(vertices[1], c, new Vector2(1f, 1f))
        };
        
        short[] indices = new short[6] { 0, 1, 2, 1, 3, 2 };
        quadCount = 1;
        
        GraphicsDevice device = Engine.Graphics.GraphicsDevice;
        vertexBuffer?.Dispose();
        vertexBuffer = new VertexBuffer(device, typeof(VertexPositionColorTexture), verts.Length, BufferUsage.WriteOnly);
        vertexBuffer.SetData(verts);
        indexBuffer?.Dispose();
        indexBuffer = new IndexBuffer(device, typeof(short), indices.Length, BufferUsage.WriteOnly);
        indexBuffer.SetData(indices);
    }
    
    internal void DrawQuad(Effect fx)
    {
        if (texture == null || vertexBuffer == null || indexBuffer == null) return;
        
        GraphicsDevice device = Engine.Graphics.GraphicsDevice;
        device.SetVertexBuffer(vertexBuffer);
        device.Indices = indexBuffer;
        
        foreach (EffectPass pass in fx.CurrentTechnique.Passes)
        {
            pass.Apply();
            device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, quadCount * 4, 0, quadCount * 2);
        }
    }
}