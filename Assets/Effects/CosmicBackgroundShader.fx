sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
sampler uImage2 : register(s2);

float zoom;
float scrollSpeedFactor;
float brightness;
float globalTime;
float3 frontStarColor;
float3 backStarColor;
float3 colorChangeInfluence1;
float3 colorChangeInfluence2;
float colorChangeStrength1;
float colorChangeStrength2;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float2 TextureCoordinates : TEXCOORD0;
    float4 Color : COLOR0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 result = 0;
    float volumetricLayerFade = 1.0;
    float distanceFromBottom = distance(coords.y, 1);
    for (int i = 0; i < 13; i++)
    {
        float time = globalTime * pow(volumetricLayerFade, 2) * 3;
        float2 p = coords * zoom;
        p.y += 1.5;

        // Perform scrolling behaviors. Each layer should scroll a bit slower than the previous one, to give an illusion of 3D.
        p += float2(time * scrollSpeedFactor, time * scrollSpeedFactor);
        p /= volumetricLayerFade;

        float totalChange = tex2D(uImage1, p);
        float4 layerColor = float4(lerp(frontStarColor, backStarColor, i / 13.0), 1.0);
        result += layerColor * totalChange * volumetricLayerFade;

        // Make the next layer exponentially weaker in intensity.
        volumetricLayerFade *= 0.91;
    }
    
    // Apply color change interpolants. This will be used later.
    float colorChangeBrightness1 = tex2D(uImage2, coords * 1.5);
    float colorChangeBrightness2 = tex2D(uImage2, coords * 1.65 + globalTime * scrollSpeedFactor);
    float totalColorChange = colorChangeBrightness1 + colorChangeBrightness2;

    // Account for the accumulated scale from the fractal noise.
    result.rgb = pow(result.rgb * 0.010714, 2.64 - totalColorChange * 1.4 + pow(distanceFromBottom, 3) * 3.9) * brightness;
    
    // Apply color changing accents to the result, to keep it less homogenous.
    result.rgb += colorChangeInfluence1 * (result.r + result.g + result.b) / 3 * colorChangeBrightness1 * colorChangeStrength1;    
    result.rgb += colorChangeInfluence2 * (result.r + result.g + result.b) / 3 * pow(colorChangeBrightness2, 4) * colorChangeStrength2;
    
    return result * sampleColor;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
