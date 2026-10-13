using System;
using Game.State;
using Shanhai.Demo.Build;
using Shanhai.Demo.Configuration;
using Shanhai.Demo.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Shanhai.Demo.Editor
{
    /// <summary>从已编辑HUD导出独立资源并迁移引用；完成后再次运行只验证，不覆盖设计资产。</summary>
    public static class DemoUiCompositionAuthoring
    {
        private const string Ui = "Assets/Game/Prefabs/UI/";
        [MenuItem("Shanhai/Demo/Refine split HUD text bounds")]
        public static void RefineDisplayLayout()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("先停止Play Mode并等待编译。");
            foreach (string file in new[] { "Item/DemoHudVitals.prefab", "Item/DemoHudRunInfo.prefab" })
            {
                var root = PrefabUtility.LoadPrefabContents(Ui + file);
                try
                {
                    Undo.RegisterFullObjectHierarchyUndo(root, "修正拆分后的单行文字范围");
                    var summary = root.transform.Find(file.Contains("Vitals") ? "StatusText" : "RunSummary") as RectTransform;
                    summary.sizeDelta = new Vector2(summary.sizeDelta.x, 24);
                    if (file.Contains("Vitals")) ((RectTransform)root.transform.Find("SkillStatusText")).anchoredPosition = new Vector2(270, -66);
                    else
                    {
                        summary.anchoredPosition = new Vector2(18, -40);
                        ((RectTransform)root.transform.Find("CheckpointStatusText")).anchoredPosition = new Vector2(18, -98);
                    }
                    PrefabUtility.SaveAsPrefabAsset(root, Ui + file);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            AssetDatabase.SaveAssets();
        }
        [MenuItem("Shanhai/Demo/Split UI composition")]
        public static void Build()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("先停止Play Mode并等待编译。");
            var previous = SceneManager.GetActiveScene();
            var temporary = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(temporary);
            var hud = PrefabUtility.LoadPrefabContents(Ui + "DemoHud.prefab");
            try
            {
                if (hud.transform.Find("VitalsPanel") != null) { Debug.Log("[Demo.UIAuthoring] 已拆分，不覆盖已编辑资源。"); return; }
                Undo.RegisterFullObjectHierarchyUndo(hud, "拆分Demo UI资源");
                Reward(hud); Completion(hud); Assembly(); Overlay(hud);
                var vitals = FullRect("VitalsPanel", hud.transform);
                Move(hud, "HealthBar", vitals); Move(hud, "SkillStatusText", vitals); Move(hud, "StatusText", vitals);
                var status = vitals.Find("StatusText").GetComponent<TMP_Text>();
                // Split the old two-line status into two independently maintained projections.
                status.text = "生命 100/100 | 能量 0/100";
                var summary = UnityEngine.Object.Instantiate(status, hud.transform); summary.name = "RunSummary";
                var summaryRect = (RectTransform)summary.transform; summaryRect.anchoredPosition += new Vector2(0, -28);
                var v = vitals.gameObject.AddComponent<DemoVitalsView>();
                Set(v, "summary", status); Set(v, "skill", vitals.Find("SkillStatusText").GetComponent<TMP_Text>());
                Set(v, "healthFill", vitals.Find("HealthBar/Fill"));
                var vAsset = Nest(vitals, "Item/DemoHudVitals.prefab").GetComponent<DemoVitalsView>();
                var run = FullRect("RunInfoPanel", hud.transform);
                Move(hud, "CheckpointStatusText", run); Move(hud, "RewardNotice", run); summary.transform.SetParent(run, false);
                var r = run.gameObject.AddComponent<DemoRunInfoView>();
                Set(r, "summary", summary); Set(r, "upgrade", run.Find("CheckpointStatusText").GetComponent<TMP_Text>());
                Set(r, "notice", run.Find("RewardNotice").GetComponent<TMP_Text>());
                var rAsset = Nest(run, "Item/DemoHudRunInfo.prefab").GetComponent<DemoRunInfoView>();
                var marks = hud.transform.Find("MarkHudPanel"); var m = marks.gameObject.AddComponent<DemoLoadoutView>();
                var items = new DemoMarkItem[4]; for (int i = 0; i < 4; i++) items[i] = marks.Find("HudSlot_" + i).GetComponent<DemoMarkItem>();
                SetArray(m, "hudSlots", items); Set(m, "openButton", marks.Find("OpenAssembly").GetComponent<Button>());
                var mAsset = Nest(marks, "Item/DemoHudMarkSummary.prefab").GetComponent<DemoLoadoutView>();
                Remove<DemoLoadoutView>(hud); Remove<DemoRewardChoiceView>(hud); Remove<DemoRewardChoiceController>(hud); Remove<DemoCompletionView>(hud);
                var display = hud.GetComponent<DemoHudController>(); Set(display, "vitals", vAsset); Set(display, "runInfo", rAsset); Set(display, "marks", mAsset);
                Set(hud.GetComponent<DemoHudForm>(), "display", display);
                PrefabUtility.SaveAsPrefabAsset(hud, Ui + "DemoHud.prefab");
                var config = AssetDatabase.LoadAssetAtPath<DemoPresentationConfig>("Assets/Game/ScriptableAssets/Demo/DemoPresentationConfig.asset");
                Set(config, "gameplayConfig", AssetDatabase.LoadAssetAtPath<DemoGameplayConfig>("Assets/Game/DataTable/Demo/DemoGameplayConfig.asset"));
                var fields = new SerializedObject(config);
                fields.FindProperty("combatOverlayPrefabAsset").stringValue = Ui + "DemoCombatOverlay.prefab";
                fields.FindProperty("rewardPrefabAsset").stringValue = Ui + "DemoRewardChoice.prefab";
                fields.FindProperty("assemblyPrefabAsset").stringValue = Ui + "DemoMarkAssembly.prefab";
                fields.FindProperty("completionPrefabAsset").stringValue = Ui + "DemoCompletion.prefab";
                fields.ApplyModifiedProperties(); AssetDatabase.SaveAssets();
                Debug.Log("[Demo.UIAuthoring] HUD三个功能区、表现层和三个独立GF窗口已保存。");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(hud);
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(temporary, true);
            }
        }
        private static GameObject Window(string name)
        {
            var root = FullRect(name, null).gameObject; root.AddComponent<Canvas>();
            var scaler = root.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
            root.AddComponent<GraphicRaycaster>(); return root;
        }
        private static void Reward(GameObject hud)
        {
            var root = Window("DemoRewardChoice");
            try
            {
                var panel = hud.transform.Find("RewardChoicePanel"); panel.SetParent(root.transform, false); panel.gameObject.SetActive(true);
                Remove<GamePauseWindow>(panel.gameObject); root.AddComponent<GamePauseWindow>();
                var view = root.AddComponent<DemoRewardChoiceView>();
                Set(view, "_panel", panel.gameObject); Set(view, "_tooltip", panel.Find("Tooltip").GetComponent<TMP_Text>());
                var buttons = new Button[3]; var labels = new TMP_Text[3]; var icons = new TMP_Text[3]; var images = new Image[3];
                for (int i = 0; i < 3; i++)
                {
                    var card = panel.Find("RewardOption_" + (i + 1)); DemoRewardCardAuthoring.Configure(card);
                    buttons[i] = card.GetComponent<Button>(); labels[i] = card.Find("Label").GetComponent<TMP_Text>();
                    icons[i] = card.Find("Icon").GetComponent<TMP_Text>(); images[i] = card.Find("MarkIcon").GetComponent<Image>();
                }
                SetArray(view, "_buttons", buttons); SetArray(view, "_labels", labels); SetArray(view, "_iconLabels", icons); SetArray(view, "_markIcons", images);
                Set(root.AddComponent<DemoRewardForm>(), "view", view);
                PrefabUtility.SaveAsPrefabAsset(root, Ui + "DemoRewardChoice.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        private static void Completion(GameObject hud)
        {
            var root = Window("DemoCompletion");
            try
            {
                var panel = hud.transform.Find("CompletionPanel"); panel.SetParent(root.transform, false); panel.gameObject.SetActive(true);
                Remove<GamePauseWindow>(panel.gameObject); root.AddComponent<GamePauseWindow>();
                var view = root.AddComponent<DemoCompletionView>(); Set(view, "panel", panel.gameObject);
                Set(view, "retryButton", panel.Find("Retry").GetComponent<Button>()); Set(view, "closeButton", panel.Find("Close").GetComponent<Button>());
                Set(root.AddComponent<DemoCompletionForm>(), "view", view);
                PrefabUtility.SaveAsPrefabAsset(root, Ui + "DemoCompletion.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        private static void Assembly()
        {
            var asset = PrefabUtility.LoadPrefabContents(Ui + "DemoMarkAssembly.prefab");
            try
            {
                var root = Window("DemoMarkAssembly");
                try
                {
                    var content = UnityEngine.Object.Instantiate(asset, root.transform); content.name = "AssemblyPanel"; content.SetActive(true);
                    if (PrefabUtility.IsAnyPrefabInstanceRoot(content))
                        PrefabUtility.UnpackPrefabInstance(content, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    Remove<GamePauseWindow>(content); root.AddComponent<GamePauseWindow>();
                    Set(root.AddComponent<DemoMarkAssemblyForm>(), "view", content.GetComponent<DemoMarkAssemblyView>());
                    PrefabUtility.SaveAsPrefabAsset(root, Ui + "DemoMarkAssembly.prefab");
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            finally { PrefabUtility.UnloadPrefabContents(asset); }
        }
        private static void Overlay(GameObject hud)
        {
            var root = Window("DemoCombatOverlay");
            try
            {
                var damage = hud.transform.Find("DamageLayer"); damage.SetParent(root.transform, false);
                var status = hud.transform.Find("EnemyStatusLayer"); status.SetParent(root.transform, false);
                var form = root.AddComponent<DemoCombatOverlayForm>(); Set(form, "damageLayer", damage); Set(form, "enemyStatusLayer", status);
                foreach (var graphic in root.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
                PrefabUtility.SaveAsPrefabAsset(root, Ui + "DemoCombatOverlay.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        private static Transform Nest(Transform source, string path)
        {
            var parent = source.parent; string name = source.name;
            var prefab = PrefabUtility.SaveAsPrefabAsset(source.gameObject, Ui + path);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent); instance.name = name;
            var from = (RectTransform)source; var to = (RectTransform)instance.transform;
            to.anchorMin = from.anchorMin; to.anchorMax = from.anchorMax; to.pivot = from.pivot;
            to.anchoredPosition = from.anchoredPosition; to.sizeDelta = from.sizeDelta;
            UnityEngine.Object.DestroyImmediate(source.gameObject); return instance.transform;
        }
        private static RectTransform FullRect(string name, Transform parent)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.sizeDelta = Vector2.zero; return rect;
        }
        private static void Move(GameObject source, string name, Transform target) => source.transform.Find(name).SetParent(target, false);
        private static void Remove<T>(GameObject root) where T : Component
        { var component = root.GetComponent<T>(); if (component != null) UnityEngine.Object.DestroyImmediate(component); }
        private static void Set(UnityEngine.Object owner, string name, UnityEngine.Object value)
        { var fields = new SerializedObject(owner); fields.FindProperty(name).objectReferenceValue = value; fields.ApplyModifiedProperties(); }
        private static void SetArray(UnityEngine.Object owner, string name, UnityEngine.Object[] values)
        {
            var fields = new SerializedObject(owner); var array = fields.FindProperty(name); array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i]; fields.ApplyModifiedProperties();
        }
    }
}
