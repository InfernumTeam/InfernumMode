texture sampleTexture;
sampler2D NoiseMap = sampler_state
{
    texture = <sampleTexture>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};

texture sampleTexture2;
sampler2D NoiseMap2 = sampler_state
{
    texture = <sampleTexture2>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};

float3 mainColor;
float3 secondaryColor;
float2 resolution;
float time;
float opacity;
float innerGlowAmount;
float innerGlowDistance;

float4 PixelShaderFunction(float2 uv : TEXCOORD0) : COLOR0
{
    // Pixelate.
    uv.x -= uv.x % (1 / (resolution.x * 2));
    uv.y -= uv.y % (1 / (resolution.y * 2));
    float distanceFromTargetPosition = distance(uv, float2(0.5, 0.5));
    float actualOpacity = opacity;
    if (distanceFromTargetPosition < innerGlowDistance)
        actualOpacity /= pow(distanceFromTargetPosition / innerGlowDistance, innerGlowAmount);

    // Calculate the swirl coordinates.
    float2 centeredCoords = uv - 0.5;
    float swirlRotation = length(centeredCoords) * 19. - time * 5.;
    float swirlSine = sin(swirlRotation);
    float swirlCosine = sin(swirlRotation + 1.57);
    float2x2 swirlRotationMatrix = float2x2(swirlCosine, -swirlSine, swirlSine, swirlCosine);
    float2 swirlCoordinates = mul(centeredCoords, swirlRotationMatrix) + 0.5;
    
    // Calculate fade, swirl arm colors, and draw the portal to the screen.
    float swirlColorFade = clamp(distanceFromTargetPosition * 3., 0., 1.) / 0.95;
    float3 swirlBaseColor = lerp(mainColor, secondaryColor, pow(swirlColorFade, 0.63));
    float4 swirlNoiseColor = lerp(float4(mainColor, 1), lerp(tex2D(NoiseMap, swirlCoordinates), tex2D(NoiseMap2, swirlCoordinates), 0) * (1. - swirlColorFade), 0.5);
    float4 endColor = lerp(float4(swirlBaseColor, 0.1), float4(0., 0., 0., 0.), swirlColorFade);
    return lerp(float4(0., 0., 0., 0.), endColor * (1. + (1. - swirlColorFade) * 2.), clamp(swirlNoiseColor.r, 0., 1.)) * actualOpacity;
}

technique Technique1
{
    pass PortalPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}