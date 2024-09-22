#version 300 es

precision highp float;

in vec2 vUv;

out vec4 outColor;

uniform sampler2D uPopulation;

void main() {
    ivec2 pixelCoord = ivec2(gl_FragCoord.xy);

    ivec2 pixelMin = ivec2(0, 0);
    ivec2 pixelMax = textureSize(uPopulation, 0);

    ivec2 topPixel = clamp(pixelCoord + ivec2(0, 1), pixelMin, pixelMax);
    ivec2 bottomPixel = clamp(pixelCoord + ivec2(0, -1), pixelMin, pixelMax);
    ivec2 leftPixel = clamp(pixelCoord + ivec2(-1, 0), pixelMin, pixelMax);
    ivec2 rightPixel = clamp(pixelCoord + ivec2(1, 0), pixelMin, pixelMax);

    vec4 population = texelFetch(uPopulation, pixelCoord, 0);
    vec4 populationTop = texelFetch(uPopulation, topPixel, 0);
    vec4 populationBottom = texelFetch(uPopulation, bottomPixel, 0);
    vec4 populationLeft = texelFetch(uPopulation, leftPixel, 0);
    vec4 populationRight = texelFetch(uPopulation, rightPixel, 0);

    // Conway's Game of Life rules
    {
        bool isAlive = population.r > 0.0;
        int numLiveNeighbors = int(populationTop.r + populationBottom.r + populationLeft.r + populationRight.r);

        if (isAlive) {
            // Rule 1: Any live cell with fewer than two live neighbours dies, as if by underpopulation.
            if (numLiveNeighbors < 2) {
                outColor = vec4(0.0, 0.0, 0.0, 1.0);
                return;
            }

            // Rule 2: Any live cell with two or three live neighbours lives on to the next generation.
            if (numLiveNeighbors == 2 || numLiveNeighbors == 3) {
                outColor = vec4(1.0, 0.0, 0.0, 1.0);
                return;
            }

            // Rule 3: Any live cell with more than three live neighbours dies, as if by overpopulation.
            if (numLiveNeighbors > 3) {
                outColor = vec4(0.0, 0.0, 0.0, 1.0);
                return;
            }
        } else {
            // Rule 4: Any dead cell with exactly three live neighbours becomes a live cell, as if by reproduction.
            if (numLiveNeighbors == 3) {
                outColor = vec4(1.0, 0.0, 0.0, 1.0);
                return;
            }
        }
    }

    //outColor = texture(uPopulation, vUv);
    //outColor = vec4(vUv, 0.0, 1.0);
    //outColor = vec4(1.0, 0.0, 0.0, 1.0);
}