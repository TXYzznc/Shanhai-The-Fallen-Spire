using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shanhai.Demo.Editor
{
    /// <summary>一次性迁移旧灰盒的已生成结构；运行时不依赖此工具。</summary>
    internal static class DemoPrefabMigration
    {
        private const string UiPath = "Assets/Game/Prefabs/UI/";
        private const string EntityPath = "Assets/Game/Prefabs/Entity/";

        [MenuItem("Shanhai/Demo/Extract prefab templates")]
        private static void Extract()
        {
            if (!EditorApplication.isPlaying)
                throw new InvalidOperationException("先在旧DemoMain进入Play Mode，待UI节点创建后再迁移。");
            GameObject hud = Find("DemoHUD");
            if (hud == null || hud.transform.Find("RewardChoicePanel") == null)
                throw new InvalidOperationException("未找到完整的DemoHUD运行时模板。");
            Directory.CreateDirectory(UiPath + "Item");
            Directory.CreateDirectory(EntityPath);
            AssetDatabase.Refresh();
            Save(hud, UiPath + "DemoHud.prefab");
            Save(Find("Player"), EntityPath + "DemoPlayer.prefab");
            Save(Find("CommonEnemy_01"), EntityPath + "DemoCommonEnemy.prefab");
            Save(Find("EliteBeast_01"), EntityPath + "DemoEliteEnemy.prefab");
            Save(Find("Boss_01"), EntityPath + "DemoBoss.prefab");
            Save(hud.transform.Find("RewardChoicePanel/RewardOption_1").gameObject, UiPath + "Item/DemoRewardCard.prefab");
            Save(hud.transform.Find("RewardChoicePanel/RewardSlot_0").gameObject, UiPath + "Item/DemoRewardSlot.prefab");
            Save(hud.transform.Find("LoadoutPanel/Slot_0").gameObject, UiPath + "Item/DemoLoadoutSlot.prefab");
            Save(hud.transform.Find("CompletionPanel/Retry").gameObject, UiPath + "Item/DemoActionButton.prefab");
            NestItems();
            AssetDatabase.SaveAssets();
            Debug.Log("[Demo.PrefabMigration] 已保存HUD、4类实体和4类嵌套Item模板；场景未删除，运行时迁移尚未验收。");
        }

        private static GameObject Find(string name)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
                foreach (GameObject root in SceneManager.GetSceneAt(i).GetRootGameObjects())
                    foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                        if (child.name == name) return child.gameObject;
            return null;
        }

        private static void Save(GameObject source, string path)
        {
            if (source == null) throw new InvalidOperationException("迁移源缺失：" + path);
            // 不能覆盖后续已编辑的设计资产。重新运行只补缺失模板。
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            var cleared = new List<(UnityEngine.Object owner, string path, UnityEngine.Object value)>();
            try
            {
                foreach (Component component in source.GetComponentsInChildren<Component>(true))
                {
                    if (component == null) throw new InvalidOperationException("存在Missing Script：" + source.name);
                    var serialized = new SerializedObject(component);
                    SerializedProperty iterator = serialized.GetIterator();
                    bool changed = false;
                    while (iterator.Next(true))
                    {
                        if (iterator.propertyType != SerializedPropertyType.ObjectReference) continue;
                        UnityEngine.Object value = iterator.objectReferenceValue;
                        if (value == null || EditorUtility.IsPersistent(value)) continue;
                        Transform target = value is GameObject go ? go.transform : (value as Component)?.transform;
                        if (target == null || target == source.transform || target.IsChildOf(source.transform)) continue;
                        if (!changed) Undo.RecordObject(component, "导出模板时排除场景引用");
                        cleared.Add((component, iterator.propertyPath, value));
                        iterator.objectReferenceValue = null; changed = true;
                    }
                    if (changed) serialized.ApplyModifiedProperties();
                }
                if (PrefabUtility.SaveAsPrefabAsset(source, path) == null)
                    throw new InvalidOperationException("Prefab保存失败：" + path);
            }
            finally
            {
                foreach (var reference in cleared)
                {
                    if (reference.owner == null) continue;
                    var serialized = new SerializedObject(reference.owner);
                    serialized.FindProperty(reference.path).objectReferenceValue = reference.value;
                    serialized.ApplyModifiedProperties();
                }
            }
        }

        private static void NestItems()
        {
            string path = UiPath + "DemoHud.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                for (int i = 1; i <= 3; i++) Replace(root, "RewardChoicePanel/RewardOption_" + i, "DemoRewardCard");
                for (int i = 0; i < 4; i++)
                {
                    Replace(root, "RewardChoicePanel/RewardSlot_" + i, "DemoRewardSlot");
                    Replace(root, "LoadoutPanel/Slot_" + i, "DemoLoadoutSlot");
                }
                Replace(root, "CompletionPanel/Retry", "DemoActionButton");
                Replace(root, "CompletionPanel/Close", "DemoActionButton");
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void Replace(GameObject root, string hierarchy, string item)
        {
            RectTransform old = root.transform.Find(hierarchy) as RectTransform;
            if (old == null) throw new InvalidOperationException("UI模板节点缺失：" + hierarchy);
            if (PrefabUtility.IsAnyPrefabInstanceRoot(old.gameObject)) return;
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(UiPath + "Item/" + item + ".prefab");
            GameObject replacement = (GameObject)PrefabUtility.InstantiatePrefab(source, old.parent);
            replacement.name = old.name;
            var rect = (RectTransform)replacement.transform;
            rect.anchorMin = old.anchorMin; rect.anchorMax = old.anchorMax; rect.pivot = old.pivot;
            rect.sizeDelta = old.sizeDelta; rect.anchoredPosition3D = old.anchoredPosition3D;
            rect.localRotation = old.localRotation; rect.localScale = old.localScale;
            rect.SetSiblingIndex(old.GetSiblingIndex());
            // 保留每个槽位/按钮已编写的文本，而非所有Item都复制第一项文字。
            TMPro.TMP_Text[] labels = old.GetComponentsInChildren<TMPro.TMP_Text>(true);
            TMPro.TMP_Text[] copies = replacement.GetComponentsInChildren<TMPro.TMP_Text>(true);
            if (labels.Length != copies.Length) throw new InvalidOperationException("Item结构不匹配：" + hierarchy);
            for (int i = 0; i < labels.Length; i++) copies[i].text = labels[i].text;
            UnityEngine.Object.DestroyImmediate(old.gameObject);
        }
    }
}
