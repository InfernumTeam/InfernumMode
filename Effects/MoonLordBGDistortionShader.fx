sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
float3 uColor;
float3 uSecondaryColor;
float uOpacity : register(C0);
float uSaturation;
float uRotation;
float uTime;
float4 uSourceRect;
float2 uWorldPosition;
float uDirection;
float3 uLightSource;
float2 uImageSize0;
float2 uImageSize1;
float2 uTopLeftFreeArea;
float2 uBottomRightFreeArea;
float2x2 uZoomMatrix;
bool uVerticalInversion;

float InverseLerp(float from, float to, float x)
{
    return saturate((x - from) / (to - from));
}
float4 PixelShaderFunction(float4 position : SV_POSITION, float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    
    float zoomDeterminant = determinant(uZoomMatrix);
    float2x2 inverseZoomMatrix = float2x2(uZoomMatrix._22, -uZoomMatrix._21, -uZoomMatrix._12, uZoomMatrix._11) / zoomDeterminant;
    float2 alteredCoords = mul(coords, inverseZoomMatrix);
    float2 topLeft = uTopLeftFreeArea;
    float2 bottomRight = uBottomRightFreeArea;
    float2 noiseCoords1 = mul(frac(coords + float2(0, uTime * -0.021)), inverseZoomMatrix) * float2(uZoomMatrix._11, uZoomMatrix._22) * 6;
    float2 noiseCoords2 = mul(frac(coords + float2(uTime * -0.03, 0)), inverseZoomMatrix) * float2(uZoomMatrix._11, uZoomMatrix._22) * 3;
    float4 noiseColor1 = float4(tex2D(uImage1, noiseCoords1).rgb * uColor, 1);
    float4 noiseColor2 = float4(tex2D(uImage1, noiseCoords2).rgb * uSecondaryColor, 1);
    float4 noiseResult = (noiseColor1 * 0.2 + noiseColor2 * 0.6);
    float2 center = float2((topLeft.x + bottomRight.x) / 2, (topLeft.y + bottomRight.y) / 2);
    
    return lerp(noiseResult, 0, 1 - pow(InverseLerp(0, 1.6, distance(coords, center)), 10));
}

technique Technique1
{
    pass DistortionPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}