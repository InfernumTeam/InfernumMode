sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
sampler uImage2 : register(s2);
sampler uImage3 : register(s3);
float3 uColor;
float3 uSecondaryColor;
float2 uScreenResolution;
float2 uScreenPosition;
float2 uTargetPosition;
float2 uDirection;
float uOpacity;
float uTime;
float uIntensity;
float uProgress;
float2 uImageSize1;
float2 uImageSize2;
float2 uImageSize3;
float2 uImageOffset;
float uSaturation;
float4 uSourceRect;
float2 uZoom;
float4 uShaderSpecificData;

float vortexSwirlSpeed;
float vortexSwirlDetail;
float vortexEdgeFadeFactor;

float luminanceThreshold;

float2x2 fbmMatrix = float2x2(1.63, 1.2, -1.2, 1.63);

float turbulentNoise(float2 coords)
{
    float2 currentCoords = coords;
    
    // Approximate, somewhat basic FBM equations with time included.
    float result = 0.5 * tex2D(uImage1, currentCoords + float2(0, uTime * -0.3));
    currentCoords = mul(currentCoords, fbmMatrix);
    currentCoords.y += uTime * 0.25;
    
    result += 0.25 * tex2D(uImage1, currentCoords);
    currentCoords = mul(currentCoords, fbmMatrix);
    currentCoords.x += uTime * 0.4;
    
    return result * 1.0666667;
}

float3 swirl(float2 coords)
{
    // Start by using turbulence as a base for the background.
    float3 result = uColor * (turbulentNoise(coords * 4) + turbulentNoise(coords * 16)) * 0.75;
    
    float2 centeredCoords = coords - 0.5;
    float distanceFromCenter = length(centeredCoords * float2(uShaderSpecificData.y / uShaderSpecificData.x, 1));
    float distanceBasedSwirlStrength = min(exp(-vortexEdgeFadeFactor * distanceFromCenter), 1) * 0.3;
    float angle = vortexSwirlSpeed * uTime + distanceFromCenter * vortexSwirlDetail;
    
    // Calculate the void vortex color. This is subtracted from the background to give the aesthetic of a black hole.
    float s = sin(angle);
    float c = sin(angle + 1.57);
    float2 swirledCoords = float2(centeredCoords.x * c - centeredCoords.y * s, centeredCoords.x * s + centeredCoords.y * c) + 0.5;
    result -= tex2D(uImage1, swirledCoords).r * distanceBasedSwirlStrength;
    
    return result;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    if (luminanceThreshold > 0)
    {
        float4 color = tex2D(uImage0, coords);
        float luminance = dot(color.rgb, float3(0.299, 0.587, 0.114));
        if (luminance < luminanceThreshold)
            return 0;
    }
    
    return float4(swirl(coords), 1) * sampleColor;
}
technique Technique1
{
    pass ScreenPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}