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
    float time = uTime * 0.184;
    float noise1 = tex2D(uImage1, coords * 4 + float2(0, time * 0.3));
    float noise2 = tex2D(uImage1, coords * 3 + float2(time * 0.07 - 0.51, time * -0.2));
    float noise3 = tex2D(uImage1, coords * 2 + float2(time * -0.039 + 0.83, time * -0.09));
    float noise = (noise1 + noise2 + noise3) * 0.4;
    
    float4 color = tex2D(uImage0, coords) * sampleColor * noise;
    return color;
}

technique Technique1
{
    pass FogPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}