sampler uImage0 : register(s0);
sampler uImage1 : register(s1);

float generalOpacity;
float globalTime;
float blackHoleRadius;
float spiralFadeSharpness;
float spiralSpinSpeed;
float2 spriteSize;
float2x2 transformation;

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (to - from));
}

float2 RotatedBy(float2 v, float theta)
{
    float s = sin(theta);
    float c = sin(theta + 1.57);
    return float2(v.x * c - v.y * s, v.x * s + v.y * c);
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float distanceFromCenterAtFirst = distance(coords, 0.5);
    
    // Apply the matrix transformation to the coordinates.
    coords = mul(coords - 0.5, transformation) + 0.5;
    
    float distanceFromCenter = distance(coords, 0.5);
    float2 distanceFromCenterPixels = distanceFromCenter * spriteSize;
    float4 color = 0;
    
    // Create spiral arms based on the classic DoG portal spin effect.
    float angle = distanceFromCenter * 11.6 - globalTime * spiralSpinSpeed;
    float2 spiralCoords = RotatedBy(coords - 0.5, angle) + 0.5;
    color += tex2D(uImage1, spiralCoords) * sampleColor;
    color += tex2D(uImage1, (0.5 - spiralCoords) * 0.25);
    
    // Make the portal fade out at the edges.
    color *= exp(distanceFromCenter * -spiralFadeSharpness) * InverseLerp(0.38, 0.32, distanceFromCenter) * 3;
    
    // Make the center a black hole.
    color = lerp(color, float4(0, 0, 0, 1), InverseLerp(0.06, 0.05, distanceFromCenterAtFirst));
    
    return color * generalOpacity;
}
technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}