using System;
using System.IO;
using Microsoft.Xna.Framework.Graphics;

namespace Celeste.Mod.Celeste3DEngine;

internal abstract class MeshData : IDisposable
{
    internal VertexPositionNormalTexture[] verts;
    
    internal float boundingSphereRadius;
    
    //internal abstract void Draw(Effect e);
    
    internal abstract void DrawWithSetup(Effect effect, Action<Texture2D> onBeforePrimitive);
    
    public abstract void Dispose();
}