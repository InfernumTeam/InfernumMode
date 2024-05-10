sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
sampler uImage2 : register(s4);
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
float2 uImageSize4;
matrix uWorldViewProjection;
float4 uShaderSpecificData;
float uCoordinateZoom;
float uTimeFactor;
bool useOutline;

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

float InverseLerp(float x, float min, float max)
{
    return saturate((x - min) / (max - min));
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TextureCoordinates;
    float4 codeOverlay = tex2D(uImage1, float2(coords.y, 1 - coords.x + uTime * 0.3)) * InverseLerp(coords.x, 0.85, 0.67) * 0.2;
    float4 noiseOverlay1 = tex2D(uImage2, coords * 2 + float2(-0.2, 0.16) * uTime).r * input.Color;
    float4 noiseOverlay2 = tex2D(uImage2, coords * 2 + float2(0.18, 0.28) * uTime).r * input.Color;
    float4 result = noiseOverlay1 + noiseOverlay2 + codeOverlay + input.Color;
    result.a *= 0.67;

    return result;
}

technique Technique1
{
    pass TrailPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
