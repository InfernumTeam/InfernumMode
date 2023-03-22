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
float4 uShaderSpecificData;

float3 fireColor;
float3 smokeColor;
float jiggleSpeed;
float completionRatio;

float4 MainFunction(float2 uv : TEXCOORD0) : COLOR
{
    // Pixelate
    float2 Resolution = float2(10, 10);
    uv.x -= uv.x % (1 / (Resolution.x * 2));
    uv.y -= uv.y % (1 / (Resolution.y * 2));
    float2 mappedUv = float2(uv.x - 0.5, (1 - uv.y) - 0.5);
    
    // Get the length of the doubled distance, so that 0 = at the center of the sprite and 1 = at the very edge of the circle
    float distanceFromCenter = length(mappedUv) * 2;
    
    //Crop the sprite into a circle
    if (distanceFromCenter > 1.2)
        return float4(0, 0, 0, 0);
    
    
    // Use the r of the main texture for the smoke texture.
    // Use the g of the main texture for the fire texture.
    // Use the b of the main texture for the alpha.
    float4 mainTexture = tex2D(uImage1, mappedUv);
        
    float3 smokePixelColor = mainTexture.r * smokeColor;
    
    float3 firePixelColor = mainTexture.g * fireColor;
    
    float4 finalColor;
    finalColor.rgb = lerp(firePixelColor, smokePixelColor, completionRatio - 0.5);
    finalColor.a = saturate(mainTexture.b * 2 * (1.6 - distanceFromCenter));
    // If the alpha is below this, return.
    if (finalColor.a < 0.3)
        return float4(0, 0, 0, 0);
    return finalColor;
}

technique Technique1
{
    pass ExplosionPass
    {
        PixelShader = compile ps_2_0 MainFunction();
    }
}