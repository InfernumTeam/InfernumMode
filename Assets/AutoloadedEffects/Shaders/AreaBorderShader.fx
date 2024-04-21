sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
sampler uImage2 : register(s4);
float3 uColor;
float3 uSecondaryColor;
float uOpacity;
float uSaturation;
float uRotation;
float globalTime;
float4 uSourceRect;
float2 uWorldPosition;
float uDirection;
float3 uLightSource;
float2 uImageSize0;
float2 uImageSize1;
float2 uImageSize4;
matrix uWorldViewProjection;
float4 uShaderSpecificData;

float2 noiseSpeed;
float timeFactor;
bool flipY;

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

float4 StarColorFunction(float2 coords)
{
    float actualTimeFactor = timeFactor + 1;
    float4 c1 = tex2D(uImage1, coords + float2(sin(globalTime * actualTimeFactor * 0.12) * 0.5, globalTime * actualTimeFactor * 0.03));
    float4 c2 = tex2D(uImage1, coords + float2(globalTime * actualTimeFactor * -0.019, sin(globalTime * actualTimeFactor * -0.09 + 0.754) * 0.6));
    return pow(c1 + c2, 2.6);
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TextureCoordinates.xy;
    float2 coordsToUse = coords;
    if (flipY)
        coordsToUse.y = 1 - coords.y;
    float baseColor = tex2D(uImage0, coordsToUse).r;
    float4 color = (StarColorFunction(coords * float2(1, 0.1) * 4) * float4(uColor, 1)) * 0.35 + input.Color * 0.65;
    return color * baseColor * 1.5 * uOpacity;
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_3_0 VertexShaderFunction();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
