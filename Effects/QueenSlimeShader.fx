sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
sampler uImage2 : register(s2);
float3 uColor;
float3 uSecondaryColor;
float uOpacity : register(C0);
float uSaturation;
float uRotation;
float uTime;
float4 uSourceRect;
float2 uWorldPosition;
float uDirection;
float3 uLightSource;
float2 uImageSize0;
float2 uImageSize1;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    float4 rainbow = tex2D(uImage1, float2((1 - coords.x) * 1.75 - 0.35, coords.y * 2));
    float4 rainbowFade = tex2D(uImage2, float2(coords.x, coords.y) * float2(1.4, 4));
    float3 result = lerp(color.rgb, rainbow.rgb, saturate(rainbowFade.g + color.r + color.g * 0.6));
    result = lerp(result, 0.4, (1 - color.r) * 0.5);
    return float4(result, 1) * color.a * sampleColor * 1.25;
}

technique Technique1
{
    pass SlimePass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}