using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(menuName = "Restoration/Wear Preset", fileName = "Wear_New")]
public class WearPreset : ScriptableObject
{
    [Header("Мягкий слой: убирает тряпка/кисть (канал R)")]
    public Color softColor = new Color(0.62f, 0.56f, 0.50f);
    [Range(0f, 1f)] public float softCoverage = 1f;
    public float softNoiseScale = 6f;

    [Header("Жёсткий слой: нужен скребок/шкурка (канал G)")]
    public Color hardColor = new Color(0.66f, 0.31f, 0.16f);
    [Range(0f, 1f)] public float hardCoverage = 0f;
    public float hardNoiseScale = 9f;
    public bool hardSolid;   // true = сплошной слой, false = пятнами
}

#if UNITY_EDITOR
public static class WearPresetCreator
{
    const string Dir = "Assets/Restoration/WearPresets";

    [MenuItem("Tools/Restoration/Create Wear Presets")]
    static void CreateAll()
    {
        EnsureFolder("Assets/Restoration");
        EnsureFolder(Dir);

        Make("Dusty",
            new Color(0.62f, 0.56f, 0.50f), 1.0f, 6f,
            Color.black, 0f, 9f, false);

        Make("Moldy",
            new Color(0.37f, 0.42f, 0.29f), 0.7f, 7f,
            Color.black, 0f, 9f, false);

        Make("Rusty",
            new Color(0.55f, 0.50f, 0.45f), 0.35f, 5f,
            new Color(0.66f, 0.31f, 0.16f), 0.6f, 10f, false);

        Make("Peeling",
            new Color(0.62f, 0.56f, 0.50f), 0.5f, 6f,
            new Color(0.78f, 0.47f, 0.42f), 0.85f, 4f, false);

        AssetDatabase.SaveAssets();
        Debug.Log("Пресеты износа созданы в " + Dir);
    }

    static void Make(string name, Color soft, float softCov, float softScale,
                     Color hard, float hardCov, float hardScale, bool solid)
    {
        string path = $"{Dir}/Wear_{name}.asset";
        var p = AssetDatabase.LoadAssetAtPath<WearPreset>(path);
        if (p == null)
        {
            p = ScriptableObject.CreateInstance<WearPreset>();
            AssetDatabase.CreateAsset(p, path);
        }
        p.softColor = soft;  p.softCoverage = softCov;  p.softNoiseScale = softScale;
        p.hardColor = hard;  p.hardCoverage = hardCov;  p.hardNoiseScale = hardScale;
        p.hardSolid = solid;
        EditorUtility.SetDirty(p);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
    }
}
#endif