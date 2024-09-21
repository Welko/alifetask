#version 300 es

out vec2 vUv;

vec2[] quad = vec2[](
    vec2(0.0, 0.0),
    vec2(1.0, 0.0),
    vec2(0.0, 1.0),
    vec2(1.0, 1.0)
);

void main() {
    vUv = quad[gl_VertexID];
    gl_Position = vec4(quad[gl_VertexID] * 2.0 - 1.0, 0.0, 1.0);
}