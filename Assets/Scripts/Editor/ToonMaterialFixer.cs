#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Editor
{
    /// <summary>
    /// Чинит материалы, у которых пропала текстура после применения "плохого" пресета:
    /// Tiling = 0 (текстура схлопывается в один цвет), прозрачный/чёрный Color, текстура лежит в _MainTex, а не в _BaseMap.
    /// Положи файл в Assets/Editor. Меню: Tools > Toon.
    /// </summary>
    public static class ToonMaterialFixer
    {
        static readonly string[] TexProps = { "_BaseMap", "_MainTex" };
        static readonly string[] ColorProps = { "_BaseColor", "_Color" };

        [MenuItem("Tools/Toon/Fix Selected Materials (tiling + color)")]
        static void FixSelected()
        {
            int n = 0;
            foreach (var m in Selection.GetFiltered<Material>(SelectionMode.Assets)) if (Fix(m)) n++;
            AssetDatabase.SaveAssets();
            Debug.Log($"[ToonMaterialFixer] Исправлено материалов: {n}");
        }

        [MenuItem("Tools/Toon/Fix ALL Flat Kit Materials in Project")]
        static void FixAll()
        {
            int n = 0, total = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Material"))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (m == null || m.shader == null || !m.shader.name.Contains("FlatKit")) continue;
                total++; if (Fix(m)) n++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[ToonMaterialFixer] Flat Kit материалов: {total}, исправлено: {n}");
        }

        static bool Fix(Material m)
        {
            bool changed = false;
            foreach (var p in TexProps)
            {
                if (!m.HasProperty(p)) continue;
                Vector2 s = m.GetTextureScale(p);
                if (Mathf.Abs(s.x) < 0.0001f || Mathf.Abs(s.y) < 0.0001f) { m.SetTextureScale(p, Vector2.one); changed = true; }
                Vector2 o = m.GetTextureOffset(p);
                if (float.IsNaN(o.x) || float.IsNaN(o.y)) { m.SetTextureOffset(p, Vector2.zero); changed = true; }
            }
            // если текстура лежит только в _MainTex, а Flat Kit читает _BaseMap
            if (m.HasProperty("_BaseMap") && m.HasProperty("_MainTex") && m.GetTexture("_BaseMap") == null && m.GetTexture("_MainTex") != null)
            { m.SetTexture("_BaseMap", m.GetTexture("_MainTex")); changed = true; }

            foreach (var c in ColorProps)
            {
                if (!m.HasProperty(c)) continue;
                Color col = m.GetColor(c);
                if (col.a < 0.01f || (col.r + col.g + col.b) < 0.05f) { m.SetColor(c, Color.white); changed = true; }
            }
            if (changed) EditorUtility.SetDirty(m);
            return changed;
        }
    }
}
#endif