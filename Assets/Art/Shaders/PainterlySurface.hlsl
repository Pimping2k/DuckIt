#ifndef DUCKIT_PAINTERLY_SURFACE
#define DUCKIT_PAINTERLY_SURFACE

// Object-space fields stay attached when the object is picked up or rotated.
float PHHash(float3 p)
{
    p = frac(p * 0.1031);
    p += dot(p, p.yzx + 33.33);
    return frac((p.x + p.y) * p.z);
}
float PHNoise(float3 p)
{
    float3 a = floor(p), f = frac(p);
    f = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(lerp(PHHash(a), PHHash(a + float3(1,0,0)), f.x),
                     lerp(PHHash(a + float3(0,1,0)), PHHash(a + float3(1,1,0)), f.x), f.y),
                lerp(lerp(PHHash(a + float3(0,0,1)), PHHash(a + float3(1,0,1)), f.x),
                     lerp(PHHash(a + float3(0,1,1)), PHHash(a + 1), f.x), f.y), f.z);
}
half3 PHBrush(half3 color, float3 p)
{
    float scale = max(_BrushScale, 0.1);
    float3 q = p * scale;
    half broad = PHNoise(q);
    // Long overlapping strokes rather than fine grain or screen-space noise.
    half stroke = smoothstep(0.35h, 0.65h, PHNoise(q * float3(0.65, 3.8, 2.1) + 13.7));
    half patches = smoothstep(0.35h, 0.65h, broad);
    half3 tint = lerp(_BrushDark.rgb, _BrushLight.rgb, patches);
    half3 result = color * lerp(half3(1,1,1), tint, _BrushStrength);
    return result * (1.0h + (stroke - 0.5h) * _StrokeStrength);
}
half PHCoverage(half mask, half noise, half threshold, half breakup)
{
    // Breakup vanishes at both endpoints: an erased pixel stays erased.
    half value = mask + (noise - 0.5h) * breakup * (4.0h * mask * (1.0h - mask));
    half aa = max(fwidth(value), 0.015h);
    return smoothstep(threshold - aa, threshold + aa, value);
}
struct WearColors
{
    half3 dust, dirt, oldPaint, rust, substrate;
    half dustOpacity, detailStrength, texturedCoating, dustRagged, cleanGlow;
};
struct WearNoise { half broad, mid, fine; };
WearNoise WearNoiseAt(float3 p)
{
    WearNoise n;
    p *= 0.0333333;
    n.broad = PHNoise(p * 9.0);
    n.mid = PHNoise(p * float3(17, 35, 22) + 7.3);
    n.fine = PHNoise(p * 130 + 21.1);
    return n;
}
half3 WearBelow(half3 base, half4 w, WearNoise n, WearColors c, out half rustA, out half paintA)
{
    half3 decorated = base;
    base = lerp(base, c.substrate * (0.96h + n.broad * 0.08h), c.texturedCoating);
    rustA = PHCoverage(w.a, n.mid * 0.65h + n.fine * 0.35h, 0.35h, _ChipBreakup);
    half rustPatch = smoothstep(0.34h, 0.65h, n.broad * 0.5h + n.mid * 0.5h);
    half3 rust = lerp(c.rust * half3(0.48,0.44,0.43),
                     c.rust * half3(0.88,0.94,0.86), rustPatch);
    half oxide = smoothstep(0.51h, 0.66h, n.fine);
    rust = lerp(rust, c.rust * half3(0.61,0.49,0.41), oxide * 0.45h);
    half3 cleanBase = base;
    base = lerp(base, rust, rustA);
    // Sparse surviving paint flecks INSIDE a worn region, never outside its mask.
    half flecks = smoothstep(0.62h, 0.73h, n.fine) * smoothstep(0.38h, 0.65h, n.mid);
    base = lerp(base, cleanBase * 0.93h, flecks * rustA * 0.65h);
    paintA = PHCoverage(w.b, n.mid * 0.7h + n.fine * 0.3h, 0.5h, _ChipBreakup);
    half3 paint = lerp(c.oldPaint, decorated, c.texturedCoating);
    paint *= 0.94h + n.broad * 0.12h;
    // A narrow dark contact edge only where a real mask transition exists.
    half edgeWidth = clamp(fwidth(w.b) * 1.8h, 0.015h, 0.07h);
    half edge = 1.0h - smoothstep(edgeWidth, edgeWidth * 2.0h, abs(w.b - 0.5h));
    base *= 1.0h - edge * _ChipShadow;
    return lerp(base, paint, paintA);
}
half3 WearAbove(half3 base, half4 w, WearNoise n, WearColors c, out half dirtA, out half dustA)
{
    dirtA = saturate(w.g) * (0.85h + n.broad * 0.15h);
    half3 dirt = c.dirt * (0.88h + n.mid * 0.18h);
    base = lerp(base, dirt, dirtA * 0.88h);
    dustA = PHCoverage(w.r, n.mid, 0.43h, c.dustRagged * 0.35h);
    half3 dust = lerp(base, c.dust * (0.94h + n.broad * 0.10h), 0.78h);
    return lerp(base, dust, dustA * c.dustOpacity);
}
#endif
