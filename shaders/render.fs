#version 300 es

precision highp float;
precision lowp usampler2D;

in vec2 vUv;

out vec4 outColor;

uniform usampler2D uData0;

ivec2 uintToDir(uint bits) {
    int direction = int(bits & 1u); // 0: horizontal, 1: vertical
    int sign = int((bits >> 1u) & 1u) * 2 - 1;
    int magnitude = int((bits >> 2u)); // Range [0, 63]
    return ivec2(
        direction == 0 ? sign * magnitude : 0,
        direction == 1 ? sign * magnitude : 0
    );
}

void main() {
    uvec4 data0 = texture(uData0, vUv);

    uint population = data0.r;
    uint faction = data0.g;
    ivec2 dir = uintToDir(data0.b);

    float r = float(population) / 255.0;
    r = r == 0.0 ? 0.0 : mix(0.5, 1.0, r);

    float g = 0.0;

    float b = 0.0;

    float a = 1.0;
    
    outColor = vec4(r, g, b, a);

    // Debug
    if (true) {
        if (population > 0u) {
            //outColor.g = float(dir.x + 63) / 126.0;
            //outColor.b = float(dir.y + 63) / 126.0;
            outColor.g = float(abs(dir.x));
            outColor.b = float(abs(dir.y));
        }
    }

    // Catch errors
    if (population > 0u && faction == 0u) {
        outColor = vec4(1.0, 0.0, 0.0, 1.0);
    } else if (population == 0u && faction > 0u) {
        outColor = vec4(0.0, 1.0, 0.0, 1.0);
    }
}