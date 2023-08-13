sampler uImage0 : register(s0);
sampler noise : register(s1);

float speed;
float time;
float intensity;
float2 direction;
float3 color;
float3 brightColor;
float2 resolution;

float Remap(float value, float2 inMinMax, float2 outMinMax)
{
    return outMinMax.x + (value - inMinMax.x) * (outMinMax.y - outMinMax.x) / (inMinMax.y - inMinMax.x);
}

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    float4 weaverPixel = tex2D(uImage0, coords);
    
    if (!any(weaverPixel))
        return float4(0, 0, 0, 0);
    
    coords.x -= coords.x % (1 / (resolution.x * 2));
    coords.y -= coords.y % (1 / (resolution.y * 2));
    
    float2 direction1 = float2(1, 1);
    float2 direction2 = float2(0.5, 0.5);
    float2 direction3 = float2(-0.7, 0.6);
    float noisePixel = tex2D(noise, coords * 12 + time * direction1 * speed).r;
    float noisePixel2 = tex2D(noise, coords * 12 + time * direction2 * speed).r;
    float noisePixel3 = tex2D(noise, coords * 12 + time * direction3 * speed).r;

    float finalNoise = noisePixel * 0.33 + noisePixel2 * 0.33 + noisePixel3 * 0.333;
    
    finalNoise = Remap(finalNoise, float2(0, 1), float2(-8 * intensity, 8 * intensity));
    
    if (finalNoise < 0)
        finalNoise = 0;
    
    float3 finalColor = lerp(color, brightColor, pow(finalNoise / 8., 0.8));
    float3 electricColor = finalColor * finalNoise;
    return weaverPixel + float4(electricColor, 1);
}
technique Technique1
{
    pass LightningOverlayPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}