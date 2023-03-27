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
float lavaHeightInterpolant;

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
float4 PixelShaderFunction(VertexShaderOutput input) : COLOR0
{
    float4 color = input.Color;
    float2 coords = input.TextureCoordinates * float2(1, lavaHeightInterpolant);
    
    // Read the noise maps and combine them to get a general texture.
    float time = uTime * 0.2;
    float noise1 = tex2D(uImage1, coords * 6 + float2(0, time)).r;
    float noise2 = tex2D(uImage1, coords * 12 + float2(time * -0.56, time * 0.8)).r;
    float textureFactor = noise1 * 0.45 + noise2 * 0.55;
    
    float opacity = lerp(0.4, 0.6, textureFactor) * color.a;
    
    // Make the opacity taper off at the very ends of the primitive.
    // This is important for giving a tiny bit of "depth" to the lava before it basically becomes pure white and obfuscates texture.
    opacity *= pow(sin(coords.x * 3.141), 0.4);
    opacity *= pow(sin(coords.y * 3.141), 0.1);
    opacity *= textureFactor + pow(coords.y, 0.2) * 2;
    
    // Approach full white a little bit based on the opacity intensity.
    color.rgb = lerp(color.rgb, 1, saturate(opacity) * 0.25);
    return color * saturate(opacity);
}

technique Technique1
{
    pass TrailPass
    {
        VertexShader = compile vs_2_0 VertexShaderFunction();
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}
