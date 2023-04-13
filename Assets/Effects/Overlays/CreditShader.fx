sampler mainImage : register(s0);

texture noiseTexture;
sampler2D NoiseTexture = sampler_state
{
    Texture = <noiseTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};

float3 lerpColor;
float lerpColorAmount;
float noiseScale;
float noiseIntensity;

float RectangularDistance(float2 center, float2 uv)
{
    float n = 20;
    float2 absoluteDistance = abs(center - uv);
    absoluteDistance.x *= 0.963;
    return pow(pow(absoluteDistance.x, n) + pow(absoluteDistance.y, n), 1.0 / n);
}

float4 PixelShaderFunction(float2 uv : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(mainImage, uv);
    float distanceFromCenter = RectangularDistance(float2(0.5, 0.5), uv);
    
    // Make the image shift towards a certain color.
    color = lerp(color, float4(lerpColor, 1), lerpColorAmount);
    
    // Create random "film grain" based noise on the image.
    float noise = tex2D(NoiseTexture, uv * noiseScale).r * noiseIntensity;
    color -= noise;
    
    float opacity = 1;
    if (distanceFromCenter > 0.9)
        opacity *= pow(1 - ((distanceFromCenter) - 0.9) / 0.1, 3);
    
    return color * opacity;
}
technique Technique1
{
    pass CreditPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}