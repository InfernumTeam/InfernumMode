sampler crystal : register(s0);
sampler noise : register(s1);

float threshold;
float2 resolution;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(crystal, uv) * sampleColor;
    if (!any(color))
        return color;
    
    uv.x -= uv.x % (1 / (resolution.x * 2));
    uv.y -= uv.y % (1 / (resolution.y * 2));
    
    float4 noiseColor = tex2D(noise, uv * 0.7 + float2(0.27, 0.32));
    
    // If the noise is over the erasure threshold, completely erase this pixel.
    return color * step(noiseColor.r, threshold);
}

technique Technique1
{
    pass CrackPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}