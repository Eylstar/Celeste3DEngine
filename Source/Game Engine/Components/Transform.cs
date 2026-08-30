using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;

namespace Celeste.Mod.Celeste3DEngine;

/// <summary> Transform component for GameObjects, handles position, rotation and scale in world space </summary>
public sealed class Transform
{
    GameObject owner;   
    
    internal Vector3 position;
    internal Quaternion rotation;
    //internal float scale;
    internal Vector3 scale;
    
    /// <summary> Position values in world space </summary>
    public Vector3 Position => position;
    
    float yaw, pitch, roll;
    
    /// <summary> Rotation values in world space as a Quaternion </summary>
    public Quaternion Rotation => rotation;
    
    /// <summary> Rotation values in world space as Euler angles in degrees </summary>
    public Vector3 RotationEuler => new Vector3(Pitch, Yaw, Roll);
    
    /// <summary> Scale value of the GameObject </summary>
    public Vector3 Scale => scale;

    /// <summary> Forward direction of the Transform </summary>
    public Vector3 Forward => Vector3.Transform(Vector3.Forward, rotation);
    
    /// <summary> Up direction of the Transform </summary>
    public Vector3 Up => Vector3.Transform(Vector3.Up, rotation);
    
    /// <summary> Right direction of the Transform </summary>
    public Vector3 Right => Vector3.Transform(Vector3.Right, rotation);
    
    internal float Yaw   => yaw;
    internal float Pitch => pitch;
    internal float Roll  => roll;
    
    /// <summary> Default constructor, position is zero, rotation is identity, scale is one on all axes </summary>
    public Transform()
    {
        position = Vector3.Zero;
        rotation = Quaternion.Identity;
        scale = Vector3.One;
        yaw = pitch = roll = 0f;
    }
    /// <summary> Creates a Transform with position, rotation in Euler angles in degrees and scale explicitly set </summary>
    public Transform(Vector3 position, Vector3 rotationEuler, Vector3 scale)
    {
        this.position = position;
        this.scale = scale;
        SetRotation(rotationEuler);
    }
    /// <summary> Creates a Transform with position, rotation Quaternion and scale explicitly set </summary>
    public Transform(Vector3 position, Quaternion rotation, Vector3 scale)
    { 
        this.position = position;
        this.scale = scale;
        SetRotation(rotation);
    }
    /// <summary> Creates a Transform at the given position with an optional rotation in Euler angles in degrees, scale defaults to one on all axes </summary>
    public Transform(Vector3 position, Vector3 rotationEuler = default)
    {
        this.position = position;
        this.scale = Vector3.One;
        SetRotation(rotationEuler);
    }
    /// <summary> Creates a Transform at the given position with the given rotation Quaternion, scale defaults to one on all axes </summary>
    public Transform(Vector3 position, Quaternion rotation)
    { 
        this.position = position;
        this.scale = Vector3.One;
        SetRotation(rotation);
    }

    
    internal Transform Copy()
    {
        return new Transform
        {
            position = this.position,
            rotation = this.rotation,
            scale = this.scale,
            yaw = this.yaw,
            pitch = this.pitch,
            roll = this.roll
        };
    }
    
    internal void SetOwner(GameObject o) => owner = o;
    
    /// <summary> Moves the transform by a delta in world space </summary>
    public void MoveBy(Vector3 delta)
    {
        position += delta;
        owner?.MarkCollidersDirty();
    }
    
    /// <summary> Sets the position of the transform in world space </summary>
    public void SetPosition(Vector3 newPosition)
    {
        position = newPosition;
        owner?.MarkCollidersDirty();
    }
    
    /// <summary> Sets the rotation of the transform using Euler angles in degrees </summary>
    public void SetRotation(Vector3 newRotationEuler)
    {
        yaw = newRotationEuler.Y;
        pitch = newRotationEuler.X;
        roll = newRotationEuler.Z;
        RebuildQuaternion();
        owner?.MarkCollidersDirty();
    }
    
    /// <summary> Sets the rotation of the transform using a Quaternion </summary>
    public void SetRotation(Quaternion rot) 
    {
        rotation = Quaternion.Normalize(rot);
        yaw = MathHelper.ToDegrees(GetYawRadians(rotation));
        pitch = MathHelper.ToDegrees(GetPitchRadians(rotation));
        roll = MathHelper.ToDegrees(GetRollRadians(rotation));
        owner?.MarkCollidersDirty();
    }
    
    /// <summary> Rotates the transform by adding to its current Euler angles in degrees </summary>
    public void Rotate(Vector3 rotationToAdd)
    {
        yaw += rotationToAdd.Y;
        pitch += rotationToAdd.X;
        roll += rotationToAdd.Z;
        RebuildQuaternion();
        owner?.MarkCollidersDirty();
    }

    /// <summary> Rotates the transform by multiplying its current rotation by a Quaternion </summary>
    public void Rotate(Quaternion rot)
    {
        rotation = Quaternion.Normalize(rot * rotation);
        yaw = MathHelper.ToDegrees(GetYawRadians(rotation));
        pitch = MathHelper.ToDegrees(GetPitchRadians(rotation));
        roll = MathHelper.ToDegrees(GetRollRadians(rotation));
        owner?.MarkCollidersDirty();
    }
    
    /// <summary> Rotates the transform to look at a target point in world space </summary>
    public void LookAt(Vector3 target)
    {
        LookAt(target, Vector3.Up);
    }
    
    /// <summary> Rotates the transform to look at a target point in world space with specified Up Vector</summary>
    public void LookAt(Vector3 target, Vector3 upDirection)
    {
        Vector3 dir = Vector3.Normalize(target - position);
        Vector3 forward = -dir;
        Vector3 right = Vector3.Normalize(Vector3.Cross(upDirection, forward));
        Vector3 up = Vector3.Cross(forward, right);
        
        Matrix rot = new Matrix(
            right.X, right.Y, right.Z, 0f,
            up.X, up.Y, up.Z, 0f,
            forward.X, forward.Y, forward.Z, 0f,
            0f, 0f, 0f, 1f);
        
        SetRotation(Quaternion.CreateFromRotationMatrix(rot));
    }
    

    /// <summary> Gets the current rotation as Euler angles in degrees </summary>
    public Vector3 GetEuler() => new Vector3(Pitch, Yaw, Roll);
    
    /// <summary> Sets the scale of the GameObject </summary>
    public void SetScale(Vector3 newScale)
    {
        scale = newScale;
        owner?.MarkCollidersDirty();
    }
    
        /// <summary> Sets the scale of the GameObject uniformly on all axes </summary>
    public void SetScale(float uniformScale) => SetScale(new Vector3(uniformScale, uniformScale, uniformScale));
    
    void RebuildQuaternion()
    {
        rotation = Quaternion.CreateFromYawPitchRoll(
            MathHelper.ToRadians(yaw), 
            MathHelper.ToRadians(pitch), 
            MathHelper.ToRadians(roll));
    }
        
    static float GetPitchRadians(Quaternion q)
    {
        float sinp = 2f * (q.W * q.X - q.Z * q.Y);
        sinp = Math.Clamp(sinp, -1f, 1f);
        return (float)Math.Asin(sinp);
    }

    static float GetYawRadians(Quaternion q)
    {
        float siny_cosp = 2f * (q.W * q.Y + q.X * q.Z);
        float cosy_cosp = 1f - 2f * (q.X * q.X + q.Y * q.Y);
        return (float)Math.Atan2(siny_cosp, cosy_cosp);
    }

    static float GetRollRadians(Quaternion q)
    {
        float sinr_cosp = 2f * (q.W * q.Z + q.X * q.Y);
        float cosr_cosp = 1f - 2f * (q.X * q.X + q.Z * q.Z);
        return (float)Math.Atan2(sinr_cosp, cosr_cosp);
    }
}