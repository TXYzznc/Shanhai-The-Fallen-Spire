using System;
using System.Collections.Generic;
using Shanhai.Demo.Configuration;
using Shanhai.Demo.Enemies;
using Shanhai.Demo.Player;
using Shanhai.Demo.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Shanhai.Demo.Editor
{
    internal static class DemoHudAuthoring
    {
        [MenuItem("Shanhai/Demo/Migrate dynamic HUD")]
        private static void Migrate()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("先停止Play Mode。");
            const string path = "Assets/Game/Prefabs/UI/DemoHud.prefab";
            GameObject prefab = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (prefab.GetComponent<DemoHudForm>() == null) prefab.AddComponent<DemoHudForm>();
                for (int i = 1; i <= 3; i++)
                {
                    GameObject card = prefab.transform.Find("RewardChoicePanel/RewardOption_" + i).gameObject;
                    if (card.GetComponent<EventTrigger>() == null) Undo.AddComponent<EventTrigger>(card);
                }
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.OpenScene("Assets/Game/Scene/Demo/DemoMain.unity", OpenSceneMode.Additive);
            try
            {
                GameObject player = null, hud = null;
                DemoWorldEntry entry = null;
                var encounters = new List<DemoEncounterController>();
                var waves = new List<DemoWaveSpawner>();
                var checkpoints = new List<DemoCheckpointController>();
                DemoEncounterController boss = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (root.name == "Player") player = root;
                    if (root.name == "DemoHUD") hud = root;
                    if (root.GetComponent<DemoWorldEntry>() != null) entry = root.GetComponent<DemoWorldEntry>();
                    encounters.AddRange(root.GetComponentsInChildren<DemoEncounterController>(true));
                    waves.AddRange(root.GetComponentsInChildren<DemoWaveSpawner>(true));
                    checkpoints.AddRange(root.GetComponentsInChildren<DemoCheckpointController>(true));
                }
                if (player == null && entry != null && hud == null)
                { Debug.Log("[Demo.HudAuthoring] 当前场景已完成动态迁移，不重新写入旧引用。"); return; }
                if (player == null) throw new InvalidOperationException("当前迁移阶段需要旧Player引用。");
                if (hud != null)
                {
                    var old = new SerializedObject(hud.GetComponent<DemoCompletionView>());
                    boss = (DemoEncounterController)old.FindProperty("bossEncounter").objectReferenceValue;
                }
                if (entry == null)
                {
                    entry = new GameObject("DemoSession").AddComponent<DemoWorldEntry>();
                    Undo.RegisterCreatedObjectUndo(entry.gameObject, "创建HUD会话入口");
                    SceneManager.MoveGameObjectToScene(entry.gameObject, scene);
                }
                Undo.RecordObject(entry, "绑定HUD会话引用");
                var fields = new SerializedObject(entry);
                fields.FindProperty("presentation").objectReferenceValue = AssetDatabase.LoadAssetAtPath<DemoPresentationConfig>("Assets/Game/ScriptableAssets/Demo/DemoPresentationConfig.asset");
                if (boss != null) fields.FindProperty("boss").objectReferenceValue = boss;
                Array(fields.FindProperty("encounters"), encounters);
                Array(fields.FindProperty("waves"), waves);
                Array(fields.FindProperty("checkpoints"), checkpoints);
                fields.ApplyModifiedProperties();
                if (hud != null) Undo.DestroyObjectImmediate(hud);
                // Launch拥有输入事件系统，玩法场景只持有当前会话对象。
                foreach (GameObject root in scene.GetRootGameObjects())
                    if (root.GetComponent<EventSystem>() != null) Undo.DestroyObjectImmediate(root);
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();
                Debug.Log("[Demo.HudAuthoring] 场景HUD已删除，GF动态HUD及显式会话引用已保存；玩家/敌人待下一阶段迁移。");
            }
            finally { EditorSceneManager.CloseScene(scene, true); SceneManager.SetActiveScene(previous); }
        }
        private static void Array<T>(SerializedProperty property, List<T> items) where T : UnityEngine.Object
        {
            property.arraySize = items.Count;
            for (int i = 0; i < items.Count; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
        }
    }
}
