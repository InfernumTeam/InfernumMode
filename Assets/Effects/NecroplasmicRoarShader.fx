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

float GetLerpValue(float x, float min, float max)
{
    return saturate((x - min) / (max - min));
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = float4(uColor, 1);
    float2 noiseCoords = coords;
    float4 baseNoiseColor = tex2D(uImage1, noiseCoords);
    float multiplier = baseNoiseColor.b * 0.1 + sin(uTime * 22 + coords.x * 6.283) * 0.09;
    noiseCoords.x += (baseNoiseColor.g * multiplier) - multiplier / 2 - uTime * 0.46;
    noiseCoords.x = frac(noiseCoords.x);
    
    color *= lerp(0.8, 1.2, tex2D(uImage1, noiseCoords));
    float distanceRatio = distance(coords.xy, float2(0.5, 0.5)) * 1.414;
    
    float opacity = color.a * pow((1 - (GetLerpValue(distanceRatio, 0.015, 0.835))), 4);
    opacity *= lerp(1, 1.9, GetLerpValue(distanceRatio, 0.67, 0.6));
    opacity *= lerp(1, 2.9, 1 - GetLerpValue(distanceRatio, 0.1, 0) * GetLerpValue(distanceRatio, 1, 0.9));
    opacity *= uOpacity;
    return color * opacity * 1.125;
}
technique Technique1
{
    pass BurstPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}