sampler uImage0 : register(s0);
sampler uImage1 : register(s1);

float globalTime;

float edgeFadePower;
float edgeTaperDistance;

float noise1Zoom;
float noise2Zoom;
float animationSpeed;
float noiseOpacityPower;
float bottomBrightnessIntensity;
float4 colorAccent;

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (to - from));
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    
    // Make the edges of the texture smoothly taper out.
    float edgeFade = InverseLerp(0.99, 0.99 - edgeTaperDistance, coords.x) * InverseLerp(0.01, edgeTaperDistance + 0.01, coords.x);
    color *= pow(edgeFade, edgeFadePower);
    
    // Make the bottom of the texture smoothly taper out.
    float bottomBrightness = InverseLerp(0.92, 0.94, coords.y);
    float bottomFade = InverseLerp(0.99, 0.97, coords.y);
    color *= pow(bottomFade, edgeFadePower) + bottomBrightness * bottomFade * bottomBrightnessIntensity;
    
    // Calculate noise coordinates.
    float2 noiseCoord1 = float2(coords.x * noise1Zoom + globalTime * animationSpeed * -0.15, 0.5);
    float2 noiseCoord2 = float2(coords.x * noise2Zoom + globalTime * animationSpeed * 0.22, 1);
    
    // Calculate noise values from the coordinates.
    float noise1 = tex2D(uImage1, noiseCoord1);
    float noise2 = tex2D(uImage1, noiseCoord2);
    float addedNoise = noise1 * 2 + noise2 * 0.8;
    
    return color * sampleColor * pow(addedNoise, noiseOpacityPower) + colorAccent * noise2 * color.a;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}