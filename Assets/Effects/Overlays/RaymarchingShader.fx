// Global params
sampler uImage0 : register(s0);
float time;
float2 screenResolution;
const float PI = 3.141596;

// Raymarch params.
texture2D noiseTexure;
sampler2D NoiseSample = sampler_state
{
    Texture = <noiseTexure>;
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};

texture2D lightsTexture;
sampler2D LightsSample = sampler_state
{
    Texture = <lightsTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};

texture2D screenTexture;
sampler2D ScreenSample = sampler_state
{
    Texture = <screenTexture>;
    MinFilter = Linear;
    MagFilter = Linear;
    AddressU = Wrap;
    AddressV = Wrap;
};

float rEmissionMultiplier = 1.0;
float rDistanceMod = 1.0;

bool HitSomething(float2 origin, float2 direction, float aspect, out float2 hitPosition)
{
    hitPosition = float2(0, 0);
    float currentDistance = 0;
    for (int i = 0; i < 10; i++)
    {
        float2 samplePoint = origin + direction * currentDistance;
        // Convert back to UV space.
        samplePoint.x /= aspect;
        
        // Early exit if we hit the edge of the screen.
        if (samplePoint.x > 1 || samplePoint.x < 0 || samplePoint.y > 1 || samplePoint.y < 0)
            break;
        
        float distanceToSurface = tex2D(uImage0, samplePoint).r / rDistanceMod;
        
        // The precision isn't enough to just check against 0.
        if (distanceToSurface < 0.001f)
        {
            hitPosition = samplePoint;
            return true;
        }
        
        // If a surface isnt hit, continue marching along the ray.
        currentDistance += distanceToSurface;
    }
    return false;
}

float4 Raymarch(float2 coords : TEXCOORD0) : COLOR0
{
    float3 color = float3(0, 0, 0);
    float emissive = 0;
    // Change from UV aspect to world aspect.
    float uv = coords;
    float aspect = screenResolution.y / screenResolution.x;
    uv.x *= aspect;
    
    float rand2PI = tex2D(NoiseSample, coords * float2(time, -time)) * 2 * PI;
    // Magic number that gives good ray distribution.
    float goldenAngle = PI * 0.7639320225;
    
    // Cast the rays.
    for (int i = 0; i < 10; i++)
    {
        // Get the direction by taking the random angle and adding the golden angle * ray number.
        float currentAngle = rand2PI + goldenAngle * float(i);
        float2 rayDirection = normalize(float2(sin(currentAngle + 1.57079), sin(currentAngle)));
        float2 rayOrigin = uv;
        
        float2 hitPosition;
        bool hit = HitSomething(rayOrigin, rayDirection, aspect, hitPosition);
        if (hit)
        {
            float4 emissiveData = tex2D(ScreenSample, hitPosition);
            emissive += max(emissiveData.r, max(emissiveData.g, emissiveData.b)) * rEmissionMultiplier;
            color += emissiveData.rgb;
        }
    }
    
    color /= emissive;
    emissive /= float(32);
    
    return float4(emissive * color, 1);
}


technique Technique1
{
    pass RaymarchPass
    {
        PixelShader = compile ps_3_0 Raymarch();
    }
}