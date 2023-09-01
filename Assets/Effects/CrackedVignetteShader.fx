sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
sampler uImage2 : register(s2);

float globalTime;
float animationSpeed;
float vignettePower;
float vignetteBrightness;
float4 primaryColor;
float4 secondaryColor;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float vignetteInterpolant = saturate(pow(distance(coords, 0.5), vignettePower) * vignetteBrightness);
    
    // Calculate crack colors based on the noise texture.
    float4 crackColor1 = tex2D(uImage1, coords * 2 + float2(0, globalTime * -animationSpeed * 0.6));
    float4 crackColor2 = tex2D(uImage1, coords * 6 + float2(globalTime * animationSpeed, 0));
    float4 crackColor = crackColor1 * primaryColor + crackColor2 * secondaryColor;
    return sampleColor * vignetteInterpolant * crackColor;
}

technique Technique1
{
    pass AutoloadPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}