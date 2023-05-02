sampler mainTexture : register(s0);

float pixelationAmount;
float lifeRatio;
float3 mainColor;
float3 bloomColor;



float4 PixelShaderFunction(float2 uv : TEXCOORD0) : COLOR0
{
    float2 UV = uv;
    uv.x += (1 - uv.y) * 0.1;
    uv.x -= uv.x % (1 / (pixelationAmount * 2));
    float distanceToCenter = distance(float2(uv.x, 0.5), float2(0.5, 0.5)) * 2 * pow(lifeRatio, 1);
    float3 color = lerp(bloomColor, mainColor, distanceToCenter - 0.25);
    
    return float4(color, 1);
}

technique Technique1
{
    pass FilterPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}