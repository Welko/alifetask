#version 300 es

precision highp float;
precision mediump usampler2D;

in vec2 vUv;

out uvec4 outColor;

uniform usampler2D uData0;

/*uniform*/ float feedRate = 0.035;
/*uniform*/ float killRate = 0.065;
/*uniform*/ float diffusionRateU = 0.16;
/*uniform*/ float diffusionRateV = 0.08;
/*uniform*/ float deltaTimeMs = 0.1;

void main() {
    ivec2 pixelCoord = ivec2(gl_FragCoord.xy);

    ivec2 pixelMin = ivec2(0, 0);
    ivec2 pixelMax = textureSize(uData0, 0) - ivec2(1, 1);

    uint validT = uint(pixelCoord.y < pixelMax.y);
    uint validB = uint(pixelCoord.y > pixelMin.y);
    uint validL = uint(pixelCoord.x > pixelMin.x);
    uint validR = uint(pixelCoord.x < pixelMax.x);

    ivec2 topPixel = pixelCoord + ivec2(0, 1);
    ivec2 bottomPixel = pixelCoord + ivec2(0, -1);
    ivec2 leftPixel = pixelCoord + ivec2(-1, 0);
    ivec2 rightPixel = pixelCoord + ivec2(1, 0);

    // Get our concentration values (c) and neighbors
    vec4 c  = vec4(texelFetch(uData0, pixelCoord, 0)) / 65535.0;
    vec4 cT = vec4(texelFetch(uData0, topPixel, 0) * validT) / 65535.0;
    vec4 cB = vec4(texelFetch(uData0, bottomPixel, 0) * validB) / 65535.0;
    vec4 cL = vec4(texelFetch(uData0, leftPixel, 0) * validL) / 65535.0;
    vec4 cR = vec4(texelFetch(uData0, rightPixel, 0) * validR) / 65535.0;

    // Gray-Scott model
    // https://en.wikipedia.org/wiki/Reaction%E2%80%93diffusion_system
    {
        float U = c.r;
        float V = c.g;

        // Calculate Laplacian for chemicals
        float lapU = -U + (cT.r + cB.r + cL.r + cR.r) / 4.0;
        float lapV = -V + (cT.g + cB.g + cL.g + cR.g) / 4.0;
    
        // Gray-Scott equations
        float uvv = U * V * V;
        float dU = diffusionRateU * lapU - uvv + feedRate * (1.0 - U);
        float dV = diffusionRateV * lapV + uvv - (feedRate + killRate) * V;

        // Update U and V with time step
        U += dU * deltaTimeMs;
        V += dV * deltaTimeMs;

        outColor = uvec4(vec4(U, V, 0.0, 0.0) * 65535.0);
        return;
    }

    outColor = uvec4(c * 65535.0);
}