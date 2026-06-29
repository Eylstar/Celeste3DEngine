using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.Celeste3DEngine;

public class BoxCollider3D : Collider3D
{
    public Vector3 size;
    public float angleRotation;
    
    Vector3 cachedHalfSize;
    Quaternion cachedFinalRotation;
    Quaternion cachedInvRot;

    public BoxCollider3D(Vector3 size, Vector3 center, float rotation = 0f, bool isTrigger = false, bool enabled = true) : base(isTrigger, enabled)
    {
        this.size = size;
        this.center = center;
        angleRotation = rotation;
    }
    

    internal override bool CheckOverlap(Vector3 camPos)
    {
        if (gameObject == null) return false;
        
        Vector3 localPos = Vector3.Transform(camPos - WorldCenter, cachedInvRot);
        
        bool inside = Math.Abs(localPos.X) <= cachedHalfSize.X && Math.Abs(localPos.Y) <= cachedHalfSize.Y && Math.Abs(localPos.Z) <= cachedHalfSize.Z;
        return inside;
    }

    internal override void CheckCollision(ref Vector3 objPos, Vector3 prevPos)
    {
        if (gameObject == null) return;
        
        Vector3 localPos = Vector3.Transform(objPos - WorldCenter, cachedInvRot);
        
        float px = cachedHalfSize.X - Math.Abs(localPos.X);
        float py = cachedHalfSize.Y - Math.Abs(localPos.Y);
        float pz = cachedHalfSize.Z - Math.Abs(localPos.Z);
        
        if (px < 0f || py < 0f || pz < 0f) return;

        float minPenetration = Math.Min(px, Math.Min(py, pz));
        Vector3 correction = Vector3.Zero;
        if (minPenetration == px)
            correction.X = (cachedHalfSize.X - Math.Abs(localPos.X)) * Math.Sign(localPos.X);
        
        else if (minPenetration == py)
            correction.Y = (cachedHalfSize.Y - Math.Abs(localPos.Y)) * Math.Sign(localPos.Y);
        
        else
            correction.Z = (cachedHalfSize.Z - Math.Abs(localPos.Z)) * Math.Sign(localPos.Z);

        Vector3 worldCorrection = Vector3.Transform(correction, cachedFinalRotation);
        objPos += worldCorrection;
    }
    
    internal void BuildCache()
    {
        if (gameObject == null) return;
        
        Transform t = gameObject.transform;
        cachedHalfSize = (size * t.scale) * 0.5f;
        Quaternion localRot = Quaternion.CreateFromAxisAngle(Vector3.Up, MathHelper.ToRadians(angleRotation));
        cachedFinalRotation = Quaternion.Normalize(t.rotation * localRot);
        cachedInvRot = Quaternion.Conjugate(cachedFinalRotation);
    }

    internal override bool RayCast(Vector3 rayOrigin, Vector3 rayDirection, float maxDistance, out RaycastHit hit)
    {
        hit = null;
        
        if (gameObject == null || rayDirection == Vector3.Zero || maxDistance <= 0f) return false;
        
        Vector3 localOrigin = Vector3.Transform(rayOrigin - WorldCenter, cachedInvRot);
        Vector3 localDirection = Vector3.Transform(rayDirection, cachedInvRot);

        float tMin = float.NegativeInfinity;
        float tMax = float.PositiveInfinity;
        
        Vector3 entryNormal = Vector3.Zero;
        Vector3 exitNormal = Vector3.Zero;

        if (!IntersectSlab(localOrigin.X, localDirection.X, -cachedHalfSize.X, cachedHalfSize.X, ref tMin, ref tMax, ref entryNormal, ref exitNormal, Vector3.UnitX) ||
            !IntersectSlab(localOrigin.Y, localDirection.Y, -cachedHalfSize.Y, cachedHalfSize.Y, ref tMin, ref tMax, ref entryNormal, ref exitNormal, Vector3.UnitY) ||
            !IntersectSlab(localOrigin.Z, localDirection.Z, -cachedHalfSize.Z, cachedHalfSize.Z, ref tMin, ref tMax, ref entryNormal, ref exitNormal, Vector3.UnitZ))
        {
            return false;
        }

        if (tMax < 0f) 
            return false;

        float tHit;
        Vector3 hitNormal;
        
        if (tMin >= 0f)
        {
            tHit = tMin;
            hitNormal = entryNormal;
        }
        else
        {
            tHit = tMax;
            hitNormal = exitNormal;
        }
        
        if (tHit < 0 || tHit > maxDistance) return false;
        
        Vector3 hitPoint = rayOrigin + rayDirection * tHit;
        Vector3 worldNormal = Vector3.Transform(hitNormal, cachedFinalRotation);
        worldNormal.Normalize();
        
        hit = new RaycastHit(hitPoint, worldNormal, tHit, this, rayOrigin, rayDirection);
        return true;
    }
    
    private static bool IntersectSlab(float origin, float direction, float min, float max,
        ref float tMin, ref float tMax, ref Vector3 entryNormal, ref Vector3 exitNormal, Vector3 axisNormal)
    {
        const float epsilon = 0.000001f;

        if (MathF.Abs(direction) < epsilon)
        {
            return origin >= min && origin <= max;
        }

        float invDir = 1f / direction;
        float t1 = (min - origin) * invDir;
        float t2 = (max - origin) * invDir;

        Vector3 normal1 = -axisNormal;
        Vector3 normal2 = axisNormal;

        if (t1 > t2)
        {
            (t1, t2) = (t2, t1);
            (normal1, normal2) = (normal2, normal1);
        }

        if (t1 > tMin)
        {
            tMin = t1;
            entryNormal = normal1;
        }

        if (t2 < tMax)
        {
            tMax = t2;
            exitNormal = normal2;
        }

        return tMin <= tMax;
    }

    internal override void DrawShapeDebug(Camera3D cam)
    {
        if (gameObject == null) return;
        
        Vector3 halfSize = (size * gameObject.transform.scale) * 0.5f;
        if (halfSize.X <= 0f || halfSize.Y <= 0f || halfSize.Z <= 0f) return;
        
        Quaternion modelRot = gameObject.transform.rotation;
        Quaternion localRot = Quaternion.CreateFromAxisAngle(Vector3.Up, MathHelper.ToRadians(angleRotation));
        Quaternion finalRot = Quaternion.Normalize(modelRot * localRot);

        DebugDrawShapes3D.DrawBox(WorldCenter, halfSize, finalRot, cam, Color.Red);
    }

    internal override float GetBroadphaseRadius()
    {
        if (gameObject == null) return 0f;
        return ((size * gameObject.transform.scale) * 0.5f).Length();
    }
    
    internal override float GetPointDistanceSquared(Vector3 p)
    {
        if (gameObject == null) return float.PositiveInfinity;
        
        Vector3 local = Vector3.Transform(p - WorldCenter, cachedInvRot);

        // cachedHalfSize est déjà (size * scale) * 0.5f
        Vector3 he = cachedHalfSize;

        // Distance point -> AABB locale (0 si inside)
        float dx = MathF.Max(MathF.Abs(local.X) - he.X, 0f);
        float dy = MathF.Max(MathF.Abs(local.Y) - he.Y, 0f);
        float dz = MathF.Max(MathF.Abs(local.Z) - he.Z, 0f);

        return dx * dx + dy * dy + dz * dz;
    }
}