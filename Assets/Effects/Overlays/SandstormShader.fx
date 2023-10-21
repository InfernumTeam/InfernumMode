sampler mainImage : register(s0);
sampler distortImage : register(s1);
sampler sandImage : register(s2);

float time;
float intensity;
float lerpIntensity;

float distortAmount = 0.0025;
float distortZoom = 1.2;
float distortSpeed = 0.2;
float sandZoom;

float2 resolution;

float3 mainColor;

float4 PixelShaderFunction(float2 uv : TEXCOORD0) : COLOR0
{
    float distortNoise = tex2D(distortImage, float2(frac(uv.x * distortZoom - time * distortSpeed), uv.y * distortZoom)).r;
    
    distortNoise = smoothstep(0.11, 0.45, distortNoise);

    float2 sineNoiseUV = float2(distortNoise * 1., distortNoise * 1.) * distortAmount;
    
    float4 color = tex2D(mainImage, uv + sineNoiseUV);
    
    float2 downsizedUV = uv;
    
    downsizedUV.x -= downsizedUV.x % (1 / (resolution.x * 2));
    downsizedUV.y -= downsizedUV.y % (1 / (resolution.y * 2));
    
    float sandNoise = tex2D(sandImage, float2(frac(downsizedUV.x * sandZoom - time * 1.2), frac(downsizedUV.y * sandZoom + time * 0.055))).r;
    float sandNoise2 = tex2D(sandImage, float2(frac(downsizedUV.x * sandZoom * 1.2 - time * 0.8), frac(downsizedUV.y * sandZoom * 1.2 + time * 0.06))).r;
    float sandNoise3 = tex2D(sandImage, float2(frac(downsizedUV.x * sandZoom * 0.8 - time * 0.9), frac(downsizedUV.y * sandZoom * 0.8 + time * 0.035))).r;

    float finalSandNoise = sandNoise * 0.233 + sandNoise2 * 0.433 + sandNoise3 * 0.333;
    float sineLerpModifier = lerp(0.5, 0.9, (1. + sin(3.1415 * time * 0.4)) * 0.5);
    color = lerp(color, float4(mainColor, 1), lerpIntensity * sineLerpModifier);
    color += float4(mainColor, 1.) * 0.8 * lerp(0.2, 1.0, finalSandNoise * sineLerpModifier) * intensity;
    return color;
}

technique Technique1
{
    pass SandstormPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}