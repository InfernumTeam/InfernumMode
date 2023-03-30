// What this is is different for each pass.
sampler uImage0 : register(s0);

// The blurred texture, used in the bloom shader.
texture2D bloomScene;
sampler2D BloomScene = sampler_state
{
    Texture = <bloomScene>;
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

float filterThreshold;
float downsampledSize;
float bloomIntensity;
float2 textureSize;
bool horizontal;

// Short list of gaussian weights.
float weights[] = { 0.227027, 0.1945946, 0.1216216, 0.054054, 0.016216 };

// Filter effect to only get the brightest pixels on a texture via a provided threshold.
float4 Filter(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    // Get the brigtest color channel on the pixel.
    float brightest = max(max(color.r, color.b), color.g);

    // If brighter than the threshold, return the pixel.
    if (brightest >= filterThreshold)
        return color;
    
    // Else return nothing.
    return float4(0, 0, 0, 0);
}

// Gaussian blur effect.
float4 Blur(float2 coords : TEXCOORD0) : COLOR0
{
    // Get the offset size based on the size of the provided texture.
    float2 offset = 1 / textureSize;
    float3 result = tex2D(uImage0, coords.xy).rgb * weights[0];

    // Loop along the weights and modify the result by the offset and weights.
    if (horizontal)
    {
        for (int i = 1; i < 5; i++)
        {
            result += tex2D(uImage0, coords + float2(offset.x * i, 0)).rgb * weights[i];
            result += tex2D(uImage0, coords - float2(offset.x * i, 0)).rgb * weights[i];
        }
    }
    else
    {
        for (int i = 1; i < 5; i++)
        {
            result += tex2D(uImage0, coords + float2(0, offset.y * i)).rgb * weights[i];
            result += tex2D(uImage0, coords - float2(0, offset.y * i)).rgb * weights[i];
        }
    }
    
    return float4(result, 1);
}

float4 Bloom(float2 coords : TEXCOORD0) : COLOR0
{
    float3 baseColor = tex2D(uImage0, coords).rgb;
    float3 blurColor = tex2D(BloomScene, coords * downsampledSize).rgb;

    // Blend them together additively.
    baseColor += blurColor * bloomIntensity;

    // Tone mapping.
    //float3 result = float3(1, 1, 1) - exp(-baseColor * bloomIntensity);
    // also gamma correct while we're at it       
    //result = pow(result, float3(1.0 / 2.2, 1.0 / 2.2, 1.0 / 2.2));
    return float4(baseColor, 1);
}

technique Technique1
{
    pass FilterPass
    {
        PixelShader = compile ps_2_0 Filter();
    }
    pass BlurPass
    {
        PixelShader = compile ps_2_0 Blur();
    }
    pass BloomPass
    {
        PixelShader = compile ps_2_0 Bloom();
    }
}