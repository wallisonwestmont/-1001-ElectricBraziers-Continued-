#version 450

// Electric Braziers - holographic brazier BASE shader.
// Same look as ElectricBrazier_Hologram (tint + soft scanlines), but the base is hidden
// wherever the flame is drawn: _MaskTex is the flame frame currently shown, and
// _FlameRect maps this quad's texture space onto the flame quad's texture space
// (flameUV = baseUV * _FlameRect.xy + _FlameRect.zw).

layout(location = 0) in vec4 vColor;
layout(location = 1) in vec2 vUv;

layout(location = 0) out vec4 outColor;

layout(set = 2, binding = 0) uniform MaterialUniforms
{
    vec4 _Color; // @color @default = vec4(1.0, 1.0, 1.0, 1.0)
    vec4 _MainTex_ST; // @default = vec4(0.0, 0.0, 1.0, 1.0)
    vec4 _FlameRect; // @default = vec4(1.0, 1.0, 0.0, 0.0)
} uMaterial;

layout(set = 2, binding = 1) uniform sampler2D _MainTex;
layout(set = 2, binding = 2) uniform sampler2D _MaskTex;

void main()
{
    vec2 uv = vUv * uMaterial._MainTex_ST.zw + uMaterial._MainTex_ST.xy;

    vec4 tex = vec4(1.0);
#ifdef USE_TEXTURE
    tex = texture(_MainTex, uv);
#endif

    vec4 color = tex * uMaterial._Color * vColor;

    // Flame coverage at this point (0 outside the flame quad).
    vec2 fuv = uv * uMaterial._FlameRect.xy + uMaterial._FlameRect.zw;
    float inside = step(0.0, fuv.x) * step(fuv.x, 1.0) * step(0.0, fuv.y) * step(fuv.y, 1.0);
    float flame = texture(_MaskTex, clamp(fuv, 0.0, 1.0)).a * inside;

    // Hide the base where the flame is: fully gone under the flame body,
    // fading out through the flame's soft outer glow.
    color.a *= 1.0 - smoothstep(0.02, 0.25, flame);

    if (color.a <= 0.001)
        discard;

    // Soft horizontal scanlines (matches the flame shader).
    float scan = 0.90 + 0.10 * sin(vUv.y * 46.0 * 6.2831853);
    color.rgb *= scan;
    color.a *= mix(1.0, scan, 0.5);

    outColor = color;
}
