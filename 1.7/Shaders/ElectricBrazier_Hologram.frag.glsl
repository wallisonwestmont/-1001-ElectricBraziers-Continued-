#version 450

// Electric Braziers - holographic brazier shader.
// Paired with the engine's default scene vertex shader (SceneDefault.vert).
// _EBHoloTime / _EBHoloFlicker are shader globals the mod sets every frame:
// the clock only advances, and flicker is only on, while the game runs unpaused at 1x speed.

layout(location = 0) in vec4 vColor;
layout(location = 1) in vec2 vUv;
layout(location = 2) in vec3 vWorldPos;

layout(location = 0) out vec4 outColor;

layout(set = 2, binding = 0) uniform MaterialUniforms
{
    vec4 _Color; // @color @default = vec4(1.0, 1.0, 1.0, 1.0)
    vec4 _MainTex_ST; // @default = vec4(0.0, 0.0, 1.0, 1.0)
    float _EBHoloTime;    // global, set by EB.HoloClock
    float _EBHoloFlicker; // global, set by EB.HoloClock (1 = flicker on)
} uMaterial;

layout(set = 2, binding = 1) uniform sampler2D _MainTex;

float hash11(float p)
{
    p = fract(p * 0.1031);
    p *= p + 33.33;
    p *= p + p;
    return fract(p);
}

// Smooth 1D value noise in [0,1].
float noise1(float x)
{
    float i = floor(x);
    float f = fract(x);
    float u = f * f * (3.0 - 2.0 * f);
    return mix(hash11(i), hash11(i + 1.0), u);
}

void main()
{
    vec4 tex = vec4(1.0);
#ifdef USE_TEXTURE
    tex = texture(_MainTex, vUv * uMaterial._MainTex_ST.zw + uMaterial._MainTex_ST.xy);
#endif

    vec4 color = tex * uMaterial._Color * vColor;
    if (color.a <= 0.001)
        discard;

    float t = uMaterial._EBHoloTime;
    float on = clamp(uMaterial._EBHoloFlicker, 0.0, 1.0);

    // Per-brazier phase so neighbouring braziers don't flicker in lockstep.
    vec2 cell = floor(vWorldPos.xz + 0.5);
    float seed = hash11(dot(cell, vec2(12.9898, 78.233))) * 97.0;

    // Faint, continuous shimmer (about +/-6%) plus rare brief dips (up to ~22%).
    float shimmer = (noise1(t * 9.0 + seed) - 0.5) * 0.12;
    float dipGate = smoothstep(0.82, 0.97, noise1(t * 1.7 + seed * 3.1));
    float dip = dipGate * 0.22 * noise1(t * 31.0 + seed);
    float flicker = 1.0 + on * (shimmer - dip);

    // Soft horizontal scanlines drifting upward. Static (but still visible) when the clock is stopped.
    float scan = 0.90 + 0.10 * sin((vUv.y * 46.0 - t * 1.2) * 6.2831853);

    color.rgb *= scan;
    color.a *= flicker * mix(1.0, scan, 0.5);

    outColor = color;
}
