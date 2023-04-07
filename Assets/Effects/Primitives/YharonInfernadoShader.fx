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

float edgeTaperPower;
float scrollSpeed;
float additiveNoiseStrength;
float subtractiveNoiseStrength;

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

// The X coordinate is the trail completion, the Y coordinate is the same as any other.
// This is simply how the primitive TextCoord is layed out in the C# code.
// Inputted images go into uImage1 sampler, in case you have a noise map or something similar.
float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 color = input.Color;
    float2 coords = input.TextureCoordinates;
    
    // Calculate noise values. These are used to give texture to the tornado, with the additive noise providing bright whites and the
    // subtractive noise providing dark reds and purples, result in a variety of hues.
    float additiveNoise = tex2D(uImage1, coords * float2(2, 0.8) + float2(uTime * -0.3, uTime * -1.7) * scrollSpeed);
    float subtractiveNoise = pow(tex2D(uImage2, coords * float2(5, 1.23) + float2(uTime * -1.9, uTime * -2.9) * scrollSpeed), 0.4) * 1.6;
    
    // Make the colors taper at the edges of the tornado.
    float edgeTaper = sin(coords.y * 3.141) * pow(sin(coords.x * 3.141), 0.75);
    float4 finalColor = color + additiveNoise * color.a * additiveNoiseStrength - float4(subtractiveNoise, subtractiveNoise, subtractiveNoise, subtractiveNoise * 1.15) * subtractiveNoiseStrength;
    
    // Add a little bit of purple to the final color based on the subtractive noise.
    finalColor.g -= subtractiveNoise * subtractiveNoiseStrength * 0.15;
    
    return saturate(finalColor) * pow(edgeTaper, edgeTaperPower);
}

technique Technique1
{
    pass TrailPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}