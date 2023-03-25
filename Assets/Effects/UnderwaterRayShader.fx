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
    
    uv.x += sin(uv.y * 5 + time * 0.5) * 0.005;
    float2 offset = float2(time * scrollSpeed.x, time * scrollSpeed.y);
    float2 offset2 = float2(-time * scrollSpeed.x, time * scrollSpeed.y);
    float2 tiling = uv * noiseTiling;
    float4 noisePixel = tex2D(NoiseMap, tiling * noiseScale + offset);
    float4 noisePixel2 = tex2D(NoiseMap, tiling * noiseScale + offset2);
    float combinedNoise = (noisePixel.r * 0.5 + noisePixel2.r * 0.5) + 0.05;
    
    float dissolve = tex2D(NoiseMap, tiling * noiseScale + float2(time * (scrollSpeed.x + 0.05), time * (scrollSpeed.y + 0.05)));
    float dissolve2 = tex2D(NoiseMap, tiling * noiseScale + float2(-time * (scrollSpeed.x + 0.05), time * (scrollSpeed.y + 0.05)));

    combinedNoise *= pow(dissolve.r * 0.5 + dissolve2.r * 0.5, dissolvePower);
    
    float opacity = 2 - distanceFromCenter;
        
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