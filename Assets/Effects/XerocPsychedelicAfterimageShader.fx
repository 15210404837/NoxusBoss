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

float2 GetVelocity(float2 coords)
{
    return float2(sin(coords.x * 34.557519) + sin(coords.x * 24.464536), 0);
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Calculate values for the warp noise.
    float warpAngle = tex2D(uImage1, coords * 7.3 + float2(uTime * 0.1, 0)).r * 16;
    float2 warpNoiseOffset = float2(sin(warpAngle + 1.57), sin(warpAngle));
    
    float4 previousColor = tex2D(uImage0, coords - warpNoiseOffset * 0.0008);
    previousColor.rgb *= 0.9;
    previousColor.a *= 0.8;
    
    return previousColor;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}