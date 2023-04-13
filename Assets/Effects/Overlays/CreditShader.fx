sampler mainImage : register(s0);

float3 lerpColor;
float lerpColorAmount;
float noiseScale;
float noiseIntensity;
float overallOpacity;

float noise2(float2 uv)
{
    return frac(sin(dot(uv, float2(12.9898, 78.233) * 2.0)) * 43758.5453);
}

float RectangularDistance(float2 center, float2 uv)
{
    float n = 23;
    float2 absoluteDistance = abs(center - uv) * 2;
    //absoluteDistance.x *= 0.963;
    return pow(pow(absoluteDistance.x, n) + pow(absoluteDistance.y, n), 1.0 / n);
}

float4 PixelShaderFunction(float2 uv : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(mainImage, uv);
    float distanceFromCenter = RectangularDistance(float2(0.5, 0.5), uv);
    
    // Make the image shift towards a certain color.
    float grayscale = dot(color.rgb, float3(0.299, 0.587, 0.114));
    
    color = float4(grayscale, grayscale, grayscale, 1) * lerp(color, float4(lerpColor, 1), lerpColorAmount);
    
    // Create random "film grain" based noise on the image.
    float noise = noise2(uv * noiseScale) * noiseIntensity;
    color -= noise;
    
    float opacity = 1;
    if (distanceFromCenter > 0.9)
        opacity *= pow(1 - ((distanceFromCenter) - 0.9) / 0.1, 3);
    
    return color * opacity * overallOpacity;
}
technique Technique1
{
    pass CreditPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}