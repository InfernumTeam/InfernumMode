sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
float3 uColor;
float3 uSecondaryColor;
float uOpacity : register(C0);
float uSaturation;
float uCircularRotation;
float uRotation;
float uTime;
float4 uSourceRect;
float2 uWorldPosition;
float uDirection;
float3 uLightSource;
float2 uImageSize0;
float2 uImageSize1;
float2 overallImageSize;
matrix uWorldViewProjection;
float2x2 localMatrix;
float4 uShaderSpecificData;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    float2 framedCoords = (coords * uImageSize0 - uSourceRect.xy) / uSourceRect.zw * 0.16;
    float4 c1 = tex2D(uImage1, framedCoords + float2(sin(uTime * 0.12) * 0.5, uTime * 0.03));
    float4 c2 = tex2D(uImage1, framedCoords + float2(uTime * -0.019, sin(uTime * -0.09 + 0.754) * 0.6));
    float4 result = pow(c1 + c2, 2.6);
    float luminence = (result.r + result.g + result.b) / 3;
    result.b += luminence * 0.8;
    
    return result * color.a * sampleColor.a;
}

technique Technique1
{
    pass TrailPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}