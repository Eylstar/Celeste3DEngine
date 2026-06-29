float4x4 World;
float4x4 View;
float4x4 Projection;
float4x4 WorldInverseTranspose;
float4x4 BoneMatrices[64];

texture DiffuseTexture;

float3 LightPos       = float3(0, 0, 0);
float3 LightColor     = float3(1, 1, 1);
float  LightIntensity = 1.0f;
float  LightRange     = 10.0f;

float UseShadows;
float4x4 LightViewProjection;
texture ShadowMap;
float ShadowBias = 0.001f;

float  UseSpot   = 0.0f;
float3 LightDir  = float3(0, -1, 0);
float  InnerCos  = 0.9f;
float  OuterCos  = 0.8f;

float ShadowBiasBase   = 0.005f;
float ShadowBiasNormal = 0.02f;
float ShadowBiasMin    = 0.003f;
float2 ShadowTexelSize = float2(1.0/1024.0, 1.0/1024.0);
float  ShadowSoftness  = 1.0f;

float NearPlane;
float FarPlane;

sampler2D TextureSampler = sampler_state
{
    Texture = <DiffuseTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};

sampler2D ShadowS = sampler_state
{
    Texture = <ShadowMap>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = None;
    AddressU = Clamp;
    AddressV = Clamp;
};

struct VSIn
{
    float4 Position : POSITION0;
    float3 Normal   : NORMAL0;
    float2 TexCoord : TEXCOORD0;
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
    float3 WorldPos : TEXCOORD0;
    float3 NormalW  : TEXCOORD1;
    float2 TexCoord : TEXCOORD2;
    float4 LightPos : TEXCOORD3;
};

float4x4 GetSkinMatrix(float4 joints, float4 weights)
{
    return BoneMatrices[joints.x] * weights.x
         + BoneMatrices[joints.y] * weights.y
         + BoneMatrices[joints.z] * weights.z
         + BoneMatrices[joints.w] * weights.w;
}

VSOut VSAdd(VSIn input)
{
    VSOut o;
    float4 worldPos = mul(input.Position, World);
    o.WorldPos = worldPos.xyz;
    o.NormalW  = normalize(mul(input.Normal, (float3x3)WorldInverseTranspose));
    float4 viewPos = mul(worldPos, View);
    o.Position = mul(viewPos, Projection);
    o.TexCoord = input.TexCoord;
    o.LightPos = mul(worldPos, LightViewProjection);
    return o;
}

VSOut VSAddSkinned(VSInSkinned input)
{
    VSOut o;
    float4x4 skin      = GetSkinMatrix(input.Joints, input.Weights);
    float4 skinnedPos  = mul(input.Position, skin);
    float3 skinnedNorm = mul(input.Normal, (float3x3)skin);
    float4 worldPos    = mul(skinnedPos, World);
    o.WorldPos = worldPos.xyz;
    o.NormalW  = normalize(mul(skinnedNorm, (float3x3)WorldInverseTranspose));
    float4 viewPos = mul(worldPos, View);
    o.Position = mul(viewPos, Projection);
    o.TexCoord = input.TexCoord;
    o.LightPos = mul(worldPos, LightViewProjection);
    return o;
}

float ShadowCompare01(float2 uv, float current01, float bias)
{
    float stored = tex2D(ShadowS, uv).r;
    return (current01 - bias) <= stored ? 1.0f : 0.0f;
}

static const float2 PoissonDisk[12] =
{
    float2(-0.326, -0.406),
    float2(-0.840, -0.074),
    float2(-0.696,  0.457),
    float2(-0.203,  0.621),
    float2( 0.962, -0.195),
    float2( 0.473, -0.480),
    float2( 0.519,  0.767),
    float2( 0.185, -0.893),
    float2( 0.507,  0.064),
    float2( 0.896,  0.412),
    float2(-0.322,  0.933),
    float2(-0.792, -0.598)
};

float SampleShadowPoisson(float2 uv, float z01, float bias, float softness)
{
    float shadow = 0.0;
    float2 spread = ShadowTexelSize * softness;
    [unroll]
    for (int i = 0; i < 12; i++)
        shadow += ShadowCompare01(uv + PoissonDisk[i] * spread, z01, bias);
    return shadow / 12.0;
}

float4 PSAdd(VSOut input) : COLOR0
{
    float3 N = normalize(input.NormalW);
    float3 Lvec = LightPos - input.WorldPos;
    float dist2 = dot(Lvec, Lvec);
    float range2 = LightRange * LightRange;
    if (dist2 >= range2) return float4(0,0,0,1);

    float invDist = rsqrt(max(dist2, 1e-8));
    float3 L = Lvec * invDist;
    float dist = dist2 * invDist;

    float normDist = dist / max(LightRange, 1e-5);
    float att = saturate(1.0 - normDist);
    att = att * att;

    float spotAtt = 1.0;
    if (UseSpot > 0.5)
    {
        float spotCos = dot(-L, LightDir);
        float denom   = max(InnerCos - OuterCos, 1e-5);
        float t       = saturate((spotCos - OuterCos) / denom);
        spotAtt = t * t * (3.0 - 2.0 * t);
    }

    float finalAtt = att * spotAtt;
    float NdotL    = saturate(dot(N, L));
    float3 albedo  = tex2D(TextureSampler, input.TexCoord).rgb;
    float3 light   = albedo * NdotL * LightColor * (LightIntensity * finalAtt);

    if (UseShadows > 0.5 && finalAtt > 0.0)
    {
        float4 lp = input.LightPos;
        if (lp.w > 1e-5)
        {
            float invW   = 1.0f / lp.w;
            float2 ndc   = lp.xy * invW;
            float2 uv    = ndc * float2(0.5f, -0.5f) + 0.5f;
            float linearZ = lp.w;
            float z01    = (linearZ - NearPlane) / (FarPlane - NearPlane);

            if (uv.x >= 0 && uv.x <= 1 && uv.y >= 0 && uv.y <= 1)
            {
                z01 = max(z01, 0.0);
                float ndl      = saturate(dot(N, L));
                float bias     = ShadowBiasBase + (1.0 - ndl) * ShadowBiasNormal;
                bias           = max(bias, ShadowBiasMin);
                float distFactor = saturate(dist / LightRange);
                float softness = ShadowSoftness * (1.0 + distFactor * 6.0);
                float shadow   = SampleShadowPoisson(uv, z01, bias, softness);
                float shadowStrength = 1.0 - distFactor * 0.7;
                shadow = lerp(1.0, shadow, shadowStrength);
                light *= shadow;
            }
        }
    }

    return float4(saturate(light) * 0.8f, 1.0f);
}

technique AddLight
{
    pass P0
    {
        VertexShader = compile vs_3_0 VSAdd();
        PixelShader  = compile ps_3_0 PSAdd();
    }
}

technique AddLightSkinned
{
    pass P0
    {
        VertexShader = compile vs_3_0 VSAddSkinned();
        PixelShader  = compile ps_3_0 PSAdd();
    }
}
