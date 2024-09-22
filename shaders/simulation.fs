#version 300 es

precision highp float;

in vec2 vUv;

out vec4 outColor;

uniform sampler2D uData0;

int byteFromFloat(float f) {
    return int(f * 255.0 + 0.5);
}

ivec2 zoToDir(float zo) {
    uint bits = floatBitsToUint(zo);
    return ivec2(
        bits & 0xFu,
        bits >> 4u
    );
}

float dirToZo(ivec2 dir) {
    uint bits = 0x00000000u;
    bits |= uint(dir.x);
    bits |= uint(dir.y) << 4u;
    return uintBitsToFloat(bits);
}

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
    
    int population = byteFromFloat(data0.r);
    int popT = byteFromFloat(data0Top.r);
    int popB = byteFromFloat(data0Bottom.r);
    int popL = byteFromFloat(data0Left.r);
    int popR = byteFromFloat(data0Right.r);

    int faction = byteFromFloat(data0.g);
    int factionT = byteFromFloat(data0Top.g);
    int factionB = byteFromFloat(data0Bottom.g);
    int factionL = byteFromFloat(data0Left.g);
    int factionR = byteFromFloat(data0Right.g);
    int factions[5] = int[](faction, factionT, factionB, factionL, factionR);

    ivec2 direction = zoToDir(data0.b);
    ivec2 dirT = zoToDir(data0Top.b);
    ivec2 dirB = zoToDir(data0Bottom.b);
    ivec2 dirL = zoToDir(data0Left.b);
    ivec2 dirR = zoToDir(data0Right.b);

    int neighborhood[255];
    neighborhood[factionT] += popT * int(dirT.y > 0);
    neighborhood[factionB] += popB * int(dirB.y < 0);
    neighborhood[factionL] += popL * int(dirL.x < 0);
    neighborhood[factionR] += popR * int(dirR.x > 0);

    int numNeighbors = neighborhood[factionT] + neighborhood[factionB] + neighborhood[factionL] + neighborhood[factionR];
    int numNeighborsHostile = numNeighbors - neighborhood[faction];

    // Simulation rules
    {
        // Consider reproduction
        // Turned off for now. TODO: Needs food :)
        /*if (faction > 0) {
            int neighborhoodPopulation = population + numNeighborsFriendly;
            if (neighborhoodPopulation > 0) {
                //population += max(1, neighborhoodPopulation / 5);
                population += 1;
            }
        }*/

        // Consider incoming
        {
            neighborhood[faction] += population;
            population = neighborhood[faction]; // Population: consider incoming friends

            // Check neighborhood and determine the winning faction
            int winner = 0;
            int maxNeighbors = 0;
            for (int i = 0; i < 5; i++) {
                int f = factions[i];
                int n = neighborhood[f];
                if (n > maxNeighbors) {
                    maxNeighbors = n;
                    winner = f;
                }
            }

            if (winner == faction) {
                // Defensive wins
                population -= numNeighborsHostile;
            } else if (winner != -1) {
                // Offensive wins
                population = neighborhood[winner] * 2 - numNeighbors - population;
            }
            
            faction = winner;
        }

        // Consider outgoing
        {
            int outgoing = direction.x + direction.y;
            //population -= outgoing;
        }
    }

    if (population <= 0) {
        faction = 0;
        direction = ivec2(0, 0);
    }

    outColor = vec4(
        float(population) / 255.0,
        float(faction) / 255.0,
        dirToZo(direction),
        0.0
    );
}