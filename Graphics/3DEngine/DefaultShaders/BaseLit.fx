float4x4 World;
float4x4 View;
float4x4 Projection;


float4x4 WorldInverseTranspose;

texture DiffuseTexture;
float3 CameraPos;

float  Shininess      = 16.0f;
float3 LightDirection = float3(0.5f, -1.0f, 0.3f);

float3 AmbientColor   = float3(0.2f, 0.2f, 0.2f);
float3 LightColor     = float3(1.0f, 1.0f, 1.0f);
float3 SpecularColor  = float3(0.2f, 0.2f, 0.2f);
float3 DiffuseColor   = float3(1.0f, 1.0f, 1.0f);
float3 EmissiveColor  = float3(0.0f, 0.0f, 0.0f);
float EmissiveIntensity = 1.0f;

float3 TintColor = float3(1.0f, 1.0f, 1.0f);


float3 FogColor;
float FogDensity;
float HeightFogDensity;
float FogHeightStart;

float NearPlane;
float FarPlane;


//float4x4 LightViewProjectionNear;
//float4x4 LightViewProjectionFar;

float4x4 LightViewProjection;

//float CascadeSplitDistance;

float ShadowBias = 0.001f;
float ShadowBiasMin = 0.0002f;
float ShadowBiasMax = 0.0030f;
float ShadowNormalBias = 1.0f;

float ShadowStrength = 1.0f;

float ReceivesShadows = 1.0f;

//float2 ShadowTexelSizeNear;
//float2 ShadowTexelSizeFar;

float2 ShadowTexelSize;

float  ShadowSoftness = 1.5f;

//float CascadeBlendWidth = 3.0f;

//texture ShadowMapNear;
//texture ShadowMapFar;

texture ShadowMap;


float4x4 BoneMatrices[64];
bool IsSkinned = false;


/*sampler2D ShadowSamplerNear = sampler_state
{
    Texture = <ShadowMapNear>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = None;
    AddressU = Clamp;
    AddressV = Clamp;
};

sampler2D ShadowSamplerFar = sampler_state
{
    Texture = <ShadowMapFar>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = None;
    AddressU = Clamp;
    AddressV = Clamp;
};*/


sampler2D ShadowSampler = sampler_state
{
    Texture = <ShadowMap>;
    MinFilter = Point;
    MagFilter = Point;
    MipFilter = None;
    AddressU = Clamp;
    AddressV = Clamp;
};


sampler2D TextureSampler = sampler_state
{
    Texture = <DiffuseTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
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
    float4 Joints : BLENDINDICES0;
    float4 Weights : BLENDWEIGHT0;
};


struct VSOut
{
    float4 Position : POSITION0;
    float3 WorldPos : TEXCOORD0;
    float3 NormalW  : TEXCOORD1;
    float2 TexCoord : TEXCOORD2;
    //float4 LightPosNear  : TEXCOORD3;
    //float4 LightPosFar   : TEXCOORD4;
    float4 LightPos   : TEXCOORD3;
    float  ViewDepth     : TEXCOORD4;
};


float4x4 GetSkinMatrix(float4 joints, float4 weights)
{
    float4x4 skinMatrix = 
        BoneMatrices[joints.x] * weights.x +
        BoneMatrices[joints.y] * weights.y +
        BoneMatrices[joints.z] * weights.z +
        BoneMatrices[joints.w] * weights.w;

    return skinMatrix;
}




VSOut VSBase(VSIn input)
{
    VSOut o;

    float4 worldPos = mul(input.Position, World);
    o.WorldPos = worldPos.xyz;

    o.NormalW = normalize(mul(input.Normal, (float3x3)WorldInverseTranspose));

    float4 viewPos = mul(worldPos, View);
    o.Position = mul(viewPos, Projection);

    o.TexCoord = input.TexCoord;
    
    //o.LightPosNear = mul(worldPos, LightViewProjectionNear);
    //o.LightPosFar  = mul(worldPos, LightViewProjectionFar);
    o.LightPos = mul(worldPos, LightViewProjection);
    o.ViewDepth = -viewPos.z;
    
    return o;
}

VSOut VSSkinned(VSInSkinned input)
{
    VSOut o;

    float4x4 skinMatrix = GetSkinMatrix(input.Joints, input.Weights);

    float4 skinnedPos = mul(input.Position, skinMatrix);
    float3 skinnedNormal = mul(input.Normal, (float3x3)skinMatrix);
    
    float4 worldPos = mul(skinnedPos, World);
    o.WorldPos = worldPos.xyz;
    o.NormalW = normalize(mul(skinnedNormal, (float3x3)WorldInverseTranspose));
    
    float4 viewPos = mul(worldPos, View);
    o.Position = mul(viewPos, Projection);
    
    o.TexCoord = input.TexCoord;
    //o.LightPosNear = mul(worldPos, LightViewProjectionNear);
    //o.LightPosFar  = mul(worldPos, LightViewProjectionFar);
    o.LightPos = mul(worldPos, LightViewProjection);
    
    o.ViewDepth = -viewPos.z;
    
    return o;
}

float ComputeAdaptiveShadowBias(float3 normalW, float3 lightDir)
{
    float ndotl = saturate(dot(normalize(normalW), normalize(lightDir)));
    float grazing = 1.0f - ndotl;

    float adaptiveBias = lerp(ShadowBiasMin, ShadowBiasMax, grazing * ShadowNormalBias);

    return max(ShadowBias, adaptiveBias);
}


float ShadowCompare(sampler2D shadowSampler, float2 uv, float currentDepth01, float bias)
{
    float stored = tex2D(shadowSampler, uv).r;
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


float SampleShadowPoisson(sampler2D shadowSampler, float4 lightPos, float2 texelSize, float3 normalW, float3 lightDir)
{
    if (lightPos.w <= 0.00001f) return 1.0f;

    float2 ndc = lightPos.xy / lightPos.w;
    float2 uv  = ndc * float2(0.5f, -0.5f) + 0.5f;

    if (uv.x < 0 || uv.x > 1 || uv.y < 0 || uv.y > 1) return 1.0f;

    float current = lightPos.z / lightPos.w;
    if (current < 0 || current > 1) return 1.0f;

    float bias = ComputeAdaptiveShadowBias(normalW, lightDir);

    float angle = Random(uv) * 6.2831853;
    float s = sin(angle);
    float c = cos(angle);

    float visibility = 0.0f;
    float2 filterRadius = texelSize * ShadowSoftness;

    [unroll]
    for (int i = 0; i < 8; i++)
    {
        float2 rotOffset = float2(
            poissonDisk[i].x * c - poissonDisk[i].y * s,
            poissonDisk[i].x * s + poissonDisk[i].y * c
        );

        float2 offset = rotOffset * filterRadius;
        visibility += ShadowCompare(shadowSampler, uv + offset, current, bias);
    }

    visibility *= (1.0f / 8.0f);
    return lerp(1.0f, visibility, ShadowStrength);
}


/*float SampleDirectionalCSM(float3 viewDepth, float3 normalW, float3 lightDir, float4 lightPosNear, float4 lightPosFar)
{
    float nearShadow = SampleShadowPoisson(
        ShadowSamplerNear,
        lightPosNear,
        ShadowTexelSizeNear,
        normalW,
        lightDir
    );

    float farShadow = SampleShadowPoisson(
        ShadowSamplerFar,
        lightPosFar,
        ShadowTexelSizeFar,
        normalW,
        lightDir
    );

    float blend = smoothstep(
        CascadeSplitDistance - CascadeBlendWidth,
        CascadeSplitDistance + CascadeBlendWidth,
        viewDepth
    );

    return lerp(nearShadow, farShadow, blend);
}*/


float ComputeExponentialHeightFog(float viewDepth, float worldY)
{
    float distFog = 1.0f - exp(-pow(viewDepth * FogDensity, 2.0f));
    float heightAmount = max(0.0f, FogHeightStart - worldY);
    float heightFog = 1.0f - exp(-pow(heightAmount * HeightFogDensity, 2.0f));
    return saturate(distFog + heightFog - distFog * heightFog);
}


float4 PSBase(VSOut input) : COLOR0
{
    float3 N = normalize(input.NormalW);
    float3 L = normalize(-LightDirection);
    float3 V = normalize(CameraPos - input.WorldPos);

    float NdotL = saturate(dot(N, L));

    float3 diffuse = DiffuseColor * NdotL * LightColor;

    float3 H = normalize(L + V);
    float spec = pow(saturate(dot(N, H)), Shininess);
    float3 specular = SpecularColor * spec * LightColor * NdotL;
    
    float shadow = SampleShadowPoisson(ShadowSampler, input.LightPos, ShadowTexelSize, N, L);
    shadow = lerp(1.0f, shadow, saturate(ReceivesShadows));
    
    shadow = NdotL <= 0.0f ? 0.0f : shadow;

    float3 tex = tex2D(TextureSampler, input.TexCoord).rgb;
    float3 albedo = tex  * TintColor;

    float3 color = albedo * (AmbientColor + diffuse * shadow) + specular * shadow;

    float fogFactor = ComputeExponentialHeightFog(input.ViewDepth, input.WorldPos.y);

    color = lerp(color, FogColor, fogFactor);
    
    color += EmissiveColor * EmissiveIntensity;

    return float4(saturate(color), 1.0f);
}

technique BaseLighting
{
    pass P0
    {
        VertexShader = compile vs_3_0 VSBase();
        PixelShader  = compile ps_3_0 PSBase();
    }
}

technique SkinnedMesh
{
    pass P0
    {
        VertexShader = compile vs_3_0 VSSkinned();
        PixelShader  = compile ps_3_0 PSBase();
    }
}