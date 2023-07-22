sampler uImage0 : register(s0);

float brightnessPower;
float3 lightDirection;
float2 pixelationZoom;
matrix uWorldViewProjection;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float3 Normal : NORMAL0;
    float2 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float3 Normal : NORMAL0;
    float2 TextureCoordinates : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    float4 pos = mul(input.Position, uWorldViewProjection);
    output.Position = pos;
    output.Position.z = 0;
    
    output.Normal = input.Normal;
    output.TextureCoordinates = input.TextureCoordinates;

    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float3 baseColor = tex2D(uImage0, round(input.TextureCoordinates / pixelationZoom) * pixelationZoom);
    float brightness = pow(saturate(dot(lightDirection, normalize(input.Normal))), brightnessPower);
    return float4(baseColor * brightness, 1);
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
