sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
sampler uImage2 : register(s2);
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
float2 uImageSize2;
matrix uWorldViewProjection;
float4 uShaderSpecificData;

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

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TextureCoordinates.xy;
    
    float brightnessInterpolant = pow(1 - coords.y, 2);
    float4 color = input.Color;
    color.gb += color.r * brightnessInterpolant * float2(1, 0.75);
    
    float noise = tex2D(uImage1, coords + float2(0, globalTime * -2)) - tex2D(uImage2, float2(coords.y, coords.x) + float2(globalTime * -3, 0)).r * coords.y;
    float edgeTaper = pow(sin(coords.x * 3.141), 2.8) * pow(sin(coords.y * 3.141), 0.8);
    
    float4 finalColor = color;
    finalColor.rgb *= noise;
    
    return color * noise * edgeTaper * (1 + brightnessInterpolant * 10);
}

technique Technique1
{
    pass AutoloadPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}