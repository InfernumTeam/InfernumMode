sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
float3 uColor;
float3 uSecondaryColor;
float uOpacity;
float uSaturation;
float uRotation;
float4 uSourceRect;
float2 uWorldPosition;
float uDirection;
float3 uLightSource;
float2 uImageSize0;
float2 uImageSize1;
float4 uShaderSpecificData;
float zoom;
float scrollSpeedFactor;
float brightness;
float uTime;
float2 uImage0Size;
float3 frontStarColor;
float3 backStarColor;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float2 TextureCoordinates : TEXCOORD0;
    float4 Color : COLOR0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float4 result = 0;
    float volumetricLayerFade = 1.0;
    for (int i = 0; i < 12; i++)
    {
        float time = uTime / volumetricLayerFade;
        float2 p = coords * zoom;
        p.y += 1.5;

        // Perform scrolling behaviors. Each layer should scroll a bit slower than the previous one, to give an illusion of 3D.
        p += float2(time * scrollSpeedFactor, time * scrollSpeedFactor);
        p /= volumetricLayerFade;

        float totalChange = tex2D(uImage1, p);
        float4 layerColor = float4(lerp(frontStarColor, backStarColor, i / 12.0), 1.0);
        result += layerColor * totalChange * volumetricLayerFade;

        // Make the next layer exponentially weaker in intensity.
        volumetricLayerFade *= 0.9;
    }

    // Account for the accumulated scale from the fractal noise.
    result.rgb = pow(result.rgb * 0.010714, 1.6) * brightness;
    return result * sampleColor;
}

technique Technique1
{
    pass CosmicPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}
