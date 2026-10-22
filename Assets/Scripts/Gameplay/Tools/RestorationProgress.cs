using System;
using Unity.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Restorable
{
    /// <summary>Читает маски в маленькую текстуру и считает, сколько износа убрано и сколько покрыто.</summary>
    internal sealed class RestorationProgress
    {
        public const int SmallSize = 64;

        public float CleanedPercent { get; private set; }
        public float PaintCoverage { get; private set; }
        public float VarnishCoverage { get; private set; }
        public float OverPaintRatio { get; private set; }
        public float[] LayerCleaned { get; } = new float[4];

        readonly RenderTexture _small;
        readonly Texture2D _readTex;
        readonly bool[] _surface;
        readonly bool[] _oldOn = new bool[SmallSize * SmallSize];
        readonly int _surfaceCount;
        readonly int[] _now = new int[4];
        readonly int[] _initial = new int[4];
        bool _initialCounted;

        /// <param name="surface">Маска поверхности в полном разрешении (maskSize × maskSize).</param>
        public RestorationProgress(bool[] surface, int maskSize)
        {
            _small = new RenderTexture(SmallSize, SmallSize, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
            _readTex = new Texture2D(SmallSize, SmallSize, TextureFormat.RGBA32, false, true);

            _surface = new bool[SmallSize * SmallSize];
            for (int y = 0; y < SmallSize; y++)
            for (int x = 0; x < SmallSize; x++)
            {
                int sx = Mathf.Min(maskSize - 1, (int)((x + 0.5f) * maskSize / SmallSize));
                int sy = Mathf.Min(maskSize - 1, (int)((y + 0.5f) * maskSize / SmallSize));
                bool on = surface[sy * maskSize + sx];
                _surface[y * SmallSize + x] = on;
                if (on) _surfaceCount++;
            }
        }

        public void Read(RenderTexture wear, RenderTexture paint)
        {
            ReadWear(wear);
            ReadPaint(paint, out int painted, out int varnished, out int overOld);

            if (!_initialCounted)
            {
                Array.Copy(_now, _initial, 4);
                _initialCounted = true;
            }

            float totalInitial = 0f, totalNow = 0f;
            for (int k = 0; k < 4; k++)
            {
                LayerCleaned[k] = _initial[k] > 0 ? Mathf.Clamp01(1f - _now[k] / (float)_initial[k]) : 1f;
                totalInitial += _initial[k];
                totalNow += _now[k];
            }
            CleanedPercent = totalInitial > 0f ? 1f - totalNow / totalInitial : 1f;

            float surface = Mathf.Max(1, _surfaceCount);
            PaintCoverage = painted / surface;
            VarnishCoverage = varnished / surface;
            OverPaintRatio = painted > 0 ? overOld / (float)painted : 0f;
        }

        public void Release()
        {
            if (_small) _small.Release();
            if (_readTex) Object.Destroy(_readTex);
        }

        void ReadWear(RenderTexture wear)
        {
            Array.Clear(_now, 0, 4);
            var data = Sample(wear);
            for (int i = 0; i < data.Length; i++)
            {
                _oldOn[i] = false;
                if (!_surface[i]) continue;
                var c = data[i];
                _now[0] += c.r;
                _now[1] += c.g;
                _now[2] += c.b;
                _now[3] += c.a;
                _oldOn[i] = c.b > 127;
            }
        }

        void ReadPaint(RenderTexture paint, out int painted, out int varnished, out int overOld)
        {
            painted = varnished = overOld = 0;
            var data = Sample(paint);
            for (int i = 0; i < data.Length; i++)
            {
                if (!_surface[i]) continue;
                var c = data[i];
                if (c.r > 127) { painted++; if (_oldOn[i]) overOld++; }
                if (c.g > 127) varnished++;
            }
        }

        NativeArray<Color32> Sample(RenderTexture rt)
        {
            Graphics.Blit(rt, _small);
            var prev = RenderTexture.active;
            RenderTexture.active = _small;
            _readTex.ReadPixels(new Rect(0, 0, SmallSize, SmallSize), 0, 0, false);
            RenderTexture.active = prev;
            return _readTex.GetRawTextureData<Color32>();
        }
    }
}
