sampler uImage0 : register(s0);

float globalTime;
float totalSplits;
float distanceBandingFactor;
float animationSpeed;
float vignetteStrength;
float contrastPower;
float greyscaleInterpolant;
float2 zoom;
float2 screenPosition;

// Hlsl's % operator applies a modulo but conserves the sign of the dividend, hence the need for a custom function.
float mod(float a, float n)
{
    return a - floor(a / n) * n;
}

float easeInOut(float x)
{
    return (x * x) * 3 - (x * x * x) * 2;
}

float easeInOut2(float x)
{
    return easeInOut(easeInOut(x));
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Calculate the distance and angle from the pixel relative to the center of the texture.
    float distanceFromCenter = distance(coords, 0.5);
    float angleFromCenter = atan2(coords.y - 0.5, coords.x - 0.5);    
        
    // Calculate the angle at which the texture should be sampled. This is calculated in such a way that the result swirl around slowly over time and distorts
    // the further away a pixel is from the center.
    float splitAngleSlice = 6.283 / totalSplits;
    float splitAngle = mod(angleFromCenter, splitAngleSlice);
    splitAngle = abs(splitAngle - splitAngleSlice * 0.5) - globalTime * animationSpeed;
    
    float offsetAngle = splitAngle + distanceFromCenter * distanceBandingFactor;
    float2 distortedCoords = float2(sin(offsetAngle + 1.57), sin(offsetAngle)) * distanceFromCenter;
    
    float4 result = tex2D(uImage0, distortedCoords * float2(1, 1.7777) * zoom - screenPosition * 0.0000036) * sampleColor;
    result.rgb = lerp(result.rgb, pow(easeInOut2(dot(result.rgb, float3(0.299, 0.587, 0.114))), contrastPower), greyscaleInterpolant);
    
    // Apply a vignette effect.
    result = lerp(result, float4(0, 0, 0, 1) * result.a, saturate(distanceFromCenter * vignetteStrength));
    
    return result;
}
technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}