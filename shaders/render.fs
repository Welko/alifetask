#version 300 es

precision highp float;

in vec2 vUv;

out vec4 outColor;

uniform sampler2D uData0;
uniform sampler2D uTransferFunction;

uniform vec2 uMouse;
uniform float uLightIntensity;
uniform vec3 uLightColor;

vec2 computeGradient(vec4 data0) {
    ivec2 pixelCoord = ivec2(gl_FragCoord.xy);
    vec2 texelSize = 1.0 / vec2(textureSize(uData0, 0));

    vec4 data0T = texture(uData0, vUv + texelSize * vec2(0.0, 1.0));
    vec4 data0B = texture(uData0, vUv + texelSize * vec2(0.0, -1.0));
    vec4 data0L = texture(uData0, vUv + texelSize * vec2(-1.0, 0.0));
    vec4 data0R = texture(uData0, vUv + texelSize * vec2(1.0, 0.0));

    vec2 gradient = vec2(
        (data0R.g - data0L.g) * 0.5,
        (data0T.g - data0B.g) * 0.5
    );

    return gradient;
}

vec3 illuminate(ivec2 pixelCoord, vec4 data0, vec2 gradient, vec2 lightPos) {
    vec3 pointLightPos = vec3(lightPos, 2.0);
    vec3 pointLightColor = uLightColor * uLightIntensity;

    vec3 surfacePos = vec3(vec2(pixelCoord), data0.g);
    vec3 surfaceNormal = normalize(vec3(-gradient, 1.0));
    
    // Shininess
    float matte = 1.0 - data0.g * data0.g * data0.g;
    
    // Diffuse
    vec3 lightDir = normalize(pointLightPos - surfacePos);
    float diff = max(dot(surfaceNormal, lightDir), 0.0);
    vec3 diffuse = pointLightColor * diff;

    // Specular
    vec3 viewDir = vec3(0.0, 0.0, -1.0);
    vec3 reflectDir = reflect(-lightDir, surfaceNormal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), 10.0);
    vec3 specular = pointLightColor * spec;

    // Attenuation
    float distance = length(pointLightPos - surfacePos);
    float attenuation = 1.0 / (1.0 + 0.1 * distance + 0.01 * distance * distance);

    return (matte * diffuse + (1.0 - matte) * specular);// * attenuation;
}

void main() {
    ivec2 pixelCoord = ivec2(gl_FragCoord.xy);
    vec4 data0 = texelFetch(uData0, pixelCoord, 0);

    outColor = texture(uTransferFunction, vec2(data0.g, 0.5));

    vec2 lightPos = uMouse.x != -1.0 && uMouse.y != -1.0 ? uMouse : vec2(textureSize(uData0, 0)) * 0.5;
    vec2 gradient = computeGradient(data0);
    outColor.rgb += illuminate(pixelCoord, data0, gradient, lightPos);

    return;
    
    float r = data0.r;
    float g = data0.g;
    float b = 0.0;
    float a = 1.0;
    outColor = vec4(r, g, b, a);
}