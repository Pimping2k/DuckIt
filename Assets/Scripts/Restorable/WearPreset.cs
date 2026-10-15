using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// Четыре вида износа. Порядок = порядок слоёв сверху вниз = каналы маски R, G, B, A.
public enum WearKind { Dust = 0, Dirt = 1, OldPaint = 2, Rust = 3 }

[Serializable]
public class WearLayer
{
    [Tooltip("Есть ли этот слой на предмете")]
    public bool enabled;
    public Color color = Color.gray;
    [Tooltip("Как слой ложится на поверхность")]
    public WearPattern pattern = WearPattern.Patchy;
    [Range(0f, 1f), Tooltip("Сколько поверхности покрыто")]
    public float coverage = 1f;
    [Tooltip("Размер деталей: больше значение = мельче")]
    public float noiseScale = 6f;
    [Range(0f, 1f), Tooltip("Больше износа на рёбрах")]
    public float edgeBias;
    [Range(0f, 1f), Tooltip("Больше износа внизу предмета")]
    public float gravityBias;
    [Tooltip("Сплошной слой (то же, что pattern = Uniform)")]
    public bool solid;
    [Range(0.1f, 1f), Tooltip("Чёткость границ отдельных участков")]
    public float sharpness = 0.75f;
    [Range(0f, 1f), Tooltip("Неровность и разный размер сколов")]
    public float breakup = 0.45f;

    public WearLayer CreateVariation(System.Random random, float coverageRange, float scaleRange, float toneRange)
    {
        var layer = (WearLayer)MemberwiseClone();
        float Signed() => (float)random.NextDouble() * 2f - 1f;
        // Preserve deliberately empty/full layers; vary partial coverage only.
        float coverageOffset = Signed() * coverageRange;
        if (coverage > 0f && coverage < 1f)
            layer.coverage = Mathf.Clamp(coverage + coverageOffset, 0.02f, 0.98f);
        layer.noiseScale = Mathf.Max(0.1f, noiseScale * (1f + Signed() * scaleRange));
        layer.color = color * (1f + Signed() * toneRange);
        layer.color.a = color.a;
        return layer;
    }

    public WearLayer() { }
    public WearLayer(bool enabled, Color color, float coverage, float noiseScale,
        WearPattern pattern = WearPattern.Patchy, float edgeBias = 0f, float gravityBias = 0f)
    {
        this.enabled = enabled; this.color = color; this.coverage = coverage; this.noiseScale = noiseScale;
        this.pattern = pattern; this.edgeBias = edgeBias; this.gravityBias = gravityBias;
    }
}

[CreateAssetMenu(menuName = "Restoration/Wear Preset", fileName = "Wear_New")]
public class WearPreset : ScriptableObject
{
    [Tooltip("Нижний слой нельзя тереть, пока над ним лежит верхний")]
    public bool stacked = true;
    [Range(1, 12), Tooltip("Ширина полосы вокруг рёбер в текселях маски")]
    public int edgeWidth = 4;

    [Header("Variation per item (does not modify the preset)")]
    [Range(0f, 0.3f)] public float coverageVariation = 0.12f;
    [Range(0f, 0.5f)] public float scaleVariation = 0.25f;
    [Range(0f, 0.2f)] public float toneVariation = 0.06f;

    [Header("Coating material")]
    [Tooltip("Рисунок исходной текстуры находится на глазури; сколы показывают однотонную основу. Только при включённом oldPaint.")]
    public bool texturedCoating;
    public Color substrateColor = new Color(0.64f, 0.48f, 0.34f, 1f);

    [Header("1 · Пыль (самый верхний)")]
    public WearLayer dust = new WearLayer(true, new Color(0.85f, 0.74f, 0.64f), 1f, 6f, WearPattern.TopFacing);

    [Header("2 · Грязь, плесень, жир")]
    public WearLayer dirt = new WearLayer(false, new Color(0.48f, 0.33f, 0.27f), 0.6f, 7f, WearPattern.Streaks);

    [Header("3 · Старая краска")]
    public WearLayer oldPaint = new WearLayer(false, new Color(0.88f, 0.44f, 0.37f), 0.72f, 7f, WearPattern.Chipped, 0.5f);

    [Header("4 · Ржавчина (самый нижний)")]
    public WearLayer rust = new WearLayer(false, new Color(0.81f, 0.42f, 0.17f), 0.4f, 11f, WearPattern.Patchy, 0.7f, 0.3f);

    public WearLayer Get(WearKind k)
    {
        switch (k)
        {
            case WearKind.Dust: return dust;
            case WearKind.Dirt: return dirt;
            case WearKind.OldPaint: return oldPaint;
            default: return rust;
        }
    }
}

#if UNITY_EDITOR
public static class WearPresetCreator
{
    const string Dir = "Assets/Restoration/WearPresets";

    [MenuItem("Tools/Restoration/Create Wear Presets")]
    static void CreateMissing() => CreateAll(false);

    // Перезаписывает ВСЕ стартовые пресеты (твои правки в них пропадут)
    [MenuItem("Tools/Restoration/Reset Wear Presets To Defaults")]
    static void ResetAll() => CreateAll(true);

    static void CreateAll(bool overwrite)
    {
        EnsureFolder("Assets/Restoration");
        EnsureFolder(Dir);

        var dustC = C(0.85f, 0.74f, 0.64f);
        var dirtC = C(0.48f, 0.33f, 0.27f);
        var paintC = C(0.88f, 0.44f, 0.37f);
        var rustC = C(0.81f, 0.42f, 0.17f);

        Make("Dusty", overwrite, p => S(p.dust, dustC, 1f, 6f, WearPattern.TopFacing));
        Make("Moldy", overwrite, p => S(p.dirt, C(0.37f, 0.42f, 0.29f), 0.7f, 7f, WearPattern.Bottom));
        Make("Grimy", overwrite, p =>
        {
            S(p.dust, dustC, 1f, 6f, WearPattern.TopFacing);
            S(p.dirt, dirtC, 0.6f, 7f, WearPattern.Streaks);
        });
        Make("Rusty", overwrite, p =>
        {
            S(p.dust, dustC, 0.5f, 6f, WearPattern.TopFacing);
            S(p.rust, rustC, 0.4f, 11f, WearPattern.Patchy, 0.7f, 0.3f);
        });
        Make("Pitted", overwrite, p =>
        {
            S(p.dust, dustC, 0.3f, 6f, WearPattern.TopFacing);
            S(p.rust, C(0.5f, 0.25f, 0.14f), 0.55f, 6f, WearPattern.Speckles);
        });
        Make("Peeling", overwrite, p =>
        {
            S(p.dust, dustC, 0.4f, 6f, WearPattern.TopFacing);
            S(p.oldPaint, paintC, 0.72f, 7f, WearPattern.Chipped, 0.5f);
        });
        // Жёсткий: всё сразу
        Make("Neglected", overwrite, p =>
        {
            S(p.dust, dustC, 1f, 6f, WearPattern.TopFacing);
            S(p.dirt, dirtC, 0.5f, 7f, WearPattern.Streaks);
            S(p.oldPaint, paintC, 0.6f, 7f, WearPattern.Chipped, 0.5f);
            S(p.rust, rustC, 0.3f, 11f, WearPattern.Patchy, 0.7f, 0.3f);
        });

        AssetDatabase.SaveAssets();
        Debug.Log("Пресеты износа готовы: " + Dir);
    }

    static Color C(float r, float g, float b) => new Color(r, g, b);

    static void S(WearLayer l, Color c, float cov, float scale, WearPattern pat, float edge = 0f, float grav = 0f)
    {
        l.enabled = true; l.color = c; l.coverage = cov; l.noiseScale = scale;
        l.pattern = pat; l.edgeBias = edge; l.gravityBias = grav;
    }

    static void Make(string name, bool overwrite, Action<WearPreset> setup)
    {
        string path = $"{Dir}/Wear_{name}.asset";
        var existing = AssetDatabase.LoadAssetAtPath<WearPreset>(path);
        if (existing != null && !overwrite) return;

        var fresh = ScriptableObject.CreateInstance<WearPreset>();
        // сначала всё выключаем, потом пресет включает нужные слои
        fresh.dust.enabled = fresh.dirt.enabled = fresh.oldPaint.enabled = fresh.rust.enabled = false;
        setup(fresh);

        if (existing == null) AssetDatabase.CreateAsset(fresh, path);
        else { EditorUtility.CopySerialized(fresh, existing); EditorUtility.SetDirty(existing); }
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }
}
#endif
