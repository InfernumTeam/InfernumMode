sampler sprite : register(s0);
sampler noiseImage : register(s1);
sampler innerNoiseImage : register(s2);

float threshold;
float noiseZoom;
float noiseSpeed;
float noiseFactor;
float brightnessFactor;
float opacity;
float thickness;
float time;
float burnRatio;
float innerNoiseFactor;
float distanceMultiplier;

float2 resolution;
float2 focalPointUV;

float3 burnColor;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float4 col = tex2D(sprite, uv);
    
    if (!any(col))
        return col;
    
    // Pixelate
    uv.x -= uv.x % (1 / (resolution.x * 2));
    uv.y -= uv.y % (1 / (resolution.y * 2));
    
    // Offset the uv by turning a noise value into an angle and direction offset.
    float noise = tex2D(noiseImage, uv).r;  
    float noiseAngle = noise * noiseZoom + time * noiseSpeed;
    float2 noiseDirection = float2(sin(noiseAngle + 1.54), sin(noiseAngle));       
    float2 noisedUV = uv + noiseDirection * 0.012 * noiseFactor;
        
    // Get distance values.
    float distanceToCenter = length(noisedUV - focalPointUV) * 1.25 * distanceMultiplier;
    float distanceToEdge = abs(distanceToCenter - burnRatio);

    // Calcuate brightness based on distances.
    float brightness = pow(thickness / distanceToEdge, brightnessFactor);
    
    // Inside of the burn should be slightly brighter based on distance, outside the burn shouldnt be visible.
    float4 originalCol = col;
    if (distanceToCenter > burnRatio)
       col *= 0;
    else
    {
        if (innerNoiseFactor > 0.)
        {
            float innerTime = time * 0.184;
            float noise1 = tex2D(noiseImage, uv * 4 + float2(0, innerTime * 0.3)).r;
            float noise2 = tex2D(noiseImage, uv * 3 + float2(innerTime * 0.07 - 0.51, innerTime * -0.2)).r;
            float noise3 = tex2D(noiseImage, uv * 2 + float2(innerTime * -0.039 + 0.83, innerTime * -0.09)).r;
            brightness += (noise1 + noise2 + noise3) * innerNoiseFactor * col.a;
        }
        else
            brightness += pow(distanceToCenter, 1.5) * col.a;
    }
    
    return col + float4(brightness * burnColor, col.a);
}

technique Technique1
{
    pass BurnBass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}