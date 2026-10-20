#ifndef WEAR_DETAIL_INCLUDED
#define WEAR_DETAIL_INCLUDED

// Реалистичные слои износа: толщина слоя (значение маски) определяет прозрачность и оттенок,
// внутри слоя цвет меняется шумом по позиции на модели (без швов по UV).

struct WearColors
{
    half3 dust;
    half3 dirt;
    half3 oldPaint;
    half3 rust;
    half dustOpacity;
    half detailStrength;
    half texturedCoating;
    half3 substrate;
    half dustRagged;   // рваный пушистый край пыли
    half cleanGlow;    // свечение чистой кромки
};

struct WearNoise
{
    half broad;   // крупные неравномерности (около 8 см)
    half mid;     // средние пятна (около 2.5 см)
    half fine;    // мелочь: оспины ржавчины, крупинки пыли (около 6 мм)
};

float WHash(float3 p)
{
    p = frac(p * 0.3183099 + 0.1);
    p *= 17.0;
    return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
}

float WNoise(float3 x)
{
    float3 i = floor(x);
    float3 f = frac(x);
    f = f * f * (3.0 - 2.0 * f);
    float a = lerp(lerp(WHash(i + float3(0, 0, 0)), WHash(i + float3(1, 0, 0)), f.x),
                   lerp(WHash(i + float3(0, 1, 0)), WHash(i + float3(1, 1, 0)), f.x), f.y);
    float b = lerp(lerp(WHash(i + float3(0, 0, 1)), WHash(i + float3(1, 0, 1)), f.x),
                   lerp(WHash(i + float3(0, 1, 1)), WHash(i + float3(1, 1, 1)), f.x), f.y);
    return lerp(a, b, f.z);
}

float WFbm(float3 p)
{
    float s = 0.0;
    float a = 0.5;
    [unroll] for (int k = 0; k < 4; k++)
    {
        s += a * WNoise(p);
        p = p * 2.03 + 17.1;
        a *= 0.5;
    }
    return s / 0.9375;
}

// detailPos = позиция на модели * Wear Detail Scale (при scale 30 это метры для модели 1 unit = 1 м)
WearNoise WearNoiseAt(float3 detailPos)
{
    float3 p = detailPos / 30.0;
    WearNoise N;
    N.broad = WFbm(p * 12.0);
    N.mid = WNoise(p * 40.0);
    N.fine = WNoise(p * 160.0);
    return N;
}

// Нижние слои: ржавчина и старая краска
half3 LegacyWearBelow(half3 albedo, half4 w, WearNoise N, WearColors C, out half rustA, out half paintA)
{
    half3 decoratedSurface = albedo;
    albedo = lerp(albedo, C.substrate * (1.0h + (N.mid - 0.5h) * 0.08h), C.texturedCoating);
    // Ржавчина: ранняя светлее и жёлтее, толща тёмная, оспины
    half tR = w.a;
    rustA = smoothstep(0.15h, 0.60h, tR);
    half core = smoothstep(0.60h, 1.0h, tR);
    half pit = smoothstep(0.70h, 0.82h, N.fine);
    half3 early = min(C.rust * half3(1.25h, 1.2h, 0.85h), 1.0h);
    half3 rcol = lerp(early, C.rust, smoothstep(0.2h, 0.6h, tR));
    rcol = lerp(rcol, C.rust * 0.45h, saturate(core * 0.8h + pit * 0.5h + (N.broad - 0.5h) * 0.5h));
    rcol *= 1.0h + (N.mid - 0.5h) * 0.25h;
    albedo = lerp(albedo, rcol, rustA);

    // Старая краска: слой с отколотыми кусками, под ним виден материал
    half tP = w.b;
    half aa = max(fwidth(tP) * 0.5h, 0.025h);
    paintA = smoothstep(0.5h - aa, 0.5h + aa, tP);
    half3 pcol = C.oldPaint * (1.0h + (N.broad - 0.5h) * 0.10h);
    pcol = lerp(pcol, decoratedSurface * (0.95h + N.broad * 0.06h), C.texturedCoating);
    // A restrained contact shadow, without a bright/dark graphic contour.
    half edge = (1.0h - abs(tP * 2.0h - 1.0h));
    albedo *= 1.0h - edge * edge * 0.045h;
    albedo = lerp(albedo, pcol, paintA);
    return albedo;
}

// Верхние слои: грязь и пыль
half3 LegacyWearAbove(half3 albedo, half4 w, WearNoise N, WearColors C, out half dirtA, out half dustA)
{
    // Thin dirt stains the material; only a thick deposit hides its pattern.
    dirtA = saturate(w.g) * (0.78h + N.mid * 0.22h);
    half3 stain = albedo * lerp(half3(1, 1, 1), C.dirt * 0.85h, 0.65h);
    half3 dcol = lerp(stain, C.dirt * (0.75h + N.broad * 0.18h), smoothstep(0.65h, 1.0h, w.g));
    albedo = lerp(albedo, dcol, dirtA);

    // Пыль в стиле Undusted: плотная матовая серая вуаль, под ней предмет виден приглушённым.
    // Край рваный и пушистый, а у линии очистки вспыхивает чистый цвет.
    half raw = saturate(w.r);
    half rag = ((N.mid - 0.5h) * 0.5h + (N.fine - 0.5h) * 0.3h) * C.dustRagged;
    half thick = smoothstep(0.28h, 0.62h, raw + rag);
    dustA = thick;

    half lum = dot(albedo, half3(0.2126h, 0.7152h, 0.0722h));
    half3 faded = lerp(albedo, lum.xxx, 0.55h);
    half3 veil = C.dust * (0.97h + N.broad * 0.06h);
    veil *= 1.0h + (N.fine - 0.5h) * 0.05h;
    half3 ducol = lerp(faded, veil, 0.72h);
    half3 cleaned = albedo;
    albedo = lerp(albedo, ducol, thick * C.dustOpacity);

    half edge = thick * (1.0h - thick) * 4.0h;
    albedo += cleaned * edge * C.cleanGlow * 0.35h;
    return albedo;
}


// Integrated soft surface: channel values are continuous coverage. Noise changes
// color inside a layer, never the boundary left by a cleaning tool.
half3 WearBelow(half3 albedo, half4 w, WearNoise N, WearColors C, out half rustA, out half paintA)
{
    if (_SoftVisualStyle < 0.5h)
        return LegacyWearBelow(albedo, w, N, C, rustA, paintA);

    half3 decorated = albedo;
    albedo = lerp(albedo, C.substrate, C.texturedCoating);
    rustA = saturate(w.a);
    half3 oxide = C.rust * lerp(0.58h, 0.88h, smoothstep(0.25h, 0.75h, N.broad));
    oxide *= 0.95h + N.mid * 0.10h;
    albedo = lerp(albedo, oxide, rustA);

    paintA = saturate(w.b);
    half3 paint = lerp(C.oldPaint * (0.98h + N.broad * 0.04h), decorated, C.texturedCoating);
    return lerp(albedo, paint, paintA);
}

half3 WearAbove(half3 albedo, half4 w, WearNoise N, WearColors C, out half dirtA, out half dustA)
{
    if (_SoftVisualStyle < 0.5h)
        return LegacyWearAbove(albedo, w, N, C, dirtA, dustA);

    dirtA = saturate(w.g);
    dustA = saturate(w.r);
    half lum = dot(albedo, half3(0.2126h, 0.7152h, 0.0722h));
    half3 muted = lerp(albedo, lum.xxx, _FilmDesaturation * 0.65h);
    half3 dirt = lerp(muted * 0.6h, C.dirt * (0.72h + N.broad * 0.12h), _FilmOpacity);
    albedo = lerp(albedo, dirt, dirtA);

    lum = dot(albedo, half3(0.2126h, 0.7152h, 0.0722h));
    muted = lerp(albedo, lum.xxx, _FilmDesaturation);
    half3 dust = lerp(muted * 0.60h, C.dust * (0.94h + N.broad * 0.08h), _FilmOpacity);
    return lerp(albedo, dust, dustA * C.dustOpacity);
}

#endif
