using UnityEditor;
using UnityEngine;

namespace Shanhai.Demo.Editor
{
    internal static class DemoEditorWindowCleanup
    {
        [MenuItem("Shanhai/Demo/Close stale FSR welcome window")]
        private static void CloseWelcome()
        {
            int closed = 0;
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window == null) continue;
                string type = window.GetType().FullName;
                string title = window.titleContent != null ? window.titleContent.text : string.Empty;
                if (type == "FastScriptReload.Editor.FastScriptReloadWelcomeScreen" || title.Contains("FastScriptReloadWelcomeScreen") || title == "Fast Script Reload")
                {
                    try { window.Close(); }
                    catch (System.NullReferenceException) { Object.DestroyImmediate(window); }
                    closed++;
                }
            }
            Debug.Log("[Demo.Editor] 已关闭FSR欢迎窗口：" + closed);
        }
    }
}
