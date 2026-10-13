using System;
using System.IO;
using Game.State;
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
    public static class DemoMarkAssemblyAuthoring
    {
        private const string Ui = "Assets/Game/Prefabs/UI/";
        private static TMP_FontAsset Font => AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Game/Font/Common/AlibabaPuHuiTi-3-85-Bold SDF.asset");
        [MenuItem("Shanhai/Demo/Author mark assembly")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("先停止Play Mode。");
            var currentHud = AssetDatabase.LoadAssetAtPath<GameObject>(Ui + "DemoHud.prefab");
            if (currentHud != null && currentHud.GetComponentInChildren<DemoVitalsView>(true) != null)
                throw new InvalidOperationException("HUD已拆分；请直接编辑独立装配窗口和Item资产，不再运行旧整体生成器。");
            var previous = SceneManager.GetActiveScene();
            var temporary = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(temporary);
            try { BuildContents(); }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(temporary, true);
            }
        }
        private static void BuildContents()
        {
            var config = AssetDatabase.LoadAssetAtPath<DemoGameplayConfig>("Assets/Game/DataTable/Demo/DemoGameplayConfig.asset");
            var fields = new SerializedObject(config); var marks = fields.FindProperty("beastMarks");
            Directory.CreateDirectory("Assets/Game/Sprites/Demo"); AssetDatabase.Refresh();
            for (int i = 0; i < marks.arraySize; i++) marks.GetArrayElementAtIndex(i).FindPropertyRelative("icon").objectReferenceValue = Icon(i);
            fields.FindProperty("duplicateMarkCurrency").intValue = 100; fields.ApplyModifiedProperties();
            var ownedPrefab = Item("DemoOwnedMarkItem", new Vector2(270, 110));
            var slotPrefab = Item("DemoMarkSlotItem", new Vector2(180, 110));
            var assembly = Assembly(ownedPrefab, slotPrefab);
            GameObject hud = PrefabUtility.LoadPrefabContents(Ui + "DemoHud.prefab");
            try
            {
                Undo.RegisterFullObjectHierarchyUndo(hud, "接入兽印装配");
                Remove(hud.transform.Find("LoadoutPanel")); Remove(hud.transform.Find("MarkHudPanel")); Remove(hud.transform.Find("RewardNotice"));
                var scaler = hud.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = .5f;
                var panel = Rect(hud.transform, "MarkHudPanel", Vector2.zero, new Vector2(400, 152));
                panel.anchorMin = panel.anchorMax = panel.pivot = Vector2.one; panel.anchoredPosition = new Vector2(-18, -18);
                var hudItems = new DemoMarkItem[4];
                for (int i = 0; i < 4; i++)
                {
                    hudItems[i] = Spawn(slotPrefab, panel, "HudSlot_" + i, new Vector2(-350 + 100 * i, -48), new Vector2(88, 92));
                    var itemRect = (RectTransform)hudItems[i].transform; itemRect.anchorMin = itemRect.anchorMax = Vector2.one;
                    hudItems[i].Render(null, SlotName(i), string.Empty); hudItems[i].Button.interactable = false;
                    foreach (var graphic in hudItems[i].GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
                }
                var open = Button(panel, "OpenAssembly", "兽印装配 [Tab]", new Vector2(-150, -124), new Vector2(200, 42));
                var openRect = (RectTransform)open.transform; openRect.anchorMin = openRect.anchorMax = Vector2.one;
                var loadout = hud.GetComponent<DemoLoadoutView>(); Set(loadout, "assemblyPrefab", assembly); Set(loadout, "openButton", open); SetArray(loadout, "hudSlots", hudItems);
                var reward = hud.transform.Find("RewardChoicePanel");
                for (int i = 0; i < 4; i++) Remove(reward.Find("RewardSlot_" + i));
                if (reward.GetComponent<GamePauseWindow>() == null) reward.gameObject.AddComponent<GamePauseWindow>();
                var completion = hud.transform.Find("CompletionPanel");
                if (completion.GetComponent<GamePauseWindow>() == null) completion.gameObject.AddComponent<GamePauseWindow>();
                for (int i = 1; i <= 3; i++)
                {
                    var card = reward.Find("RewardOption_" + i); Remove(card.Find("MarkIcon"));
                    var image = Rect(card, "MarkIcon", new Vector2(0, 42), new Vector2(56, 56)).gameObject.AddComponent<Image>();
                    image.preserveAspect = true; image.raycastTarget = false;
                    DemoRewardCardAuthoring.Configure(card);
                }
                reward.Find("Tooltip").GetComponent<TMP_Text>().raycastTarget = false;
                var notice = Text(hud.transform, "RewardNotice", string.Empty, new Vector2(0, 380), new Vector2(650, 45), 22);
                Set(hud.GetComponent<DemoRewardChoiceView>(), "notice", notice);
                reward.gameObject.SetActive(false); completion.gameObject.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(hud, Ui + "DemoHud.prefab");
            }
            finally { PrefabUtility.UnloadPrefabContents(hud); }
            AssetDatabase.SaveAssets();
            Debug.Log("[Demo.MarkAssembly] 兽印图标、装配UI、HUD四槽与奖励暂停预制体已保存。");
        }
        private static string SlotName(int i) { switch (i) { case 0: return "普攻"; case 1: return "技能"; case 2: return "大招"; default: return "属性"; } }
        private static Sprite Icon(int index)
        {
            string path = "Assets/Game/Sprites/Demo/BeastMark" + (index + 1) + ".png";
            if (!File.Exists(path))
            {
                var texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
                var pixels = new Color[4096]; Color tint = index == 0 ? new Color(1f, .42f, .2f) : index == 1 ? new Color(.25f, .7f, 1f) : new Color(.75f, .4f, 1f);
                for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++)
                {
                    float dx = x - 31.5f, dy = y - 31.5f; float radius = Mathf.Sqrt(dx * dx + dy * dy);
                    bool glyph = index == 0 ? Mathf.Abs(dx - dy * .45f) < 4 && Mathf.Abs(dy) < 20
                        : index == 1 ? dy > -14 && dy < 16 - Mathf.Abs(dx) * 1.3f && Mathf.Abs(dx) < 22
                        : Mathf.Abs(dy + dx * .65f) < 4 && Mathf.Abs(dx) < 23 || Mathf.Abs(dy + dx * .65f - 10) < 3 && Mathf.Abs(dx) < 16;
                    pixels[y * 64 + x] = radius > 30 ? Color.clear : glyph ? Color.white : radius > 26 ? tint : tint * new Color(.25f, .25f, .25f, 1);
                }
                texture.SetPixels(pixels); texture.Apply(); File.WriteAllBytes(path, texture.EncodeToPNG()); UnityEngine.Object.DestroyImmediate(texture);
            }
            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path); importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single; importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed; importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        private static DemoMarkItem Item(string name, Vector2 size)
        {
            var root = Rect(null, name, Vector2.zero, size).gameObject;
            try
            {
                var image = root.AddComponent<Image>(); image.color = new Color(.08f, .14f, .2f);
                var button = root.AddComponent<Button>(); button.targetGraphic = image;
                var icon = Rect(root.transform, "Icon", new Vector2(0, 3), new Vector2(44, 44)).gameObject.AddComponent<Image>(); icon.preserveAspect = true; icon.raycastTarget = false; icon.enabled = false;
                var title = Text(root.transform, "Title", "兽印", new Vector2(0, 36), new Vector2(size.x - 8, 24), 18);
                var status = Text(root.transform, "Status", string.Empty, new Vector2(0, -35), new Vector2(size.x - 8, 28), 16);
                var empty = Text(root.transform, "Empty", "空", new Vector2(0, 2), new Vector2(50, 35), 20);
                var item = root.AddComponent<DemoMarkItem>(); Set(item, "button", button); Set(item, "icon", icon); Set(item, "title", title); Set(item, "status", status); Set(item, "empty", empty);
                return PrefabUtility.SaveAsPrefabAsset(root, Ui + "Item/" + name + ".prefab").GetComponent<DemoMarkItem>();
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        private static DemoMarkAssemblyView Assembly(DemoMarkItem ownedPrefab, DemoMarkItem slotPrefab)
        {
            var root = Rect(null, "DemoMarkAssembly", Vector2.zero, Vector2.zero).gameObject;
            try
            {
                var rect = (RectTransform)root.transform; rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                root.AddComponent<Image>().color = new Color(0, 0, 0, .75f); root.AddComponent<GamePauseWindow>();
                var body = Rect(root.transform, "Body", Vector2.zero, new Vector2(1040, 670)); body.gameObject.AddComponent<Image>().color = new Color(.035f, .065f, .1f, .99f);
                Text(body, "Title", "兽印装配", new Vector2(-350, 285), new Vector2(290, 50), 32);
                var count = Text(body, "Count", "已拥有 0 个兽印", new Vector2(25, 285), new Vector2(280, 45), 22);
                var close = Button(body, "Close", "关闭 [Tab / Esc]", new Vector2(375, 285), new Vector2(250, 45));
                Text(body, "OwnedHeading", "已拥有兽印", new Vector2(-350, 225), new Vector2(270, 35), 22);
                var list = Rect(body, "OwnedList", new Vector2(-350, -20), new Vector2(270, 430));
                var layout = list.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing = 10; layout.childControlHeight = false; layout.childForceExpandHeight = false;
                layout.childControlWidth = true; layout.childForceExpandWidth = true;
                var empty = Text(body, "EmptyList", "尚未获得兽印\n击败精英敌人或Boss可获得", new Vector2(-350, 90), new Vector2(270, 150), 21);
                Text(body, "SlotHeading", "四个装配槽", new Vector2(-85, 225), new Vector2(190, 35), 22);
                var slots = new DemoMarkItem[4]; for (int i = 0; i < 4; i++) slots[i] = Spawn(slotPrefab, body, "Slot_" + i, new Vector2(-85, 140 - 112 * i), new Vector2(180, 100));
                var detail = Text(body, "Detail", "选择兽印查看效果", new Vector2(285, 45), new Vector2(380, 340), 21); detail.alignment = TextAlignmentOptions.TopLeft;
                var equip = Button(body, "Equip", "装配", new Vector2(285, -165), new Vector2(380, 50));
                var unequip = Button(body, "Unequip", "卸下目标槽兽印", new Vector2(285, -225), new Vector2(380, 45));
                var message = Text(body, "Message", "选择兽印与目标槽位，再点击装配", new Vector2(0, -282), new Vector2(1000, 32), 20);
                Text(body, "Hint", "兽印只在本局保留，卸下不会失去兽印", new Vector2(0, -315), new Vector2(1000, 25), 18);
                var view = root.AddComponent<DemoMarkAssemblyView>(); Set(view, "ownedItemPrefab", ownedPrefab); Set(view, "ownedRoot", list); SetArray(view, "slots", slots);
                Set(view, "detail", detail); Set(view, "message", message); Set(view, "count", count); Set(view, "emptyList", empty);
                Set(view, "equipButton", equip); Set(view, "unequipButton", unequip); Set(view, "closeButton", close);
                root.SetActive(false); return PrefabUtility.SaveAsPrefabAsset(root, Ui + "DemoMarkAssembly.prefab").GetComponent<DemoMarkAssemblyView>();
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }
        private static DemoMarkItem Spawn(DemoMarkItem prefab, Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab.gameObject, parent); go.name = name;
            var rect = (RectTransform)go.transform; rect.anchoredPosition = pos; rect.sizeDelta = size; return go.GetComponent<DemoMarkItem>();
        }
        private static RectTransform Rect(Transform parent, string name, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = 5; Undo.RegisterCreatedObjectUndo(go, "创建兽印UI节点");
            go.transform.SetParent(parent, false); var rect = (RectTransform)go.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f); rect.anchoredPosition = pos; rect.sizeDelta = size; return rect;
        }
        private static TMP_Text Text(Transform parent, string name, string value, Vector2 pos, Vector2 size, float fontSize)
        {
            var text = Rect(parent, name, pos, size).gameObject.AddComponent<TextMeshProUGUI>(); text.font = Font;
            text.text = value; text.fontSize = fontSize; text.alignment = TextAlignmentOptions.Center; text.raycastTarget = false; return text;
        }
        private static Button Button(Transform parent, string name, string value, Vector2 pos, Vector2 size)
        {
            var rect = Rect(parent, name, pos, size); var image = rect.gameObject.AddComponent<Image>(); image.color = new Color(.13f, .3f, .37f);
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; Text(rect, "Label", value, Vector2.zero, size - new Vector2(12, 4), 20); return button;
        }
        private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            Undo.RecordObject(target, "绑定兽印UI引用"); var fields = new SerializedObject(target); fields.FindProperty(field).objectReferenceValue = value; fields.ApplyModifiedProperties();
        }
        private static void SetArray(UnityEngine.Object target, string field, DemoMarkItem[] items)
        {
            var fields = new SerializedObject(target); var array = fields.FindProperty(field); array.arraySize = items.Length;
            for (int i = 0; i < items.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = items[i]; fields.ApplyModifiedProperties();
        }
        private static void Remove(Transform node) { if (node != null) UnityEngine.Object.DestroyImmediate(node.gameObject); }
    }
}
