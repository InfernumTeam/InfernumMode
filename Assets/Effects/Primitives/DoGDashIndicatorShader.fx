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
    // Ignore the primtive color and just use this.
    float4 color = float4(uColor, 1);
    float2 coords = input.TextureCoordinates;
    
    float4 fadeMapColor = tex2D(uImage1, float2(frac(coords.x * 0.8 - uTime * 0.5), coords.y));
    float bloomFadeout = pow(sin(coords.y * 3.141), 8);

    // Calcuate the grayscale version of the pixel and use it as the opacity.
    float opacity = (0.3 + fadeMapColor.r) * uOpacity * bloomFadeout;
    
    float4 finalColor = color;
    if (uOpacity > 1)
        finalColor = lerp(color, float4(1, 1, 1, opacity), opacity - 0.3);
    
    // Fade out at the ends of the streak.
    if (coords.x < 0.1)
        opacity *= pow(coords.x / 0.1, 3);
    if (coords.x > 0.9)
        opacity *= pow(1 - (coords.x - 0.9) / 0.1, 3);
    
    return finalColor * saturate(opacity);
}

technique Technique1
{
    pass TrailPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
