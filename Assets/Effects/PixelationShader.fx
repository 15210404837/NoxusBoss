sampler uImage0 : register(s0);

float2 pixelationFactor;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Pixelate coords.
    coords = round(coords / pixelationFactor) * pixelationFactor;
    
    float4 color = tex2D(uImage0, coords);
    return color;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}