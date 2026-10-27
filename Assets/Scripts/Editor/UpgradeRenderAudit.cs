using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

internal static class UpgradeRenderAudit
{
    [InitializeOnLoadMethod]
    private static void Schedule()
    {
        if (SessionState.GetBool("DuckIt.UpgradeRenderAudit.Completed", false)) return;
        EditorApplication.delayCall += Run;
    }

    [MenuItem("Tools/Diagnostics/Audit Upgrade Rendering")]
    private static void Run()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            EditorApplication.delayCall += Run;
            return;
        }
        SessionState.SetBool("DuckIt.UpgradeRenderAudit.Completed", true);
        var report = new StringBuilder();
        report.AppendLine("Unity: " + Application.unityVersion);
        report.AppendLine("Quality: " + QualitySettings.names[QualitySettings.GetQualityLevel()]);
        report.AppendLine("Pipeline: " + AssetDatabase.GetAssetPath(GraphicsSettings.currentRenderPipeline));
        int total = 0, failed = 0;
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (!(asset is Material mat)) continue;
                total++;
                var shader = mat.shader;
                if (shader == null || shader.name == "Hidden/InternalErrorShader" || !shader.isSupported || ShaderUtil.ShaderHasError(shader))
                {
                    failed++;
                    report.AppendLine("BROKEN: " + path + " | " + mat.name + " | " + (shader ? shader.name : "null"));
                }
            }
        }
        foreach (var guid in AssetDatabase.FindAssets("t:Shader", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (shader == null) continue;
            foreach (var msg in ShaderUtil.GetShaderMessages(shader))
                report.AppendLine("SHADER: " + path + " | " + msg.severity + " | " + msg.message);
        }
        report.AppendLine("Materials: " + total + "; broken: " + failed);
        File.WriteAllText(Path.Combine(Application.dataPath, "../Logs/upgrade-render-audit.txt"), report.ToString());
        Debug.Log("Upgrade rendering audit: " + total + " materials; " + failed + " broken. Report: Logs/upgrade-render-audit.txt");
    }
}
