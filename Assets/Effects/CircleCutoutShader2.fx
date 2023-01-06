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
float ectoplasmCutoffOffsetMax;

float TriangleWave(float x)
{
    if (x % 2 < 1)
        return x % 2;
    return -(x % 2) + 2;
}
float InverseLerp(float a, float b, float t)
{
    return saturate((t - a) / (b - a));
}
float2 RotatedBy(float2 v, float angle)
{
    float si = sin(angle);
    float co = sin(angle + 1.57);
    return float2(v.x * co - v.y * si, v.x * si + v.y * co);
}
float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float distanceFromCenter = distance(coords, 0.5);
    float4 color = tex2D(uImage0, coords);
    float2 ectoplasmCoords = RotatedBy(coords - 0.5, distanceFromCenter * 3.14 + uTime * 0.4) + 0.5;
    ectoplasmCoords *= 10;
    ectoplasmCoords.y -= uTime * 0.1;
    
    float4 ectoplasmColor = tex2D(uImage1, ectoplasmCoords);
    float cutoff = uCircleRadius / length(uImageSize0);
    float ectoplasmCutoffOffset = (TriangleWave(coords.y * 74 + uTime * 4.56) * ectoplasmCutoffOffsetMax) / uImageSize0.x;
    float opacity = InverseLerp(cutoff * 0.8, cutoff, distanceFromCenter);
    float ectoplasmInterpolant = InverseLerp(cutoff * 0.85 + ectoplasmCutoffOffset, cutoff * 1.6, distanceFromCenter) * InverseLerp(cutoff * 2.25, cutoff * 1.64, distanceFromCenter);
    return color * sampleColor * opacity + ectoplasmColor * ectoplasmInterpolant;
}

technique Technique1
{
    pass CutoutPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}