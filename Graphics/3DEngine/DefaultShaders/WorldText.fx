float4x4 World;
float4x4 View;
float4x4 Projection;

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


struct VSInputText
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
    float4 Color    : COLOR0;
};

struct VSOutputText
{
    float4 Position : POSITION0;
    float2 TexCoord : TEXCOORD0;
    float4 Color    : COLOR0;
};

VSOutputText VSText(VSInputText input)
{
    VSOutputText o;

    float4 worldPos = mul(input.Position, World);
    float4 viewPos  = mul(worldPos, View);
    o.Position = mul(viewPos, Projection);

    o.TexCoord = input.TexCoord;
    o.Color    = input.Color;
    return o;
}

float4 PSText(VSOutputText input) : COLOR0
{
    float4 t = tex2D(TextureSampler, input.TexCoord);
    return t * input.Color;
}

technique UnlitText
{
    pass P0
    {
        VertexShader = compile vs_3_0 VSText();
        PixelShader  = compile ps_3_0 PSText();
    }
}

