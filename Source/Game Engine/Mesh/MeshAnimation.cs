using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Celeste3DEngine;

// Node hierarchy and bind pose data for skinned meshes
internal sealed class GLBNodeHierarchy
{
    internal int nodeCount;
    internal int[] parentIndices;
    internal Matrix[] localBindPoses;
    internal int[] topologicalOrder;
}


// Animation data for GLB files
internal sealed class MeshAnimation : CustomAnimation
{
    internal List<AnimationTimeline> channels = new();
}

// Animation channel data for a single node in a GLB animation
internal sealed class AnimationTimeline
{
    internal int nodeIndex;
    internal string path;
    internal Keyframe<Vector3>[] translationKeyframes;
    internal Keyframe<Quaternion>[] rotationKeyframes;
    internal Keyframe<Vector3>[] scaleKeyframes;
}

// Generic keyframe struct for animation data
internal struct Keyframe<T>
{
    internal float time;
    internal T value;
}