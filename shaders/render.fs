#version 300 es

precision highp float;

in vec2 vUv;

out vec4 outColor;

uniform sampler2D uData0;

void main() {
    vec4 data0 = texture(uData0, vUv);

    float r = data0.r;
    r = r == 0.0 ? 0.0 : mix(0.1, 1.0, r);

    float g = 0.0;

    float b = 0.0;

    float a = 1.0;
    
    outColor = vec4(r, g, b, a);
}