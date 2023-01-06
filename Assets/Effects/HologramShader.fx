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

float4 PixelShaderFunction(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float frameY = (coords.y * uImageSize0.y - uSourceRect.y) / uSourceRect.w;
    float horizontalOffset = (sin(uTime * 2.718 + frameY * 34) + sin(uTime * 3.141 + frameY * 22.9)) * lerp(0, 2, uOpacity) * 0.5;
    horizontalOffset = floor(horizontalOffset) / uImageSize0.x;
    float brightness = lerp(0.85, 1.9, sin(uTime * 2.85 + frameY * 2) * 0.5 + 0.5);
    float4 noiseColor = tex2D(uImage1, float2(coords.x, frameY - uTime * 0.03));
    float4 color = tex2D(uImage0, coords + float2(horizontalOffset, 0)) * sampleColor;
    float3 hologramColor = noiseColor.rgb * uColor * sampleColor.rgb;
    color = float4(lerp(color.rgb, hologramColor * brightness, uOpacity * 0.65), 1) * color.a;
    color.a = 0;
    
    return color;
}

technique Technique1
{
    pass HologramPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}