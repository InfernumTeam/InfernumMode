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

float distortionAmount;
float wiggleSpeed;

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Get the target coo-rds, and the UV coords.
    float2 targetCoords = (uTargetPosition - uScreenPosition) / uScreenResolution;
    float2 uvCoords = (coords - targetCoords) * (uScreenResolution / uScreenResolution.y);
    
    // Get a sine wave from 0-1 to make the distortion move.
    float sine = (1 + sin(uTime * wiggleSpeed)) / 2;
    // Get the amount to multiply the distorted coords by.
    float distortionScalar = lerp(-0.001 * distortionAmount, 0.001 * distortionAmount, sine);
    // Get the distorted coords, using a texture for the distortion.
    float2 distortedUV = coords + tex2D(uImage1, float2(frac(uvCoords.x * 0.5 + uTime * 0.1), uvCoords.y)).r * distortionScalar;
    // Return the original texture with the distorted coords.
    float4 color = tex2D(uImage0, distortedUV);
    
    return color;
}

technique Technique1
{
    pass ScreenPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}