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
matrix uWorldViewProjection;
float4 uShaderSpecificData;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

float GetLerpValue(float x, float min, float max)
{
    return saturate((x - min) / (max - min));
}
VertexShaderOutput VertexShaderFunction(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    float4 pos = mul(input.Position, uWorldViewProjection);
    output.Position = pos;
    
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;

    return output;
}

// The X coordinate is the trail completion, the Y coordinate is the same as any other.
// This is simply how the primitive TextCoord is layed out in the C# code.
float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 color = input.Color;
    float2 noiseCoords = input.TextureCoordinates;
    float4 baseNoiseColor = tex2D(uImage1, noiseCoords);
    float multiplier = baseNoiseColor.b * 0.1 + sin(uTime * 22 + input.TextureCoordinates.x * 6.283) * 0.09;
    noiseCoords.x += (baseNoiseColor.g * multiplier) - multiplier / 2 - uTime * 0.46;
    noiseCoords.x = frac(noiseCoords.x);
    
    color *= lerp(0.8, 1.2, tex2D(uImage1, noiseCoords));
    float distanceRatio = distance(input.TextureCoordinates.xy, float2(0.5, 0.5)) * 1.414;
    
    float opacity = color.a * pow((1 - (GetLerpValue(distanceRatio, 0.15, 0.965))), 0.4);
    opacity *= lerp(1, 1.9, GetLerpValue(distanceRatio, 0.67, 0.6));
    opacity *= pow(uOpacity, 0.45) * GetLerpValue(distanceRatio, 0.85, 0.6);
    float4 endColor = color * opacity;
    return endColor * lerp(0.9, 1.4, pow(endColor.r, 4));
}
technique Technique1
{
    pass BurstPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}