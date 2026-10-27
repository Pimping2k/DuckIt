using System;
using UnityEngine;

namespace Gameplay.Tools
{
    public enum RestoreTool
    {
        None,
        Clean,    // сухая кисть: пыль
        Wash,     // губка: пыль, грязь
        Scrape,   // скребок: старая краска
        Sand,     // шкурка: старая краска, ржавчина
        Peel,     // пинцет: отходящие края старой краски
        Paint,
        Varnish
    }

    /// <summary>Форма и сила кисти. Сила задаётся в секунду, радиус — доля наибольшего размера предмета (круг на поверхности).</summary>
    [Serializable]
    public struct Brush
    {
        [Range(0.005f, 0.2f)] public float radius;
        [Min(0f)] public float strength;
        [Range(0f, 0.99f)] public float hardness;

        public Brush(float radius, float strength, float hardness)
        {
            this.radius = radius;
            this.strength = strength;
            this.hardness = hardness;
        }

        public static Brush Default => new Brush(0.04f, 2.5f, 0.6f);
    }

    /// <summary>
    /// Что инструмент делает с поверхностью. Единственное место, где описаны эффекты.
    /// wearSub: сколько снимается каждого слоя (пыль, грязь, старая краска, ржавчина).
    /// paintAdd: что наносится (новая краска, лак).
    /// gateByWear: не наносить там, где виден износ.
    /// wetAdd: сколько влаги остаётся (губка ~3).
    /// needWet: какие слои снимаются только по мокрому.
    /// peel: брать только отходящие края.
    /// forcedHardness: если больше 0, перекрывает жёсткость кисти.
    /// </summary>
    [Serializable]
    public struct SurfaceEffect
    {
        public Vector4 wearSub;
        public Vector2 paintAdd;
        public bool gateByWear;
        public float wetAdd;
        public Vector4 needWet;
        public bool peel;
        public float forcedHardness;

        public bool AddsCoating => paintAdd != Vector2.zero || wetAdd > 0f;
        public bool RemovesWear => wearSub != Vector4.zero;
        public bool IsEmpty => !AddsCoating && !RemovesWear;

        public static SurfaceEffect Of(RestoreTool tool)
        {
            switch (tool)
            {
                case RestoreTool.Clean:
                    return new SurfaceEffect { wearSub = new Vector4(1, 0.15f, 0, 0) };
                case RestoreTool.Wash:
                    return new SurfaceEffect { wearSub = new Vector4(1, 1, 0, 0), needWet = new Vector4(0, 1, 0, 0), wetAdd = 3f };
                case RestoreTool.Scrape:
                    return new SurfaceEffect { wearSub = new Vector4(0, 0.4f, 1, 0.2f) };
                case RestoreTool.Sand:
                    return new SurfaceEffect { wearSub = new Vector4(0, 0.3f, 0.6f, 1) };
                case RestoreTool.Peel:
                    return new SurfaceEffect { wearSub = new Vector4(0, 0, 1, 0), peel = true, forcedHardness = 0.9f };
                case RestoreTool.Paint:
                    return new SurfaceEffect { paintAdd = new Vector2(1, 0), gateByWear = true };
                case RestoreTool.Varnish:
                    return new SurfaceEffect { paintAdd = new Vector2(0, 1), gateByWear = true };
                default:
                    return default;
            }
        }
    }
}