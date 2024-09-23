#version 300 es

precision highp float;
precision highp usampler2D;

in vec2 vUv;

out vec4 outColor;

uniform usampler2D uData0;

void main() {
    ivec2 pixelCoord = ivec2(gl_FragCoord.xy);
    uvec4 data0 = texelFetch(uData0, pixelCoord, 0);

    float r = float(data0.r) / 65536.0;;
    float g = float(data0.g) / 65536.0;
    float b = 0.0;
    float a = 1.0;
    
    outColor = vec4(r, g, b, a);
}