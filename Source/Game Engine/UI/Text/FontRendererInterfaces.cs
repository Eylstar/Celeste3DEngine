using System.Collections.Generic;
using FontStashSharp;
using FontStashSharp.Interfaces;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Monocle;
using Point = System.Drawing.Point;
using VertexPositionColorTexture = FontStashSharp.Interfaces.VertexPositionColorTexture;

namespace Celeste.Mod.Celeste3DEngine;

internal class SpriteBatchRenderer : IFontStashRenderer
{
    public GraphicsDevice GraphicsDevice => Engine.Graphics.GraphicsDevice;
    
    public ITexture2DManager TextureManager { get; } = new Texture2DManager();
    
    public void Draw(object texture, System.Numerics.Vector2 pos, System.Drawing.Rectangle? src,
        FSColor color, float rotation, System.Numerics.Vector2 scale, float depth)
    {
        Texture2D tex = (Texture2D)texture;
        
        Vector2 xnaPos = new Vector2(pos.X, pos.Y);
        Vector2 xnaScale = new Vector2(scale.X, scale.Y);
        Color xnaColor = new Color(color.R, color.G, color.B, color.A);
        
        Rectangle? xnaRect = src.HasValue ?
            new Rectangle(src.Value.X, src.Value.Y, src.Value.Width, src.Value.Height) : null;
        
        Monocle.Draw.SpriteBatch.Draw(tex, xnaPos, xnaRect, xnaColor, rotation, Vector2.Zero, xnaScale, SpriteEffects.None, depth);
    }
}

internal class MeshWorldRenderer : IFontStashRenderer2
{
    internal List<VertexPositionColorTexture> vertices = new();
    internal List<short> indices = new();
    internal object lastTexture;
    
    public ITexture2DManager TextureManager { get; } = new Texture2DManager();
    
    
    public void DrawQuad(object texture, 
        ref VertexPositionColorTexture topLeft, 
        ref VertexPositionColorTexture topRight, 
        ref VertexPositionColorTexture bottomLeft, 
        ref VertexPositionColorTexture bottomRight)
    {
        lastTexture = texture;
        short baseIndex = (short)vertices.Count;
        
        vertices.Add(topLeft);
        vertices.Add(topRight);
        vertices.Add(bottomLeft);
        vertices.Add(bottomRight);
        
        indices.Add(baseIndex);
        indices.Add((short)(baseIndex + 1));
        indices.Add((short)(baseIndex + 2));
        
        indices.Add((short)(baseIndex + 1));
        indices.Add((short)(baseIndex + 3));
        indices.Add((short)(baseIndex + 2));
    }

    public void Clear()
    {
        vertices.Clear();
        indices.Clear();
        lastTexture = null;
    }
}

internal class Texture2DManager : ITexture2DManager
{
    public object CreateTexture(int width, int height) => new Texture2D(Engine.Graphics.GraphicsDevice, width, height);

    public Point GetTextureSize(object texture) => new Point(((Texture2D)texture).Width, ((Texture2D)texture).Height);

    public void SetTextureData(object texture, System.Drawing.Rectangle bounds, byte[] data)
    {
        Texture2D tex = (Texture2D)texture;
        tex.SetData(0, new Rectangle(bounds.X, bounds.Y, bounds.Width, bounds.Height), data, 0, data.Length);
    }
}