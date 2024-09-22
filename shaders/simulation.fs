#version 300 es

precision highp float;

in vec2 vUv;

out vec4 outColor;

uniform sampler2D uData0;

struct Cell {
    int population;
    int faction;
    int numFriends;
    int numEnemies;
};

void main() {
    ivec2 pixelCoord = ivec2(gl_FragCoord.xy);

    ivec2 pixelMin = ivec2(0, 0);
    ivec2 pixelMax = textureSize(uData0, 0);

    ivec2 topPixel = clamp(pixelCoord + ivec2(0, 1), pixelMin, pixelMax);
    ivec2 bottomPixel = clamp(pixelCoord + ivec2(0, -1), pixelMin, pixelMax);
    ivec2 leftPixel = clamp(pixelCoord + ivec2(-1, 0), pixelMin, pixelMax);
    ivec2 rightPixel = clamp(pixelCoord + ivec2(1, 0), pixelMin, pixelMax);

    vec4 data0 = texelFetch(uData0, pixelCoord, 0);
    vec4 data0Top = texelFetch(uData0, topPixel, 0);
    vec4 data0Bottom = texelFetch(uData0, bottomPixel, 0);
    vec4 data0Left = texelFetch(uData0, leftPixel, 0);
    vec4 data0Right = texelFetch(uData0, rightPixel, 0);
    
    int population = int(data0.r * 255.0);
    int popT = int(data0Top.r * 255.0);
    int popB = int(data0Bottom.r * 255.0);
    int popL = int(data0Left.r * 255.0);
    int popR = int(data0Right.r * 255.0);

    Cell cell = Cell(population, 0, popT + popB + popL + popR, 0);

    // Simulation rules
    {
        if (cell.population == 0) {
            
        } else {
            population = 0;
        }
    }

    outColor = vec4(
        float(cell.population) / 255.0,
        0.0,
        0.0,
        0.0
    );
}