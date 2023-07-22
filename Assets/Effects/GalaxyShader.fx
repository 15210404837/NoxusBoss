sampler uImage0 : register(s0);

float globalTime;
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
    // Apply the matrix transformation to the coordinates firstly.
    coords = mul(coords - 0.5, transformation) + 0.5;
    float4 color = 0;
    
    // Create spiral arms based on the classic DoG portal spin effect.
    float distanceFromCenter = length(coords - 0.5);
    float angle = distanceFromCenter * 11.6 - globalTime * 0.94;
    color += tex2D(uImage0, RotatedBy(coords - 0.5, angle) + 0.5);
    
    // Make the portal fade out at the edges.
    color *= exp(distanceFromCenter * -7.4) * 3;
    
    // Apply the sample color.
    color *= sampleColor;
    
    // Make the center of the portal stronger.
    color += InverseLerp(0.16, 0.04, distanceFromCenter) * sampleColor.a;
    
    return color;
}
technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}