#version 300 es

precision highp float;
precision mediump usampler2D;

in vec2 vUv;

out uvec4 outColor;

uniform usampler2D uData0;

uniform float uFeedRate;
uniform float uKillRate;
uniform float uDiffusionRateU;
uniform float uDiffusionRateV;
uniform float uDeltaTimeMs;

vec2 laplacian(uvec2 c, ivec2 pixelCoord) {
    ivec2 pixelMin = ivec2(0, 0);
    ivec2 pixelMax = textureSize(uData0, 0) - ivec2(1, 1);

    bool validT = pixelCoord.y < pixelMax.y;
    bool validB = pixelCoord.y > pixelMin.y;
    bool validL = pixelCoord.x > pixelMin.x;
    bool validR = pixelCoord.x < pixelMax.x;
    bool validTL = validT && validL;
    bool validTR = validT && validR;
    bool validBL = validB && validL;
    bool validBR = validB && validR;

    ivec2 pixelT = validT ? pixelCoord + ivec2(0, 1) : ivec2(pixelCoord.x, pixelMax.y);
    ivec2 pixelB = validB ? pixelCoord + ivec2(0, -1) : ivec2(pixelCoord.x, pixelMin.y);
    ivec2 pixelL = validL ? pixelCoord + ivec2(-1, 0) : ivec2(pixelMin.x, pixelCoord.y);
    ivec2 pixelR = validR ? pixelCoord + ivec2(1, 0) : ivec2(pixelMax.x, pixelCoord.y);
    ivec2 pixelTL = validTL ? pixelCoord + ivec2(-1, 1) : ivec2(pixelMin.x, pixelMax.y);
    ivec2 pixelTR = validTR ? pixelCoord + ivec2(1, 1) : ivec2(pixelMax.x, pixelMax.y);
    ivec2 pixelBL = validBL ? pixelCoord + ivec2(-1, -1) : ivec2(pixelMin.x, pixelMin.y);
    ivec2 pixelBR = validBR ? pixelCoord + ivec2(1, -1) : ivec2(pixelMax.x, pixelMin.y);

    // Wrap around
    uvec2 rg = c;
    uvec2 rgT = texelFetch(uData0, pixelT, 0).rg;
    uvec2 rgB = texelFetch(uData0, pixelB, 0).rg;
    uvec2 rgL = texelFetch(uData0, pixelL, 0).rg;
    uvec2 rgR = texelFetch(uData0, pixelR, 0).rg;
    uvec2 rgTR = texelFetch(uData0, pixelTR, 0).rg;
    uvec2 rgBR = texelFetch(uData0, pixelBR, 0).rg;
    uvec2 rgBL = texelFetch(uData0, pixelBL, 0).rg;
    uvec2 rgTL = texelFetch(uData0, pixelTL, 0).rg;

    return vec2(
        vec2(rgTL + rgTR + rgBL + rgBR) * 0.05 +
        vec2(rgT + rgB + rgL + rgR) * 0.2 +
        vec2(rg) * -1.0
    ) / 65535.0;
}

void main() {
    ivec2 pixelCoord = ivec2(gl_FragCoord.xy);
    
    // Concentration (c)
    uvec4 c = texelFetch(uData0, pixelCoord, 0);

    // Gray-Scott model
    // https://en.wikipedia.org/wiki/Reaction%E2%80%93diffusion_system
    {
        float U = float(c.r) / 65535.0;
        float V = float(c.g) / 65535.0;

        // Calculate Laplacian for chemicals
        vec2 lap = laplacian(c.rg, pixelCoord);
    
        // Gray-Scott equations
        float uvv = U * V * V;
        float dU = uDiffusionRateU * lap.x - uvv + uFeedRate * (1.0 - U);
        float dV = uDiffusionRateV * lap.y + uvv - (uFeedRate + uKillRate) * V;

        // Update U and V with time step
        U += dU * uDeltaTimeMs;
        V += dV * uDeltaTimeMs;

        outColor = uvec4(vec4(U, V, 0.0, 0.0) * 65535.0);
        return;
    }

    outColor = c;
}