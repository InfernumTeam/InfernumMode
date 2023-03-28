sampler uImage0 : register(s0);

texture2D mainTexture;
sampler2D MainTexture = sampler_state
{
    Texture = <mainTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

float2 screenResolution;
float2 sunPosition;
float time;
float intensity;

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    // Get the offset to the position of the sun.
    float2 offset = (coords - sunPosition);
    // Make this affected by the screen res.
    float2 ratioOffset = offset * float2(screenResolution.x / screenResolution.y, 1);
    // Use the length of that to get an intensity value.
    float offsetIntensity = (3 - length(offset)) / 3;
    // And use that combined with a sample of a gradient texture using ingame time to get the light color.
    return tex2D(MainTexture, float2(time, 0)) * offsetIntensity * intensity;
    
}
technique Technique1
{
    pass LightingPass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}