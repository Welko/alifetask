#version 300 es

precision highp float;

in vec2 vUv;

out vec4 outColor;

uniform int uMode; // 0: draw, 1: erase
uniform vec2 uStart;
uniform vec2 uEnd;
uniform float uRadius;

void main() {
    vec2 pixelCoord = gl_FragCoord.xy;
    float radiusSquared = uRadius * uRadius;

    // Shortest distance from the pixel to the line
    vec2 line = uEnd - uStart;
    float lineLengthSquared = dot(line, line);
    vec2 lineDir = lineLengthSquared == 0.0 ? vec2(0.0, 0.0) : line/sqrt(lineLengthSquared);
    vec2 startToPixel = pixelCoord - uStart;
    float proj = dot(startToPixel, lineDir);
    vec2 projPoint = uStart + proj * lineDir;
    vec2 projPointToPixel = pixelCoord - projPoint;
    float distToLineSquared = dot(projPointToPixel, projPointToPixel);

    if (distToLineSquared < radiusSquared) {
        if (uMode == 0) {
            outColor = vec4(1.0, 1.0, 1.0, 1.0);
        } else {
            outColor = vec4(1.0, 0.0, 0.0, 1.0);
        }
    } else {
        //discard;
        outColor = vec4(0.0, 0.0, 1.0, 1.0);
    }
return;
    vec2 pixelToStart = pixelCoord - uStart;
    float distanceToStartSquared = dot(pixelToStart, pixelToStart);
    if (distanceToStartSquared > radiusSquared) {
        if (pixelCoord.x > uEnd.x) {
            outColor = vec4(0.0, 1.0, 0.0, 1.0);
            return;
        }
        outColor = vec4(0.0, 0.0, 1.0, 1.0);
        return;
    }
    outColor = vec4(1.0, 0.0, 0.0, 1.0);
    return;
}