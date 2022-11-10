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
float blurExponent;
float blurSigmaSquared;
float blurMaxOffset;
float blurAdditiveBrightness;
float maxSaturationAdditive;
bool onlyShowBlurMap;
float prefilteringThreshold;
float blurSaturationBiasInterpolant;

// Table of 12 evenly spaced directions, based on <cos(a), sin(a)>
float2 directions[] =
{
    float2(1, 0),
    float2(0.86603, 0.5),
    float2(0.5, 0.86603),
    float2(0, 1),
    float2(-0.5, 0.86603),
    float2(-0.86603, 0.5),
    float2(-1, 0),
    float2(-0.86603, -0.5),
    float2(-0.5, -0.86603),
    float2(0, -1),
    float2(0.5, -0.86603),
    float2(0.86603, -0.5)
};

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (to - from));
}

float3 hsv2rgb(float3 c)
{
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
    return c.z * lerp(K.xxx, saturate(p - K.xxx), c.y);
}
float3 rgb2hsv(float3 c)
{
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
    float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

    float d = q.x - min(q.w, q.y);
    float e = 1e-6;
    return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

float4 Filter(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    float4 blurColor = tex2D(uImage1, coords);
    float blurLuminosity = (blurColor.r + blurColor.g + blurColor.b) / 3;
    float luminosity = (color.r + color.g + color.b) / 3;
    
    // Bias blur values towards higher saturations.
    float3 blurHsv = rgb2hsv(blurColor.rgb);
    blurHsv.y = saturate(blurHsv.y + blurSaturationBiasInterpolant * InverseLerp(0.24, 0.32, blurLuminosity) * InverseLerp(0.28, 0.36, luminosity));
    blurColor = float4(hsv2rgb(blurHsv), 1);

    float3 colorHsv = rgb2hsv(color.rgb);
    
    // Make colors have more saturation, thus making them more vivid, the stronger of a blur effect there is at a given pixel.
    colorHsv.y = saturate(colorHsv.y + maxSaturationAdditive * blurLuminosity);
    color = float4(hsv2rgb(colorHsv), 1);
    color = 1 - exp(-color);
    
    if (onlyShowBlurMap)
        return blurColor;
    
    return color + pow(blurColor, blurExponent) * blurAdditiveBrightness;
}

float4 Downsample(float2 coords : TEXCOORD0) : COLOR0
{
    float2 maxOffset = blurMaxOffset / uImageSize1;
    float4 result = 0;
    
    // Samples pixels in a circular area around the pixel for blending purposes.
    for (float a = 0; a < 12; a++)
    {
        float angle = 6.283 * a / 12;
        for (float i = 0; i < 12; i++)
            result += tex2D(uImage0, coords + float2(sin(angle + 1.57), sin(angle)) * maxOffset * i / 12);
    }
    return result / 145;
}

float4 DownsampleFast(float2 coords : TEXCOORD0) : COLOR0
{
    float2 maxOffset = blurMaxOffset / uImageSize1;
    float4 result = 0;
    
    // Samples pixels in a circular area around the pixel for blending purposes.
    for (float a = 0; a < 12; a++)
    {
        for (float i = 0; i < 12; i++)
            result += tex2D(uImage0, coords + directions[a] * maxOffset * i / 12);
    }
    return result / 145;
}

float4 Prefiltering(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    float luminance = color.r * 0.21f + color.g * 0.72f + color.b * 0.07f;

    if (luminance > prefilteringThreshold)
        return color * (luminance - prefilteringThreshold) / (1 - prefilteringThreshold);
    
    return 0;
}

technique Technique1
{
    pass ScreenPass
    {
        PixelShader = compile ps_3_0 Filter();
    }
    pass DownsamplePass
    {
        PixelShader = compile ps_3_0 Downsample();
    }
    pass DownsampleFastPass
    {
        PixelShader = compile ps_3_0 DownsampleFast();
    }
    pass PrefilteringPass
    {
        PixelShader = compile ps_2_0 Prefiltering();
    }
}