sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
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
float4 uShaderSpecificData;
float uCircleRadius;

float InverseLerp(float a, float b, float t)
{
    return saturate((t - a) / (b - a));
}
float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    float cutoff = uCircleRadius / length(uImageSize0);
    float opacity = InverseLerp(cutoff * 0.8, cutoff, distance(coords, 0.5));
    return color * sampleColor * opacity;
}

technique Technique1
{
    pass CutoutPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}