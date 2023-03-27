texture noiseMap;
sampler2D NoiseMap = sampler_state
{
    texture = <noiseMap>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};


float3 mainColor;

float2 noiseTiling;
float2 scrollSpeed;

float noiseScale;
float time;
float dissolvePower;
float sceneOpacity;

float4 PixelShaderFunction(float2 uv : TEXCOORD0) : COLOR0
{
    // Crop in a circle
    float distanceFromCenter = length(uv - float2(0.5, 0.5)) * 2;
    
    // Distort the x coords based o a sine wave.
    uv.x += sin(uv.y * 5 + time * 0.5) * 0.005;
    // Create two offsets going in opposite directions for noise sampling.
    float2 offset = float2(time * scrollSpeed.x, time * scrollSpeed.y);
    float2 offset2 = float2(-time * scrollSpeed.x, time * scrollSpeed.y);
    // Modify the coords by a provided tiling amount.
    float2 tiling = uv * noiseTiling;
    // Get both noise values.
    float4 noisePixel = tex2D(NoiseMap, tiling * noiseScale + offset);
    float4 noisePixel2 = tex2D(NoiseMap, tiling * noiseScale + offset2);
    // Add them together at 50% strength, to basically create the illusion of layered noise.
    // Changing the ratio will make the fore/background more prominant, as long as the ratio equals 1.
    float combinedNoise = (noisePixel.r * 0.5 + noisePixel2.r * 0.5) + 0.05;
    
    // Get another two noise value with a modified offset.
    float dissolve = tex2D(NoiseMap, tiling * noiseScale + float2(time * (scrollSpeed.x + 0.05), time * (scrollSpeed.y + 0.05)));
    float dissolve2 = tex2D(NoiseMap, tiling * noiseScale + float2(-time * (scrollSpeed.x + 0.05), time * (scrollSpeed.y + 0.05)));

    // Combine them, and multiply them to the original noies after raising them to a provided power.
    combinedNoise *= pow(dissolve.r * 0.5 + dissolve2.r * 0.5, dissolvePower);
    
    // Get the main opacity.
    float opacity = 2 - distanceFromCenter;
    
    // Make it fade out at the edges, doesn't really matter now this is screenwide.
    if (distanceFromCenter > 0.7)
        opacity *= (1 - (distanceFromCenter - 0.7) / 0.3);
    
    return float4(mainColor, 1) * combinedNoise * opacity * 0.32 * sceneOpacity;
}

technique Technique1
{
    pass RayPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}