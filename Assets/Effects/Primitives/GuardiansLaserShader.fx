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
matrix uWorldViewProjection;
float4 uShaderSpecificData;

bool flipY;
float stretchAmount;
bool pillarVarient;
float scrollSpeed;

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

VertexShaderOutput VertexShaderFunction(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;
    float4 pos = mul(input.Position, uWorldViewProjection);
    output.Position = pos;

    output.Color = input.Color;
    output.TextureCoordinates = input.TextureCoordinates;

    return output;
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 color = input.Color;
    float2 coords = input.TextureCoordinates;

    float4 fadeMapColor = tex2D(uImage1, float2(frac(coords.x * stretchAmount - uTime * scrollSpeed), coords.y));
    float bloomFadeout = pow(sin(coords.y * 3.141), 8);
    float opacity = (fadeMapColor.r + 0.75) * bloomFadeout;
    float bloomFadeout2 = pow(sin(coords.y * 3.141), 18);
    
    float4 fadeMapColor2 = tex2D(uImage2, float2(frac(coords.x * (stretchAmount * 1.66666666) - uTime * scrollSpeed * 1.1), coords.y));
    float opacity2 = saturate(fadeMapColor2.r - 0.5) * bloomFadeout2;
    float4 colorCorrected = float4(uColor, fadeMapColor2.r * bloomFadeout * 3);

    if (coords.x > 0.86)
    {
        opacity *= pow(1 - (coords.x - 0.86) / 0.14, 7);
        opacity2 *= pow(1 - (coords.x - 0.86) / 0.14, 7);
    }

    return color * opacity + colorCorrected * 5 * opacity2;
}

technique Technique1
{
    pass TrailPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}