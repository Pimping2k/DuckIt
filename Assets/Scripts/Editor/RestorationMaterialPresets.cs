#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Tools > Restoration > Create Material Presets
/// Генерирует набор материалов на базе FlatKit в Assets/Materials/Presets.
/// Можно запускать повторно: существующие материалы обновятся, ссылки на них не сломаются.
/// </summary>
public static class RestorationMaterialPresets
{
    const string OutDir = "Assets/Materials/Presets";
    const string ShaderItem = "FlatKit/Stylized Surface With Outline"; // предметы
    const string ShaderEnv  = "FlatKit/Stylized Surface";             // окружение

    static readonly Color OutlineColor = Hex("#5B3F4A"); // тёплый сливово-коричневый вместо чёрного

    class P
    {
        public string name, hex;
        public float shade = 0.38f;      // насколько тень тянется к лавандовому
        public bool extra;               // второй, более тёмный слой тени
        public float edge = 0.05f;       // мягкость края тени
        public float spec;               // размер блика, 0 = выкл
        public float specSmooth = 0.05f;
        public float rim;                // размер рима, 0 = выкл
        public float rimSmooth = 0.5f;
        public float outline = 2.5f;     // толщина контура в пикселях
        public string glint = "#FFF4E6"; // цвет блика и рима
        public string gradientHex;       // только окружение: цвет градиента у пола
    }

    // ---------- ПРЕДМЕТЫ (Clean + Dirty) ----------
    static readonly P[] Items =
    {
        new P { name = "Ceramic",  hex = "#FFF3E3", shade = 0.38f, edge = 0.04f, spec = 0.18f, specSmooth = 0.05f, rim = 0.45f, rimSmooth = 0.6f },
        new P { name = "Wood",     hex = "#DDA877", shade = 0.40f, edge = 0.07f, extra = true },
        new P { name = "Metal",    hex = "#BDCADB", shade = 0.45f, edge = 0.03f, extra = true, spec = 0.28f, specSmooth = 0.08f, rim = 0.40f, rimSmooth = 0.5f, glint = "#F4F8FF" },
        new P { name = "Fabric",   hex = "#EBA7AE", shade = 0.35f, edge = 0.25f, rim = 0.55f, rimSmooth = 0.9f, outline = 2.0f },
        new P { name = "Plastic",  hex = "#8FCBE6", shade = 0.40f, edge = 0.04f, spec = 0.12f, specSmooth = 0.03f, rim = 0.40f, rimSmooth = 0.5f },
        new P { name = "Paper",    hex = "#F6E6C4", shade = 0.30f, edge = 0.12f, outline = 2.0f },
        // Глянцевая краска: цвет задаётся из кода через RestorationLook.SetTint, лак через SetGloss.
        new P { name = "PaintGlossy", hex = "#FFE9DC", shade = 0.38f, edge = 0.04f, spec = 0.20f, specSmooth = 0.04f, rim = 0.40f, rimSmooth = 0.5f },
    };

    // ---------- ОКРУЖЕНИЕ (без контура) ----------
    static readonly P[] Env =
    {
        new P { name = "Floor",   hex = "#D9A87C", shade = 0.35f, edge = 0.10f },
        new P { name = "Wall",    hex = "#FFE8D1", shade = 0.30f, edge = 0.10f, gradientHex = "#F3C7B6" },
        new P { name = "Bench",   hex = "#B98259", shade = 0.40f, edge = 0.07f, extra = true },
        new P { name = "Accent",  hex = "#CDB9E6", shade = 0.35f, edge = 0.15f },
    };

    [MenuItem("Tools/Restoration/Create Material Presets")]
    public static void CreateAll()
    {
        var itemShader = Shader.Find(ShaderItem);
        var envShader  = Shader.Find(ShaderEnv);
        if (itemShader == null || envShader == null)
        {
            Debug.LogError($"Не найден шейдер. Нужны: '{ShaderItem}' и '{ShaderEnv}'.");
            return;
        }

        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(OutDir)) AssetDatabase.CreateFolder("Assets/Materials", "Presets");

        foreach (var p in Items)
        {
            Build(GetOrCreate($"{OutDir}/Item_{p.name}_Clean.mat", itemShader), p, dirty: false, withOutline: true);
            Build(GetOrCreate($"{OutDir}/Item_{p.name}_Dirty.mat", itemShader), p, dirty: true,  withOutline: true);
        }
        foreach (var p in Env)
            Build(GetOrCreate($"{OutDir}/Env_{p.name}.mat", envShader), p, dirty: false, withOutline: false);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"Готово: материалы созданы в {OutDir}");
    }

    static Material GetOrCreate(string path, Shader shader)
    {
        var m = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (m == null)
        {
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, path);
        }
        else m.shader = shader;
        return m;
    }

    static void Build(Material m, P p, bool dirty, bool withOutline)
    {
        Color baseC = Hex(p.hex);
        if (dirty) baseC = RestorationLook.Dirtify(baseC);

        m.SetColor("_BaseColor", baseC);

        // Cel Shading Mode = Single (без текстур-ступеней, ничего лишнего не нужно)
        m.SetFloat("_CelPrimaryMode", 1);
        m.EnableKeyword("_CELPRIMARYMODE_SINGLE");
        m.SetColor("_ColorDim", RestorationLook.Shade(baseC, p.shade));
        m.SetFloat("_SelfShadingSize", 0.5f);
        m.SetFloat("_ShadowEdgeSize", p.edge);
        m.SetFloat("_Flatness", 1f);

        // Второй слой тени (глубже)
        Toggle(m, "_CelExtraEnabled", "DR_CEL_EXTRA_ON", p.extra);
        if (p.extra)
        {
            m.SetColor("_ColorDimExtra", RestorationLook.Shade(baseC, p.shade + 0.25f));
            m.SetFloat("_SelfShadingSizeExtra", 0.6f);
            m.SetFloat("_ShadowEdgeSizeExtra", p.edge);
            m.SetFloat("_FlatnessExtra", 1f);
        }

        // Блик и рим. Keyword'ы одинаковы для Clean и Dirty, у грязного просто почти погашен цвет:
        // так Material.Lerp между ними работает корректно.
        Color glint = Hex(p.glint);
        if (dirty) glint = Color.Lerp(Color.black, glint, 0.12f);

        Toggle(m, "_SpecularEnabled", "DR_SPECULAR_ON", p.spec > 0f);
        if (p.spec > 0f)
        {
            m.SetColor("_FlatSpecularColor", glint);
            m.SetFloat("_FlatSpecularSize", p.spec);
            m.SetFloat("_FlatSpecularEdgeSmoothness", p.specSmooth);
        }

        Toggle(m, "_RimEnabled", "DR_RIM_ON", p.rim > 0f);
        if (p.rim > 0f)
        {
            m.SetColor("_FlatRimColor", glint);
            m.SetFloat("_FlatRimLightAlign", 0f);
            m.SetFloat("_FlatRimSize", p.rim);
            m.SetFloat("_FlatRimEdgeSmoothness", p.rimSmooth);
        }

        // Градиент от пола (для стен)
        bool grad = !string.IsNullOrEmpty(p.gradientHex);
        Toggle(m, "_GradientEnabled", "DR_GRADIENT_ON", grad);
        if (grad)
        {
            m.SetFloat("_GradientSpace", 0);
            m.EnableKeyword("_GRADIENTSPACE_WORLD");
            m.SetColor("_ColorGradient", Hex(p.gradientHex));
            m.SetFloat("_GradientCenterX", 0f);
            m.SetFloat("_GradientCenterY", 0f);
            m.SetFloat("_GradientSize", 3f);
            m.SetFloat("_GradientAngle", 0f);
        }

        // Тёплый свет слегка окрашивает материал: "лампа" чувствуется
        m.SetFloat("_LightContribution", 0.3f);

        // Реальные тени Unity: цветные (сливовые), а не серые
        m.SetFloat("_UnityShadowMode", 2);
        m.EnableKeyword("_UNITYSHADOWMODE_COLOR");
        m.DisableKeyword("_UNITYSHADOWMODE_MULTIPLY");
        m.SetColor("_UnityShadowColor", RestorationLook.Shade(baseC, p.shade + 0.2f));
        m.SetFloat("_UnityShadowSharpness", 2f);

        if (withOutline)
        {
            m.SetColor("_OutlineColor", OutlineColor);
            m.SetFloat("_OutlineWidth", p.outline); // в пикселях при Camera Distance Impact = 0
            m.SetFloat("_OutlineScale", 1f);
            m.SetFloat("_OutlineDepthOffset", 0f);
            m.SetFloat("_CameraDistanceImpact", 0f);
        }

        EditorUtility.SetDirty(m);
    }

    static void Toggle(Material m, string prop, string keyword, bool on)
    {
        m.SetFloat(prop, on ? 1f : 0f);
        if (on) m.EnableKeyword(keyword); else m.DisableKeyword(keyword);
    }

    static Color Hex(string s)
    {
        ColorUtility.TryParseHtmlString(s, out var c);
        return c;
    }
}
#endif
