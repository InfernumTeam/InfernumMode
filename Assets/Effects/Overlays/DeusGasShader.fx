sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
sampler uImage2 : register(s2);
sampler uImage3 : register(s3);

float generalOpacity;
float time;
float scale;
float brightness;
float focalPointOpacity;
float2 focalPoint;
float3 supernovaColor1;
float3 supernovaColor2;
float3 bloomColor;

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (to - from));
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Calculate noise values for the gas.
    float2 noiseCoords1 = (coords - 0.5) * scale * 0.2 + 0.5 + float2(time * 0.14, 0);
    float2 noiseCoords2 = (coords - 0.5) * scale * 0.32 + 0.5 + float2(time * -0.08, 0);
    float2 noiseCoords3 = (coords - 0.5) * scale * 0.14 + 0.5 + float2(0, time * -0.06);
    float4 noiseColor1 = tex2D(uImage1, noiseCoords1) * float4(supernovaColor1, 1) * sampleColor * 1.5;
    float4 noiseColor2 = tex2D(uImage1, noiseCoords2) * float4(supernovaColor2, 1) * sampleColor * 1.5;
    float4 noiseColor3 = tex2D(uImage2, frac(noiseCoords3)) * sampleColor;
    
    float4 brightColor = tex2D(uImage3, frac(noiseCoords3 - noiseCoords2)) * float4(bloomColor, 1);
    //float2 focalPoint = float2(0.5, 0.5); //float2((1 + sin(3.1415 * time * coords.x * 0.3)) * 0.5, (1 + sin(3.1415 * time * coords.y * 0.3)));
    float distanceFromPoint = length(coords + brightColor.rg * 0.19 - focalPoint) * 2;
    float distanceFade = InverseLerp(0.35, 0.19, distanceFromPoint);
    brightColor *= distanceFade * 0.5 * focalPointOpacity;
    
    // Calculate edge fade values. These are used to make the gas naturally fade at those edges.
    //float2 edgeDistortion = tex2D(uImage3, noiseCoords1 * 2.5).rb * 0.0093;
    //float distanceFromCenter = length(coords + edgeDistortion - 0.5) * 1.414;
    //float distanceFade = InverseLerp(0.45, 0.39, distanceFromCenter);
    
    float4 result = (noiseColor1 + noiseColor2) * sampleColor.a;
    result.a = sampleColor.a * 1.25;
    return ((result - noiseColor3 * 0.15 + brightColor) * brightness + (brightness - 1) * 0.25) * generalOpacity;
}
technique Technique1
{
    pass GasPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}