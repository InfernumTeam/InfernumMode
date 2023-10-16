sampler screen : register(s0);

float barSize;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    if (uv.y < barSize || uv.y > 1 - barSize)
        return float4(0, 0, 0, 0);
    
    return tex2D(screen, uv) * sampleColor;
}

technique Technique1
{
    pass BarPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}