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

const float PI = 3.14159265359;
const float TAU = 6.28318530718;

float zoom = 1;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float3 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float3 TextureCoordinates : TEXCOORD0;
};

VertexShaderOutput VertexShaderFunction(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    float4 pos = mul(input.Position, uWorldViewProjection);
    output.Position = pos;
    
    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;
    output.TextureCoordinates.y = (output.TextureCoordinates.y - 0.5) / input.TextureCoordinates.z + 0.5;
    return output;
}

// The X coordinate is the trail completion, the Y coordinate is the same as any other.
// This is simply how the primitive TextCoord is layed out in the C# code.
float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TextureCoordinates;
    
    float time = uTime * 0.5 + 23;
    float2 position = fmod(coords * TAU * zoom, TAU) - 500;
    
    float2 position2 = position;
    float color = 1;
    float intensity = 0.005;
    
    for (int i = 0; i < 2; i++)
    {
        float time2 = time * (1 - (PI / float(i + 1)));
        position2 = position + float2(sin((time2 - position2.x) + 1.54) + sin(time2 + position2.y), sin(time2 - position2.y) + sin((time2 + position2.x) + 1.54));
        color += 1 / length(float2(position.x / (sin(position2.x + time2) / intensity), position.y / (sin((position2.y + time2) + 1.54) / intensity)));
    }
    
    color /= 2.0;
    color = 1.24 - pow(abs(color), 1.45);
    color = pow(abs(color), 6);
    float3 finalColor = float3(color, color, color);
    finalColor = saturate(finalColor + float3(0, 0.55, 0.74));
    
    float opacity = uOpacity;
    opacity *= pow(sin(coords.x * 3.141), 3);
    opacity *= pow(sin(coords.y * 3.141), 1.2);
    return float4(finalColor, 1) * opacity;
}

technique Technique1
{
    pass WaterPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}