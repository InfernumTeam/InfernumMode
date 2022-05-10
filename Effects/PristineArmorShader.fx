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
float uIntensity;
float uVibrancy;

float4 CosineInterpolation(float value)
{
    return (1 - cos(3.141592 * value)) / 2;
}

float4 ArmorCircle(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 baseColor = tex2D(uImage0, coords);
    
    float2 armorCoords = coords / float2(3, 7);
    armorCoords += float2(sin(uTime * 1.2 * uSaturation), cos(uTime * 0.8 * uSaturation));
    armorCoords.y %= 1;
    armorCoords.x %= 1;
    
    float4 armorColor = tex2D(uImage1, armorCoords);
    
    // Use cosine interpolation for more smooth transitioning.
    float3 colorToUse = lerp(baseColor, armorColor, CosineInterpolation(uIntensity)).rgb;
    
    return float4(colorToUse.rgb, 1) * baseColor.a * uVibrancy;
}

technique Technique1
{
    pass PristinePass
    {
        PixelShader = compile ps_2_0 ArmorCircle();
    }
}