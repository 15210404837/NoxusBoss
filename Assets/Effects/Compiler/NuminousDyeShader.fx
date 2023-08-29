sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
float3 uColor;
float3 uSecondaryColor;
float uOpacity;
float2 uTargetPosition;
float uSaturation;
float uRotation;
float uTime;
float4 uSourceRect;
float2 uWorldPosition;
float uDirection;
float3 uLightSource;
float2 uImageSize0;
float2 uImageSize1;
float4 uLegacyArmorSourceRect;
float2 uLegacyArmorSheetSize;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Calculate the 0-1 coords value relative to whatever frame in the texture is being used.
    float2 framedCoords = (coords * uImageSize0 - uSourceRect.xy) / uSourceRect.zw;
    float2 pixelationFactor = 4 / uSourceRect.zw;
    float2 snappedFramedCoords = round(framedCoords / pixelationFactor) * pixelationFactor;
    
    // Get the pixel's color on the base texture.
    float4 color = tex2D(uImage0, coords);
    
    float brightness = (color.r + color.g + color.b) / 3;
    float brightnessIncrease = abs(sin(uTime * -2.5)) * brightness * 0.5;
    float2 mapCoords = float2(brightness * 1.4 + brightnessIncrease, 0.5);
    float4 result = tex2D(uImage1, clamp(mapCoords, 0.01, 0.94));
    
    return result * color.a * sampleColor.a;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}