sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
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
float4 uShaderSpecificData;
float distortionIntensity;
float2 center;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Calculate noise and the general collapse intensity. The noise is used to make the collapse effect not feel awkwardly perfect, as though
    // some parts are being distorted more rapidly than others.
    float noise = tex2D(uImage1, coords * 4);
    float collapseIntensity = (45 + noise * 75) * pow(distortionIntensity, 3);
    
    // Determine the relative collapse intensity for the given pixel. This is based on distance from the center point.
    float intensity = max(0, 1 - distance(coords, center) * collapseIntensity);
    float4 color = tex2D(uImage0, coords);
    
    // If the intensity is zero, then this pixel has been completely absorbed by the distortion and should be invisible so that the background effect can appear.
    if (intensity <= 0)
        return 0;
    
    // Apply the collapse effect.
    float2 distortionOffset = (coords - center) * (1 - intensity);
    float2 collapsedCoords = coords - 0.85 * distortionOffset;
    float4 collapsedColor = tex2D(uImage0, collapsedCoords);
    
    // Return the collapsed color with the collapse intensity applied.
    return collapsedColor * intensity;
}

technique Technique1
{
    pass DistortionPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}