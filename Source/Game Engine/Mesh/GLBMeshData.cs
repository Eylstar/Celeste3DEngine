using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using SharpGLTF.Memory;
using SharpGLTF.Schema2;
using Quaternion = Microsoft.Xna.Framework.Quaternion;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Vector4 = Microsoft.Xna.Framework.Vector4;

namespace Celeste.Mod.Celeste3DEngine;


// Class used to cache loaded meshes to avoid redundant loading of the same GLB file
internal class CachedMeshEntry
{
    internal GLBMeshData meshData;
    internal int referenceCount;
}



internal sealed class GLBMeshData : MeshData
{
    string cacheKey;
    internal static Dictionary<string, CachedMeshEntry> meshCache = new();
    
    internal List<CustomPrimitive> primitives = new();
    
    internal GLBNodeHierarchy nodeHierarchy;
    internal Dictionary<string, CustomAnimation> animationsDictionnary = new();
    
    string name;
    
    internal static GLBMeshData CreateFromStream(Stream stream, string modelName)
    {
        // Check if the same mesh is cached, and reuse it
        if (meshCache.TryGetValue(modelName, out CachedMeshEntry meshEntry))
        {
            Logger.Info("GLBMeshData", $"Using cached mesh for '{modelName}'");
            
            meshEntry.referenceCount++;
            meshEntry.meshData.cacheKey = modelName;
            meshEntry.meshData.name = modelName;
            return meshEntry.meshData;
        }
        else
        {
            // Not cached, parse new mesh data and add to cache
            GLBMeshData meshData = ParseMesh(stream, modelName);
            meshCache[modelName] = new CachedMeshEntry { meshData = meshData, referenceCount = 1 };
            meshData.cacheKey = modelName;
            meshData.name = modelName;
            return meshData;
        }
    }
    
    // Read GLB using SharpGLTF, extract vertex data, create buffers and textures
    static GLBMeshData ParseMesh(Stream stream, string name = "")
    {
        if (stream.CanSeek) stream.Seek(0, SeekOrigin.Begin);
        
        GLBMeshData meshData = new GLBMeshData();
        
        GraphicsDevice device = Engine.Instance.GraphicsDevice;
        
        // Cache for loaded textures to avoid duplicate loading from stream
        Dictionary<string, Texture2D> loadedTextures = new Dictionary<string, Texture2D>();
        
        //Read the GLB model skipping validation
        ModelRoot model = ModelRoot.ReadGLB(stream, new ReadSettings(SharpGLTF.Validation.ValidationMode.Skip ));
        
        List<Vector3> vertPositions = new List<Vector3>();
        bool containsSkinnedMesh = false;

        // Iterate through all nodes and their meshes/primitives to extract vertex data and create the primitives
        foreach (Node node in model.LogicalNodes)
        {
            if (node.Mesh == null) continue;
            
            Matrix nodeMatrix = XNAHelper.ToXNA(node.WorldMatrix);
            Matrix invNodeMatrix = Matrix.Invert(nodeMatrix);

            foreach (MeshPrimitive gltfPrim in node.Mesh.Primitives)
            {
                var positions = gltfPrim.GetVertexAccessor("POSITION").AsVector3Array();
                var normals = gltfPrim.GetVertexAccessor("NORMAL")?.AsVector3Array();
                var uvs = gltfPrim.GetVertexAccessor("TEXCOORD_0")?.AsVector2Array();
                var indices = gltfPrim.GetIndexAccessor().AsIndicesArray();

                var joints = gltfPrim.GetVertexAccessor("JOINTS_0")?.AsVector4Array();
                var weights = gltfPrim.GetVertexAccessor("WEIGHTS_0")?.AsVector4Array();

                bool isSkinned = joints != null && weights != null && node.Skin != null;
                if (isSkinned) containsSkinnedMesh = true;

                VertexPositionNormalTexture[] staticVertices = null;
                SkinnedVertex[] skinnedVertices = null;

                if (isSkinned)
                    skinnedVertices = new SkinnedVertex[positions.Count];
                else
                    staticVertices = new VertexPositionNormalTexture[positions.Count];
                    
                // Buffers and texture for this primitive
                VertexBuffer vertBuffer;
                IndexBuffer indexBuffer;
                uint[] indexArray;
                
                Texture2D t = ExtractTexture(gltfPrim, loadedTextures);
                
                CreateVertexTypeData();
                BindGPUValues();
                CreatePrimitive();
                
                // Create vertex data array
                void CreateVertexTypeData()
                {
                    for (int i = 0; i < positions.Count; i++)
                    {
                        Vector3 pos = XNAHelper.ToXNA(positions[i]); 
                        Vector3 normal = normals != null ? XNAHelper.ToXNA(normals[i]) : Vector3.UnitY; 
                        Vector2 uv = uvs != null ? new Vector2(uvs[i].X, uvs[i].Y) : Vector2.Zero; 
                        if (!isSkinned) 
                        { 
                            pos = Vector3.Transform(pos, nodeMatrix); 
                            normal = Vector3.Normalize(Vector3.TransformNormal(normal, Matrix.Transpose(invNodeMatrix))); 
                            staticVertices[i] = new VertexPositionNormalTexture(pos, normal, uv); 
                        }
                        else
                            skinnedVertices[i] = new SkinnedVertex(pos, normal, uv, XNAHelper.ToXNA(joints[i]), XNAHelper.ToXNA(weights[i]));

                        vertPositions.Add(pos);
                    }
                }

                // Convert vertex and index data to GPU buffers
                void BindGPUValues()
                {
                    indexArray = new uint[indices.Count];
                    for (int i = 0; i < indices.Count; i++)
                        indexArray[i] = indices[i];
                    
                    if (isSkinned)
                    {
                        vertBuffer = new VertexBuffer(device, SkinnedVertex.VertexDeclaration, skinnedVertices.Length, BufferUsage.None);
                        vertBuffer.SetData(skinnedVertices);
                    }
                    else
                    {
                        vertBuffer = new VertexBuffer(device, typeof(VertexPositionNormalTexture), staticVertices.Length, BufferUsage.None);
                        vertBuffer.SetData(staticVertices);
                    }
                    
                    indexBuffer = new IndexBuffer(device, IndexElementSize.ThirtyTwoBits, indexArray.Length, BufferUsage.None);
                    indexBuffer.SetData(indexArray);
                    
                }

                // Create a CustomPrimitive or SkinnedPrimitive, and add it to the mesh data's primitives list
                void CreatePrimitive()
                {
                    if (isSkinned)
                    {
                        Skin skin = node.Skin;
                        int boneCount = skin.Joints.Count;
                        Matrix[] invBindMatrices = new Matrix[boneCount];
                        int[] nodeIndices = new int[boneCount];
                    
                        for (int j = 0; j < boneCount; j++)
                        {
                            (Node jointNode, Matrix4x4 invBind) = skin.GetJoint(j);
                            invBindMatrices[j] = XNAHelper.ToXNA(invBind);
                            nodeIndices[j] = jointNode.LogicalIndex;
                        }
                    
                        SkinnedPrimitive prim = new SkinnedPrimitive
                        {
                            vertBuffer = vertBuffer,
                            indexBuffer = indexBuffer,
                            texture = t,
                            inverseBindMatrices = invBindMatrices,
                            nodeIndices = nodeIndices,
                            indexCount = indexArray.Length
                        };
                        meshData.primitives.Add(prim);
                    }
                    else
                    {
                        CustomPrimitive prim = new CustomPrimitive
                        {
                            vertBuffer = vertBuffer,
                            indexBuffer = indexBuffer,
                            indexCount = indexArray.Length,
                            texture = t
                        };
                        meshData.primitives.Add(prim);
                    }
                }
            }
        }

        // Iterate through animations, extract keyframe data and add to mesh data's animations dictionary
        foreach (Animation anim in model.LogicalAnimations)
        {
            Logger.Info("GLBMeshData", $"Processing animation '{anim.Name}' with {anim.Channels.Count} channels.");
            CustomAnimation glbanim = new CustomAnimation {name = anim.Name ?? $"Anim_{anim.LogicalIndex}"};
            float maxTime = 0f;

            //Create a new Timeline for every animation Channel (move rotate or scale)
            foreach (AnimationChannel gltfChannel in anim.Channels)
            {
                string path = gltfChannel.TargetNodePath.ToString().ToLower();
                AnimationTimeline timeline = new AnimationTimeline { nodeIndex = gltfChannel.TargetNode.LogicalIndex, path = path };

                switch (path)
                {
                    case "translation":
                        var transKeys = gltfChannel.GetTranslationSampler().GetLinearKeys().ToArray();
                        timeline.translationKeyframes = new Keyframe<Vector3>[transKeys.Length];
                        for (int i = 0; i < transKeys.Length; i++)
                        {
                            timeline.translationKeyframes[i] = new Keyframe<Vector3>
                            {
                                time = transKeys[i].Key,
                                value = XNAHelper.ToXNA(transKeys[i].Value)
                            };
                            if (transKeys[i].Key > maxTime) maxTime = transKeys[i].Key;
                        }
                        break;
                    
                    case "rotation":
                        var rotKeys = gltfChannel.GetRotationSampler().GetLinearKeys().ToArray();
                        timeline.rotationKeyframes = new Keyframe<Quaternion>[rotKeys.Length];
                        for (int i = 0; i < rotKeys.Length; i++)
                        {
                            timeline.rotationKeyframes[i] = new Keyframe<Quaternion>
                            {
                                time = rotKeys[i].Key,
                                value = XNAHelper.ToXNA(rotKeys[i].Value)
                            };
                            if (rotKeys[i].Key > maxTime) maxTime = rotKeys[i].Key;
                        }
                        break;
                    
                    case "scale":
                        var scaleKeys = gltfChannel.GetScaleSampler().GetLinearKeys().ToArray();
                        timeline.scaleKeyframes = new Keyframe<Vector3>[scaleKeys.Length];
                        for (int i = 0; i < scaleKeys.Length; i++)
                        {
                            timeline.scaleKeyframes[i] = new Keyframe<Vector3>
                            {
                                time = scaleKeys[i].Key,
                                value = XNAHelper.ToXNA(scaleKeys[i].Value)
                            };
                            if (scaleKeys[i].Key > maxTime) maxTime = scaleKeys[i].Key;
                        }
                        break;
                }
                glbanim.channels.Add(timeline);
            }
            glbanim.duration = maxTime;
            meshData.animationsDictionnary[glbanim.name] = glbanim;
        }

        // Build the node hierarchy and bind pose data for skinning calculations in shader
        if (containsSkinnedMesh)
        {
            // Get the model nodes and bind their data into the hierarchy
            IReadOnlyList<Node> nodes = model.LogicalNodes;
            int nodeCount = nodes.Count;
            
            GLBNodeHierarchy hierarchy = new GLBNodeHierarchy
            {
                nodeCount = nodeCount,
                parentIndices = new int[nodeCount],
                localBindPoses = new Matrix[nodeCount]
            };
            
            for (int i = 0; i < nodeCount; i++)
            {
                Node n = nodes[i];
                hierarchy.parentIndices[i] = n.VisualParent?.LogicalIndex ?? -1;
                hierarchy.localBindPoses[i] = XNAHelper.ToXNA(n.LocalMatrix);
            }
            
            hierarchy.topologicalOrder = ComputeTopologicalOrder(hierarchy.parentIndices, nodeCount);
            
            meshData.nodeHierarchy = hierarchy;
        }
        
        meshData.boundingSphereRadius = getBoundingSphereRadius(vertPositions);
        
        return meshData;
    }

    static int[] ComputeTopologicalOrder(int[] parentIndices, int count)
    {
        int[] result = new int[count];
        int idx = 0;
        bool[] visited = new bool[count];

        void Visit(int i)
        {
            if (visited[i]) return;
            if (parentIndices[i] != -1) Visit(parentIndices[i]);
            visited[i] = true;
            result[idx++] = i;
        }
        
        for (int i = 0; i < count; i++)
            Visit(i);
        
        return result;
    }
    
    static float getBoundingSphereRadius(List<Vector3> vertPositions)
    {
        Vector3 center = Vector3.Zero;
        foreach (Vector3 p in vertPositions)
            center += p;
        center /= vertPositions.Count;
        
        float maxDistSq = 0f;
        foreach (var p in vertPositions)
        {
            float dSq = Vector3.DistanceSquared(p, center);
            if (dSq > maxDistSq) maxDistSq = dSq;
        }
        return MathF.Sqrt(maxDistSq);
    }

    // Extract base color texture from material, or create 1x1 texture from base color factor if texture is missing
    static Texture2D ExtractTexture(MeshPrimitive prim, Dictionary<string, Texture2D> loadedTextures)
    {
        Texture2D t = null;
        GraphicsDevice device = Engine.Instance.GraphicsDevice;
                    
        MaterialChannel? baseColorChannel = prim.Material?.FindChannel("BaseColor");
        MemoryImage? baseColorImage = baseColorChannel?.Texture?.PrimaryImage?.Content;
                    
        //Texture
        if (baseColorImage != null && !baseColorImage.Value.Content.IsEmpty)
        {
            //Try to reuse already loaded texture if possible
            string textureKey = baseColorChannel.Value.Texture.LogicalIndex.ToString();
            if (!loadedTextures.TryGetValue(textureKey, out t))
            {
                byte[] imageData = baseColorImage.Value.Content.ToArray();
                using MemoryStream ms = new MemoryStream(imageData);
                t = Texture2D.FromStream(device, ms);
                loadedTextures[textureKey] = t;
            }
            //else
                //Logger.Info("GLBMeshData", $"Reusing loaded texture for '{prim.Material?.Name}'");
        }
        //Fallback to base color factor if texture is missing
        else if (baseColorChannel != null)
        {
            Vector4 factor = XNAHelper.ToXNA((System.Numerics.Vector4)baseColorChannel.Value.Parameters[0].Value);
            t = new Texture2D(device, 1, 1);
            t.SetData(new[] { new Color(factor.X, factor.Y, factor.Z, factor.W) });
        }
        
        return t;
    }

    internal override void DrawWithSetup(Effect effect, Action<Texture2D> onBeforePrimitive)
    {
        GraphicsDevice device = Engine.Instance.GraphicsDevice;
        foreach (CustomPrimitive prim in primitives)
        {
            if (prim.vertBuffer == null || prim.vertBuffer.IsDisposed) continue;
            
            onBeforePrimitive(prim.texture);

            device.SetVertexBuffer(prim.vertBuffer); 
            device.Indices = prim.indexBuffer;

            foreach (var pass in effect.CurrentTechnique.Passes)
            {
                pass.Apply(); 
                device.DrawIndexedPrimitives(Microsoft.Xna.Framework.Graphics.PrimitiveType.TriangleList, 0, 0, prim.vertBuffer.VertexCount, 0, prim.indexCount / 3);
            }
        }
    }

    public override void Dispose()
    {
        // Decrease reference count in cache, and dispose buffers/textures if no more references
        if (meshCache.TryGetValue(cacheKey, out CachedMeshEntry meshEntry))
        {
            int newRefCount = meshEntry.referenceCount - 1;
            if (newRefCount <= 0)
            {
                foreach (CustomPrimitive prim in meshEntry.meshData.primitives)
                {
                    prim.vertBuffer?.Dispose();
                    prim.indexBuffer?.Dispose();
                    prim.texture?.Dispose();
                }

                meshEntry.meshData.primitives.Clear();
                meshCache.Remove(cacheKey);
                
                Logger.Info("GLBMeshData", $"Disposed mesh '{cacheKey}' from cache, no more references.");
            }
            else
            {
                meshEntry.referenceCount = newRefCount;
            }
        }
        else
        {
            // Not found in cache, dispose directly (should not happen if all meshes are created through CreateFromStream), fallback.
            foreach (CustomPrimitive prim in primitives)
            {
                prim.vertBuffer?.Dispose();
                prim.indexBuffer?.Dispose();
                prim.texture?.Dispose();
            }
            primitives.Clear();
        }
    }
    
    // Clear the Mesh cache and dispose all cached meshes (used when unloading the mod, called by CleanEngine)
    internal static void ClearCache()
    {
        foreach (var entry in meshCache.Values)
        {
            foreach (CustomPrimitive prim in entry.meshData.primitives)
            {
                prim.vertBuffer?.Dispose();
                prim.indexBuffer?.Dispose();
                prim.texture?.Dispose();
            }
            entry.meshData?.primitives?.Clear();
        }
        meshCache?.Clear();
    }
}



// Class used to represent and store primitive data
internal class CustomPrimitive
{
    internal VertexBuffer vertBuffer;
    internal IndexBuffer indexBuffer;
    internal Texture2D texture;
    internal int indexCount;
}

// Class used to represent and store skinned primitive data
internal sealed class SkinnedPrimitive : CustomPrimitive
{
    internal Matrix[] inverseBindMatrices;
    internal int[] nodeIndices;
}


// Vertex structure for skinned meshes, includes joint indices and weights for skinning calculations in shader
internal struct SkinnedVertex : IVertexType
{
    public Vector3 Position;
    public Vector3 Normal;
    public Vector2 UV;
    public Vector4 Joints;
    public Vector4 Weights;
    
    internal SkinnedVertex(Vector3 position, Vector3 normal, Vector2 uv, Vector4 joints, Vector4 weights)
    {
        Position = position;
        Normal = normal;
        UV = uv;
        Joints = joints;
        Weights = weights;
    }
    
    internal static readonly VertexDeclaration VertexDeclaration = new VertexDeclaration
    (
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
        new VertexElement(24, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
        new VertexElement(32, VertexElementFormat.Vector4, VertexElementUsage.BlendIndices, 0),
        new VertexElement(48, VertexElementFormat.Vector4, VertexElementUsage.BlendWeight, 0)
    );
    
    VertexDeclaration IVertexType.VertexDeclaration => VertexDeclaration;
}