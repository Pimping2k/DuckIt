using System;
using UnityEngine;

namespace Gameplay.Tools
{
    /// <summary>
    /// Растеризует меш в UV-пространство и запоминает для каждого тексела позицию на модели,
    /// направление, высоту и близость к рёбрам. По этим данным пресет раскладывает износ.
    /// </summary>
    internal static class SurfaceDataBuilder
    {
        /// <summary>Пространство позиций: (локальная точка - min) / maxDim. Одинаково для текстуры позиций и центра кисти.</summary>
        public static void PositionSpace(Mesh mesh, out Vector3 min, out float maxDim)
        {
            var b = mesh.bounds;
            min = b.min;
            maxDim = Mathf.Max(b.size.x, Mathf.Max(b.size.y, b.size.z));
            if (maxDim < 1e-5f) maxDim = 1f;
        }

        /// <summary>Текстура позиций: xyz = позиция тексела на модели, w = 1 там, где позиция известна (поверхность и шов UV).</summary>
        public static Color[] BuildPositionMap(SurfaceData d, int size)
        {
            var px = new Color[size * size];
            for (int i = 0; i < px.Length; i++)
                if (d.on[i]) px[i] = new Color(d.pos[i].x, d.pos[i].y, d.pos[i].z, 1f);
            PadGutter(px, d.on, size);
            return px;
        }

        public static SurfaceData Build(Mesh mesh, Transform meshTransform, int size, bool useUV0, int edgeWidth)
        {
            var d = new SurfaceData(size);

            if (mesh == null || !mesh.isReadable)
            {
                Debug.LogWarning("RestorableItem: у меша выключен Read/Write, паттерны износа будут упрощены.");
                FillFallback(d, size);
                return d;
            }

            var nrm = Rasterize(d, mesh, meshTransform, size, useUV0);
            MeasureEdges(d, nrm, size);
            WidenEdges(d, size, edgeWidth);
            return d;
        }

        /// <summary>Начальная маска износа (R пыль, G грязь, B старая краска, A ржавчина) с заполненными швами UV.</summary>
        public static Color32[] BuildInitialWear(WearLayer[] layers, Vector3[] offsets, SurfaceData d, int size, bool uniformDust)
        {
            var px = new Color32[size * size];
            bool fullDust = uniformDust && layers[0].enabled && layers[0].coverage > 0f;
            for (int i = 0; i < px.Length; i++)
            {
                if (!d.on[i]) continue;
                px[i] = new Color32(
                    fullDust ? (byte)255 : WearPatterns.Sample(layers[0], d, i, offsets[0]),
                    WearPatterns.Sample(layers[1], d, i, offsets[1]),
                    WearPatterns.Sample(layers[2], d, i, offsets[2]),
                    WearPatterns.Sample(layers[3], d, i, offsets[3]));
            }
            PadGutter(px, d.on, size);
            return px;
        }

        static void FillFallback(SurfaceData d, int size)
        {
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int i = y * size + x;
                d.on[i] = true;
                d.pos[i] = new Vector3(x / (float)size, y / (float)size, 0f);
                d.up[i] = 0.5f;
                d.height[i] = y / (float)size;
            }
        }

        static Vector3[] Rasterize(SurfaceData d, Mesh mesh, Transform tf, int size, bool useUV0)
        {
            var uv = useUV0 ? mesh.uv : mesh.uv2;
            var verts = mesh.vertices;
            var norms = mesh.normals;
            var tris = mesh.triangles;
            bool hasN = norms != null && norms.Length == verts.Length;
            var b = mesh.bounds;
            PositionSpace(mesh, out _, out float maxDim);
            d.center = b.size / (maxDim * 2f);
            float hy = Mathf.Max(b.size.y, 1e-5f);
            var nrm = new Vector3[size * size];
            var normalMatrix = tf.worldToLocalMatrix.transpose;

            for (int t = 0; t < tris.Length; t += 3)
            {
                int i0 = tris[t], i1 = tris[t + 1], i2 = tris[t + 2];
                Vector2 a = uv[i0] * size, bb = uv[i1] * size, c = uv[i2] * size;

                int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, bb.x, c.x)));
                int maxX = Mathf.Min(size - 1, Mathf.CeilToInt(Mathf.Max(a.x, bb.x, c.x)));
                int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, bb.y, c.y)));
                int maxY = Mathf.Min(size - 1, Mathf.CeilToInt(Mathf.Max(a.y, bb.y, c.y)));

                float den = (bb.y - c.y) * (a.x - c.x) + (c.x - bb.x) * (a.y - c.y);
                if (Mathf.Abs(den) < 1e-8f) continue;

                for (int y = minY; y <= maxY; y++)
                for (int x = minX; x <= maxX; x++)
                {
                    float px = x + 0.5f, py = y + 0.5f;
                    float w1 = ((bb.y - c.y) * (px - c.x) + (c.x - bb.x) * (py - c.y)) / den;
                    float w2 = ((c.y - a.y) * (px - c.x) + (a.x - c.x) * (py - c.y)) / den;
                    float w3 = 1f - w1 - w2;
                    if (w1 < -1e-5f || w2 < -1e-5f || w3 < -1e-5f) continue;

                    int idx = y * size + x;
                    Vector3 p = verts[i0] * w1 + verts[i1] * w2 + verts[i2] * w3;
                    Vector3 n = hasN ? (norms[i0] * w1 + norms[i1] * w2 + norms[i2] * w3).normalized : Vector3.up;

                    d.on[idx] = true;
                    d.pos[idx] = (p - b.min) / maxDim;
                    d.height[idx] = Mathf.Clamp01((p.y - b.min.y) / hy);
                    nrm[idx] = n;
                    d.up[idx] = Mathf.Clamp01(normalMatrix.MultiplyVector(n).normalized.y);
                }
            }
            return nrm;
        }

        // Рёбра: там, где нормаль резко меняется от тексела к соседу.
        static void MeasureEdges(SurfaceData d, Vector3[] nrm, int size)
        {
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                int i = y * size + x;
                if (!d.on[i]) continue;
                float crease = 0f, curvature = 0f;
                int samples = 0;
                if (x > 0) MeasureNeighbour(d, nrm, i, i - 1, ref crease, ref curvature, ref samples);
                if (x + 1 < size) MeasureNeighbour(d, nrm, i, i + 1, ref crease, ref curvature, ref samples);
                if (y > 0) MeasureNeighbour(d, nrm, i, i - size, ref crease, ref curvature, ref samples);
                if (y + 1 < size) MeasureNeighbour(d, nrm, i, i + size, ref crease, ref curvature, ref samples);
                d.edge[i] = Mathf.Clamp01((crease - 0.08f) * 5f);
                d.cavity[i] = samples > 0 ? Mathf.Clamp01(-curvature / samples * 0.07f) : 0f;
            }
        }

        static void MeasureNeighbour(SurfaceData d, Vector3[] normals, int i, int j,
            ref float crease, ref float curvature, ref int samples)
        {
            if (!d.on[j]) return;
            Vector3 delta = d.pos[j] - d.pos[i];
            float distanceSquared = delta.sqrMagnitude;
            // Соседние пиксели атласа могут принадлежать несвязанным частям развёртки.
            if (distanceSquared < 1e-9f || distanceSquared > 0.0025f) return;
            Vector3 normalDelta = normals[j] - normals[i];
            crease = Mathf.Max(crease, normalDelta.magnitude);
            curvature += Vector3.Dot(normalDelta, delta) / distanceSquared;
            samples++;
        }

        // Расширяем полосу вокруг рёбер.
        static void WidenEdges(SurfaceData d, int size, int edgeWidth)
        {
            var tmp = new float[size * size];
            for (int pass = 0; pass < edgeWidth; pass++)
            {
                Array.Copy(d.edge, tmp, tmp.Length);
                for (int y = 1; y < size - 1; y++)
                for (int x = 1; x < size - 1; x++)
                {
                    int i = y * size + x;
                    if (!d.on[i]) continue;
                    d.edge[i] = Mathf.Max(Mathf.Max(tmp[i], tmp[i - 1]),
                        Mathf.Max(tmp[i + 1], Mathf.Max(tmp[i - size], tmp[i + size])));
                }
            }
        }

        // Заполняем только зазор вокруг UV, чтобы билинейная фильтрация не создавала чистые швы.
        static void PadGutter<T>(T[] pixels, bool[] surface, int size)
        {
            var filled = (bool[])surface.Clone();
            var previous = new bool[filled.Length];
            for (int pass = 0; pass < 2; pass++)
            {
                Array.Copy(filled, previous, filled.Length);
                for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    int i = y * size + x;
                    if (previous[i]) continue;
                    int source = x > 0 && previous[i - 1] ? i - 1 :
                        x + 1 < size && previous[i + 1] ? i + 1 :
                        y > 0 && previous[i - size] ? i - size :
                        y + 1 < size && previous[i + size] ? i + size : -1;
                    if (source < 0) continue;
                    pixels[i] = pixels[source];
                    filled[i] = true;
                }
            }
        }
    }
}