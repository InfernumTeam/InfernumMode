sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
float3 uColor;
float3 uSecondaryColor;
float uOpacity;
float uSaturation;
float uRotation;
float uTime;
float4 uSourceRect;
float2 uWorldPosition;
float uDirection;
float3 uLightSource;
float2 uImageSize0;
float2 uImageSize1;
float4 uShaderSpecificData;
float uTimeFactor;
float2 uZoomFactor;
float uZoomFactorSecondary;
float uSecondaryLavaPower;
float2 uNoiseReadZoomFactor;
float4 uNPCRectangle;
float2 uActualImageSize0;

float2 RotatedBy(float2 v, float angle)
{
    float c = sin(angle + 1.57);
    float s = sin(angle);
    return float2(v.x * c - v.y * s, v.x * s + v.y * c);
}

float Noise(float2 coords)
{
    return tex2D(uImage1, coords * uNoiseReadZoomFactor).r;
}

// Creates distortions in the noise patterns by picking offset positions and subtracting.
// This helps in creating a flow-like effect.
float2 Gradient2D(float2 coords)
{
    float2 flattenedCoords = coords;
    float x = Noise(flattenedCoords + float2(0.1, 0)).r - Noise(flattenedCoords - float2(0.1, 0)).r;
    float y = Noise(flattenedCoords + float2(0, 0.1)).r - Noise(flattenedCoords - float2(0, 0.1)).r;
    return float2(x, y);
}

float CalculateLavaFlowIntensity(float2 p)
{
    float gain = 2.2;
    float amplitude = 0.5;
    float result = 0;
    float2 p2 = p;
    
    // Create increasingly detailed lava based on repeated, increasingly small "bumps".
    // This technique is analogous to fractal noise, which makes crisp textures by repeatedly summing noise of different frequencies and diminishing amplitudes.
    // https://thebookofshaders.com/13/ provides a good overview of this concept.
    for (float i = 1; i < 5; i++)
    {
        // Offset based on time.
        p += uTime * uTimeFactor * 0.2;
        p2 -= uTime * uTimeFactor * 0.4;
        
        float2 gradient = Gradient2D(i * p * 0.34 + uTime * uTimeFactor * 0.1);
        
        // Rotate the displacement field.
        float positionBasedRotationalOffset = dot(p, float2(0.05, 0.03)) * 40;
        gradient = RotatedBy(gradient, uTime * uTimeFactor * 0.6 - positionBasedRotationalOffset);
        
        // Displace the point based on the gradient.
        p += gradient * 0.5;
        
        // Add noise octaves.
        result += (sin(Noise(p) * 7) * 0.5 + 0.5) * amplitude;
        
        // Advect between the two points.
        p = lerp(p2, p, 0.75);
        
        amplitude *= 0.714;
        p *= gain;
        p2 *= gain - 0.8;
    }
    return result;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    // Adjust coords such that <0,0> is the top left of the frame and <1, 1> is the bottom right of the frame instead of the entire sheet.
    float2 framedCoords = (coords * uActualImageSize0 - uNPCRectangle.xy) / uNPCRectangle.zw;
    
    float4 color = tex2D(uImage0, coords);
    
    // Main lava color.
    float4 burnColor1 = float4(uColor, 1) / CalculateLavaFlowIntensity(framedCoords * uZoomFactor);
    
    // Additive burn color.
    float4 burnColor2 = float4(uSecondaryColor, 1) * pow(CalculateLavaFlowIntensity(framedCoords * uZoomFactor * uZoomFactorSecondary), uSecondaryLavaPower);
    if (uOpacity <= 0)
        return color * sampleColor;
    
    // Clear any opacity artifacts. This might be a bit unoptimal but it works fine.
    float3 resultingColorRGB = burnColor1 + burnColor2;
    float4 resultingColor = float4(resultingColorRGB, 1) * sampleColor;
    return lerp(color * sampleColor, resultingColor * color.a, uOpacity);
}

technique Technique1
{
    pass BurnPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}