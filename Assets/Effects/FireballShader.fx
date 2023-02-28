sampler uImage0 : register(s0);

texture sampleTexture2;
sampler2D NoiseMap = sampler_state
{
    texture = <sampleTexture2>;
    magfilter = LINEAR;
    minfilter = LINEAR;
    mipfilter = LINEAR;
    AddressU = wrap;
    AddressV = wrap;
};

float3 mainColor;
float2 resolution;
float speed;
float zoom;
float dist;
float time;
float opacity;

float4 PixelShaderFunction(float2 coords : TEXCOORD0) : COLOR0
{
    //Pixelate
    coords.x -= coords.x % (1 / (resolution.x * 2));
    coords.y -= coords.y % (1 / (resolution.y * 2));
    float2 mappedUv = float2(coords.x - 0.5, (1 - coords.y) - 0.5);
    
    // Get the length of the doubled distance, so that 0 = at the center of the sprite and 1 = at the very edge of the circle
    float distanceFromCenter = length(mappedUv) * 2;
    
    // Crop the sprite into a circle
    if (distanceFromCenter > 1)
        return float4(0, 0, 0, 0);
    
    // An intensity from the distance to the center
    float mainOpacity = max(0, 1. - distanceFromCenter);
    
    // Make the opacity increase exponentially when past a certain threshold to the center.
    if (distanceFromCenter < 0.8)
        mainOpacity /= pow(distanceFromCenter / 0.8, 3.5);
    
    // From Iban's RoverDriveShield shader:
    //"Blow up" the noise map so it looks circular.
    float blowUpPower = 2.5;
    float blowUpSize = 0.5;
    float blownUpUVX = pow(abs(coords.x - 0.5), blowUpPower);
    float blownUpUVY = pow(abs(coords.y - 0.5), blowUpPower);
    float2 blownUpUV = float2(-blownUpUVY * blowUpSize * 0.5 + coords.x * (1 + blownUpUVY * blowUpSize), -blownUpUVX * blowUpSize * 0.5 + coords.y * (1 + blownUpUVX * blowUpSize));

    // Get the texture coords from the modified coords.
    float2 textureCoord = float2(blownUpUV.x, blownUpUV.y + time * speed);
    float4 noiseTexture = tex2D(NoiseMap, textureCoord);
    
    // Modify the opacity by the noisemap.
    float i = noiseTexture.r;
    mainOpacity *= mainOpacity;
    mainOpacity /= i;

    // Fade the edges.
    if (distanceFromCenter > 0.6)
        mainOpacity *= pow((1 - ((distanceFromCenter - 0.6) / 0.4)), 5);

    // Modify the color by the opacity, toned down and clamped to allow for more of the main color to show.
    float3 color = mainColor * clamp(pow(mainOpacity, 0.5), 0, 2.7);
    // Multiply the final color by a provided opacity.
    return float4(color, 1) * opacity;
}

technique Technique1
{
    pass FirePass
    {
        PixelShader = compile ps_2_0 PixelShaderFunction();
    }
}