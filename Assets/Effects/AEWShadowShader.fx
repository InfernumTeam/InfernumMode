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
float2 actualSize;
float4 uShaderSpecificData;
float lightFormInterpolant;
float2 screenMoveOffset;
float darkFormInterpolant;

float4 PerformLightEffects(float4 baseColor, float2 coords)
{
    // Basically just a single four-way offset additive blending trick but in a shader.
    float2 offset = 10 / actualSize * lightFormInterpolant;
    float brightness = lerp(1, 12, lightFormInterpolant);
    float4 left = tex2D(uImage0, coords + float2(-offset.x, 0)) * brightness;
    float4 right = tex2D(uImage0, coords + float2(offset.x, 0)) * brightness;
    float4 top = tex2D(uImage0, coords + float2(0, -offset.y)) * brightness;
    float4 bottom = tex2D(uImage0, coords + float2(0, offset.y)) * brightness;
    return (left + right + top + bottom + baseColor) * 0.2;
}

float4 PerformDarknessEffects(float4 baseColor, float2 coords)
{
    return lerp(baseColor, baseColor * float4(uSecondaryColor, 1) * 0.3, pow(darkFormInterpolant, 0.35));
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    if (lightFormInterpolant > 0)
        color = PerformLightEffects(color, coords);
    if (darkFormInterpolant > 0)
        color = PerformDarknessEffects(color, coords);

    return color * sampleColor + tex2D(uImage2, coords) * float4(uColor, 1) * sqrt(darkFormInterpolant) * 11;
}

float4 UpdatePreviousState(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    float2 offset = 1 / actualSize;
    
    // Sample pixels in the cardinal directions, with a bit of a wind offset to make it look like shadowy flames swaying.
    float2 windOffset = (float2(sin(uTime + coords.x * 120) * 1.6, 4) + screenMoveOffset * 0.5) / actualSize;
    
    float4 left = tex2D(uImage0, coords + float2(-offset.x, 0) + windOffset);
    float4 right = tex2D(uImage0, coords + float2(offset.x, 0) + windOffset);
    float4 top = tex2D(uImage0, coords + float2(0, -offset.y) + windOffset);
    float4 bottom = tex2D(uImage0, coords + float2(0, offset.y) + windOffset);
    float4 average = (left + right + top + bottom) * 0.25 - (1 - tex2D(uImage1, coords)) * 0.02;
    
    // Incorporate some noise to give a sense of texture to the shadow.
    float4 noise = tex2D(uImage2, frac(coords * 3)) * length(tex2D(uImage1, coords));
    
    return lerp(average, noise, 0.034);
}

technique Technique1
{
    pass BurnPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
    pass UpdatePass
    {
        PixelShader = compile ps_2_0 UpdatePreviousState();
    }
}