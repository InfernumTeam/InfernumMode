sampler uImage0 : register(s0);
float dDistanceModifier;


float4 Displacement(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    float distanceTo = distance(color.rg, coords.xy);
    float mapped = saturate(distanceTo * dDistanceModifier);
    return float4(mapped, mapped, mapped, 1);
}

technique Technique1
{
    pass DisplacementPass
    {
        PixelShader = compile ps_2_0 Displacement();
    }
}