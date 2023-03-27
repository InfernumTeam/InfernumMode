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

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 targetCoords = (uTargetPosition - uScreenPosition) / uScreenResolution;
    float2 offset = (coords - targetCoords) * (uScreenResolution / uScreenResolution.y);
    float distanceFromTarget = length(offset);
    float distortDistance = uProgress * uColor.z * 10;
    
    // Refer to the ScreenShakeProj file for information on the color values.
    if (abs(distanceFromTarget - distortDistance) < 150)
    {
        float distanceFromDistortEdge = distanceFromTarget - distortDistance;
        float squeezeFactor = 1 - pow(abs(distanceFromDistortEdge * 10), 0.8);
        float distortionInterpolant = 0.001 * distanceFromDistortEdge * squeezeFactor;
        
        // Calculate the direction of the distortion.
        float2 distortionDirection = -normalize(offset);
        
        // Apply distortion effects by offseting the coordinates.
        coords += (distortionDirection * distortionInterpolant) / (distortDistance * 3);
        
        // Calculate the final color. It is brighter at the start.
        float4 color = tex2D(uImage0, coords);
        color += (color * distortionInterpolant) / (distortDistance * 5);

        return color;
    }
    
    return tex2D(uImage0, coords);
}
technique Technique1
{
    pass DyePass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}