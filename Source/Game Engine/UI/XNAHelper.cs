using FontStashSharp;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Celeste3DEngine;

internal class XNAHelper
{
    internal static System.Numerics.Vector2 ToNumerics(Vector2 v) => new System.Numerics.Vector2(v.X, v.Y);
    
    internal static System.Numerics.Vector3 ToNumerics(Vector3 v) => new System.Numerics.Vector3(v.X, v.Y, v.Z);
    
    internal static System.Numerics.Vector4 ToNumerics(Vector4 v) => new System.Numerics.Vector4(v.X, v.Y, v.Z, v.W);
    
    internal static Vector2 ToXNA(System.Numerics.Vector2 v) => new Vector2(v.X, v.Y);
    internal static Vector3 ToXNA(System.Numerics.Vector3 v) => new Vector3(v.X, v.Y, v.Z);
    internal static Vector4 ToXNA(System.Numerics.Vector4 v) => new Vector4(v.X, v.Y, v.Z, v.W);
    
    internal static Quaternion ToXNA(System.Numerics.Quaternion q) => new Quaternion(q.X, q.Y, q.Z, q.W);
    
    internal static Matrix ToXNA(System.Numerics.Matrix4x4 m) => new Matrix(
        m.M11, m.M12, m.M13, m.M14,
        m.M21, m.M22, m.M23, m.M24,
        m.M31, m.M32, m.M33, m.M34,
        m.M41, m.M42, m.M43, m.M44);
    
    internal static FSColor ToFSColor(Color c, float alpha) => new FSColor(c.R, c.G, c.B, (byte)(alpha * 255));
    
    internal static Color ToXNAColor(FSColor c) => new Color(c.R, c.G, c.B, c.A);
}