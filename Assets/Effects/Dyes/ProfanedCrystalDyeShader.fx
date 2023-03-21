sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
sampler uImage2 : register(s2);
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
float2 uImageSize2;
float2 uTargetPosition;
float4 uLegacyArmorSourceRect;
float2 uLegacyArmorSheetSize;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 framedCoords = (coords * uImageSize0 - uSourceRect.xy) / uSourceRect.zw;
    float4 baseColor = tex2D(uImage0, coords);
    
    // Generate the crystal pattern.
    float crystalScale = 0.6;
    float4 crystalValue = tex2D(uImage1, framedCoords * crystalScale);
    float4 normal = tex2D(uImage2, framedCoords * crystalScale);
    
    float3 lightDirection = float3(0.5, 0, 0.05);
    
    // Apply normal map brightness effects.
    float brightness = max(0, dot(normalize(normal.xyz), normalize(lightDirection)));
    
    // Apply crystal effect.
    float3 crystalColorRGB = lerp(uColor, float3(1, 0.8, 0.74), pow(brightness, 0.6));
    float4 crystalColor = float4(crystalColorRGB * (1 + brightness), 1) * pow(crystalValue, 3) * baseColor.a;
    return lerp(baseColor, crystalColor, sqrt(brightness) * 0.9);
}
technique Technique1
{
    pass DyePass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}