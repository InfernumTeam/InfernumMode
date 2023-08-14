sampler mainImage : register(s0);
sampler lightingImage : register(s1);
float threshold;
float2 rtSize;
float3 mainColor;
float3 edgeColor;

float2 convertToScreenCoords(float2 coords)
{
    return coords * rtSize;
}

float2 convertFromScreenCoords(float2 coords)
{
    return coords / rtSize;
}


float4 Edge(float4 sampleColor : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float4 pixelOnRT = tex2D(mainImage, uv);
    float4 lighting = tex2D(lightingImage, uv);
    
    float lightingModifier = (lighting.r + lighting.g + lighting.b) / 3;
    
    if (lightingModifier == 0)
        lightingModifier = 1;
    
    if (pixelOnRT.a <= threshold && pixelOnRT.a > 0.)
    {
        float left = tex2D(mainImage, convertFromScreenCoords(convertToScreenCoords(uv) + float2(-2, 0))).a;
        if (left <= 0)
            return float4(edgeColor * lightingModifier, 1);
        
        float right = tex2D(mainImage, convertFromScreenCoords(convertToScreenCoords(uv) + float2(2, 0))).a;
        if (right <= 0)
            return float4(edgeColor * lightingModifier, 1);
        
        float top = tex2D(mainImage, convertFromScreenCoords(convertToScreenCoords(uv) + float2(0, -2))).a;
        if (top <= 0)
            return float4(edgeColor * lightingModifier, 1);
        
        float bottom = tex2D(mainImage, convertFromScreenCoords(convertToScreenCoords(uv) + float2(0, 2))).a;
        if (bottom <= 0)
            return float4(edgeColor * lightingModifier, 1);
    }
    
    if (pixelOnRT.a > 0.)
        return float4(mainColor * lightingModifier, 1.);
    
    return tex2D(mainImage, uv);
}

technique Technique1
{
    pass EdgePass
    {
        PixelShader = compile ps_2_0 Edge();
    }
}