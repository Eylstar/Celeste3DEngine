using System;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Celeste3DEngine;

/// <summary> A 3D sphere collider </summary>
public class SphereCollider3D : Collider3D
{
    /// <summary> Radius of the sphere collider in local space </summary>
    public float radius;
    
    internal float scaledRadius => radius * Math.Max(gameObject.transform.scale.X, Math.Max(gameObject.transform.scale.Y, gameObject.transform.scale.Z));

    /// <summary> Creates a new SphereCollider3D with a radius and center offset </summary>
    public SphereCollider3D(float radius, Vector3 center, bool isTrigger = false, bool enabled = true) : base(isTrigger, enabled)
    {
        this.radius = radius;
        this.center = center;
    }

    /// <summary> Checks if a point in world space is overlapping with this sphere collider </summary>
    public override bool CheckOverlap(Vector3 camPos)
    {
        if (gameObject == null) return false;
        
        Vector3 diff = camPos - WorldCenter;
        return diff.LengthSquared() < scaledRadius * scaledRadius;
    }
    
    /// <summary> Checks if a point in world space is colliding with this sphere collider and adjusts the position to resolve the collision </summary>
    public override void CheckCollision(ref Vector3 objPos, Vector3 prevPos)
    {
        if (gameObject == null) return;
        
        Vector3 toCam = objPos - WorldCenter;
        float dist = toCam.Length();

        if (dist <= 0f)
        {
            Vector3 fromPrev = prevPos - WorldCenter;
            if (fromPrev.LengthSquared() > 0.0001f)
            {
                fromPrev.Normalize();
                objPos = WorldCenter + fromPrev * scaledRadius;
            }
            else
                objPos = WorldCenter + new Vector3(0f, 0f, scaledRadius);
            return;
        }

        if (dist < scaledRadius)
        {
            Vector3 normal = toCam / dist;
            objPos = WorldCenter + normal * scaledRadius;
        }
    }

    /// <summary> Checks if a ray intersects with this sphere collider and returns the hit information </summary>
    public override bool RayCast(Vector3 rayOrigin, Vector3 rayDirection, float maxDistance, out RaycastHit hit)
    {
        hit = null;
        if (gameObject == null || rayDirection == Vector3.Zero || maxDistance <= 0f) return false;
        
        Vector3 oc = rayOrigin - WorldCenter;
        float a = Vector3.Dot(rayDirection, rayDirection);
        float b = 2f * Vector3.Dot(oc, rayDirection);
        float c = Vector3.Dot(oc, oc) - scaledRadius * scaledRadius;

        float discriminant = b * b - 4f * a * c;
        if (discriminant < 0f) return false;

        float sqrtDisc = (float)Math.Sqrt(discriminant);
        float t1 = (-b - sqrtDisc) / (2f * a);
        float t2 = (-b + sqrtDisc) / (2f * a);

        float t = (t1 >= 0f && t1 <= maxDistance) ? t1 : ((t2 >= 0f && t2 <= maxDistance) ? t2 : -1f);
        if (t < 0f) return false;
        
        Vector3 hitPoint = rayOrigin + rayDirection * t;
        Vector3 normal = Vector3.Normalize(hitPoint - WorldCenter);

        hit = new RaycastHit(hitPoint, normal, t, this, rayOrigin, rayDirection);
        return true;
    }

    /// <summary> Draws the sphere collider in debug mode </summary>
    public override void DrawShapeDebug(Camera3D cam)
    {
        if (gameObject == null) return;
        DebugDrawShapes3D.DrawSphere(WorldCenter, scaledRadius, cam, Color.Blue);
    }

    /// <summary> Returns the radius of the sphere collider for broadphase collision detection </summary>
    public override float GetBroadphaseRadius()
    {
        if (gameObject == null) return radius;
        return scaledRadius;
    }
} 