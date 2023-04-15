sampler mainTexture : register(s0);

float time;
float cellResolution;
float intensity;

float Random1(float value, float seed)
{
    return frac(sin(value * 345.456) * seed);
}

float Random2(float2 uv, float seed)
{
    return frac(sin(dot(uv, float2(123.456, 43.21))) * seed);
}

// Created from this tutorial: https://www.youtube.com/watch?v=Rl3clbrsI40
float2 Drops(float2 uv, float seed)
{
    // Shift the Y value.
    uv.y += Random1(0.5, seed);
    uv *= cellResolution;
    
    // Shift the X value by the row.
    float rowIndex = floor(uv.y);
    uv.x += Random1(rowIndex, seed + 1654.2);
    
    // Modify the Y value by time and a random amount, to simulate them falling.
    uv.y -= time * (0.01 + 0.1 * Random1(rowIndex, seed + 867.65));
    
    // Get the index and the coords for the current cell.
    float2 cellIndex = floor(uv);
    float2 cellUv = frac(uv);
    
    // Get the distance from the center, and use it to determine if the current pixel is inside the drop.
    float distanceFromCellCenter = distance(cellUv, float2(0.5, 0.5));
    float isInsideDrop = 1.0 - step(0.1, distanceFromCellCenter);
    
    // Filter out a lot of the drops by comparing it with a high step value.
    float isDropShown = step(0.8, Random2(cellIndex, seed + 363.21));
    
    // Calculate the intensity based on the time and a bunch of random numbers.
    float dropIntensity = 1.0 - frac(time * 0.2 + Random2(cellIndex, seed + 2342.52) * 2.0) * 2.0;
    dropIntensity = clamp(sign(dropIntensity) * abs(pow(dropIntensity, 4.0)), 0., 1.);
    
    // Get the unit vector to the center of the cell from the current pixel.
    float2 directionToCenter = normalize(float2(0.5, 0.5) - cellUv);
    
    // And use it to get the info of the current pixel in the current drop.
    float2 dropValue = directionToCenter * distanceFromCellCenter * distanceFromCellCenter * (40. * intensity);
    
    // Return this value, accounting for whether it is inside the drop, if the drop is shown, and the intensity of the drop.
    return dropValue * isDropShown * isInsideDrop * dropIntensity;
}

float4 PixelShaderFunction(float2 uv : TEXCOORD0) : COLOR0
{
    // Initialize the drops info for the current pixel.
    float2 drops = float2(0, 0);
    
    // Simulate it 3 times for more drops.
    for (int i = 0; i < 3; i++)
        drops += Drops(uv, 123.45 + float(i));
    
    // Distort the uv by the drops.
    uv += drops;
    
    // Return the texture with the distorted uv.
    return tex2D(mainTexture, uv);
}

technique Technique1
{
    pass RainPass
    {
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}