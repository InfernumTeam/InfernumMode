sampler mainImage : register(s0);
sampler noiseImage : register(s1);
sampler sheepImage : register(s2);

float time;
float warpSpeed;
float fadeAmount;

float2 sheepPosition;
float2 screenSize;
float2 screenPosition;
float2 sheepSize;

float2 DirectionTo(float2 start, float2 end)
{
    return normalize(end - start);
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{   
    // Sample noise, use it as a rotation value and turn that into an offset to offset the image by.
    float warpAngle = tex2D(noiseImage, uv * 9.3 + float2(time * 0.2, 0)).r * 16;
    float2 warpNoiseOffset = float2(sin(warpAngle + 1.57), sin(warpAngle));
    
    // Dampen vertical movement.
    warpNoiseOffset.y *= 0.5;
    
    // Offset the image by the offset, and fade it out.
    float4 color = tex2D(mainImage, uv - warpNoiseOffset * warpSpeed);
    color.rgb *= fadeAmount;
    color.a *= fadeAmount;
    
    return color * sampleColor;
    
}

technique Technique1
{
    pass AfterimagePass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}