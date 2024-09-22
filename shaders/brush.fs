#version 300 es

precision highp float;

in vec2 vUv;

out vec4 outColor;

// Data
uniform sampler2D uData0;

// Brush parameters
uniform int uMode; // 0: draw, 1: erase
uniform vec2 uStart;
uniform vec2 uEnd;
uniform float uRadius;
uniform vec4 uColor;

void main() {
    vec2 pixelCoord = gl_FragCoord.xy;
    float radiusSquared = uRadius * uRadius;

    // Shortest distance from the pixel to the line
    vec2 line = uEnd - uStart;
    float lineLength = length(line);
    vec2 lineDir = lineLength == 0.0 ? vec2(0.0, 0.0) : line/lineLength;
    vec2 startToPixel = pixelCoord - uStart;
    float proj = dot(startToPixel, lineDir);
    vec2 projPoint = uStart + proj * lineDir;
    vec2 projPointToPixel = pixelCoord - projPoint;
    float distToLineSquared = dot(projPointToPixel, projPointToPixel);

    // We consider a line segment
    if (proj < 0.0) {
        distToLineSquared = dot(startToPixel, startToPixel);
    } else if (proj > lineLength) {
        vec2 endToPixel = pixelCoord - uEnd;
        distToLineSquared = dot(endToPixel, endToPixel);
    }
    
    outColor = texelFetch(uData0, ivec2(pixelCoord), 0);

    if (distToLineSquared < radiusSquared) {
        if (uMode == 0) {
            outColor.r += uColor.r / 255.0;
            outColor.g = uColor.g / 255.0;
        } else {
            outColor = vec4(0.0, 0.0, 0.0, 1.0);
        }
    }
}