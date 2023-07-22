sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
sampler uImage2 : register(s2);

float2 zoom;
float2 tileOverlayOffset;
float2 inversionZoom;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, (coords - 0.5) * inversionZoom + 0.5);
    
    if (tex2D(uImage1, (coords + tileOverlayOffset) / zoom).a <= 0 && tex2D(uImage2, (coords + tileOverlayOffset) / zoom).a <= 0)
        return 0;
    
    return color;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}