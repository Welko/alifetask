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
    dir = clamp(dir, ivec2(-63, -63), ivec2(63, 63));
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

    uint validT = uint(pixelCoord.y < pixelMax.y);
    uint validB = uint(pixelCoord.y > pixelMin.y);
    uint validL = uint(pixelCoord.x > pixelMin.x);
    uint validR = uint(pixelCoord.x < pixelMax.x);

    ivec2 topPixel = pixelCoord + ivec2(0, 1);
    ivec2 bottomPixel = pixelCoord + ivec2(0, -1);
    ivec2 leftPixel = pixelCoord + ivec2(-1, 0);
    ivec2 rightPixel = pixelCoord + ivec2(1, 0);

    uvec4 data0 = texelFetch(uData0, pixelCoord, 0);
    uvec4 data0Top = texelFetch(uData0, topPixel, 0) * validT;
    uvec4 data0Bottom = texelFetch(uData0, bottomPixel, 0) * validB;
    uvec4 data0Left = texelFetch(uData0, leftPixel, 0) * validL;
    uvec4 data0Right = texelFetch(uData0, rightPixel, 0) * validR;

    ivec2 direction = uintToDir(data0.b);
    ivec2 dirT = uintToDir(data0Top.b);
    ivec2 dirB = uintToDir(data0Bottom.b);
    ivec2 dirL = uintToDir(data0Left.b);
    ivec2 dirR = uintToDir(data0Right.b);

    int attraction = int(data0.a);
    int attrT = int(data0Top.a);
    int attrB = int(data0Bottom.a);
    int attrL = int(data0Left.a);
    int attrR = int(data0Right.a);
    
    int population = int(data0.r);
    // Only consider neighbor populations if they are heading towards us
    int popT = int(data0Top.r);
    int popB = int(data0Bottom.r);
    int popL = int(data0Left.r);
    int popR = int(data0Right.r);

    uint faction = data0.g;
    uint factionT = data0Top.g;
    uint factionB = data0Bottom.g;
    uint factionL = data0Left.g;
    uint factionR = data0Right.g;
    uint factions[5] = uint[](faction, factionT, factionB, factionL, factionR);

    // A neighbor needs at least 5 population to explore/attack
    // Then it sends 1/5 of its population to explore/attack
    int neighborhood[256];
    neighborhood[factionT] += popT / 5;
    neighborhood[factionB] += popB / 5;
    neighborhood[factionL] += popL / 5;
    neighborhood[factionR] += popR / 5;
    
    int numNeighborsEmpty =
        int(factionT == 0u) +
        int(factionB == 0u) +
        int(factionL == 0u) +
        int(factionR == 0u);

    int numNeighborsHostile =
        int(factionT != faction && popT >= 5) +
        int(factionB != faction && popB >= 5) +
        int(factionL != faction && popL >= 5) +
        int(factionR != faction && popR >= 5);

    int numNeighborsFriendly =
        int(factionT == faction) +
        int(factionB == faction) +
        int(factionL == faction) +
        int(factionR == faction);

    int popNeighbors = 0;
    for (int i = 0; i < 255; i++) {
        popNeighbors += neighborhood[i];
    }

    int popNeighborsHostile = popNeighbors - neighborhood[faction];
    int popNeighborsFriendly = popNeighbors - popNeighborsHostile;

    // Defense
    neighborhood[faction] += population;
    
    // Attraction
    {
        direction +=
            dirT * attrT +
            dirB * attrB +
            dirL * attrL +
            dirR * attrR;
        if (direction.x > 63 || direction.y > 63) {
            direction = ivec2(normalize(vec2(direction)) * 63.0);
        }

        if (faction != 0u) {
            if (factionT != 0u && factionT != faction) direction = ivec2(0, 1);
            if (factionB != 0u && factionB != faction) direction = ivec2(0, -1);
            if (factionL != 0u && factionL != faction) direction = ivec2(-1, 0);
            if (factionR != 0u && factionR != faction) direction = ivec2(1, 0);
        }

        if (faction != 0u && popNeighborsHostile > 0) attraction = 255;
        else attraction = 1;
    }

    // Population movement
    {
        // Consider incoming enemies
        if (popNeighborsHostile > 0) {
            // Check neighborhood and determine the winning faction
            uint winner = 0u;
            int maxNeighbors = 0;
            for (int i = 0; i < 5; i++) {
                uint f = factions[i];
                int n = neighborhood[f];
                if (n > maxNeighbors) {
                    maxNeighbors = n;
                    winner = f;
                }
            }

            if (winner == faction) {
                // Defensive wins
                population -= popNeighborsHostile;
            } else {
                // Offensive wins
                population = neighborhood[winner] * 2 - popNeighbors - population;
            }
            population = max(1, population);
            
            faction = winner;
        }

        // Consider outgoing troops
        if (population >= 5) {
            population -= (population / 5) * (numNeighborsHostile + numNeighborsEmpty);
        }

        // Consider reinforcements
        {
            if (population > 0) {
                //if (direction.x > 0 && (factionR == faction)) population--;
                //if (direction.x < 0 && (factionL == faction)) population--;
                //if (direction.y > 0 && (factionT == faction)) population--;
                //if (direction.y < 0 && (factionB == faction)) population--;
                if (direction.x > 0 && (factionR == faction)) population--;
                if (direction.x < 0 && (factionL == faction)) population--;
                if (direction.y > 0 && (factionT == faction)) population--;
                if (direction.y < 0 && (factionB == faction)) population--;
            }
            //if (dirT.y < 0 && (factionT == faction) && popT > 0) { population++; faction = factionT; };
            //if (dirB.y > 0 && (factionB == faction) && popB > 0) { population++; faction = factionB; };
            //if (dirL.x > 0 && (factionL == faction) && popL > 0) { population++; faction = factionL; };
            //if (dirR.x < 0 && (factionR == faction) && popR > 0) { population++; faction = factionR; };
            if (dirT.y < 0 && (factionT == faction) && popT > 0) { population++; faction = factionT; };
            if (dirB.y > 0 && (factionB == faction) && popB > 0) { population++; faction = factionB; };
            if (dirL.x > 0 && (factionL == faction) && popL > 0) { population++; faction = factionL; };
            if (dirR.x < 0 && (factionR == faction) && popR > 0) { population++; faction = factionR; };
        }
    }

    // Reproduction
    if (faction > 0u && population < 128) {
        //population += max(1, (population + popNeighborsFriendly) / 10);
        //population += 1 + numNeighborsFriendly;
        //population++;
    }

    if (population <= 0) {
        faction = 0u;
        //direction = ivec2(0, 0);
    }

    outColor = uvec4(
        uint(clamp(population, 0, 255)),
        faction,
        dirToUint(direction),
        uint(clamp(attraction, 1, 255))
    );
}