using System;
using System.Collections.Generic;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Enemies;
using Shanhai.Demo.Player;
using Shanhai.Demo.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityGameFramework.Runtime;

namespace Shanhai.Demo.Editor
{
    internal static class DemoEntityAuthoring
    {
        [MenuItem("Shanhai/Demo/Migrate dynamic entities")]
        private static void Migrate()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("先停止Play Mode。");
            foreach (string name in new[] { "DemoPlayer", "DemoCommonEnemy", "DemoEliteEnemy", "DemoBoss" })
            {
                string path = "Assets/Game/Prefabs/Entity/" + name + ".prefab";
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    root.SetActive(true);
                    // GF Entity.OnInit会AddComponent指定逻辑类型，Prefab不能预挂第二份EntityLogic。
                    foreach (var logic in root.GetComponents<EntityLogic>()) Undo.DestroyObjectImmediate(logic);
                    foreach (MonoBehaviour component in root.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        if (component == null) throw new InvalidOperationException("实体模板Missing Script：" + name);
                        if (component.GetType().Namespace == "Shanhai.Demo.QA") { Undo.DestroyObjectImmediate(component); continue; }
                        if (component is DemoPlayerController || component is DemoPlayerCombatController || component is DemoRunFlowController
                            || component is DemoEnemyController || component is DemoSpriteFeedback)
                        {
                            var fields = new SerializedObject(component);
                            fields.FindProperty("m_Enabled").boolValue = false; fields.ApplyModifiedProperties();
                        }
                    }
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            Scene previous = SceneManager.GetActiveScene();
            Scene scene = EditorSceneManager.OpenScene("Assets/Game/Scene/Demo/DemoMain.unity", OpenSceneMode.Additive);
            try
            {
                DemoWorldEntry entry = null; GameObject player = null;
                var remove = new HashSet<GameObject>();
                var waves = new List<DemoWaveSpawner>(); var encounters = new List<DemoEncounterController>();
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (root.GetComponent<DemoWorldEntry>() != null) entry = root.GetComponent<DemoWorldEntry>();
                    if (root.GetComponent<DemoPlayerController>() != null) player = root;
                    waves.AddRange(root.GetComponentsInChildren<DemoWaveSpawner>(true));
                    encounters.AddRange(root.GetComponentsInChildren<DemoEncounterController>(true));
                }
                if (entry == null) throw new InvalidOperationException("先迁移动态HUD。");
                var world = new SerializedObject(entry);
                if (player != null)
                {
                    var movement = new SerializedObject(player.GetComponent<DemoPlayerController>());
                    world.FindProperty("inputSource").objectReferenceValue = movement.FindProperty("inputSourceBehaviour").objectReferenceValue;
                    world.FindProperty("gameCamera").objectReferenceValue = movement.FindProperty("aimCamera").objectReferenceValue;
                    world.FindProperty("playerStart").vector3Value = player.transform.position;
                    world.FindProperty("playerScale").vector3Value = player.transform.localScale;
                    world.ApplyModifiedProperties(); remove.Add(player);
                }
                foreach (var wave in waves)
                {
                    var fields = new SerializedObject(wave); var old = fields.FindProperty("enemies");
                    if (old.arraySize > 0)
                    {
                        var positions = fields.FindProperty("spawnPositions"); var euler = fields.FindProperty("spawnEuler"); var scales = fields.FindProperty("spawnScales");
                        positions.arraySize = euler.arraySize = scales.arraySize = old.arraySize;
                        for (int i = 0; i < old.arraySize; i++)
                        {
                            var enemy = (GameObject)old.GetArrayElementAtIndex(i).objectReferenceValue;
                            if (enemy == null) throw new InvalidOperationException("旧波次引用缺失。");
                            positions.GetArrayElementAtIndex(i).vector3Value = enemy.transform.position;
                            euler.GetArrayElementAtIndex(i).vector3Value = enemy.transform.eulerAngles;
                            scales.GetArrayElementAtIndex(i).vector3Value = enemy.transform.localScale; remove.Add(enemy);
                        }
                        old.arraySize = 0;
                    }
                    fields.FindProperty("player").objectReferenceValue = null; fields.ApplyModifiedProperties();
                }
                foreach (var encounter in encounters)
                {
                    var fields = new SerializedObject(encounter); var enemy = (GameObject)fields.FindProperty("enemy").objectReferenceValue;
                    if (enemy != null)
                    {
                        fields.FindProperty("spawnPosition").vector3Value = enemy.transform.position;
                        fields.FindProperty("spawnEuler").vector3Value = enemy.transform.eulerAngles;
                        fields.FindProperty("spawnScale").vector3Value = enemy.transform.localScale; remove.Add(enemy);
                    }
                    fields.FindProperty("player").objectReferenceValue = null;
                    fields.FindProperty("enemy").objectReferenceValue = null;
                    fields.FindProperty("enemyReceiver").objectReferenceValue = null;
                    fields.FindProperty("runFlow").objectReferenceValue = null; fields.ApplyModifiedProperties();
                }
                foreach (GameObject target in remove) Undo.DestroyObjectImmediate(target);
                EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
                Debug.Log("[Demo.EntityAuthoring] 玩家和敌人场景对象已删除，刷怪姿态转为配置；四类GF实体Prefab已保存。");
            }
            finally { EditorSceneManager.CloseScene(scene, true); SceneManager.SetActiveScene(previous); }
        }
    }
}
