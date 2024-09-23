#version 300 es

precision highp float;

in vec2 vUv;

out vec4 outColor;

uniform sampler2D uData0;

uniform float uFeedRate;
uniform float uKillRate;
uniform float uDiffusionRateU;
uniform float uDiffusionRateV;
uniform float uDeltaTime;

vec2 laplacian(vec2 c) {
    vec2 texelSize = 1.0 / vec2(textureSize(uData0, 0));

    vec2 rg = c;
    vec2 rgT  = texture(uData0, vUv + texelSize * vec2( 0,  1)).rg;
    vec2 rgB  = texture(uData0, vUv + texelSize * vec2( 0, -1)).rg;
    vec2 rgL  = texture(uData0, vUv + texelSize * vec2(-1,  0)).rg;
    vec2 rgR  = texture(uData0, vUv + texelSize * vec2( 1,  0)).rg;
    vec2 rgTR = texture(uData0, vUv + texelSize * vec2( 1,  1)).rg;
    vec2 rgBR = texture(uData0, vUv + texelSize * vec2( 1, -1)).rg;
    vec2 rgBL = texture(uData0, vUv + texelSize * vec2(-1, -1)).rg;
    vec2 rgTL = texture(uData0, vUv + texelSize * vec2(-1,  1)).rg;

    return vec2(
        (rgTL + rgTR + rgBL + rgBR) * 0.05 +
        (rgT + rgB + rgL + rgR) * 0.2 +
        (rg) * -1.0
    );
}

void main() {
    // Concentration (c)
    vec4 c = texture(uData0, vUv);

    // Gray-Scott model
    // https://en.wikipedia.org/wiki/Reaction%E2%80%93diffusion_system
    {
        float U = float(c.r);
        float V = float(c.g);

        // Calculate Laplacian for chemicals
        vec2 lap = laplacian(c.rg);
    
        // Gray-Scott equations
        float uvv = U * V * V;
        float dU = uDiffusionRateU * lap.x - uvv + uFeedRate * (1.0 - U);
        float dV = uDiffusionRateV * lap.y + uvv - (uFeedRate + uKillRate) * V;

        // Update U and V with time step
        U += dU * uDeltaTime;
        V += dV * uDeltaTime;

        outColor = vec4(U, V, 0.0, 0.0);
        return;
    }

    outColor = c;
}