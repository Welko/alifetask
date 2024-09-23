#version 300 es

precision highp float;

in vec2 vUv;

out vec4 outColor;

uniform sampler2D uData0;

float sampleLightY(sampler2D channel, vec2 fragCoord)
{
    vec2 iResolution = vec2(textureSize(channel, 0));

    float light = 0.0;
#   define S(DX, DY, WEIGHT) light += texture(uData0, (fragCoord+vec2(DX, DY))/iResolution.xy).y*WEIGHT;
    S( 0,  1, -0.2)
    S( 0, -1,  0.2)
    S( 1,  0,  0.2)
    S(-1,  0, -0.2)
#   undef S
    return light;
}

vec3 hsv2rgb(vec3 hsv)
{
    vec3 rgb;
    float h = mod(hsv.x * 6.0, 6.0);
    float q = h-float(int(h));
    if      (h < 1.0) rgb = vec3( 1.0,    q,  0.0);
    else if (h < 2.0) rgb = vec3(1.-q,  1.0,  0.0);
    else if (h < 3.0) rgb = vec3( 0.0,  1.0,    q);
    else if (h < 4.0) rgb = vec3( 0.0, 1.-q,  1.0);
    else if (h < 5.0) rgb = vec3(   q,  0.0,  1.0);
    else if (h < 6.0) rgb = vec3( 1.0,  0.0, 1.-q);
    rgb = hsv.z*(1.0-hsv.y*(1.0-rgb));
    return rgb;
}

vec4 mainImage(vec2 fragCoord)
{
    vec2 iResolution = vec2(textureSize(uData0, 0));

	vec2 uv = fragCoord.xy / iResolution.xy;
    float uValue = texture(uData0, uv).x;
    float light = sampleLightY(uData0, fragCoord)*8.0;
	return vec4(hsv2rgb(vec3(1.0-uValue*0.8-0.16,
                                  light > 0.0 ? 1.-light : 1.,
                                  light > 0.0 ? 1. : 1.+light)), 1.0);
}

void main() {
    ivec2 pixelCoord = ivec2(gl_FragCoord.xy);
    vec4 data0 = texelFetch(uData0, pixelCoord, 0);

    float r = data0.r;
    float g = data0.g;
    float b = 0.0;
    float a = 1.0;

    vec3 color0 = vec3(uvec3(0u, 0u, 0u));
    vec3 color1 = vec3(uvec3(0x00u, 0x5cu, 0x5au)) / 255.0; // #005c5a
    vec3 color2 = vec3(uvec3(0x00u, 0x82u, 0x63u)) / 255.0; // #008263
    vec3 color3 = vec3(uvec3(0x00u, 0xffu, 0x00u)) / 255.0; // #03ff00

    vec3 resultColor;
    if (g < 0.5) {
        resultColor = mix(color0, color1, g * 2.0);
    } else if (g < 0.75) {
        resultColor = mix(color1, color2, (g - 0.5) * 4.0);
    } else {
        resultColor = mix(color2, color3, (g - 0.75) * 4.0);
    }
    outColor = vec4(resultColor, 1.0);
    return;
    
    outColor = vec4(r, g, b, a);

    //outColor = mainImage(gl_FragCoord.xy);
}