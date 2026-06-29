using Microsoft.Xna.Framework;

namespace Celeste.Mod.Celeste3DEngine;

public static class RayCast
{
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

public class RaycastHit
{
    public Vector3 Point;
    public Vector3 Normal;
    public float Distance;
    public Collider3D Collider;
    
    internal Vector3 Origin;
    internal Vector3 Direction;
    
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

public struct Ray
{
    public Vector3 Origin;
    public Vector3 Direction;
    public float MaxDistance;
    
    public Ray(Vector3 origin, Vector3 direction, float maxDistance)
    {
        Origin = origin;
        Direction = direction;
        MaxDistance = maxDistance;
    }
}