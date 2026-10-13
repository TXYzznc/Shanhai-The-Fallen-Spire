using System;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Enemies;
using Shanhai.Demo.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shanhai.Demo.Editor
{
    internal static class DemoEnemyStatusAuthoring
    {
        private const string ItemPath = "Assets/Game/Prefabs/UI/Item/DemoEnemyStatus.prefab";
        [MenuItem("Shanhai/Demo/Author enemy status bars")]
        private static void Build()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("先停止Play Mode并等待编译。");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ItemPath) == null)
            {
                var root = new GameObject("DemoEnemyStatus", typeof(RectTransform));
                Undo.RegisterCreatedObjectUndo(root, "创建敌人状态Item");
                try
                {
                    var rect = (RectTransform)root.transform;
                    rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.pivot = new Vector2(.5f, 0f); rect.sizeDelta = new Vector2(120, 58);
                    var item = Undo.AddComponent<DemoEnemyStatusItem>(root);
                    var health = Bar(root.transform, "Health", 33, 8, new Color(.85f, .22f, .22f));
                    var stagger = Bar(root.transform, "Stagger", 22, 5, new Color(.25f, .65f, 1f));
                    var charge = Bar(root.transform, "Charge", 0, 4, new Color(1f, .75f, .2f));
                    var hp = Label(root.transform, "HealthText", 41, 16, 15, "60/60");
                    var status = Label(root.transform, "StatusText", 5, 16, 13, "失衡积累");
                    Set(item, "healthFill", health); Set(item, "staggerFill", stagger); Set(item, "staggerRow", stagger.parent.gameObject);
                    Set(item, "chargeFill", charge); Set(item, "chargeRow", charge.parent.gameObject);
                    Set(item, "healthLabel", hp); Set(item, "statusLabel", status);
                    root.SetActive(false); PrefabUtility.SaveAsPrefabAsset(root, ItemPath);
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            string hudPath = "Assets/Game/Prefabs/UI/DemoCombatOverlay.prefab";
            var hud = PrefabUtility.LoadPrefabContents(hudPath);
            try
            {
                Transform layer = hud.transform.Find("EnemyStatusLayer");
                if (layer == null)
                {
                    var go = new GameObject("EnemyStatusLayer", typeof(RectTransform)); Undo.RegisterCreatedObjectUndo(go, "创建敌人状态显示层");
                    go.transform.SetParent(hud.transform, false);
                    var rect = (RectTransform)go.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                    rect.offsetMin = rect.offsetMax = Vector2.zero; go.transform.SetSiblingIndex(0); layer = rect;
                }
                Set(hud.GetComponent<DemoCombatOverlayForm>(), "enemyStatusLayer", layer); PrefabUtility.SaveAsPrefabAsset(hud, hudPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(hud); }
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ItemPath).GetComponent<DemoEnemyStatusItem>();
            foreach (string name in new[] { "DemoCommonEnemy", "DemoEliteEnemy", "DemoBoss" })
            {
                string path = "Assets/Game/Prefabs/Entity/" + name + ".prefab"; var actor = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var binding = actor.GetComponent<DemoEnemyStatusBinding>();
                    if (binding == null) binding = Undo.AddComponent<DemoEnemyStatusBinding>(actor);
                    Set(binding, "prefab", prefab); Set(binding, "health", actor.GetComponent<DemoDamageReceiver>());
                    Set(binding, "enemy", actor.GetComponent<DemoEnemyController>()); Set(binding, "sprite", actor.GetComponent<SpriteRenderer>());
                    Undo.RecordObject(binding, "设置状态绑定默认禁用"); binding.enabled = false;
                    PrefabUtility.SaveAsPrefabAsset(actor, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(actor); }
            }
            AssetDatabase.SaveAssets(); Debug.Log("[Demo.EnemyStatusAuthoring] 血条/失衡/蓄力Item、HUD层和3类动态敌人引用已保存。");
        }
        private static RectTransform Bar(Transform parent, string name, float y, float height, Color color)
        {
            var root = Rect(parent, name, y, height); var bg = Undo.AddComponent<Image>(root.gameObject);
            bg.color = new Color(.04f, .05f, .07f, .9f); bg.raycastTarget = false;
            var fill = Rect(root, "Fill", 0, height); fill.anchorMin = Vector2.zero; fill.anchorMax = Vector2.one;
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            var image = Undo.AddComponent<Image>(fill.gameObject); image.color = color; image.raycastTarget = false;
            return fill;
        }
        private static RectTransform Rect(Transform parent, string name, float y, float height)
        {
            var go = new GameObject(name, typeof(RectTransform)); Undo.RegisterCreatedObjectUndo(go, "创建状态条节点");
            go.transform.SetParent(parent, false); var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, 0f); rect.pivot = new Vector2(.5f, 0f);
            rect.anchoredPosition = new Vector2(0, y); rect.sizeDelta = new Vector2(120, height); return rect;
        }
        private static TMP_Text Label(Transform parent, string name, float y, float height, float size, string value)
        {
            var rect = Rect(parent, name, y, height); var text = Undo.AddComponent<TextMeshProUGUI>(rect.gameObject);
            text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Game/Font/Common/AlibabaPuHuiTi-3-85-Bold SDF.asset");
            text.fontSize = size; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false; text.text = value;
            return text;
        }
        private static void Set(UnityEngine.Object target, string name, UnityEngine.Object value)
        {
            Undo.RecordObject(target, "绑定敌人状态引用"); var fields = new SerializedObject(target);
            fields.FindProperty(name).objectReferenceValue = value; fields.ApplyModifiedProperties();
        }
    }
}
