using System;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Celeste3DEngine;

public class SphereCollider3D : Collider3D
{
    public float radius;

    public SphereCollider3D(float radius, Vector3 center, bool isTrigger = false, bool enabled = true) : base(isTrigger, enabled)
    {
        this.radius = radius;
        this.center = center;
    }

    internal override bool CheckOverlap(Vector3 camPos)
    {
        if (gameObject == null) return false;
        
        float scaledRadius = radius * Math.Max(gameObject.transform.scale.X, Math.Max(gameObject.transform.scale.Y, gameObject.transform.scale.Z));
        Vector3 diff = camPos - WorldCenter;
        return diff.LengthSquared() < scaledRadius * scaledRadius;
    }
    
    internal override void CheckCollision(ref Vector3 objPos, Vector3 prevPos)
    {
        if (gameObject == null) return;

        float scaledRadius = radius * Math.Max(gameObject.transform.scale.X, Math.Max(gameObject.transform.scale.Y, gameObject.transform.scale.Z));

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

    internal override bool RayCast(Vector3 rayOrigin, Vector3 rayDirection, float maxDistance, out RaycastHit hit)
    {
        hit = null;
        if (gameObject == null || rayDirection == Vector3.Zero || maxDistance <= 0f) return false;

        float scaledRadius = radius * Math.Max(gameObject.transform.scale.X, Math.Max(gameObject.transform.scale.Y, gameObject.transform.scale.Z));

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

    internal override void DrawShapeDebug(Camera3D cam)
    {
        if (gameObject == null) return;
        
        float scaledRadius = radius * Math.Max(gameObject.transform.scale.X, Math.Max(gameObject.transform.scale.Y, gameObject.transform.scale.Z));
        DebugDrawShapes3D.DrawSphere(WorldCenter, scaledRadius, cam, Color.Blue);
    }

    internal override float GetBroadphaseRadius()
    {
        if (gameObject == null) return radius;
        return radius * Math.Max(gameObject.transform.scale.X, Math.Max(gameObject.transform.scale.Y, gameObject.transform.scale.Z));
    }
    
    internal override float GetPointDistanceSquared(Vector3 p)
    {
        if (gameObject == null) return float.PositiveInfinity;

        float scaledRadius = radius * Math.Max(gameObject.transform.scale.X, Math.Max(gameObject.transform.scale.Y, gameObject.transform.scale.Z));

        Vector3 d = p - WorldCenter;
        float len = d.Length();

        float dist = len - scaledRadius;
        if (dist <= 0f) return 0f;

        return dist * dist;
    }
}