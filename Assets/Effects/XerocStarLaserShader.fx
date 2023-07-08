sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
sampler uImage2 : register(s2);
float3 uColor;
float3 uSecondaryColor;
float uOpacity;
float uSaturation;
float uRotation;
float uTime;
float4 uSourceRect;
float2 uWorldPosition;
float uDirection;
float3 uLightSource;
float2 uImageSize0;
float2 uImageSize1;
float2 uImageSize2;
matrix uWorldViewProjection;
float2 uCorrectionOffset;
float4 uShaderSpecificData;
float uStretchReverseFactor;
float scrollOffset;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    float4 pos = mul(input.Position, uWorldViewProjection);
    output.Position = pos + float4(uCorrectionOffset.x, uCorrectionOffset.y, 0, 0);
    output.Position.z = 0;
    
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;

    return output;
}

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (to - from));
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 color = input.Color;
    float2 coords = input.TextureCoordinates;
    float stretchReverseFactor = uStretchReverseFactor;
    if (stretchReverseFactor <= 0)
        stretchReverseFactor = 0.3;
    float adjustedCompletionRatio = coords.x * stretchReverseFactor + scrollOffset;
    
    // Read the fade map as a streak.
    float time = uTime * stretchReverseFactor * 4;
    float bloomFadeout = pow(sin(coords.y * 3.141), 4);
    float4 fadeMapColor1 = tex2D(uImage1, float2(frac(adjustedCompletionRatio * 20 - time * 1.6), coords.y));
    float4 fadeMapColor2 = tex2D(uImage1, float2(frac(adjustedCompletionRatio * 7 - time * 0.8), coords.y * 0.5));
    float opacity = (0.5 + fadeMapColor1.g) * bloomFadeout;
    
    // Fade out at the ends of the streak.
    if (coords.x < 0.023)
        opacity *= pow(coords.x / 0.023, 6);
    if (coords.x > 0.95)
        opacity *= pow(1 - (coords.x - 0.95) / 0.05, 6);
    float4 finalColor = color * opacity * 6;
    
    return finalColor + fadeMapColor2 * finalColor.a;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
