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
float noiseIntensity;
float horizontalDisplacementFactor;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Calculate noise values for both texture and positional displacement.
    float noise = tex2D(uImage1, coords * 3 + float2(0, uTime * 0.54)).r;
    float pixelOffsetNoise = tex2D(uImage1, coords * 3 + float2(0, uTime * 0.43)) * 2 - 1;
    
    float4 baseColor = tex2D(uImage0, coords + float2(pixelOffsetNoise * horizontalDisplacementFactor, 0)) * sampleColor;
    return baseColor - (1 - float4(uColor, 0)) * baseColor.a * pow(noise, 3) * noiseIntensity;
}
technique Technique1
{
    pass GlitchPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}