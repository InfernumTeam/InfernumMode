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
float2 actualImageSize;
float4 uActualSourceRect;

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (to - from));
}
float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float frameY = (coords.y * actualImageSize.y - uActualSourceRect.y) / uActualSourceRect.w;
    float noiseThreshold = uSaturation - frameY;
    noiseThreshold = abs(pow(noiseThreshold, 0.5)) * sign(noiseThreshold);
    
    float noise = step(tex2D(uImage1, coords).r, noiseThreshold);
    float4 color = tex2D(uImage0, coords) * noise;
    
    if (noise < noiseThreshold)
    {
        float4 idealColor = float4(uColor, 1) * tex2D(uImage0, coords).a;
        idealColor.a = 0;
        
        color = lerp(color, idealColor, InverseLerp(noiseThreshold - 0.1, noiseThreshold, frameY));
    }
    
    if (color.a > 0)
        color.a = noise;
    return color;
}

technique Technique1
{
    pass HologramPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}