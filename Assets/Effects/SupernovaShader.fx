sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
sampler uImage2 : register(s2);
sampler uImage3 : register(s3);
sampler uImage4 : register(s4);
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

float scale;
float brightness;

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (to - from));
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Calculate noise values for the supernova.
    float2 noiseCoords1 = (coords - 0.5) * scale * 0.2 + 0.5 + float2(uTime * 0.14, 0);
    float2 noiseCoords2 = (coords - 0.5) * scale * 0.32 + 0.5 + float2(uTime * -0.08, 0);
    float2 noiseCoords3 = (coords - 0.5) * scale * 0.14 + 0.5 + float2(0, uTime * -0.06);
    float4 noiseColor1 = tex2D(uImage1, noiseCoords1) * float4(uColor, 1) * sampleColor * 1.5;
    float4 noiseColor2 = tex2D(uImage1, noiseCoords2) * float4(uSecondaryColor, 1) * sampleColor * 1.5;
    float4 noiseColor3 = tex2D(uImage2, frac(noiseCoords3)) * sampleColor;
    
    // Calculate edge fade values. These are used to make the supernova naturally fade at those edges.
    float2 edgeDistortion = tex2D(uImage3, noiseCoords1 * 2.5).rb * 0.0093;
    float distanceFromCenter = length(coords + edgeDistortion - 0.5) * 1.414;
    float distanceFade = InverseLerp(0.45, 0.39, distanceFromCenter);
    
    float4 result = (noiseColor1 + noiseColor2) * sampleColor.a;
    result.a = sampleColor.a * 1.25;
    return ((result - noiseColor3 * 0.15) * brightness + (brightness - 1) * 0.25) * distanceFade * uOpacity;
}
technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}