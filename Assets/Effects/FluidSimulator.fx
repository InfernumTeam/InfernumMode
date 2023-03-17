sampler generalField : register(s0);
sampler velocityField : register(s1);
sampler colorField : register(s2);

float dt = 0.0167;
float viscosity;
float vorticityAmount;
float velocityPersistence;
float densityDecayFactor;
float densityClumpingFactor;
float decelerationFactor;
float2 simulationArea;

float colorInterpolateSharpness;
float lifetimeFadeStops;
float4 lifetimeFadeColors[8];

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

float4 MultiColorLerp(float4 colors[8], float t, int numStops)
{
    // Handle t outside the gradient's range.
    if (t <= 0)
        return colors[0];

    if (t >= 1)
        return colors[numStops - 1];

    // Find the span t sits within.
    int end = int(t / numStops);

    // Interpolate colors for this span.
    float colorInterpolant = t * numStops % 1;
    return lerp(colors[clamp(end, 0, numStops - 1)], colors[clamp(end + 1, 0, numStops - 1)], colorInterpolant);
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
    
    // Make the density dissipate over time. The underlying dot product makes it relative to the current velocity of the fluid.
    fluidData.z -= dt * dot(float3(densityDifference, ddx.x + ddy.y), fluidData.xyz);
    fluidData.z *= densityDecayFactor;
    
    // Perform advection to keep the velocity moving based on the density of the surrounding pixels and the Laplacian.
    float2 fluidSource = saturate(coords - dt * fluidData.xy * stepSize);
    float2 laplacian = right.xy + left.xy + top.xy + top.xy - 4 * fluidData.xy;
    float2 viscosityForce = velocityPersistence * laplacian;
    fluidData.xyw = tex2D(velocityField, fluidSource).xyw;
    fluidData.xy += dt * (viscosityForce - densityClumpingFactor / dt * densityDifference);
    
    // Make the velocity incrementally decay.
    fluidData.xy *= decelerationFactor;
    
    // Place hard limits on the density and stored curl values as a means of preventing potential error explosions.
    fluidData.zw = clamp(fluidData.zw, float2(0, -10), float2(10, 10));
    
    return fluidData;
}

float4 UpdateVelocity_Vorticity(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
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
    
    // Apply an effect similar to vorticity confinement. This encourages the creation of swirling motion across the field.
    // The resulting curl calculations are stored in the W channel for ease of use.
    fluidData.w = top.x - bottom.x + right.y - left.y;
    float3 gradient = float3(ddy.z, -ddx.z, 0);
    float3 vorticity = cross(gradient, normalize(float3(top.w - bottom.w, left.w - right.w, fluidData.w)));
    vorticity *= vorticityAmount * length(fluidData.xyz) / (length(vorticity) + 1e-5);
    fluidData.xy += vorticity.xy;
    
    return fluidData;
}

float4 Diffuse(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 stepSize = 1.0 / simulationArea;
    float4 center = tex2D(generalField, coords);
    float4 left = tex2D(generalField, coords + float2(stepSize.x, 0));
    float4 right = tex2D(generalField, coords - float2(stepSize.x, 0));
    float4 top = tex2D(generalField, coords + float2(0, stepSize.y));
    float4 bottom = tex2D(generalField, coords - float2(0, stepSize.y));
    float4 result = (center + viscosity * (left + right + top + bottom)) / (4 * viscosity + 1);
    return result;
}

float4 Advect(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    return tex2D(generalField, saturate(coords - GetVelocity(coords) * dt / simulationArea));
}

float4 DrawResult(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{
    float2 stepSize = 1 / simulationArea;
    float density = tex2D(velocityField, coords).z;
    float left = tex2D(velocityField, coords + float2(stepSize.x, 0)).z;
    float right = tex2D(velocityField, coords - float2(stepSize.x, 0)).z;
    float top = tex2D(velocityField, coords + float2(0, stepSize.y)).z;
    float bottom = tex2D(velocityField, coords - float2(0, stepSize.y)).z;
    
    // Correct for density "gaps" in cases where there's small areas where there's no density surrounding by lots of density.
    float densityCorrectionThresold = 1.8;
    if (density < densityCorrectionThresold)
    {
        if (left > density && left > densityCorrectionThresold)
            density = left;
        if (right > density && right > densityCorrectionThresold)
            density = right;
        if (top > density && top > densityCorrectionThresold)
            density = top;
        if (bottom > density && bottom > densityCorrectionThresold)
            density = bottom;
    }
    
    float4 colorData = tex2D(colorField, coords);
    float4 color = float4(colorData.rgb, 1);
    
    // Make the color change based on the lifetime interpolation if applicable.
    if (lifetimeFadeStops >= 2)
    {
        float4 interpolatedColor = MultiColorLerp(lifetimeFadeColors, 1 - exp(-colorInterpolateSharpness * density), lifetimeFadeStops);
        color = lerp(color, interpolatedColor, interpolatedColor.a / (density * 0.088 + 1));
    }
    
    return color * density;
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
    pass VelocityUpdateVorticityPass
    {
        PixelShader = compile ps_2_0 UpdateVelocity_Vorticity();
    }
    pass DrawResultPass
    {
        PixelShader = compile ps_2_0 DrawResult();
    }
}