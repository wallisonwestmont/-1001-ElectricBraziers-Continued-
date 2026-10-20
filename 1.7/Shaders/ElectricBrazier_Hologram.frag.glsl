#version 450

// Electric Braziers - holographic brazier shader.
// Paired with the engine's default scene vertex shader (SceneDefault.vert).
// Static: tint + soft scanlines. The flicker comes from the mod picking flame frames on long ticks.

layout(location = 0) in vec4 vColor;
layout(location = 1) in vec2 vUv;

layout(location = 0) out vec4 outColor;

layout(set = 2, binding = 0) uniform MaterialUniforms
{
    vec4 _Color; // @color @default = vec4(1.0, 1.0, 1.0, 1.0)
    vec4 _MainTex_ST; // @default = vec4(0.0, 0.0, 1.0, 1.0)
} uMaterial;

layout(set = 2, binding = 1) uniform sampler2D _MainTex;

void main()
{
    vec4 tex = vec4(1.0);
#ifdef USE_TEXTURE
    tex = texture(_MainTex, vUv * uMaterial._MainTex_ST.zw + uMaterial._MainTex_ST.xy);
#endif

    vec4 color = tex * uMaterial._Color * vColor;
    if (color.a <= 0.001)
        discard;

    // Soft horizontal scanlines.
    float scan = 0.90 + 0.10 * sin(vUv.y * 46.0 * 6.2831853);
    color.rgb *= scan;
    color.a *= mix(1.0, scan, 0.5);

    outColor = color;
}
