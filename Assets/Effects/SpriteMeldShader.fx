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

float spriteCount;
float meldInterpolant;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 imageColor1 = tex2D(uImage0, coords) * sampleColor;
    float4 imageColor2 = tex2D(uImage1, coords) * sampleColor;
    float4 imageColor3 = tex2D(uImage2, coords) * sampleColor;
    float4 imageColor4 = tex2D(uImage3, coords) * sampleColor;
    float4 color = imageColor1;
    
    if (spriteCount == 2)
        return lerp(imageColor1, imageColor2, meldInterpolant);
    if (spriteCount == 3)
    {
        if (meldInterpolant < 0.5)
            return lerp(imageColor1, imageColor2, meldInterpolant * 2);
        else
            return lerp(imageColor2, imageColor3, (meldInterpolant - 0.5) * 2);
    }
    if (spriteCount == 4)
    {
        if (meldInterpolant < 0.333)
            return lerp(imageColor1, imageColor2, meldInterpolant * 3);
        else if (meldInterpolant < 0.666)
            return lerp(imageColor2, imageColor3, (meldInterpolant - 0.333) * 3);
        else
            return lerp(imageColor3, imageColor4, (meldInterpolant - 0.666) * 3);
    }
    
    return color;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}