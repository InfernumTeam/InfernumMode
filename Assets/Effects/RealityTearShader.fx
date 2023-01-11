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
float uCoordinateZoom;
float uTimeFactor;
bool useOutline;

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

float InverseLerp(float x, float min, float max)
{
    return saturate((x - min) / (max - min));
}

float4 StarColorFunction(float2 coords)
{
    float timeFactor = uTimeFactor + 1;
    float4 c1 = tex2D(uImage1, coords + float2(sin(uTime * timeFactor * 0.12) * 0.5, uTime * timeFactor * 0.03));
    float4 c2 = tex2D(uImage1, coords + float2(uTime * timeFactor * -0.019, sin(uTime * timeFactor * -0.09 + 0.754) * 0.6));
    return pow(c1 + c2, 2.6);
}

float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float2 coords = input.TextureCoordinates;
    float4 color = StarColorFunction(coords * float2(1, 0.1) * (uCoordinateZoom + 1)) * input.Color;
    
    float bloomPulse = sin(uTime * 7.1 - coords.x * 12.55) * 0.5 + 0.5;
    float opacity = pow(sin(3.141 * coords.y), 4 - bloomPulse * 2);
    float fadeToWhite = pow(1 - sin(3.141 * coords.y), 1.4) * 4;
    if (fadeToWhite >= 1)
        fadeToWhite = 1;
    
    if (useOutline)
        color *= (pow(1 - sin(3.141 * coords.y), 9) * 35000) + 1;
    
    return color * opacity * (uSaturation + 1) * 1.6;
}

technique Technique1
{
    pass TrailPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
