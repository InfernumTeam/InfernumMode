sampler mainImage : register(s0);
sampler ringNoiseImage : register(s1);

texture innerNoiseTexture;
sampler2D innerNoiseImage = sampler_state
{
    texture = <innerNoiseTexture>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};

float time;
float noiseZoom;
float noiseSpeed;
float noiseFactor;
float size;
float brightnessFactor;
float opacity;
float thickness;
float innerNoiseFactor;

float2 screenSize;
float2 explosionCenterUV;
float2 resolution;

float3 mainColor;

float4 MainImage(float2 uv : TEXCOORD0) : COLOR0
{
    // Pixelate.
    uv.x -= uv.x % (1. / (resolution.x * 2.));
    uv.y -= uv.y % (1. / (resolution.y * 2.));
    
    // Use a noise value, turn it into an angle and then turn that into a direction to get an offset based on noise. Very useful and clever trick.
    float noise = tex2D(ringNoiseImage, uv).r;
    float noiseAngle = noise * noiseZoom + time * noiseSpeed;
    float2 noiseDirection = float2(sin(noiseAngle + 1.54), sin(noiseAngle));
    
    // Calcuate distance values.
    float distanceToCenter = distance(uv + noiseDirection * 0.0022 * noiseFactor, explosionCenterUV);
    float distanceToEdge = abs(distanceToCenter - size);
    
    // Get the brightness of the current pixel.
    float brightness = pow(thickness / distanceToEdge, brightnessFactor);
    
    // Add an inner noise effect to the inside of the pulse.
    if (distanceToCenter < size)
    {
        float innerNoise1 = tex2D(innerNoiseImage, uv * 4. + float2(0., time * 0.3)).r;
        float innerNoise2 = tex2D(innerNoiseImage, uv * 3. + float2(time * 0.07 - 0.51, time * -0.2)).r;
        float innerNoise3 = tex2D(innerNoiseImage, uv * 2. + float2(time * -0.039 + 0.83, time * -0.09)).r;
        float innerNoise = (innerNoise1 + innerNoise2 + innerNoise3) * pow(distanceToCenter / size, innerNoiseFactor) * 0.6;
        
        brightness += innerNoise;
    }
    
    return float4(mainColor, 1.) * brightness * opacity;
}

technique Technique1
{
    pass PulsePass
    {
        PixelShader = compile ps_3_0 MainImage();
    }
}