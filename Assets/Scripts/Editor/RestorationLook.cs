using UnityEngine;

/// <summary>
/// Общие хелперы для материалов FlatKit (Stylized Surface / With Outline).
/// Это рантайм-хук для будущей системы починки: тут же живёт общая логика цвета.
/// ВАЖНО: на предметах используй renderer.material (инстанс), а не sharedMaterial,
/// иначе перекрасишь все предметы с этим материалом сразу.
/// </summary>
public static class RestorationLook
{
    // Тень не серая, а лавандово-сливовая: именно это даёт "ламповость".
    public static readonly Color ShadowTint = new Color(0.557f, 0.424f, 0.604f); // #8E6C9A
    // Пыль/грязь: тёплый приглушённый беж.
    public static readonly Color DustTint = new Color(0.62f, 0.56f, 0.50f);

    static readonly int BaseColorId   = Shader.PropertyToID("_BaseColor");
    static readonly int DimId         = Shader.PropertyToID("_ColorDim");
    static readonly int DimExtraId    = Shader.PropertyToID("_ColorDimExtra");
    static readonly int UShadowColId  = Shader.PropertyToID("_UnityShadowColor");
    static readonly int SpecColorId   = Shader.PropertyToID("_FlatSpecularColor");

    /// Цвет тени, выведенный из базового цвета.
    public static Color Shade(Color c, float amount)
    {
        var r = Color.Lerp(c, ShadowTint, amount);
        r.a = 1f;
        return r;
    }

    /// Состояние "грязный": тусклее, темнее, ближе к пыльному бежу.
    public static Color Dirtify(Color c, float amount = 1f)
    {
        Color.RGBToHSV(c, out float h, out float s, out float v);
        var dull = Color.HSVToRGB(h, s * 0.4f, v * 0.82f);
        dull = Color.Lerp(dull, DustTint, 0.3f);
        return Color.Lerp(c, dull, Mathf.Clamp01(amount));
    }

    /// Этап "покраска": меняет базовый цвет и пересчитывает все тени под него.
    public static void SetTint(Material m, Color tint, float shade = 0.38f, float shadeExtra = 0.6f)
    {
        m.SetColor(BaseColorId, tint);
        m.SetColor(DimId, Shade(tint, shade));
        if (m.HasProperty(DimExtraId))   m.SetColor(DimExtraId, Shade(tint, shadeExtra));
        if (m.HasProperty(UShadowColId)) m.SetColor(UShadowColId, Shade(tint, shadeExtra));
    }

    /// Этап "лак": t = 0 матовый, t = 1 глянцевый. Спекуляр должен быть включён в материале.
    public static void SetGloss(Material m, float t, Color glint)
    {
        m.SetColor(SpecColorId, Color.Lerp(Color.black, glint, Mathf.Clamp01(t)));
    }

    /// Этап "очистка" (заглушка): плавный переход грязный -> чистый.
    /// Работает между материалами ОДНОГО шейдера с одинаковыми keyword'ами (так их и генерирует пресет-скрипт).
    /// Для очистки кистью по маске понадобится свой шейдер.
    public static void BlendDirty(Material target, Material dirty, Material clean, float t)
    {
        target.Lerp(dirty, clean, Mathf.Clamp01(t));
    }
}
