sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
sampler uImage2 : register(s2);
sampler uImage3 : register(s3);
float3 uColor;
float3 uSecondaryColor;
float2 uScreenResolution;
float2 uScreenPosition;
float2 uTargetPosition;
float2 uDirection;
float uOpacity;
float uTime;
float uIntensity;
float uProgress;
float2 uImageSize1;
float2 uImageSize2;
float2 uImageSize3;
float2 uImageOffset;
float uSaturation;
float4 uSourceRect;
float2 uZoom;
float4 uShaderSpecificData;

float Random(float2 coords)
{
    return tex2D(uImage0, coords);
}
float4 PixelShaderFunction(float4 sampleColor : TEXCOORD, float2 coords : TEXCOORD0) : COLOR0
{
    float r = Random(coords + float2(0.158, uTime * 0.01)) * 0.05;
    float n1 = Random(coords * 1613 + float2(uTime * 0.07, uTime * -0.04) - r);
    float n2 = Random(coords * 1759 + float2(uTime * 0.061, -uTime * 0.0464) + r);
    float x1 = sin(coords.y * 15924.185 + uTime * 5.1);
    float x2 = sin(coords.x * 7197.294 + uTime * -2.7182 * coords.y);
    float interpolant = (x1 + x2) * 0.25 + 0.5;
    float fadeToMaxColor = saturate(interpolant * 0.6 + uIntensity * 0.75);
    return float4(lerp(uColor, uSecondaryColor, fadeToMaxColor), 1) * lerp(n1, n2, interpolant) * uIntensity * 0.24;
}
technique Technique1
{
    pass DyePass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}