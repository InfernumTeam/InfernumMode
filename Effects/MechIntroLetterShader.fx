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

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (to - from));
}
float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords) * sampleColor;
    if ((color.r + color.g + color.b) / 3 < 0.04)
        return color;
    
    float metalTextureInterpolant = sin(coords.x * 126 + uLetterCompletionRatio * 431.415 + uTime * 7) + sin(coords.y * 16 + uLetterCompletionRatio * 297.182 + uTime * 8);
    metalTextureInterpolant = metalTextureInterpolant * 0.25 + 0.5;
    color = lerp(color, color.a * 1.5, metalTextureInterpolant * 0.7);
    
    float gleamInterpolant = tex2D(uImage1, float2(uLetterCompletionRatio + uTime * 1.2 + coords.x / 13, coords.y) * 0.45).r;
    color = float4(lerp(color.rgb, uColor, gleamInterpolant * 0.9), 1) * color.a;
    return color;
}

technique Technique1
{
    pass LetterPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}