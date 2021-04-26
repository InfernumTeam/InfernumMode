sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
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

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    if (!any(color))
        return color;
    float4 color1 = tex2D(uImage1, coords.xy);

    if (color1.g > uOpacity * 1.1)
        color.rgba = 0;
    else if (color1.g > uOpacity)
        color = float4(0.1, lerp(0.16, 0.7, cos(coords.x * 2.7 + uTime * 1.5)), 1, 1) * 0.4;
    return color;
}

technique Technique1
{
    pass DeathPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}