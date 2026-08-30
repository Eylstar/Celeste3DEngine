using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Celeste3DEngine;

internal abstract class CustomAnimation
{
    internal string name;
    internal float duration;
}

/// <summary> Represents a single animation for a mesh, containing multiple channels for different nodes </summary>
public sealed class AnimationPlayer : Behaviour
{
    GLBMeshData meshData;
    
    MeshAnimation currentAnimation;
    MeshAnimation toAnimation;
    
    float blendTime = 0f;
    float blendDuration;
    float blendFactor;
    
    float currentTime = 0f;
    internal bool isPlaying = false;
    
    /// <summary> Whether an animation is currently playing </summary>
    public bool IsPlaying => isPlaying;
    
    bool animLoop = true;
    bool toLoop = true;
    
    float toAnimationTime = 0f;
    
    internal Matrix[] boneMatrices;
    
    Matrix[] localMatricesBuffer;
    Matrix[] toLocalMatricesBuffer;
    Matrix[] globalMatricesBuffer;
    
    public override void Start()
    {
        base.Start();
        
        meshData = gameObject.GetComponent<MeshRenderer>()?.GetModel()?.GetMesh() as GLBMeshData;
        
        if (meshData == null)
            Logger.Warn("AnimationPlayer", $"No GLBMeshData found on GameObject '{gameObject.name}'. AnimationPlayer will not function.");
        
        GLBNodeHierarchy hierarchy = meshData?.nodeHierarchy;
        if (hierarchy == null) return;
        
        // Precompute bind pose matrices in global space from the NodeHierarchy data
        int count = hierarchy.nodeCount;
        boneMatrices = new Matrix[count];
        
        Matrix[] localBindPoses = new Matrix[count];
        
        // Convert local bind poses to global space using the hierarchy
        foreach (int i in hierarchy.topologicalOrder)
        {
            int parent = hierarchy.parentIndices[i];
            localBindPoses[i] = parent == -1 ? hierarchy.localBindPoses[i] : hierarchy.localBindPoses[i] * localBindPoses[parent];
        }

        for (int i = 0; i < count; i++)
            boneMatrices[i] = localBindPoses[i];
        
        // Buffers to store computed local and global matrices during animation updates
        localMatricesBuffer = new Matrix[count];
        toLocalMatricesBuffer = new Matrix[count];
        globalMatricesBuffer = new Matrix[count];
    }
    
    
    /// <summary> Starts playing the specified animation. If loop is true, the animation will repeat indefinitely. </summary>
    public void Play(string animName, bool loop = true)
    {
        if (meshData == null || !meshData.animationsDictionnary.TryGetValue(animName, out currentAnimation))
        {
            Logger.Warn("AnimationPlayer", $"MeshData null or animation '{animName}' not found in mesh data. Cannot play animation.");
            return;
        }
        
        currentTime = 0f;
        isPlaying = true;
        animLoop = loop;
        ResetBlendValues();
    }
    
    /// <summary> Stops the current playing animation. </summary>
    public void Stop()
    {
        currentTime = 0f;
        isPlaying = false;
        currentAnimation = null;
        ResetBlendValues();
    }
    
    /// <summary> Smoothly transitions from the current animation to the specified animation over the given duration. </summary>
    public void CrosseFade(string toAnimName, float duration, bool loop = true)
    {
        if (meshData == null || !meshData.animationsDictionnary.TryGetValue(toAnimName, out MeshAnimation anim))
        {
            Logger.Warn("AnimationPlayer", $"MeshData null or animation '{toAnimName}' not found in mesh data. Cannot crossfade to animation.");
            return;
        }
        
        if (!isPlaying)
        {
            Play(toAnimName, loop);
            return;
        }

        if (anim == currentAnimation || anim == toAnimation) return;
        
        toLoop = loop;
        blendDuration = duration;
        ResetBlendValues();
        toAnimation = anim;
        isPlaying = true;
    }

    public override void Update(float deltaTime)
    {
        base.Update(deltaTime);
        
        if (!isPlaying || currentAnimation == null || meshData?.nodeHierarchy == null) return;
        
        // Advance the current animation time
        currentTime += deltaTime;
        
        // Handle animation looping or clamping when reaching the end of the animation duration
        if (currentTime > currentAnimation.duration)
        {
            if (animLoop)
                currentTime %= currentAnimation.duration;
            else
            {
                currentTime = currentAnimation.duration;
                isPlaying = false;
                currentAnimation = null;
            }
        }

        // If we're blending to another animation, advance the blend time and compute the blend factor
        if (toAnimation != null)
        {
            toAnimationTime += deltaTime;

            if (toAnimationTime > toAnimation.duration)
                toAnimationTime %= toAnimation.duration;
            
            blendTime += deltaTime;
            blendFactor = Math.Clamp(blendTime / blendDuration, 0f, 1f);

            if (blendTime >= blendDuration)
            {
                currentAnimation = toAnimation;
                currentTime = toAnimationTime;
                animLoop = toLoop;
                ResetBlendValues();
            }
        }

        ComputeBoneMatrices();
    }
    
    
    // Computes the final bone transformation matrices for the current animation state
    void ComputeBoneMatrices()
    {
        GLBNodeHierarchy hierarchy = meshData.nodeHierarchy;
        int count = hierarchy.nodeCount;
        
        ComputeLocalMatrices(currentAnimation, currentTime, hierarchy, count, localMatricesBuffer);

        if (toAnimation != null)
        {
            ComputeLocalMatrices(toAnimation, toAnimationTime, hierarchy, count, toLocalMatricesBuffer);

            for (int i = 0; i < count; i++)
            {
                localMatricesBuffer[i].Decompose(out Vector3 scale, out Quaternion rotation, out Vector3 translation);
                toLocalMatricesBuffer[i].Decompose(out Vector3 toScale, out Quaternion toRotation, out Vector3 toTranslation);
                
                localMatricesBuffer[i] = Matrix.CreateScale(Vector3.Lerp(scale, toScale, blendFactor)) *
                                 Matrix.CreateFromQuaternion(Quaternion.Slerp(rotation, toRotation, blendFactor)) *
                                 Matrix.CreateTranslation(Vector3.Lerp(translation, toTranslation, blendFactor));
            }
        }
        
        globalMatricesBuffer.Initialize();
        foreach (int i in hierarchy.topologicalOrder)
        {
            int parentIndex = hierarchy.parentIndices[i];
            globalMatricesBuffer[i] = parentIndex == -1 ? localMatricesBuffer[i] : localMatricesBuffer[i] * globalMatricesBuffer[parentIndex];
        }
        
        for (int i = 0; i < count; i++)
            boneMatrices[i] = globalMatricesBuffer[i];
    }

    // Computes the local transformation matrices for each node in the hierarchy based on the current animation and time
    void ComputeLocalMatrices(MeshAnimation anim, float time, GLBNodeHierarchy hierarchy, int count, Matrix[] buffer)
    {
        for (int i = 0; i < count; i++)
            buffer[i] = hierarchy.localBindPoses[i];
        
        foreach (AnimationTimeline channel in anim.channels)
        {
            int index = channel.nodeIndex;
            buffer[index].Decompose(out Vector3 scale, out Quaternion rotation, out Vector3 translation);
            
            if (channel.translationKeyframes != null)
                translation = SampleVector3(channel.translationKeyframes, time);
            
            if (channel.rotationKeyframes != null)
                rotation = SampleQuaternion(channel.rotationKeyframes, time);
            
            if (channel.scaleKeyframes != null)
                scale = SampleVector3(channel.scaleKeyframes, time);
            
            buffer[index] = Matrix.CreateScale(scale) * Matrix.CreateFromQuaternion(rotation) * Matrix.CreateTranslation(translation);
        }
    }
    
    
    void ResetBlendValues()
    {
        toAnimation = null;
        toAnimationTime = 0f;
        blendTime = 0f;
        blendFactor = 0f;
    }
    
    
    // Linear interpolation for Vector3 keyframes
    static Vector3 SampleVector3(Keyframe<Vector3>[] keys, float time)
    {
        if (keys.Length == 1) return keys[0].value;
        
        for (int i = 0; i < keys.Length - 1; i++)
        {
            if (time >= keys[i].time && time <= keys[i + 1].time)
            {
                float t = (time - keys[i].time) / (keys[i + 1].time - keys[i].time);
                return Vector3.Lerp(keys[i].value, keys[i + 1].value, t);
            }
        }
    
        if (time <= keys[0].time) return keys[0].value;
        return keys[keys.Length - 1].value;
    }

    // Spherical linear interpolation for Quaternion keyframes
    static Quaternion SampleQuaternion(Keyframe<Quaternion>[] keys, float time)
    {
        if (keys.Length == 1) return keys[0].value;
    
        for (int i = 0; i < keys.Length - 1; i++)
        {
            if (time >= keys[i].time && time <= keys[i + 1].time)
            {
                float t = (time - keys[i].time) / (keys[i + 1].time - keys[i].time);
                return Quaternion.Slerp(keys[i].value, keys[i + 1].value, t);
            }
        }
    
        if (time <= keys[0].time) return keys[0].value;
        return keys[keys.Length - 1].value;
    }
}