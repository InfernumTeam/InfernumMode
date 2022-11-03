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
    
    float4 vignetteColor = color * lerp(0.7, 1, vignetteOpacity);
    float luminosity = (vignetteColor.r + vignetteColor.g + vignetteColor.b) / 3;
    float4 blendColor;
    if (length(uColor) < 0.5)
        blendColor = float4((uColor * vignetteColor.rgb * 2.0) * (1.0 - uColor.rgb * 2.0), 1);
    else
        blendColor = float4(vignetteColor.rgb * (1.0 - uColor.rgb) + sqrt(vignetteColor.rgb) * (uColor.rgb * 2.0 - 1.0), 1);
    
    return lerp(vignetteColor, blendColor, uOpacity * 0.6) * (1.0 + luminosity * uOpacity * 1.7);
}

technique Technique1
{
    pass ScreenPass
    {
        PixelShader = compile ps_2_0 Filter();
    }
}