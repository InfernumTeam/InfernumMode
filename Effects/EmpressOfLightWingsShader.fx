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

float GetLerpValue(float x, float from, float to)
{
    return saturate((x - from) / (to - from));
}
float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 rainbowCoords = coords;
    rainbowCoords.x = saturate(sin(rainbowCoords.x * 3.141));
    rainbowCoords.x += uTime * 0.75;
    float4 color = tex2D(uImage0, coords);
    float4 rainbow = tex2D(uImage1, rainbowCoords);
    if (color.r == 0)
        return color;
    return rainbow * color.a * sampleColor.a;
}

technique Technique1
{
    pass SlimePass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}