using System;
using UnityEditor;
using UnityEngine;

namespace Restorable
{
    public enum WearKind { Dust = 0, Dirt = 1, OldPaint = 2, Rust = 3 }

    [Serializable]
    public class WearLayer
    {
        [Tooltip("Есть ли этот слой на предмете")]
        public bool enabled;
        public Color color = Color.gray;
        [Range(0f, 1f), Tooltip("Сколько поверхности покрыто (1 = почти всё)")]
        public float coverage = 1f;
        [Tooltip("Размер пятен: меньше = крупные пятна, больше = мелкие")]
        public float noiseScale = 6f;
        [Tooltip("Сплошной слой без пятен (coverage игнорируется)")]
        public bool solid;

        public WearLayer() { }
        public WearLayer(bool enabled, Color color, float coverage, float noiseScale, bool solid = false)
        {
            this.enabled = enabled; this.color = color; this.coverage = coverage;
            this.noiseScale = noiseScale; this.solid = solid;
        }
    }

    [CreateAssetMenu(menuName = "Restoration/Wear Preset", fileName = "Wear_New")]
    public class WearPreset : ScriptableObject
    {
        [Tooltip("Нижний слой нельзя тереть, пока над ним лежит верхний (сначала пыль, потом краска, потом ржавчина)")]
        public bool stacked = true;

        [Header("1 · Пыль (самый верхний)")]
        public WearLayer dust = new WearLayer(true, new Color(0.62f, 0.56f, 0.50f), 1f, 6f);

        [Header("2 · Грязь, плесень, жир")]
        public WearLayer dirt = new WearLayer(false, new Color(0.40f, 0.33f, 0.26f), 0.6f, 7f);

        [Header("3 · Старая краска")]
        public WearLayer oldPaint = new WearLayer(false, new Color(0.78f, 0.47f, 0.42f), 0.85f, 4f);

        [Header("4 · Ржавчина (самый нижний)")]
        public WearLayer rust = new WearLayer(false, new Color(0.66f, 0.31f, 0.16f), 0.6f, 10f);

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

        // Существующие пресеты НЕ перезаписываются: твои настройки в безопасности.
        [MenuItem("Tools/Restoration/Create Wear Presets")]
        static void CreateAll()
        {
            EnsureFolder("Assets/Restoration");
            EnsureFolder(Dir);

            Make("Dusty", p => { S(p.dust, true, C(0.62f, 0.56f, 0.50f), 1f, 6f); });

            Make("Moldy", p => { S(p.dirt, true, C(0.37f, 0.42f, 0.29f), 0.7f, 7f); });

            Make("Rusty", p =>
            {
                S(p.dust, true, C(0.62f, 0.56f, 0.50f), 0.5f, 6f);
                S(p.rust, true, C(0.66f, 0.31f, 0.16f), 0.6f, 10f);
            });

            Make("Peeling", p =>
            {
                S(p.dust, true, C(0.62f, 0.56f, 0.50f), 0.4f, 6f);
                S(p.oldPaint, true, C(0.78f, 0.47f, 0.42f), 0.85f, 4f);
            });

            // Жёсткий: всё сразу
            Make("Neglected", p =>
            {
                S(p.dust, true, C(0.62f, 0.56f, 0.50f), 1f, 6f);
                S(p.dirt, true, C(0.40f, 0.33f, 0.26f), 0.6f, 7f);
                S(p.oldPaint, true, C(0.78f, 0.47f, 0.42f), 0.7f, 4f);
                S(p.rust, true, C(0.66f, 0.31f, 0.16f), 0.45f, 10f);
            });

            AssetDatabase.SaveAssets();
            Debug.Log("Пресеты износа созданы в " + Dir);
        }

        static Color C(float r, float g, float b) => new Color(r, g, b);

        static void S(WearLayer l, bool on, Color c, float cov, float scale)
        {
            l.enabled = on; l.color = c; l.coverage = cov; l.noiseScale = scale;
        }

        static void Make(string name, Action<WearPreset> setup)
        {
            string path = $"{Dir}/Wear_{name}.asset";
            if (AssetDatabase.LoadAssetAtPath<WearPreset>(path) != null) return;

            var p = ScriptableObject.CreateInstance<WearPreset>();
            setup(p);
            AssetDatabase.CreateAsset(p, path);
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
            AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(path));
        }
    }
#endif
}