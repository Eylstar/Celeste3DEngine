using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Celeste.Mod.Celeste3DEngine;

internal abstract class MeshData : IDisposable
{
    internal VertexPositionNormalTexture[] verts;
    
    internal float boundingSphereRadius;
    internal Vector3 boundingSphereCenter;
    
    //internal abstract void Draw(Effect e);
    
    internal abstract void DrawWithSetup(Effect effect, Action<Texture2D> onBeforePrimitive);
    
    public abstract void Dispose();
}