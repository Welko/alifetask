#version 300 es

precision highp float;
precision mediump usampler2D;

in vec2 vUv;

out uvec4 outColor;

uniform usampler2D uData0;

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

    vec4 data0  = vec4(texelFetch(uData0, pixelCoord, 0)) / 65535.0;
    vec4 data0T = vec4(texelFetch(uData0, topPixel, 0) * validT) / 65535.0;
    vec4 data0B = vec4(texelFetch(uData0, bottomPixel, 0) * validB) / 65535.0;
    vec4 data0L = vec4(texelFetch(uData0, leftPixel, 0) * validL) / 65535.0;
    vec4 data0R = vec4(texelFetch(uData0, rightPixel, 0) * validR) / 65535.0;

    outColor = uvec4(data0) * 0u + uvec4(1u, 0u, 0u, 1u);
}