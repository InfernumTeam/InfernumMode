sampler screen : register(s0);
sampler noiseSample : register(s1);

float time;
float2 screenPosition;
float2 screenSize;
float3 colors[3] =
{
    float3(36. / 255., 94. / 255., 187. / 255.),
    float3(28. / 255., 175. / 255., 189. / 255.),
    float3(19. / 255., 255. / 255., 203. / 255.)
};

float3 multicolorLerp(float increment)
{
    increment = increment % 1.;
    int currentColorIndex = int(increment * 3);
    float3 currentColor = colors[currentColorIndex];
    float3 nextColor = colors[int(float(currentColorIndex + 1) % 3.)];
    return lerp(currentColor, nextColor, increment * 3. % 1.);
}

float4 PixelShaderFunction(float2 uv : TEXCOORD0) : COLOR0
{
    // Pixelate.
    uv.x -= uv.x % (1 / screenSize.x);
    uv.y -= uv.y % (1 / screenSize.y);
    float noiseScale = 13.;
    float noiseSpeed = 0.1;
    float3 color = colors[0];
    float gain = 1.;
    
    // FBM.
    for (int i = 1; i < 3; i++)
    {
        float noise = tex2D(noiseSample, uv * noiseScale + frac(float2(time * noiseSpeed, -time * noiseSpeed))).r * gain;
        color += colors[i] * noise;
        noiseScale *= 0.75;
        noiseSpeed *= 0.6;
        gain *= 0.75;
    }
    
    return float4(color, 1);
}

technique Technique1
{
    pass WaterPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}