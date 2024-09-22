#version 300 es

precision highp float;

in vec2 vUv;

out vec4 outColor;

uniform sampler2D uData0;

ivec2 zoToDir(float zo) {
    uint bits = floatBitsToUint(zo);
    return ivec2(
        bits & 0xFu,
        bits >> 4u
    );
}

void main() {
    vec4 data0 = texture(uData0, vUv);

    int population = int(data0.r * 255.0);
    int faction = int(data0.g * 255.0);

    float r = float(population) / 255.0;
    r = r == 0.0 ? 0.0 : mix(0.1, 1.0, r);

    float g = 0.0;

    float b = 0.0;

    float a = 1.0;
    
    outColor = vec4(r, g, b, a);

    // Debug
    if (false) {
        ivec2 dir = zoToDir(data0.b);
        outColor.g = float(dir.x) * 16.0;
        outColor.b = float(dir.y) * 16.0;
    }

    // Catch errors
    if (population > 0 && faction == 0) {
        outColor = vec4(1.0, 0.0, 1.0, 1.0);
    } else if (population == 0 && faction > 0) {
        outColor = vec4(0.0, 1.0, 0.0, 1.0);
    }
}