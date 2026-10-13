using System;
using System.Collections;
using Shanhai.Demo.Configuration;
using Shanhai.Demo.Player;
using Shanhai.Demo.UI;
using Shanhai.Demo.Enemies;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Shanhai.Demo.Editor
{
    internal static class DemoWorldUiAcceptanceProbe
    {
        private static IEnumerator _routine;
        private static double _deadline;
        [MenuItem("Shanhai/Demo/Verify HUD scene reentry")]
        private static void Begin()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("需要先从Launch进入游戏。");
            Stop(); _deadline = EditorApplication.timeSinceStartup + 45;
            _routine = Run(); EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += Changed;
            AssemblyReloadEvents.beforeAssemblyReload += Stop;
        }
        private static IEnumerator Run()
        {
            DemoWorldEntry first = UnityEngine.Object.FindObjectOfType<DemoWorldEntry>();
            if (first == null || !first.IsReady) throw new InvalidOperationException("本局HUD未准备完成。");
            DemoHudForm hud = first.Hud;
            int nodes = hud.GetComponentsInChildren<Transform>(true).Length;
            foreach (GameObject root in SceneManager.GetSceneByName("DemoMain").GetRootGameObjects())
                if (root.GetComponent<Canvas>() != null) throw new InvalidOperationException("场景仍有直接UI根节点。");
            // 面板反复打开不应重建节点。
            for (int i = 0; i < 3; i++)
            {
                first.Ui.Open();
                while (first.Ui.RewardView == null) yield return null;
                first.Ui.Hide(); first.Ui.RewardChoice.ResetForRetry();
            }
            if (hud.GetComponentsInChildren<Transform>(true).Length != nodes) throw new InvalidOperationException("奖励面板重复创建节点。");
            var waves = UnityEngine.Object.FindObjectsOfType<DemoWaveSpawner>();
            Array.Sort(waves, (a, b) => a.Wave.CompareTo(b.Wave));
            first.Player.GetComponent<Rigidbody2D>().position = waves[0].transform.position;
            Physics2D.SyncTransforms();
            while (waves[0].SpawnedCount != 4) yield return null;
            var entities = UnityEngine.Object.FindObjectsOfType<DemoSessionEntity>();
            if (first.OwnedEntityCount != 5 || entities.Length != 5) throw new InvalidOperationException("退出用例未包含存活敌人。");
            var ids = new int[entities.Length];
            for (int i = 0; i < ids.Length; i++) ids[i] = entities[i].Id;
            var config = AssetDatabase.LoadAssetAtPath<DemoPresentationConfig>("Assets/Game/ScriptableAssets/Demo/DemoPresentationConfig.asset");
            var oldOverlay = first.Ui.Overlay;
            var oldFeedback = first.Feedback;
            var oldHealth = first.Player.GetComponent<Shanhai.Demo.Combat.DemoPlayerHealthController>();
            var oldFlow = first.Player.GetComponent<DemoRunFlowController>();
            first.Player.GetComponent<Shanhai.Demo.Combat.DemoPlayerCombatController>().ResetForRetry();
            first.Player.GetComponent<Shanhai.Demo.Combat.DemoPlayerCombatController>().TryUseSkill();
            first.Player.GetComponent<Shanhai.Demo.Combat.DemoPlayerHealthController>().ReceiveDamage(new Shanhai.Demo.Combat.DemoDamageInfo(1f, null));
            if (oldFeedback.ActiveCount == 0) throw new InvalidOperationException("退出用例没有活动表现。");
            GF.Scene.UnloadScene(config.GameSceneAsset);
            while (GF.Scene.SceneIsUnloading(config.GameSceneAsset)) yield return null;
            if (first != null || hud.Available) throw new InvalidOperationException("离开场景未释放会话/HUD。");
            yield return null;
            if (oldFeedback != null || oldOverlay.DamageLayer.GetComponentsInChildren<DemoDamageTextItem>(true).Length != 0)
                throw new InvalidOperationException("退出后仍有表现对象。");
            if (oldHealth != null && typeof(Shanhai.Demo.Combat.DemoPlayerHealthController).GetField("DamageApplied", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(oldHealth) != null)
                throw new InvalidOperationException("回收玩家仍持有扣血监听。");
            if (oldFlow != null && typeof(DemoRunFlowController).GetField("WorldReset", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(oldFlow) != null)
                throw new InvalidOperationException("回收玩家仍持有会话重试监听。");
            foreach (int id in ids) if (GF.Entity.HasEntity(id) || GF.Entity.IsLoadingEntity(id)) throw new InvalidOperationException("退出场景后仍有旧实体。");
            var combatField = typeof(DemoVitalsView).GetField("_combat", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var healthField = typeof(DemoVitalsView).GetField("_health", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (combatField.GetValue(hud.Display.Vitals) != null || healthField.GetValue(hud.Display.Vitals) != null)
                throw new InvalidOperationException("回收HUD仍引用旧玩家。");
            GF.Scene.LoadScene(config.MenuSceneAsset, "Demo.WorldUIQA");
            DemoMenuEntry menu;
            do { yield return null; menu = UnityEngine.Object.FindObjectOfType<DemoMenuEntry>(); }
            while (menu == null || !menu.IsReady);
            if (!menu.RequestStart()) throw new InvalidOperationException("重进菜单无法开始。");
            DemoWorldEntry second;
            do { yield return null; second = UnityEngine.Object.FindObjectOfType<DemoWorldEntry>(); }
            while (second == null || !second.IsReady);
            if (second.Hud.GetComponentsInChildren<Transform>(true).Length != nodes || second.Ui.RewardChoice.IsOpen)
                throw new InvalidOperationException("重进后HUD结构/奖励状态残留。");
            if (second.Feedback.ActiveCount != 0 || second.Feedback.AttackCount != 0 || second.Feedback.HitCount != 0)
                throw new InvalidOperationException("重进后表现状态残留。");
            if (second.OwnedEntityCount != 1 || second.Player.GetComponent<Rigidbody2D>().position.sqrMagnitude > .01f)
                throw new InvalidOperationException("重进后不是新的起点会话。");
            // 保留会话已经完成的Boss绑定，只读取新玩家引用。
            yield return null;
            if (combatField.GetValue(second.Hud.Display.Vitals) == null)
                throw new InvalidOperationException("重进后HUD没有绑定新玩家。");
            Debug.Log("[Demo.WorldUIQA] PASS：动态HUD/面板复用/5个存活实体与活动表现退出回收/场景退出解绑/菜单重进与起点恢复；节点=" + nodes);
        }
        private static void Tick()
        {
            try
            {
                if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup > _deadline) throw new InvalidOperationException("HUD重进验证超时。");
                if (!_routine.MoveNext()) Stop();
            }
            catch (Exception error) { Stop(); Debug.LogException(error); }
        }
        private static void Changed(PlayModeStateChange state) { if (state == PlayModeStateChange.ExitingPlayMode) Stop(); }
        private static void Stop()
        {
            EditorApplication.update -= Tick; EditorApplication.playModeStateChanged -= Changed;
            AssemblyReloadEvents.beforeAssemblyReload -= Stop; _routine = null;
        }
    }
}
