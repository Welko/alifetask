#version 300 es

precision highp float;

in vec2 vUv;

out vec4 outColor;

//uniform sampler2D uTexture;

void main() {
    //outColor = texture(uTexture, vUv);
    outColor = vec4(vUv, 0.0, 1.0);
    //outColor = vec4(1.0, 0.0, 0.0, 1.0);
}