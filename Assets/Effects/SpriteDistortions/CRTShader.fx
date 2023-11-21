sampler mainImage : register(s0);

// This is used to allow the image to smoothly transition from gameplay to the screen.
float globalIntensity;
// How curved the image should be at the edges, but inversely.
float curvature;
// How opaque the scanlines should be.
float scanlineOpacity;
// How round the vignette should be.
float vignetteRoundness;
// How opaque the vignette should be.
float vignetteOpacity;
// How much brightness to multiply the image by
float brightnessMultiplier;
// The strength of the chromatic aberration.
float chromaticStrength;

float2 screenSize;

float2 CurveRemap(float2 uv)
{
    // Distort the UV when approaching the edges of the screen via a cubic function.
    // outUV = inUV(abs(inUV * 2 — 1) / curvature)² + inUV
    float2 curveUV = uv;
    curveUV = curveUV * 2 - float2(1, 1);
    float2 offset = abs(curveUV.yx) / float2(curvature, curvature);
    curveUV += curveUV * offset * offset;
    curveUV = curveUV * 0.5 + 0.5;
    
    return lerp(uv, curveUV, globalIntensity);
}

float4 CreateScanLine(float uvAxis, float resolutionAxis, float opacity)
{
    // Use a sine wave to create scan lines across the screen. This
    // only does one direction, so it needs to be ran twice with both opposing axis pairs.
    float intensity = sin(uvAxis * resolutionAxis * 3.1415 * 2.0);
    intensity = ((0.5 * intensity) + 0.5) * 0.9 + 0.1;
    intensity = pow(intensity, opacity);
    return float4(intensity, intensity, intensity, 1.0);
}

float4 AddVignette(float2 uv, float opacity)
{
    // Darken the intensity the further to the edges of the screen.
    float roundness = vignetteRoundness * globalIntensity;
    float intensity = uv.x * uv.y * (1 - uv.x) * (1 - uv.y);
    intensity = saturate(pow((screenSize.x / roundness) * intensity, max(opacity, 1)));
    
    return float4(intensity, intensity, intensity, 1);
}

float4 PixelShaderFunction(float2 uv : TEXCOORD0) : COLOR0
{
    float2 curvedUV = CurveRemap(uv);
    
    // Don't loop around the texture; if the UV is outside of the 0-1 range, return black.
    if (curvedUV.x > 1. || curvedUV.x < 0. || curvedUV.y > 1.0 || curvedUV.y < 0.)
        return float4(0, 0, 0, 0);
    
    // Get the screen pixel.
    float4 pixel = tex2D(mainImage, curvedUV);
    
    // Perform some basic chromatic aberration.
    float intensity = chromaticStrength * globalIntensity;
    pixel.r = tex2D(mainImage, curvedUV + float2(-0.707, -0.707) * intensity).r;
    pixel.g = tex2D(mainImage, curvedUV + float2(0.707, 0.707) * intensity).g;
    pixel.b = tex2D(mainImage, curvedUV + float2(0, 1) * intensity).b;
    
    // Add scan lines in both axis directions.
    pixel *= CreateScanLine(uv.x, screenSize.y, scanlineOpacity * globalIntensity);
    pixel *= CreateScanLine(uv.y, screenSize.x, scanlineOpacity * globalIntensity);

    // Add a vignette effect. This hides the nasty harsh edges from the curvature, and really sells the effect.
    pixel *= AddVignette(curvedUV, vignetteOpacity * globalIntensity);
    
    // Brighten up the image, this looks terrible but in a good way for the desired effect.
    float scaledBrightness = lerp(1, max(brightnessMultiplier, 1), globalIntensity);
    pixel *= float4(scaledBrightness, scaledBrightness, scaledBrightness, 1);
    
    return pixel;
}

technique Technique1
{
    pass CRTPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}