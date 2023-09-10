sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
sampler uImage2 : register(s2);

float globalTime;
float darknessNoiseScrollSpeed;
float brightnessNoiseScrollSpeed;
float2 darknessScrollOffset;
float2 brightnessScrollOffset;
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
    float2 baseCoords = input.TextureCoordinates;
    float2 coords = frac(input.TextureCoordinates * float2(2, 1));
    float3 finalColor = color.rgb;
    
    // Calculate color-affecting interpolants.
    float darknessNoise = tex2D(uImage1, coords * float2(2, 1) + darknessScrollOffset + float2(globalTime * -darknessNoiseScrollSpeed, 0));
    float fadeToWhite = InverseLerp(0.2 - darknessNoise * 0.15, 0, baseCoords.x) * 0.85;
    float brightnessNoise = tex2D(uImage1, coords * float2(2, 1) + brightnessScrollOffset + float2(globalTime * -brightnessNoiseScrollSpeed, 0));
    
    // Apply darkness effects.
    finalColor += float3(-0.75, -0.75, -0.45) * darknessNoise;
    
    // Apply brightness effects.
    finalColor *= lerp(1, 6, pow(brightnessNoise, 2));
    finalColor.rg -= brightnessNoise * 0.4;
    
    // Increase the contrast of the color a good bit, based on the brightness interpolant.
    // This "splits" the color bands to their extremes the higher the power is.
    float contrastPower = brightnessNoise * 0.75 + 1.15;
    finalColor = finalColor * contrastPower + (1 - contrastPower) * 0.5;
    
    // Calculate the highlight. This allows dark parts to have a bit of interesting texturing.
    float3 highlight = (1 - tex2D(uImage2, coords * float2(1.5, 1) + float2(0, globalTime * -0.06))) * lerp(0.5, 5, pow(darknessNoise, 3));
    
    // Calculate the edge-fade opacity.
    float opacity = InverseLerp(0, 0.125, coords.y) * InverseLerp(1, 0.875, coords.y);
    
    return lerp(float4(finalColor + pow(highlight, 3.6), 1), 2, fadeToWhite) * opacity;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
