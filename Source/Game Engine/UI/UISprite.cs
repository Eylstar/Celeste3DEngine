using System;

using FontStashSharp;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.Celeste3DEngine;

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
    
    public Vector2 Position => position;
    public Vector2 Scale => scale;
    public Color Color => color;
    public float Alpha => alpha;
    public float Rotation => rotation;
    
    
    VertexBuffer vertexBuffer;
    IndexBuffer indexBuffer;
    int quadCount;

    public float pixelPerUnit = 200f;
    
    internal bool dirty = true;
    
    public UISprite(string texturePath) : this(texturePath, Vector2.Zero) { }
    
    public UISprite(string texturePath, Vector2 position, Vector2 scale = default, Color? color = null, float alpha = 1f, float rotation = 0f)
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
    
    public void SetPosition(Vector2 newPos)
    {
        position = newPos;
        dirty = true;
    }
    public void SetScale(Vector2 newScale)
    {
        scale = newScale;
        dirty = true;
    }
    public void SetColor(Color newColor)
    {
        color = newColor;
        dirty = true;
    }
    public void SetAlpha(float newAlpha)
    {
        alpha = newAlpha;
        dirty = true;
    }
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
        
        float halfWidth = (texture.Width * scale.X) /  pixelPerUnit / 2f * uniformScale;
        float halfHeight = (texture.Height * scale.Y) / pixelPerUnit / 2f * uniformScale;
        
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