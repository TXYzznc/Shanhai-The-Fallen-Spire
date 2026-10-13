using System;
using System.IO;
using System.Collections.Generic;
using Shanhai.Demo.Configuration;
using Shanhai.Demo.Player;
using Shanhai.Demo.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Shanhai.Demo.Editor
{
    internal static class DemoMenuAuthoring
    {
        private const string PrefabPath = "Assets/Game/Prefabs/UI/DemoStartMenu.prefab";
        private const string ConfigPath = "Assets/Game/ScriptableAssets/Demo/DemoPresentationConfig.asset";
        private const string ScenePath = "Assets/Game/Scene/Demo/DemoMenu.unity";

        [MenuItem("Shanhai/Demo/Author start menu")]
        private static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("请先停止Play Mode。");
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath));
            AssetDatabase.Refresh();
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Game/Font/Common/AlibabaPuHuiTi-3-85-Bold SDF.asset");
            if (font == null) throw new InvalidOperationException("菜单字体缺失。");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                var root = new GameObject("DemoStartMenu", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster));
                Undo.RegisterCreatedObjectUndo(root, "创建开始菜单模板");
                try
                {
                    var form = root.AddComponent<DemoStartMenuForm>();
                    RectTransform background = Rect("Background", root.transform, Vector2.zero, Vector2.zero);
                    background.anchorMin = Vector2.zero; background.anchorMax = Vector2.one;
                    background.gameObject.AddComponent<Image>().color = new Color(.035f, .065f, .09f, 1f);
                    Text("Title", root.transform, font, "渡山海", new Vector2(0, 145), new Vector2(600, 110), 64);
                    Button start = Button(root.transform, font, "StartGame", "开始游戏", 10);
                    Button quit = Button(root.transform, font, "Quit", "退出", -75);
                    TMP_Text status = Text("Status", root.transform, font, string.Empty, new Vector2(0, -155), new Vector2(600, 60), 22);
                    var serialized = new SerializedObject(form);
                    serialized.FindProperty("startButton").objectReferenceValue = start;
                    serialized.FindProperty("quitButton").objectReferenceValue = quit;
                    serialized.FindProperty("statusText").objectReferenceValue = status;
                    serialized.ApplyModifiedProperties();
                    PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            var config = AssetDatabase.LoadAssetAtPath<DemoPresentationConfig>(ConfigPath);
            if (config == null) { config = ScriptableObject.CreateInstance<DemoPresentationConfig>(); AssetDatabase.CreateAsset(config, ConfigPath); }
            Undo.RecordObject(config, "设置开始菜单资源");
            var settings = new SerializedObject(config);
            settings.FindProperty("menuPrefabAsset").stringValue = PrefabPath;
            settings.FindProperty("menuSceneAsset").stringValue = ScenePath;
            settings.FindProperty("gameSceneAsset").stringValue = "Assets/Game/Scene/Demo/DemoMain.unity";
            settings.FindProperty("hudPrefabAsset").stringValue = "Assets/Game/Prefabs/UI/DemoHud.prefab";
            settings.FindProperty("playerPrefabAsset").stringValue = "Assets/Game/Prefabs/Entity/DemoPlayer.prefab";
            settings.FindProperty("commonEnemyPrefabAsset").stringValue = "Assets/Game/Prefabs/Entity/DemoCommonEnemy.prefab";
            settings.FindProperty("eliteEnemyPrefabAsset").stringValue = "Assets/Game/Prefabs/Entity/DemoEliteEnemy.prefab";
            settings.FindProperty("bossPrefabAsset").stringValue = "Assets/Game/Prefabs/Entity/DemoBoss.prefab";
            settings.ApplyModifiedProperties();
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                Scene previous = SceneManager.GetActiveScene();
                Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                try
                {
                    var entry = new GameObject("DemoMenuEntry").AddComponent<DemoMenuEntry>();
                    SceneManager.MoveGameObjectToScene(entry.gameObject, scene);
                    var serialized = new SerializedObject(entry);
                    serialized.FindProperty("presentation").objectReferenceValue = config;
                    serialized.ApplyModifiedProperties();
                    EditorSceneManager.SaveScene(scene, ScenePath);
                }
                finally { EditorSceneManager.CloseScene(scene, true); SceneManager.SetActiveScene(previous); }
            }
            AssetDatabase.SaveAssets();
            var buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!buildScenes.Exists(item => item.path == ScenePath))
            {
                buildScenes.Add(new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = buildScenes.ToArray();
            }
            Debug.Log("[Demo.MenuAuthoring] 已创建开始菜单Prefab、配置和轻量入口场景。");
        }

        private static RectTransform Rect(string name, Transform parent, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position; rect.sizeDelta = size;
            return rect;
        }
        private static TMP_Text Text(string name, Transform parent, TMP_FontAsset font, string value, Vector2 position, Vector2 size, int fontSize)
        {
            TMP_Text text = Rect(name, parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font; text.text = value; text.fontSize = fontSize;
            text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false;
            return text;
        }
        private static Button Button(Transform parent, TMP_FontAsset font, string name, string label, float y)
        {
            var template = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/Item/DemoActionButton.prefab");
            if (template == null) throw new InvalidOperationException("按钮Item模板缺失。");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(template, parent);
            go.name = name;
            var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = new Vector2(0, y); rect.sizeDelta = new Vector2(300, 64);
            var text = go.GetComponentInChildren<TMP_Text>(true);
            text.font = font; text.text = label; text.fontSize = 30;
            return go.GetComponent<Button>();
        }
    }
}
