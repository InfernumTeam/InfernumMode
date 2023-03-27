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
float4 uShaderSpecificData;
float2 sheetSize;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords) * sampleColor;
    if (!any(color))
        return color;
    
    // Framed coordinates -- a UV of the NPC's current framed sprite, instead of the whole sheet
    float2 framedCoords = (coords * sheetSize - uShaderSpecificData.xy) / uShaderSpecificData.zw;
    float2 pixel = 1 / sheetSize;
    framedCoords = floor(framedCoords / pixel) * pixel;
    
    float4 noiseColor = tex2D(uImage1, framedCoords * 0.5);
    
    // If the noise over the erasure threshold, completely erase this pixel.
    if (noiseColor.r > 0.42)
        color = 0;

    return color;
}

technique Technique1
{
    pass CrackPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}