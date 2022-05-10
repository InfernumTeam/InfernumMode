sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
float3 uColor;
float3 uSecondaryColor;
float uOpacity;
float uTime;
float uIntensity;
float uProgress;
float2 uImageSize1;
float2 uImageSize2;
float uLetterCompletionRatio;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 noiseColor = tex2D(uImage1, float2(coords.x, coords.y));
    float4 color = tex2D(uImage0, coords) * sampleColor;
    
    if ((color.r + color.g + color.b) / 3 < 0.03 || color.a < 0.05)
        return color;
    
    float3 blendColor = lerp(uColor, uSecondaryColor, cos(uTime * 4 + uLetterCompletionRatio * 4) * 0.5 + 0.5);
    float blendFactor = (cos(uTime * 9 + coords.x * 18) * 0.5 + 0.5) * 0.5;
    
    // Allow the fade to pulse upward based on how far up the pixel is.
    blendFactor += (cos(uTime * -13 - coords.y * 7.1)) * 0.5;
    float brightness = blendFactor * 0.5 + noiseColor.r * 0.5;
    
    // Cause the effects to taper off at the bottom of the sprite.
    if (coords.y < 0.2)
    {
        brightness *= coords.y / 0.2;
        blendFactor *= coords.y / 0.2;
    }
    float4 colorBlendMultiplier = lerp(float4(blendColor, 1), float4(1, 1, 1, 1), saturate(pow(blendFactor * 1.5, 2)));
    return (lerp(color, float4(blendColor, 1), blendFactor * 0.5 + 0.2) * color.a) * colorBlendMultiplier * (1 + brightness) * sampleColor.a;
}

technique Technique1
{
    pass LetterPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}