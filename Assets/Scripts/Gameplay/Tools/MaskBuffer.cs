using UnityEngine;

namespace Restorable
{
    /// <summary>Пара RenderTexture: рисуем из Texture в запасную и меняем местами.</summary>
    internal sealed class MaskBuffer
    {
        RenderTexture _front, _back;

        /// <summary>Актуальная маска.</summary>
        public RenderTexture Texture => _front;

        public MaskBuffer(int size)
        {
            _front = Create(size);
            _back = Create(size);
        }

        public void Blit(Material stamp, int pass)
        {
            Graphics.Blit(_front, _back, stamp, pass);
            (_front, _back) = (_back, _front);
        }

        public void Clear()
        {
            var prev = RenderTexture.active;
            RenderTexture.active = _front;
            GL.Clear(false, true, Color.clear);
            RenderTexture.active = prev;
        }

        public void Release()
        {
            if (_front) _front.Release();
            if (_back) _back.Release();
        }

        static RenderTexture Create(int size)
        {
            // Half-float: 8 бит округляют слабые края мазка до нуля.
            var format = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGBHalf)
                ? RenderTextureFormat.ARGBHalf : RenderTextureFormat.ARGB32;
            var rt = new RenderTexture(size, size, 0, format, RenderTextureReadWrite.Linear)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            rt.Create();
            return rt;
        }
    }
}
