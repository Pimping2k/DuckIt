using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Editor
{
    public class SceneSwitcherWindow : EditorWindow
    {
        private Vector2 scrollPosition;

        [MenuItem("Tools/Scene Switcher")]
        public static void ShowWindow()
        {
            GetWindow<SceneSwitcherWindow>("Scene Switcher");
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Сцены в Build Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // Получаем список сцен из Build Settings
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

            if (scenes.Length == 0)
            {
                EditorGUILayout.HelpBox("В Build Settings не добавлено ни одной сцены.\nПерейдите в File -> Build Settings и добавьте сцены.", MessageType.Warning);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (var scene in scenes)
            {
                // Пропускаем сцены, которые отключены в Build Settings (снята галочка)
                if (!scene.enabled) continue;

                // Загружаем ассет сцены, чтобы получить её имя
                SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);

                if (sceneAsset == null) continue;

                string sceneName = sceneAsset.name;

                // Проверяем, открыта ли эта сцена сейчас
                bool isCurrentScene = SceneManager.GetActiveScene().path == scene.path;

                // Выделяем цвет для активной сцены
                GUI.enabled = !isCurrentScene;

                // Отрисовываем кнопку для каждой сцены
                if (GUILayout.Button(isCurrentScene ? $"{sceneName} (Открыта)" : sceneName, GUILayout.Height(30)))
                {
                    OpenScene(scene.path);
                }

                GUI.enabled = true;
            }

            EditorGUILayout.EndScrollView();
        }

        private void OpenScene(string scenePath)
        {
            // Если текущая сцена измена, предлагаем сохранить её
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(scenePath);
            }
        }
    }
}