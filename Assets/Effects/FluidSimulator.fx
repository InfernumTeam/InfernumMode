sampler generalField : register(s0);
sampler velocityField : register(s1);
sampler colorField : register(s2);

float dt = 0.04;
float viscosity;
float vorticityAmount;
float densityDecayFactor;
float densityClumpingFactor;
float2 simulationArea;

// STUPID MiscShaderData needs this. Pretty much all of it is unused.
sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
sampler uImage2 : register(s2);
sampler uImage3 : register(s3);
float3 uColor;
float3 uSecondaryColor;
float2 uScreenResolution;
float2 uScreenPosition;
float2 uTargetPosition;
float2 uDirection;
float uOpacity;
float uTime;
float uIntensity;
float uProgress;
float2 uImageSize1;
float2 uImageSize2;
float2 uImageSize3;
float2 uImageOffset;
float uSaturation;
float4 uSourceRect;
float2 uZoom;
float4 uShaderSpecificData;

float2 GetVelocity(float2 coords)
{
    return tex2D(velocityField, coords).xy;
}

// Density is stored in the Z channel, and curl is stored in the W channel.
// This is a bit strange in a mathematical sense, but it does save the need to create separate render targets in GPU memory, so it's definitely a worthwhile investment.
float4 UpdateVelocity(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 stepSize = 1 / simulationArea;
    
    // Calculate values in the cardinal directions for the sake of future finite-difference estimations.
    float4 fluidData = tex2D(velocityField, coords);
    float4 right = tex2D(velocityField, coords + float2(stepSize.x, 0));
    float4 left = tex2D(velocityField, coords - float2(stepSize.x, 0));
    float4 top = tex2D(velocityField, coords - float2(0, stepSize.y));
    float4 bottom = tex2D(velocityField, coords + float2(0, stepSize.y));
    
    // Calculate gradients based on finite difference approximations.
    float3 ddx = (right - left).xyz * 0.5;
    float3 ddy = (bottom - top).xyz * 0.5;
    float2 densityDifference = float2(ddx.z, ddy.z);
    
    // Make the density dissipate over time. The underlying dot product clears away the divergence of the field (thus satisfying the Continuity Navier-Stokes equation) due to the underlying
    // math for the dot product. If the result is not than zero than it will be gradually subtracted off until it is.
    fluidData.z -= dt * dot(float3(densityDifference, ddx.x + ddy.y), fluidData.xyz);
    fluidData.z *= densityDecayFactor;
    
    // Perform advection to keep the velocity moving based on the density of the surrounding pixels and the Laplacian.
    float2 coordsHistory = coords - dt * fluidData.xy * stepSize;
    float2 laplacian = right.xy + left.xy + top.xy + top.xy - 4 * fluidData.xy;
    float2 viscosityForce = 0.1 * laplacian;
    fluidData.xyw = tex2D(velocityField, coordsHistory).xyw;
    fluidData.xy += dt * (viscosityForce - densityClumpingFactor / dt * densityDifference);
    
    // Make the velocity incrementally decay.
    fluidData.xy = max(0, abs(fluidData.xy) - 5e-4) * sign(fluidData.xy);
    
    // Apply vorticity confinement. This encourages the creation of swirling motion across the field.
    // The resulting curl calculations are stored in the W channel for ease of use.
    fluidData.w = top.x - bottom.x + right.y - left.y;
    float2 vorticity = float2(abs(bottom.w) - abs(top.w), abs(left.w) - abs(right.w));
    vorticity *= vorticityAmount / (length(vorticity) + 1e-5) * fluidData.w;
    fluidData.xy += vorticity;
    
    // Place hard limits on the velocity, density, and stored curl values as a means of preventting potential error explosions.
    fluidData = clamp(fluidData, float4(-25, -25, 0, -0.5), float4(25, 25, 3, 0.5));
    
    return fluidData;
}

float4 Diffuse(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 delta = 1.0 / simulationArea;
    float4 center = tex2D(generalField, coords);
    float4 left = tex2D(generalField, coords + float2(delta.x, 0));
    float4 right = tex2D(generalField, coords - float2(delta.x, 0));
    float4 top = tex2D(generalField, coords + float2(0, delta.y));
    float4 bottom = tex2D(generalField, coords - float2(0, delta.y));
    return (center + viscosity * (left + right + top + bottom)) / (4 * viscosity + 1);
}

float4 Advect(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    return tex2D(generalField, coords - GetVelocity(coords) * dt * 10 / simulationArea);
}

float4 DrawResult(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float density = tex2D(velocityField, coords).z;
    float4 color = tex2D(colorField, coords);
    color *= density;
        
    return color;
}

technique Technique1
{
    pass DiffusePass
    {
        PixelShader = compile ps_2_0 Diffuse();
    }
    pass AdvectPass
    {
        PixelShader = compile ps_2_0 Advect();
    }
    pass VelocityUpdatePass
    {
        PixelShader = compile ps_2_0 UpdateVelocity();
    }
    pass DrawResultPass
    {
        PixelShader = compile ps_2_0 DrawResult();
    }
}