sampler mainImage : register(s0);
sampler overlayImage : register(s1);
sampler lightingImage : register(s2);
float threshold;
float2 rtSize;
float2 screenPosition;
float2 singleFrameScreenOffset;
float2 layerOffset;
float4 mainColor;
float4 edgeColor;
bool useOverlayImage;

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
    
    // Read the brightness from the alpha channel.
    float metaballBrightness = pixelOnRT.a;

    if (metaballBrightness > 0.)
    {
        if (metaballBrightness > threshold)
            return (useOverlayImage ? tex2D(overlayImage, uv + layerOffset + singleFrameScreenOffset) : mainColor) * metaballBrightness;

        float left = tex2D(mainImage, convertFromScreenCoords(convertToScreenCoords(uv) + float2(-2, 0))).a;
        if (left <= threshold)
            return edgeColor * metaballBrightness;
        
        float right = tex2D(mainImage, convertFromScreenCoords(convertToScreenCoords(uv) + float2(2, 0))).a;
        if (right <= threshold)
            return edgeColor * metaballBrightness;
        
        float top = tex2D(mainImage, convertFromScreenCoords(convertToScreenCoords(uv) + float2(0, -2))).a;
        if (top <= threshold)
            return edgeColor * metaballBrightness;
        
        float bottom = tex2D(mainImage, convertFromScreenCoords(convertToScreenCoords(uv) + float2(0, 2))).a;
        if (bottom <= threshold)
            return edgeColor * metaballBrightness;
        
        return float4(0, 0, 0, 0);
    }
    return tex2D(mainImage, uv);
}

technique Technique1
{
    pass EdgePass
    {
        PixelShader = compile ps_2_0 Edge();
    }
}