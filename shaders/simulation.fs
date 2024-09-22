#version 300 es

precision highp float;
precision lowp usampler2D;

in vec2 vUv;

out uvec4 outColor;

uniform usampler2D uData0;

int byteFromFloat(float f) {
    return int(f * 255.0 + 0.5);
}

ivec2 uintToDir(uint bits) {
    int direction = int(bits & 1u); // 0: horizontal, 1: vertical
    int sign = int((bits >> 1u) & 1u) * 2 - 1;
    int magnitude = int((bits >> 2u)); // Range [0, 63]
    return ivec2(
        direction == 0 ? sign * magnitude : 0,
        direction == 1 ? sign * magnitude : 0
    );
}

uint dirToUint(ivec2 dir) {
    ivec2 absDir = abs(dir);
    int direction = absDir.x > absDir.y ? 0 : 1;
    int sign = int(direction == 0 ? dir.x >= 0 : dir.y >= 0);
    int magnitude = direction == 0 ? absDir.x : absDir.y;
    uint bits = uint(direction) | (uint(sign) << 1) | (uint(magnitude) << 2);
    return bits;
}

void main() {
    ivec2 pixelCoord = ivec2(gl_FragCoord.xy);

    ivec2 pixelMin = ivec2(0, 0);
    ivec2 pixelMax = textureSize(uData0, 0) - ivec2(1, 1);

    ivec2 topPixel = clamp(pixelCoord + ivec2(0, 1), pixelMin, pixelMax);
    ivec2 bottomPixel = clamp(pixelCoord + ivec2(0, -1), pixelMin, pixelMax);
    ivec2 leftPixel = clamp(pixelCoord + ivec2(-1, 0), pixelMin, pixelMax);
    ivec2 rightPixel = clamp(pixelCoord + ivec2(1, 0), pixelMin, pixelMax);

    uvec4 data0 = texelFetch(uData0, pixelCoord, 0);
    uvec4 data0Top = texelFetch(uData0, topPixel, 0);
    uvec4 data0Bottom = texelFetch(uData0, bottomPixel, 0);
    uvec4 data0Left = texelFetch(uData0, leftPixel, 0);
    uvec4 data0Right = texelFetch(uData0, rightPixel, 0);

    ivec2 direction = uintToDir(data0.b);
    ivec2 dirT = uintToDir(data0Top.b);
    ivec2 dirB = uintToDir(data0Bottom.b);
    ivec2 dirL = uintToDir(data0Left.b);
    ivec2 dirR = uintToDir(data0Right.b);
    
    uint population = data0.r;
    // Only consider neighbor populations if they are heading towards us
    uint popT = data0Top.r;
    uint popB = data0Bottom.r;
    uint popL = data0Left.r;
    uint popR = data0Right.r;

    uint faction = data0.g;
    uint factionT = data0Top.g;
    uint factionB = data0Bottom.g;
    uint factionL = data0Left.g;
    uint factionR = data0Right.g;
    uint factions[5] = uint[](faction, factionT, factionB, factionL, factionR);

    uint neighborhood[255];
    neighborhood[factionT] += popT * uint(dirT.y < 0);
    neighborhood[factionB] += popB * uint(dirB.y > 0);
    neighborhood[factionL] += popL * uint(dirL.x > 0);
    neighborhood[factionR] += popR * uint(dirR.x < 0);

    uint numNeighbors = neighborhood[factionT] + neighborhood[factionB] + neighborhood[factionL] + neighborhood[factionR];
    uint numNeighborsHostile = numNeighbors - neighborhood[faction];

    // Population movement
    if(false) {
        // Consider reproduction
        // Turned off for now. TODO: Needs food :)
        /*if (faction > 0) {
            uint neighborhoodPopulation = population + numNeighborsFriendly;
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
            uint winner = 0u;
            uint maxNeighbors = 0u;
            for (int i = 0; i < 5; i++) {
                uint f = factions[i];
                uint n = neighborhood[f];
                if (n > maxNeighbors) {
                    maxNeighbors = n;
                    winner = f;
                }
            }

            if (winner == faction) {
                // Defensive wins
                population -= numNeighborsHostile;
            } else if (winner != 0u) {
                // Offensive wins
                population = neighborhood[winner] * 2u - numNeighbors - population;
            }
            
            faction = winner;
        }

        // Consider outgoing
        {
            uint outgoing = uint(abs(direction.x) + abs(direction.y));
            population -= outgoing;
        }
    }

    // Update direction
    {
        // Enemy neighbors attract
        ivec2 enemyAttraction = ivec2(0, 0);
        if (faction > 0u) {
            if (factionT != faction && popT > 0u) enemyAttraction.y += int(popT);
            if (factionB != faction && popB > 0u) enemyAttraction.y -= int(popB);
            if (factionL != faction && popL > 0u) enemyAttraction.x -= int(popL);
            if (factionR != faction && popR > 0u) enemyAttraction.x += int(popR);
        }

        // Friends follow friends
        ivec2 friendAttraction = ivec2(0, 0);
        if (faction > 0u) {
            if (factionT == faction && (dirT.x != 0 || dirT.y != 0)) friendAttraction += ivec2(0, 1) * int(popT);
            if (factionB == faction && (dirB.x != 0 || dirB.y != 0)) friendAttraction += ivec2(0, -1) * int(popB);
            if (factionL == faction && (dirL.x != 0 || dirL.y != 0)) friendAttraction += ivec2(-1, 0) * int(popL);
            if (factionR == faction && (dirR.x != 0 || dirR.y != 0)) friendAttraction += ivec2(1, 0) * int(popR);
        }

        ivec2 attraction = 2 * sign(enemyAttraction) + sign(friendAttraction);
        bool isHorizontal = abs(attraction.x) > abs(attraction.y);
        attraction = ivec2(
            isHorizontal ? attraction.x : 0,
            isHorizontal ? 0 : attraction.y
        );
        direction = attraction;
    }

    if (population <= 0u) {
        faction = 0u;
        direction = ivec2(0, 0);
    }

    outColor = uvec4(
        population,
        faction,
        dirToUint(direction),
        0u
    );
}