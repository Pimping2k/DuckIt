using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [InitializeOnLoad]
    public class HierarchySeparator
    {
        private static Dictionary<char, Texture2D> gradientCache = new Dictionary<char, Texture2D>();

        static HierarchySeparator()
        {
            EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
        }

        static void OnHierarchyGUI(int instanceID, Rect selectionRect)
        {
            GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

            if (obj != null && obj.name.StartsWith("---"))
            {
                string rawName = obj.name;
                char lastChar = rawName[rawName.Length - 1];
                char colorKey = '0';

                if (lastChar >= '1' && lastChar <= '5')
                {
                    colorKey = lastChar;
                    rawName = rawName.Substring(0, rawName.Length - 1);
                }

                // Смещаем прямоугольник влево, чтобы гарантированно перекрыть иконку кубика
                Rect bgRect = new Rect(selectionRect.x - 28, selectionRect.y, selectionRect.width + 50, selectionRect.height);

                // 1. Рисуем непрозрачный фон цвета редактора Unity (скрываем оригинальный текст)
                EditorGUI.DrawRect(bgRect, new Color(0.22f, 0.22f, 0.22f, 1f));

                // 2. Рисуем наш градиент поверх
                Texture2D glowTex = GetSoftGradientTexture(colorKey);
                if (glowTex != null)
                {
                    GUI.DrawTexture(bgRect, glowTex, ScaleMode.StretchToFill, true);
                }

                // 3. Рисуем наш отформатированный текст по центру
                GUIStyle textStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    normal = new GUIStyleState() { textColor = Color.white }
                };

                string displayName = rawName.Replace("-", "").Trim().ToUpper();
                EditorGUI.LabelField(selectionRect, displayName, textStyle);
            }
        }

        static Texture2D GetSoftGradientTexture(char key)
        {
            if (gradientCache.ContainsKey(key) && gradientCache[key] != null)
            {
                return gradientCache[key];
            }

            Color colorCenter;
        
            // Выбираем только центральный цвет. Края теперь будут просто "растворяться"
            switch (key)
            {
                case '1': colorCenter = new Color(0.15f, 0.35f, 0.7f, 1f); break; // Синий
                case '2': colorCenter = new Color(0.7f, 0.25f, 0.25f, 1f); break; // Красный
                case '3': colorCenter = new Color(0.25f, 0.6f, 0.25f, 1f); break; // Зеленый
                case '4': colorCenter = new Color(0.6f, 0.5f, 0.15f, 1f); break;  // Желтый
                case '5': colorCenter = new Color(0.5f, 0.25f, 0.6f, 1f); break;  // Фиолетовый
                default:  colorCenter = new Color(0.35f, 0.35f, 0.35f, 1f); break; // Темно-серый
            }

            // Цвет краев делаем полностью прозрачным, так как мы уже нарисовали серую подложку под ним
            Color colorEdge = new Color(colorCenter.r, colorCenter.g, colorCenter.b, 0f);

            int width = 128;
            int height = 16;
            Texture2D tex = new Texture2D(width, height);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;
            tex.hideFlags = HideFlags.HideAndDontSave;

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    float u = (float)x / (width - 1);
                
                    // Вычисляем дистанцию только по оси X, чтобы градиент растягивался по всей высоте строки, но затухал по краям
                    float distX = Mathf.Abs(u - 0.5f) * 2f; 

                    // Плавное затухание
                    float alpha = Mathf.SmoothStep(1f, 0f, distX);

                    Color pixelColor = colorCenter;
                    pixelColor.a = alpha;

                    tex.SetPixel(x, y, pixelColor);
                }
            }
        
            tex.Apply();
            gradientCache[key] = tex;
            return tex;
        }
    }
}