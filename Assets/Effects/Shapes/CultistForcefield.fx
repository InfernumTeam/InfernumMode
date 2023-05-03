sampler uImage0 : register(s0);

texture sampleTexture2;
sampler2D NoiseMap = sampler_state
{
    texture = <sampleTexture2>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};

float3 mainColor;
float2 resolution;
float noiseScale;
float2 noiseDirection;
float time;
float fresnelPower;
float scrollSpeed;
float fill;
float opacity;
bool useOuterGlow;

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    // Pixelate.
    coords.x -= coords.x % (1 / (resolution.x * 2));
    coords.y -= coords.y % (1 / (resolution.y * 2));
    
    // Crop in a circle
    float distanceFromCenter = length(coords - float2(0.5, 0.5)) * 2;
    if (distanceFromCenter > 1)
        return float4(0, 0, 0, 0);
    
    float2 offset = float2(time * scrollSpeed, time * scrollSpeed) * noiseDirection;
    float4 noise = tex2D(NoiseMap, float2(coords.x * noiseScale + offset.x, coords.y * noiseScale + offset.y));
    float4 noise2 = tex2D(NoiseMap, coords * noiseScale - offset);
    float4 finalNoise = noise * 0.66 + noise2 * 0.33;
    float noiseValue = finalNoise.r;
    float fresnel = pow(distanceFromCenter, fresnelPower) + pow(distanceFromCenter, fresnelPower * 0.5) * 0.6;
    if (distanceFromCenter > 0.97 && useOuterGlow)
        return float4(mainColor, 1) * pow(1 - (distanceFromCenter / 0.03), 10);
    return float4(mainColor, 1) * (((noiseValue * 1.6 + 0.6)) * fresnel + fill) * opacity;

}

technique Technique1
{
    pass ShieldPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}