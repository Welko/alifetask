#version 300 es

precision highp float;

in vec2 vUv;

out vec4 outColor;

uniform sampler2D uData0;

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

    int faction = int(data0.g * 255.0);
    int factionT = int(data0Top.g * 255.0);
    int factionB = int(data0Bottom.g * 255.0);
    int factionL = int(data0Left.g * 255.0);
    int factionR = int(data0Right.g * 255.0);
    int factions[4] = int[](factionT, factionB, factionL, factionR);

    int neighborhood[255];
    neighborhood[factionT] += popT;
    neighborhood[factionB] += popB;
    neighborhood[factionL] += popL;
    neighborhood[factionR] += popR;

    int numNeighbors = popT + popB + popL + popR;
    int numNeighborsFriendly = neighborhood[faction];
    int numNeighborsHostile = numNeighbors - numNeighborsFriendly;

    // Now consider the defending faction
    int defenseBoost = population;
    neighborhood[faction] += population;

    // Simulation rules
    {
        // Consider reproduction
        if (faction > 0) {
            int neighborhoodPopulation = population + numNeighborsFriendly;
            if (neighborhoodPopulation > 0) {
                //population += max(1, neighborhoodPopulation / 5);
                population += 1;
            }
        }

        // Consider conflict between factions
        {
            // Check neighborhood and determine the winning faction
            int winner = -1;
            int maxNeighbors = 0;
            for (int i = 0; i < 4; i++) {
                int f = factions[i];
                int n = neighborhood[f];
                if (n > maxNeighbors) {
                    maxNeighbors = n;
                    winner = f;
                }
            }

            if (winner == faction) {
                // Defensive wins
                population = max(1, population - numNeighborsHostile);
            } else if (winner != -1) {
                // Offensive wins
                population = 1;
            }
            
            faction = winner;
        }
    }

    outColor = vec4(
        float(population) / 255.0,
        float(faction) / 255.0,
        0.0,
        0.0
    );
}