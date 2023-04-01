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
    float3 framedCoords3 = float3(framedCoords, 0);
    float4 baseColor = tex2D(uImage0, coords);
    float luminosity = dot(baseColor.rgb, float3(0.212, 0.716, 0.072));
    
    // Generate the crystal pattern.
    float crystalScale = 0.42;
    float2 crystalPosition = frac(framedCoords * crystalScale + float2(uTime * uDirection * -0.017, 0) + uWorldPosition * 0.00016);
    float4 crystalValue = tex2D(uImage1, crystalPosition);
    float4 normal = tex2D(uImage2, crystalPosition);
    
    float3 lightDirection = float3(0.5, 0, 1.65);
    
    // Apply normal map brightness effects.
    float brightness = max(0, dot(normalize(normal.xyz), normalize(lightDirection - framedCoords3)));
    
    // Apply the crystal effect.
    float3 crystalColorRGB = lerp(uColor, float3(1, 0.8, 0.74), pow(brightness, 0.6));
    float4 crystalColor = float4(crystalColorRGB * (1 + brightness), 1) * pow(crystalValue, 3) * baseColor.a;
    float crystalColorInterpolant = pow(brightness, 0.2) * 0.45 + 0.35;
    crystalColorInterpolant *= pow(luminosity, 0.1);
    
    // Tamper with the individual color channels based on the luminosity of the base color to give some variance to the result.
    crystalColor.g -= luminosity * 0.3;
    crystalColor.b += luminosity;
    
    return lerp(baseColor, crystalColor, crystalColorInterpolant);
}
technique Technique1
{
    pass DyePass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}