sampler mainSample : register(s0);

float intensity;
float2 resolution;

// I can't believe it's come to this.
float fakeAtan(float x)
{
    return x / (sqrt(pow(x, 2)) + 1) * 1.5707;
}

// Adapted from https://www.shadertoy.com/view/MsjGzh
float4 Main(float2 coords : TEXCOORD0) : COLOR0
{
    float2 uv = coords;
    float aspect = resolution.x / resolution.y;
    float2 centerCoords = float2(0.5, 0.5 / aspect);
    float2 direction = uv - centerCoords;
    float dist = sqrt(dot(direction, direction));

    float power = (2.0 * 3.141592 / (2.0 * sqrt(dot(centerCoords, centerCoords)))) * (-intensity);

    float bind = 0;
    
    if (power > 0) 
        bind = sqrt(dot(centerCoords, centerCoords));
    else
    {
        if (aspect < 1) 
            bind = centerCoords.x;
        else
            bind = centerCoords.y;
    }
    // Fisheye
    if (power > 0)
        uv = (centerCoords + normalize(direction)) * tan(dist * power) * bind / tan(bind * power);
     // Anti-Fisheye
     else if (power < 0)
        uv = (centerCoords + normalize(direction)) * fakeAtan(dist * power * -10.0) / fakeAtan(-power * bind * 10.0) * bind;

    return tex2D(mainSample, uv);
}

technique Technique1
{
    pass FisheyePass
    {
        PixelShader = compile ps_2_0 Main();
    }
}