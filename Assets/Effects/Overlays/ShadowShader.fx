sampler uImage0 : register(s0);

texture2D mainTexture;
sampler2D MainTexture = sampler_state
{
    Texture = <mainTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};

float threshold;

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    // Get the screen color.
    float4 color = tex2D(uImage0, coords);
    float4 finalColor = float4(0, 0, 0, 0);
    float brightest = max(max(color.r, color.b), color.g);

    // If its blue (sky should be mostly blue) is lower than the threshold, decrease its alpha.
    if (color.b < threshold)
        finalColor = float4(1, 1, 1, pow(1 - color.b, 0.35));

    return finalColor;
}

float4 PixelShaderFunction2(float2 coords : TEXCOORD0) : COLOR0
{
    // Get the screen color
    float4 color = tex2D(uImage0, coords);
    // Subtract the color of the shadow RT from it.
    color.rgb -= tex2D(MainTexture, coords).rgb;
    return color;
}

technique Technique1
{
    pass GetShadowPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
    pass UseShadowPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction2();
    }
}