#version 300 es

precision highp float;

in vec2 vUv;

out vec4 outColor;

uniform sampler2D uData0;
uniform sampler2D uTransferFunction;

void main() {
    ivec2 pixelCoord = ivec2(gl_FragCoord.xy);
    vec4 data0 = texelFetch(uData0, pixelCoord, 0);

    outColor = texture(uTransferFunction, vec2(data0.g, 0.5));
    return;
    
    float r = data0.r;
    float g = data0.g;
    float b = 0.0;
    float a = 1.0;
    outColor = vec4(r, g, b, a);
}