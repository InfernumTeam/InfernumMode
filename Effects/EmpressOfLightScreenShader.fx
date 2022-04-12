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

float GetLerpValue(float x, float from, float to)
{
    return saturate((x - from) / (to - from));
}
float4 Filter(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    float distanceFromCenter = saturate(distance(coords, 0.5) * 1.414);
    float vignetteStart = lerp(1, 0.85, uIntensity);
    float vignetteOpacity = GetLerpValue(vignetteStart, distanceFromCenter, 1);
    return color * lerp(0.7, 1, vignetteOpacity);
}

technique Technique1
{
    pass ScreenPass
    {
        PixelShader = compile ps_2_0 Filter();
    }
}