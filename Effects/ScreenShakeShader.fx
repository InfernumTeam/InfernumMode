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
    float2 uvCoords = (coords - targetCoords) * (uScreenResolution / uScreenResolution.y);
    float dotField = dot(uvCoords, uvCoords);
    float ripple = dotField * uColor.y * 3.141 - uProgress * uColor.z;

    if (ripple < 0 && ripple > uColor.x * -6.283)
        ripple = saturate(sin(ripple));
    else
        ripple = 0;

    float2 sampleCoords = coords + ((ripple * uOpacity / uScreenResolution) * uvCoords);

    return tex2D(uImage0, sampleCoords);
}
technique Technique1
{
    pass DyePass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}