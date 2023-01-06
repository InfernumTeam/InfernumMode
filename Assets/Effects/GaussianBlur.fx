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
float2 uImageSize0;
float2 uImageSize1;
float2 uImageSize2;
float2 uImageSize3;
float2 uImageOffset;
float uSaturation;
float4 uSourceRect;
float2 uZoom;
float4 uShaderSpecificData;

// Table of 12 evenly spaced directions, based on <cos(a), sin(a)>.
// This is for performance reasons.
float2 directions[] =
{
    float2(1, 0),
    float2(0.86603, 0.5),
    float2(0.5, 0.86603),
    float2(0, 1),
    float2(-0.5, 0.86603),
    float2(-0.86603, 0.5),
    float2(-1, 0),
    float2(-0.86603, -0.5),
    float2(-0.5, -0.86603),
    float2(0, -1),
    float2(0.5, -0.86603),
    float2(0.86603, -0.5)
};

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{    
    // Determine the max offset based off of the saturation given, and the
    // size of the texture this is being applied to.
    float2 maxOffset = uSaturation / uImageSize0;
    
    // Initialize the result var.
    float4 result = 0;
    
    // Samples pixels in a circular area around the pixel for blending purposes.
    for (float a = 0; a < 12; a++)
    {
        for (float i = 0; i < 12; i++)
            result += tex2D(uImage0, coords + directions[a] * maxOffset * i / 12);
    }
    return result / 145;
}

technique Technique1
{
    pass ScreenPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}