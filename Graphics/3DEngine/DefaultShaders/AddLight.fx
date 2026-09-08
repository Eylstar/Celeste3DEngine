#include "Wind.fxh"

float UseWind = 0.0f;


float4x4 World;
float4x4 View;
float4x4 Projection;
float4x4 WorldInverseTranspose;
float4x4 BoneMatrices[64];

texture DiffuseTexture;

float  Shininess      = 16.0f;
float3 CameraPos;
float3 SpecularColor = float3(0.2f, 0.2f, 0.2f);

float3 LightPos       = float3(0, 0, 0);
float3 LightColor     = float3(1, 1, 1);
float  LightIntensity = 1.0f;
float  LightRange     = 10.0f;

float UseShadows;
float4x4 LightViewProjection;
texture ShadowMap;

float  UseSpot   = 0.0f;
float3 LightDir  = float3(0, -1, 0);
float  InnerCos  = 0.9f;
float  OuterCos  = 0.8f;

float ShadowBias = 0.001f;
float ShadowBiasMin = 0.0002f;
float ShadowBiasMax = 0.0030f;
float ShadowNormalBias = 1.0f;
float ShadowStrength = 1.0f;
float2 ShadowTexelSize = float2(1.0/1024.0, 1.0/1024.0);
float  ShadowSoftness  = 1.5f;

float DistanceAttenuationFactor = 1.5f;

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
    
    if(UseWind > 0.5f)
            worldPos.xyz = ApplyWind(input.Position.xyz, worldPos.xyz);
            
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


float ComputeAdaptiveShadowBias(float3 normalW, float3 lightDir)
{
    float ndotl = saturate(dot(normalize(normalW), normalize(lightDir)));
    float grazing = 1.0f - ndotl;
    float adaptiveBias = lerp(ShadowBiasMin, ShadowBiasMax, grazing * ShadowNormalBias);
    return max(ShadowBias, adaptiveBias);
}

float ShadowCompare(float2 uv, float currentDepth01, float bias)
{
    float stored = tex2D(ShadowS, uv).r;
    return (currentDepth01 - bias) <= stored ? 1.0f : 0.0f;
}

float Random(float2 uv){return frac(sin(dot(uv, float2(12.9898,78.233))) * 43758.5453);}

static const float2 poissonDisk[8] =
{
    float2(-0.94201624, -0.39906216),
    float2( 0.94558609, -0.76890725),
    float2(-0.09418410, -0.92938870),
    float2( 0.34495938,  0.29387760),
    float2(-0.91588581,  0.45771432),
    float2(-0.81544232, -0.87912464),
    float2(-0.38277543,  0.27676845),
    float2( 0.97484398,  0.75648379)
};

float SampleShadowPoisson(float4 lightPos, float3 normalW, float3 lightDir, float distFactor)
{
    if (lightPos.w <= 0.00001f) return 1.0f;

    float2 ndc = lightPos.xy / lightPos.w;
    float2 uv  = ndc * float2(0.5f, -0.5f) + 0.5f;

    if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1) return 1.0f;

    float current = (lightPos.w - NearPlane) / (FarPlane - NearPlane);
    if (current < 0 || current > 1) return 1.0f;

    float bias = ComputeAdaptiveShadowBias(normalW, lightDir);

    float angle = Random(uv) * 6.2831853;
    float s = sin(angle);
    float c = cos(angle);

    float visibility = 0.0f;
    
    float distScale = 1.0 + saturate(distFactor) * DistanceAttenuationFactor;
    float2 filterRadius = ShadowTexelSize * ShadowSoftness * distScale;

    [unroll]
    for (int i = 0; i < 8; i++)
    {
        float2 rotOffset = float2(
            poissonDisk[i].x * c - poissonDisk[i].y * s,
            poissonDisk[i].x * s + poissonDisk[i].y * c
        );

        float2 offset = rotOffset * filterRadius;
        visibility += ShadowCompare(uv + offset, current, bias);
    }

    visibility *= (1.0f / 8.0f);
    return lerp(1.0f, visibility, ShadowStrength);
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
    
    float3 V = normalize(CameraPos - input.WorldPos);
    float3 H = normalize(L + V);
    float spec = pow(saturate(dot(N, H)), Shininess);
    float3 specular = SpecularColor * spec * LightColor * (LightIntensity * finalAtt);
    light += specular;
    
    if (UseShadows > 0.5 && finalAtt > 0.0)
    {
        float shadow = SampleShadowPoisson(input.LightPos, N, L, normDist);
        light *= shadow;
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
