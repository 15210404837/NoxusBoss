sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
sampler uImage2 : register(s2);
sampler uImage3 : register(s3);

float globalTime;
float uStretchReverseFactor;
float scrollOffset;
float scrollSpeedFactor;
float2 uCorrectionOffset;
matrix uWorldViewProjection;

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
    float time = globalTime * stretchReverseFactor * scrollSpeedFactor * 4;
    float bloomFadeout = pow(sin(coords.y * 3.141), 0.92);
    float4 fadeMapColor1 = tex2D(uImage1, float2(frac(adjustedCompletionRatio * 20 - time * 1.6), coords.y));
    float4 fadeMapColor2 = tex2D(uImage1, float2(frac(adjustedCompletionRatio * 7 - time * 0.8), coords.y * 0.5));
    float darkenMapColor = tex2D(uImage3, float2(frac(adjustedCompletionRatio * 22.5 - time * 4), frac(coords.y * 1.5))).r;
    float opacity = (0.5 + fadeMapColor1.g) * bloomFadeout;
    
    // Fade out at the sides of the streak.
    float edgeFadeOutInterpolant = tex2D(uImage2, float2(0.5, coords.x * 0.75 - time * 1.4));
    float edgeFadeOut = lerp(0.023, 0.5, edgeFadeOutInterpolant);
    if (coords.y < edgeFadeOut)
        opacity *= pow(coords.y / edgeFadeOut, 6);
    if (coords.y > 1 - edgeFadeOut)
        opacity *= pow(1 - (coords.y - 1 + edgeFadeOut) / edgeFadeOut, 6);
    
    // Apply bloom to the resulting colors.
    float4 finalColor = color * opacity * 2;
    finalColor += (fadeMapColor2 + bloomFadeout * 0.4) * finalColor.a;
    
    // Subtract from the color based on the blackout streak.
    finalColor.rgb = lerp(finalColor.rgb, 0, pow(darkenMapColor, 0.25) * 0.85);
    
    return pow(finalColor, 2);
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
