float4x4 World;
float4x4 View;
float4x4 Projection;

float4 TintColor = float4(1, 1, 1, 1);

float AlphaCutoff = 0.0f;

texture DiffuseTexture;

sampler2D TextureSampler = sampler_state
{
    Texture = <DiffuseTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    MipFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};

struct VSInput
{
    float4 Position : POSITION0;
    float3 Normal   : NORMAL0;
    float2 TexCoord : TEXCOORD0;
};

struct VSOutput
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
};

VSOutput VSMain(VSInput input)
{
    VSOutput o;

    float4 worldPos = mul(input.Position, World);
    float4 viewPos  = mul(worldPos, View);
    o.Position      = mul(viewPos, Projection);

    o.TexCoord = input.TexCoord;
    return o;
}

float4 PSMain(VSOutput input) : COLOR0
{
    float4 tex = tex2D(TextureSampler, input.TexCoord);
    float4 col = tex * TintColor;

    if (AlphaCutoff > 0.0f && col.a < AlphaCutoff)
        discard;

    return col;
}

technique UnlitMesh
{
    pass P0
    {
        VertexShader = compile vs_3_0 VSMain();
        PixelShader  = compile ps_3_0 PSMain();
    }
}
