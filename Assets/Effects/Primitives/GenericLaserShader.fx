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

bool strongerFade;

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
    // Get the pixel from the provided streak/fade map.
    float4 fadeMapColor = tex2D(uImage1, float2(frac(coords.x * 5 - uTime * 2.5), coords.y));
    
    // Define the shape fadeout.
    float bloomFadeout = pow(sin(coords.y * 3.141), 6);

    // Calcuate the grayscale version of the pixel and use it as the opacity.
    float opacity = (fadeMapColor.r + 0.5) * bloomFadeout;
    // Lerp between the base color, and the provided one.
    float4 colorCorrected = lerp(color, float4(uColor, opacity), opacity);
    
    //// Fade out at the top and bottom of the streak.
    //if (coords.y < 0.05)
    //    opacity *= pow(coords.y / 0.05, 6);
    //if (coords.y > 0.95)
    //    opacity *= pow(1 - (coords.y - 0.95) / 0.05, 6);
    
    // Also fade out at the beginning and end of the streak.
    if (strongerFade)
    {
        if (coords.x < 0.1)
            opacity *= pow(coords.x / 0.1, 6);
        if (coords.x > 0.87)
            opacity *= pow(1 - (coords.x - 0.87) / 0.13, 12);
    }
    else
    {
        if (coords.x < 0.018)
            opacity *= pow(coords.x / 0.018, 6);
        if (coords.x > 0.95)
            opacity *= pow(1 - (coords.x - 0.95) / 0.05, 6);
    }
    return colorCorrected * opacity * 2.5;
}

technique Technique1
{
    pass TrailPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}   