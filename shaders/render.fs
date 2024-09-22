#version 300 es

precision highp float;

in vec2 vUv;

out vec4 outColor;

uniform sampler2D uPopulation;

void main() {
    vec4 population = texture(uPopulation, vUv);
    
    outColor = vec4(population.r, 0.0, 0.0, 1.0);
}