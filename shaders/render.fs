#version 300 es

precision highp float;

in vec2 vUv;

out vec4 outColor;

uniform sampler2D uData0;

void main() {
    vec4 data0 = texture(uData0, vUv);
    
    outColor = vec4(data0.r, 0.0, 0.0, 1.0);
}