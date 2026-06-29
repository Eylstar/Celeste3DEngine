float4x4 World;
float4x4 LightViewProjection;
float4x4 BoneMatrices[64];

struct VSIn 
{ 
    float4 Position : POSITION0; 
};

struct VSInSkinned
{
    float4 Position : POSITION0;
    float3 Normal   : NORMAL0;
    float2 TexCoord : TEXCOORD0;
    float4 Joints   : BLENDINDICES0;
    float4 Weights  : BLENDWEIGHT0;
};

struct VSOut 
{ 
    float4 Position : POSITION0; 
    float Depth : TEXCOORD0;
};

float4x4 GetSkinMatrix(float4 joints, float4 weights)
{
    return BoneMatrices[joints.x] * weights.x
         + BoneMatrices[joints.y] * weights.y
         + BoneMatrices[joints.z] * weights.z
         + BoneMatrices[joints.w] * weights.w;
}

VSOut VSMain(VSIn input)
{
    VSOut o;
    float4 worldPos  = mul(input.Position, World);
    float4 lightClip = mul(worldPos, LightViewProjection);
    o.Position = lightClip;
    o.Depth = lightClip.z / lightClip.w;
    return o;
}

VSOut VSMainSkinned(VSInSkinned input)
{
    VSOut o;
    float4x4 skin    = GetSkinMatrix(input.Joints, input.Weights);
    float4 skinnedPos = mul(input.Position, skin);
    float4 worldPos  = mul(skinnedPos, World);
    float4 lightClip = mul(worldPos, LightViewProjection);
    o.Position = lightClip;
    o.Depth = lightClip.z / lightClip.w;
    return o;
}

float4 PSMain(VSOut input) : COLOR0 
{ 
    return float4(saturate(input.Depth), 0, 0, 1); 
}

technique ShadowDepth
{
    pass P0
    {
        VertexShader = compile vs_3_0 VSMain();
        PixelShader  = compile ps_3_0 PSMain();
    }
}

technique ShadowDepthSkinned
{
    pass P0
    {
        VertexShader = compile vs_3_0 VSMainSkinned();
        PixelShader  = compile ps_3_0 PSMain();
    }
}