using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.Celeste3DEngine;

/// <summary> Utility class for drawing debug shapes in 3D space (Box and Sphere) </summary>
public static class DebugDrawShapes3D
{
    static BasicEffect effect;

    static void EnsureEffect()
    {   
        if (effect != null && !effect.IsDisposed) return;

        effect = new BasicEffect(Engine.Graphics.GraphicsDevice)
        {
            VertexColorEnabled = true,
            LightingEnabled = false
        };
    }

    /// <summary> Draws a wireframe box in 3D space </summary>
    public static void DrawBox(Vector3 center, Vector3 size, Quaternion rotation, Camera3D cam, Color color)
    {
        EnsureEffect();
        Vector3 halfSize = size * 0.5f;

        Vector3[] localCorners =
        {
            new Vector3(-halfSize.X, -halfSize.Y, -halfSize.Z),
            new Vector3(-halfSize.X, -halfSize.Y, +halfSize.Z),
            new Vector3(-halfSize.X, +halfSize.Y, -halfSize.Z),
            new Vector3(-halfSize.X, +halfSize.Y, +halfSize.Z),
            new Vector3(+halfSize.X, -halfSize.Y, -halfSize.Z),
            new Vector3(+halfSize.X, -halfSize.Y, +halfSize.Z),
            new Vector3(+halfSize.X, +halfSize.Y, -halfSize.Z),
            new Vector3(+halfSize.X, +halfSize.Y, +halfSize.Z),
        };
        

        Vector3[] worldCorners = new Vector3[8];
        for (int i = 0; i < 8; i++)
        {
            worldCorners[i] = Vector3.Transform(localCorners[i], rotation) + center;
        }

        int[] edges =
        {
            0, 1, 1, 3, 3, 2, 2, 0,
            4, 5, 5, 7, 7, 6, 6, 4,
            0, 4, 1, 5, 2, 6, 3, 7 
        };

        VertexPositionColor[] verts = new VertexPositionColor[edges.Length];
        for (int i = 0; i < edges.Length; i++)
        {
            verts[i] = new VertexPositionColor(worldCorners[edges[i]], color);
        }

        GraphicsDevice device = Engine.Graphics.GraphicsDevice;

        effect.World = Matrix.Identity;
        effect.View = cam.View;
        effect.Projection = cam.Projection;

        foreach (EffectPass pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            device.DrawUserPrimitives(PrimitiveType.LineList, verts, 0, edges.Length / 2);
        }
    }
    
    /// <summary> Draws a wireframe sphere in 3D space </summary>
    public static void DrawSphere(Vector3 center, float radius, Camera3D cam, Color color)
    {
        EnsureEffect();

        const int segments = 32;
        VertexPositionColor[] verts = new VertexPositionColor[segments * 3 * 2];
        int index = 0;

        effect.World = Matrix.Identity;
        effect.View = cam.View;
        effect.Projection = cam.Projection;

        float step = MathHelper.TwoPi / segments;

        for (int i = 0; i < segments; i++)
        {
            float a0 = i * step;
            float a1 = (i + 1) * step;

            Vector3 p0 = center + new Vector3((float)Math.Cos(a0), 0, (float)Math.Sin(a0)) * radius;
            Vector3 p1 = center + new Vector3((float)Math.Cos(a1), 0, (float)Math.Sin(a1)) * radius;

            verts[index++] = new VertexPositionColor(p0, color);
            verts[index++] = new VertexPositionColor(p1, color);
        }

        for (int i = 0; i < segments; i++)
        {
            float a0 = i * step;
            float a1 = (i + 1) * step;

            Vector3 p0 = center + new Vector3((float)Math.Cos(a0), (float)Math.Sin(a0), 0) * radius;
            Vector3 p1 = center + new Vector3((float)Math.Cos(a1), (float)Math.Sin(a1), 0) * radius;

            verts[index++] = new VertexPositionColor(p0, color);
            verts[index++] = new VertexPositionColor(p1, color);
        }

        for (int i = 0; i < segments; i++)
        {
            float a0 = i * step;
            float a1 = (i + 1) * step;

            Vector3 p0 = center + new Vector3(0, (float)Math.Cos(a0), (float)Math.Sin(a0)) * radius;
            Vector3 p1 = center + new Vector3(0, (float)Math.Cos(a1), (float)Math.Sin(a1)) * radius;

            verts[index++] = new VertexPositionColor(p0, color);
            verts[index++] = new VertexPositionColor(p1, color);
        }

        foreach (var pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            Engine.Graphics.GraphicsDevice.DrawUserPrimitives(
                PrimitiveType.LineList, verts, 0, verts.Length / 2
            );
        }
    }
    
    /// <summary> Draws a line between 2 points in 3D space </summary>
    public static void DrawLine(Vector3 start, Vector3 end, Camera3D cam, Color color)
    {
        EnsureEffect();

        VertexPositionColor[] verts = new VertexPositionColor[]
        {
            new VertexPositionColor(start, color),
            new VertexPositionColor(end, color)
        };

        effect.World = Matrix.Identity;
        effect.View = cam.View;
        effect.Projection = cam.Projection;

        foreach (var pass in effect.CurrentTechnique.Passes)
        {
            pass.Apply();
            Engine.Graphics.GraphicsDevice.DrawUserPrimitives(
                PrimitiveType.LineList, verts, 0, 1
            );
        }
    }
}