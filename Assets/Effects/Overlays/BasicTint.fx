sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
sampler uImage2 : register(s2);
sampler uImage3 : register(s3);
float3 uColor;
float3 uSecondaryColor;
float2 uScreenResolution;
float2 uScreenPosition;
float2 uTargetPosition;
float2 uDirection;
float uOpacity;
float uTime;
float uIntensity;
float uProgress;
float2 uImageSize1;
float2 uImageSize2;
float2 uImageSize3;
float2 uImageOffset;
float uSaturation;
float4 uSourceRect;
float2 uZoom;
float4 uShaderSpecificData;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Get the color of the pixel at the current coords.
    float4 color = tex2D(uImage0, coords);
    // Calcuate the brightness of each pixel,
    float luminosity = (color.r + color.g + color.b) / 3;
    // This needs to be on one line due to alpha fuckery.
    // It's basically multiplying the original color by the new one made by the shader, while also multiplying
    // it by the passed through color, which is being multiplied by the passed through opacity.
    return lerp(color, float4(uColor * luminosity, 1) * color.a, uOpacity);
}

technique Technique1
{
    pass BasicTint
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}   