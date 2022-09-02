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

float2 Gradient2D(float2 coords)
{
    float2 flattenedCoords = coords;
    float x = Noise(flattenedCoords + float2(0.1, 0)).r - Noise(flattenedCoords - float2(0.1, 0)).r;
    float y = Noise(flattenedCoords + float2(0, 0.1)).r - Noise(flattenedCoords - float2(0, 0.1)).r;
    return float2(x, y);
}

float CalculateLavaFlowIntensity(float2 p)
{
    float z = 2.;
    float rz = 0.;
    float2 bp = p;
    for (float i = 1; i < 7; i++)
    {
		//primary flow speed
        p += uTime * uTimeFactor * 0.2;
		
		//secondary flow speed (speed of the perceived flow)
        bp -= uTime * uTimeFactor * 0.4;
		
		//displacement field (try changing time multiplier)
        float2 gr = Gradient2D(i * p * 0.34 + uTime * uTimeFactor * 0.1);
		
		//rotation of the displacement field
        gr = RotatedBy(gr, uTime * uTimeFactor * 0.6 - (0.05 * p.x + 0.03 * p.y) * 40.);
		
		//displace the system
        p += gr * .5;
		
		//add noise octave
        rz += (sin(Noise(p).r * 7.0) * 0.5 + 0.5) / z;
		
		//blend factor (blending displaced system with base system)
		//you could call this advection factor (.5 being low, .95 being high)
        p = lerp(bp, p, 0.77);
		
		//intensity scaling
        z *= 1.4;
		//octave scaling
        p *= 2.2;
        bp *= 1.4;
    }
    return rz;
}

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 framedCoords = (coords * uActualImageSize0 - uNPCRectangle.xy) / uNPCRectangle.zw;
    
    float4 color = tex2D(uImage0, coords);
    float4 burnColor1 = float4(uColor, 1) / CalculateLavaFlowIntensity(framedCoords * uZoomFactor);
    float4 burnColor2 = float4(uSecondaryColor, 1) * pow(CalculateLavaFlowIntensity(framedCoords * uZoomFactor * uZoomFactorSecondary), uSecondaryLavaPower);
    if (uOpacity <= 0)
        return color * sampleColor;
    
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