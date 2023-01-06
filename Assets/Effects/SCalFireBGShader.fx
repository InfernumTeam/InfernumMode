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
float4 uvArenaArea;

bool InsideArena(float2 coords)
{
    return uvArenaArea.x < coords.x + 0.001 &&
    coords.x < uvArenaArea.x + uvArenaArea.z &&
    uvArenaArea.y < coords.y + 0.001 &&
    coords.y < uvArenaArea.y + uvArenaArea.w;
}

float InverseLerp(float x, float a, float b)
{
    return saturate((x - a) / (b - a));
}
float RectangularDistance(float2 a, float2 b)
{
    float n = 100;
    float2 absoluteDistance = abs(a - b);
    absoluteDistance.x *= 1.72;
    return pow(pow(absoluteDistance.x, n) + pow(absoluteDistance.y, n), 1.0 / n);
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 arenaCenter = uvArenaArea.xy + uvArenaArea.zw * 0.5;
    float distanceFromCenter = RectangularDistance(arenaCenter, coords);
    float2 noiseCoordsBase = coords * 1.2;
    float4 noise1 = tex2D(uImage1, noiseCoordsBase + float2(0, uTime * 0.064) + uScreenPosition * 0.0001);
    float4 noise2 = tex2D(uImage1, noiseCoordsBase + float2(uTime * 0.022, uTime * -0.049) + uScreenPosition * 0.0001);
    float4 noise = (noise1 + noise2) * 0.6;
    float noiseFadeInterpolant = InverseLerp(distanceFromCenter, 0.7, 1.4);
    float4 noiseColor = pow(noise, 3) * noiseFadeInterpolant * 1.4 + 0.2;
    
    float4 color = tex2D(uImage0, coords);
    return lerp(color, noiseColor, noiseFadeInterpolant * 0.5);
}
technique Technique1
{
    pass DyePass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}