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
float4 uShaderSpecificData;

float InverseLerp(float start, float end, float x)
{
    return saturate((x - start) / (end - start));
}

float4 PixelShaderFunction(float4 position : SV_POSITION, float2 coords : TEXCOORD0) : COLOR0
{
    float distanceFromTargetPosition = distance(coords, 0.5);
    
    // Calculate the swirl coordinates.
    float2 centeredCoords = coords - 0.5;
    float swirlRotation = distanceFromTargetPosition * 13 - uTime * 2;
    float swirlSine = sin(swirlRotation);
    float swirlCosine = cos(swirlRotation);
    float2x2 swirlRotationMatrix = float2x2(swirlCosine, -swirlSine, swirlSine, swirlCosine);
    float2 swirlCoordinates = mul(centeredCoords, swirlRotationMatrix) + 0.5;
    
    // Calculate fade, swirl arm colors, and draw the portal to the screen.
    float swirlColorFade = saturate(distanceFromTargetPosition * 4) / (uOpacity + 0.0001);
    float4 swirlNoiseColor = tex2D(uImage0, swirlCoordinates) * (1 - swirlColorFade);
    float blackInterpolant = pow(InverseLerp(0.1, 0.27, distanceFromTargetPosition), 0.6);
    swirlNoiseColor.rgb *= blackInterpolant;
    
    return swirlNoiseColor * 4;
}

technique Technique1
{
    pass ScreenPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}