using System;
using UnityEngine;

/// Как слой износа раскладывается по поверхности
public enum WearPattern
{
    Patchy,     // обычные пятна
    TopFacing,  // оседает на верхних поверхностях (пыль)
    Edges,      // собирается на рёбрах и резких изгибах
    Streaks,    // вертикальные подтёки (грязь)
    Bottom,     // снизу вверх (сырость, плесень)
    Flakes,     // отдельные чешуйки
    Chipped,    // крупные остатки облупившегося покрытия с рваными краями
    Speckles,   // мелкие точки (оспины ржавчины, споры)
    Uniform     // сплошной слой
}

/// Данные поверхности по текселям маски. Строится один раз при старте предмета.
public class SurfaceData
{
    public readonly int size;
    public readonly bool[] on;
    public readonly Vector3[] pos;  // позиция в объектных координатах, делённая на наибольший размер меша
    public readonly float[] up;     // 0..1: насколько поверхность смотрит вверх в мире
    public readonly float[] height; // 0..1: высота по мешу
    public readonly float[] edge;   // 0..1: рёбра и резкие изгибы нормали
    public readonly float[] cavity; // 0..1: вогнутые участки, где задерживаются отложения
    public Vector3 center = new Vector3(0.5f, 0.5f, 0.5f);

    public SurfaceData(int size)
    {
        this.size = size;
        int n = size * size;
        on = new bool[n];
        pos = new Vector3[n];
        up = new float[n];
        height = new float[n];
        edge = new float[n];
        cavity = new float[n];
    }
}

public static class WearPatterns
{
    /// Значение слоя (0..255) для одного текселя маски
    public static byte Sample(WearLayer l, SurfaceData d, int i, Vector3 off)
    {
        if (!l.enabled) return 0;
        if (l.coverage <= 0f) return 0;
        if (l.solid || l.pattern == WearPattern.Uniform) return 255;

        Vector3 p = d.pos[i];
        float s = Mathf.Max(0.1f, l.noiseScale);
        if (l.pattern == WearPattern.TopFacing)
            return Dust(l, d, i, p * s + off);
        // Domain warp breaks up regular contours without introducing UV seams.
        Vector3 q = p * s + off;
        q += new Vector3(Noise3(q * 0.65f + Vector3.one * 19f),
            Noise3(q * 0.65f + Vector3.one * 43f),
            Noise3(q * 0.65f + Vector3.one * 71f)) * (l.breakup * 1.4f);
        float noise = Noise3(q) * 0.72f + Noise3(q * 2.17f + Vector3.one * 13f) * 0.28f;
        float v;

        switch (l.pattern)
        {
            case WearPattern.Flakes:
                return Flakes(l, d, i, q, l.coverage);

            case WearPattern.Chipped:
                return Chipped(l, d, i, q, off);

            case WearPattern.Speckles:
            {
                float sp = Noise3(q * 4f);
                float thr = Mathf.Lerp(0.85f, 0.45f, l.coverage);
                return (byte)(Smooth(sp, thr - 0.08f, thr + 0.08f) * 255f);
            }

            case WearPattern.Edges:
                v = noise * 0.65f + d.edge[i] * 0.55f;
                break;

            case WearPattern.Streaks:
                v = Noise3(new Vector3(q.x, p.y * s * 0.18f + off.y, q.z)) * 0.75f
                    + noise * 0.25f - d.up[i] * 0.12f + (1f - d.height[i]) * 0.12f;
                break;

            case WearPattern.Bottom:
                v = noise * 0.6f + Mathf.Pow(1f - d.height[i], 1.5f) * 0.5f;
                break;

            default:
                v = noise;
                break;
        }

        v += l.edgeBias * d.edge[i] * 0.6f + l.gravityBias * (1f - d.height[i]) * 0.5f;
        return (byte)(Shape(v, l.coverage, l.sharpness) * 255f);
    }

    // Dust is a continuous deposit. Coverage controls its amount, not a hard cutout.
    static byte Dust(WearLayer l, SurfaceData d, int i, Vector3 q)
    {
        float settled = Smooth(d.up[i], 0.08f, 0.8f);
        float sheltered = d.cavity[i];
        float distribution = 0.055f + settled * 0.82f + sheltered * 0.35f;
        float broad = Noise3(q * 0.33f) * 0.75f + Noise3(q * 1.31f) * 0.25f;
        float thickness = l.coverage * distribution * Mathf.Lerp(0.72f, 1.16f, broad);
        return (byte)(Mathf.Clamp01(thickness) * 255f);
    }

    // Most coating survives; losses grow from contact zones and a few coherent impacts.
    // Noise only breaks the boundary, rather than choosing arbitrary surface cells.
    static byte Chipped(WearLayer l, SurfaceData d, int i, Vector3 q, Vector3 off)
    {
        if (l.coverage >= 1f) return 255;
        float h = d.height[i];
        Vector3 radial = d.pos[i] - d.center;
        radial.y = 0f;
        radial.Normalize();
        float impacts = 0f;
        for (int k = 0; k < 3; k++)
        {
            float angle = (off.x * 0.173f + k * 0.381966f) * Mathf.PI * 2f;
            var direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
            float level = 0.18f + Mathf.Repeat(off.y * 0.137f + k * 0.618034f, 1f) * 0.64f;
            float along = (h - level) / (0.12f + k * 0.025f);
            float around = (1f - Vector3.Dot(radial, direction)) / 0.24f;
            float impact = Mathf.Exp(-along * along - around * around);
            impacts = Mathf.Max(impacts, impact * (k == 0 ? 1f : 0.75f));
        }
        float broad = Noise3(q * 0.47f + Vector3.one * 7f);
        float detail = Noise3(q * 2.7f);
        float lip = Smooth(h, 0.80f, 0.99f);
        float foot = 1f - Smooth(h, 0.025f, 0.18f);
        float contact = Mathf.Max(lip, foot * 0.85f) * (0.65f + broad * 0.35f);
        float damage = Mathf.Max(contact * 0.85f, impacts * 0.88f)
            + d.edge[i] * l.edgeBias * 0.22f;
        damage += ((broad - 0.5f) * 0.3f + (detail - 0.5f) * 0.12f) * l.breakup;
        damage += (1f - h) * l.gravityBias * 0.12f;
        float threshold = Mathf.Lerp(0.02f, 0.98f, l.coverage);
        float feather = Mathf.Lerp(0.09f, 0.022f, l.sharpness);
        return (byte)(Smooth(threshold - damage, -feather, feather) * 255f);
    }

    // Чешуйки: ячейки Воронова, часть ячеек "выбрана", внутри выбранной краска слезла
    static byte Flakes(WearLayer l, SurfaceData d, int i, Vector3 q, float cov, bool coating = false)
    {
        int cx = Mathf.FloorToInt(q.x), cy = Mathf.FloorToInt(q.y), cz = Mathf.FloorToInt(q.z);
        if (cov <= 0f) return 0;
        if (cov >= 1f) return 255;
        float f1 = 9f, f2 = 9f, chosen = 1f;

        for (int dz = -1; dz <= 1; dz++)
        for (int dy = -1; dy <= 1; dy++)
        for (int dx = -1; dx <= 1; dx++)
        {
            int x = cx + dx, y = cy + dy, z = cz + dz;
            var fp = new Vector3(x + Hash(x, y, z, 1), y + Hash(x, y, z, 2), z + Hash(x, y, z, 3));
            float dist = (fp - q).sqrMagnitude;
            if (dist < f1) { f2 = f1; f1 = dist; chosen = Hash(x, y, z, 4); }
            else if (dist < f2) f2 = dist;
        }

        // Select whole irregular cells rather than small equal circles around their centres.
        // A continuous boundary also gives the tweezers an exposed edge to grab.
        float chance = Mathf.Clamp01(cov + (coating ? -1f : 1f) * d.edge[i] * l.edgeBias * 0.22f
            + (1f - d.height[i]) * l.gravityBias * 0.12f);
        float selection = 1f - Smooth(chosen, chance - 0.025f, chance + 0.025f);
        float gap = Mathf.Sqrt(f2) - Mathf.Sqrt(f1);
        float feather = Mathf.Lerp(0.10f, 0.018f, l.sharpness);
        float ragged = (Noise3(q * 4.3f) - 0.5f) * l.breakup * 0.13f;
        float inside = Smooth(gap + ragged, 0.0f, feather);
        return (byte)(selection * inside * 255f);
    }

    // ---- Вспомогательные функции ----
    static float Shape(float v, float coverage, float sharpness)
    {
        float t = Mathf.Lerp(0.85f, 0.15f, coverage);
        float feather = Mathf.Lerp(0.22f, 0.035f, sharpness);
        return Smooth(v, t - feather, t + feather);
    }

    static float Smooth(float x, float a, float b)
    {
        float t = Mathf.Clamp01((x - a) / Mathf.Max(1e-5f, b - a));
        return t * t * (3f - 2f * t);
    }

    /// Псевдо-3D шум из трёх срезов Перлина: без швов по UV, всё считается от позиции на модели.
    /// Смещение off должно быть положительным.
    public static float Noise3(Vector3 p)
    {
        float n = (Mathf.PerlinNoise(p.x, p.y) + Mathf.PerlinNoise(p.y, p.z) + Mathf.PerlinNoise(p.z, p.x)) / 3f;
        return Mathf.Clamp01(0.5f + (n - 0.5f) * 1.6f);
    }

    static float Hash(int x, int y, int z, int s)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263 + z * 2147483629 + s * 1274126177;
            h = (h ^ (h >> 13)) * 1274126177;
            h ^= h >> 16;
            return (h & 0x7fffffff) / (float)int.MaxValue;
        }
    }
}
