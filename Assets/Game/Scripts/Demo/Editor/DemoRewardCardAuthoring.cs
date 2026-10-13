using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Shanhai.Demo.Editor
{
    public static class DemoRewardCardAuthoring
    {
        /// <summary>卡片内部只显示内容，命中与悬停由背景统一处理。</summary>
        internal static void Configure(Transform card)
        {
            Undo.RegisterFullObjectHierarchyUndo(card.gameObject, "修正奖励卡布局和鼠标命中范围");
            ((RectTransform)card).sizeDelta = new Vector2(210, 160);
            var label = card.Find("Label").GetComponent<TMP_Text>();
            SetRect((RectTransform)label.transform, new Vector2(0, -34), new Vector2(194, 64));
            label.fontSize = 22; label.raycastTarget = false;
            var legacyIcon = card.Find("Icon");
            if (legacyIcon != null) SetRect((RectTransform)legacyIcon, new Vector2(0, 36), new Vector2(48, 48));
            var markIcon = card.Find("MarkIcon");
            if (markIcon != null) SetRect((RectTransform)markIcon, new Vector2(0, 36), new Vector2(48, 48));
            foreach (var graphic in card.GetComponentsInChildren<Graphic>(true))
            {
                graphic.raycastTarget = graphic.transform == card;
                graphic.raycastPadding = Vector4.zero;
            }
        }
        private static void SetRect(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = position; rect.sizeDelta = size;
        }
        [MenuItem("Shanhai/Demo/Fix reward card hover")]
        public static void Fix()
        {
            if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new InvalidOperationException("先停止Play Mode并等待编译。");
            const string itemPath = "Assets/Game/Prefabs/UI/Item/DemoRewardCard.prefab";
            var item = PrefabUtility.LoadPrefabContents(itemPath);
            try { Configure(item.transform); PrefabUtility.SaveAsPrefabAsset(item, itemPath); }
            finally { PrefabUtility.UnloadPrefabContents(item); }
            string hudPath = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/UI/DemoRewardChoice.prefab") != null
                ? "Assets/Game/Prefabs/UI/DemoRewardChoice.prefab" : "Assets/Game/Prefabs/UI/DemoHud.prefab";
            var hud = PrefabUtility.LoadPrefabContents(hudPath);
            try
            {
                var panel = hud.transform.Find("RewardChoicePanel");
                for (int i = 1; i <= 3; i++) Configure(panel.Find("RewardOption_" + i));
                panel.Find("Tooltip").GetComponent<TMP_Text>().raycastTarget = false;
                PrefabUtility.SaveAsPrefabAsset(hud, hudPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(hud); }
            AssetDatabase.SaveAssets();
            Debug.Log("[Demo.RewardHover] 三张卡的内容范围及射线目标已修正并保存。");
        }
    }
}
