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
    
    // Get the pixel from the image wanted to draw.
    float4 fadeMapColor = tex2D(uImage1, float2(frac(coords.x * 5 - uTime * 2.6), coords.y));
   // float4 fadeMapColor2 = tex2D(uImage2, float2(frac(coords.x * 5 - uTime * 2.6), coords.y));

    //float bigOpacity = max(fadeMapColor.r, fadeMapColor2.r);
    
    float bloomFadeout = pow(sin(coords.y * 3.141), 2);
    // Calcuate the grayscale version of the pixel and use it as the opacity.
    float opacity = (fadeMapColor.r) * bloomFadeout;
    float4 colorCorrected = lerp(color, float4(uColor, 1), opacity * 0.8);
        
    // Fade out at the top and bottom of the streak.
    if (coords.x < 0.05)
        opacity *= pow(coords.x / 0.05, 6);
    if (coords.x > 0.95)
        opacity *= pow(1 - (coords.x - 0.95) / 0.05, 6);
    
    return colorCorrected * opacity;
}

technique Technique1
{
    pass TrailPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}