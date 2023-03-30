// Global params
sampler uImage0 : register(s0);
float time;
float2 screenResolution;
const float PI = 3.141596;

// Jump Flood params
float2 jfimageResolution;
float jfOffset;

// Displacement params
float dDistanceModifier;

float4 Voronoi(float2 UV : TEXCOORD0) : COLOR0
{
    float2 uv = UV;
    
    if (screenResolution.x > screenResolution.y)
        uv.y = ((uv.y - 0.5) * (screenResolution.x / screenResolution.y)) + 0.5;
    else
        uv.x = ((uv.x - 0.5) * (screenResolution.y / screenResolution.x)) + 0.5;

    float4 sceneColor = tex2D(uImage0, UV);
    return float4(UV.x * sceneColor.a, UV.y * sceneColor.a, 0.0, 1.0);
}

float4 JumpFlood(float2 coords : TEXCOORD0) : COLOR0
{
    float closestDistance = 9.9;
    float2 closestPosition = float2(0, 0);
    
    // Jump Flooding Algorithm. Don't ask.
    for (float x = -1; x <= 1; x++)
    {
        for (float y = -1; y <= 1; y++)
        {
            float2 offset = coords;
            offset += float2(x, y) * (1 / jfimageResolution) * jfOffset;

            float2 position = tex2D(uImage0, offset).xy;
            float distanceTo = distance(position.xy, coords.xy);
            
            if (position.x != 0 && position.y != 0 && distanceTo < closestDistance)
            {
                closestDistance = distanceTo;
                closestPosition = position;
            }
        }
    }
    return float4(closestPosition, 0, 1);
}

float4 Displacement(float2 coords : TEXCOORD0) : COLOR0
{
    float4 color = tex2D(uImage0, coords);
    float distanceTo = distance(color.xy, coords.xy);
    float mapped = saturate(distanceTo * dDistanceModifier);
    return float4(mapped, mapped, mapped, 1);
}

technique Technique1
{
    pass VoronoiPass
    {
        PixelShader = compile ps_2_0 Voronoi();
    }
    pass JumpFloodPass
    {
        PixelShader = compile ps_3_0 JumpFlood();
    }
    pass DisplacementPass
    {
        PixelShader = compile ps_3_0 Displacement();
    }
}