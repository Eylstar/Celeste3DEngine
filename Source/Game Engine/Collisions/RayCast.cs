using Microsoft.Xna.Framework;

namespace Celeste.Mod.Celeste3DEngine;

/// <summary> A static class for performing raycasting in the 3D scene </summary>
public static class RayCast
{
    /// <summary> Casts a ray and returns whether it hit any colliders, along with the hit information </summary>
    public static bool Cast(Ray ray, out RaycastHit hitInfo, bool ignoreTriggers = true)
    {
        return Cast(ray.Origin, ray.Direction, ray.MaxDistance, out hitInfo, ignoreTriggers);
    }
    
    /// <summary> Casts a ray and returns whether it hit any colliders, along with the hit information </summary>
    public static bool Cast(Vector3 origin, Vector3 direction, float maxDistance, out RaycastHit hitInfo, bool ignoreTriggers = true)
    {
        if (direction == Vector3.Zero)
        {
            hitInfo = null;
            return false;
        }
        
        direction = Vector3.Normalize(direction);
        
        CollisionSystem collisionSystem = EngineEntity.Current3DScene?.collisionSystem;
        if (collisionSystem == null)
        {
            Logger.Warn("RayCast","RayCast.Cast called but no CollisionSystem found in the current scene.");
            hitInfo = null;
            return false;
        }
        
        return collisionSystem.FindCastHit(origin, direction, maxDistance, out hitInfo, ignoreTriggers);
    }
}

/// <summary> Information about a raycast hit </summary>
public class RaycastHit
{
    /// <summary> The point in world space where the ray hit the collider </summary>
    public Vector3 Point;
    /// <summary> The normal of the surface at the hit point </summary>
    public Vector3 Normal;
    /// <summary> The distance from the ray's origin to the hit point </summary>
    public float Distance;
    /// <summary> The collider that was hit by the ray </summary>
    public Collider3D Collider;
    /// <summary> The origin of the ray that hit the collider </summary>
    public Vector3 Origin;
    /// <summary> The direction of the ray that hit the collider </summary>
    public Vector3 Direction;
    
    /// <summary> Creates a new RaycastHit with the given information </summary>
    public RaycastHit(Vector3 point, Vector3 normal, float distance, Collider3D collider, Vector3 origin, Vector3 direction)
    {
        Point = point;
        Normal = normal;
        Distance = distance;
        Collider = collider;
        Origin = origin;
        Direction = direction;
    }
}

/// <summary> A ray defined by an origin, direction, and maximum distance </summary>
public struct Ray
{
    /// <summary> The origin point of the ray </summary>
    public Vector3 Origin;
    /// <summary> The direction of the ray </summary>
    public Vector3 Direction;
    /// <summary> The maximum distance the ray can travel </summary>
    public float MaxDistance;
    
    /// <summary> Creates a new Ray with an origin, direction, and maximum distance </summary>
    public Ray(Vector3 origin, Vector3 direction, float maxDistance)
    {
        Origin = origin;
        Direction = direction;
        MaxDistance = maxDistance;
    }
}