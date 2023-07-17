sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
sampler uImage2 : register(s2);
sampler uImage3 : register(s3);
float3 uColor;
float3 uSecondaryColor;
float2 uScreenResolution;
float2 uScreenPosition;
float2 uTargetPosition;
float2 uDirection;
float uOpacity;
float uTime;
float uIntensity;
float uProgress;
float2 uImageSize1;
float2 uImageSize2;
float2 uImageSize3;
float2 uImageOffset;
float uSaturation;
float4 uSourceRect;
float2 uZoom;
float4 uShaderSpecificData;

float3 colorShift;
float3 lightDirection;

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (from - to));
}

// Refer to the following links for an explanation as to how this function works.
// http://dev.thi.ng/gradients/
// https://iquilezles.org/articles/palettes/
float3 palette(float t, float3 a, float3 b, float3 c, float3 d)
{
    return a + b * sin(6.28318 * (c * t + d) + 1.5707);
}

float TriangleWave(float x)
{
    if (x % 2 < 1)
        return x % 2;
    return -(x % 2) + 2;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords) * sampleColor;
    float distanceFromEdge = length(coords - float2(1, 0.5));
    
    // Calculate values for the warp noise that will be applied to the palette colors.
    float warpAngle = tex2D(uImage1, coords * 1.3 + float2(uTime * 0.2, 0)).r * 16;
    float2 warpNoiseOffset = float2(TriangleWave(warpAngle + 1.57), TriangleWave(warpAngle));
    float psychedelicInterpolant = tex2D(uImage1, coords * 0.9 + warpNoiseOffset * 0.023).r * 1.45;
    
    // Calculate the base psychedelic color from the warp noise.
    float3 psychedelicColor = palette(psychedelicInterpolant, colorShift, float3(0.5, 0.5, 0.5), float3(1.5, 1, 0), float3(0.5, 0.2, 0.25)) * 0.8;
    float4 psychedelicColor4 = float4(psychedelicColor, 1) * color.a;
    
    // Calculate ring-based brighness values.
    float ringBrightness = saturate(0.2 / TriangleWave(uTime * 2.33 - distanceFromEdge * 5)) + 1;
    float4 result = lerp(color, psychedelicColor4, color.r * 0.8) * ringBrightness;
    
    // Apply the normal map to the result to apply texturing.
    float brightness = saturate(dot(lightDirection, tex2D(uImage2, coords).xyz));
    result.rgb *= brightness;
    
    return result;
}
technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}