using System;
using System.Collections;
using System.Collections.Generic;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Configuration;
using Shanhai.Demo.Enemies;
using Shanhai.Demo.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Shanhai.Demo.Editor
{
    /// <summary>实体结构/生命周期专项；传送和直接伤害只验证生命周期，不代替语义战斗回归。</summary>
    internal static class DemoEntityAcceptanceProbe
    {
        private static IEnumerator _routine;
        private static double _deadline;
        private static DemoWorldEntry _world;
        private static bool _movementEnabled, _combatEnabled;
        private static DemoEntityLease _testLease;
        [MenuItem("Shanhai/Demo/Verify dynamic entity lifecycle")]
        private static void Begin()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("先从Launch进入游戏。");
            Stop(); _deadline = EditorApplication.timeSinceStartup + 45;
            _routine = Run(); EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += Changed; AssemblyReloadEvents.beforeAssemblyReload += Stop;
        }
        private static T[] ReadArray<T>(UnityEngine.Object target, string name) where T : UnityEngine.Object
        {
            var field = new SerializedObject(target).FindProperty(name); var result = new T[field.arraySize];
            for (int i = 0; i < result.Length; i++) result[i] = (T)field.GetArrayElementAtIndex(i).objectReferenceValue;
            return result;
        }
        private static void Require(bool condition, string message)
        { if (!condition) throw new InvalidOperationException(message); }
        private static IEnumerator Run()
        {
            do { yield return null; _world = UnityEngine.Object.FindObjectOfType<DemoWorldEntry>(); }
            while (_world == null || !_world.IsReady);
            Require(_world != null && _world.IsReady && _world.OwnedEntityCount == 1, "初始会话应只有动态玩家。");
            foreach (var root in SceneManager.GetSceneByName("DemoMain").GetRootGameObjects())
                Require(root.GetComponentsInChildren<DemoPlayerController>(true).Length == 0 && root.GetComponentsInChildren<DemoEnemyController>(true).Length == 0,
                    "玩法场景仍有预置实体。");
            GameObject player = _world.Player; var movement = player.GetComponent<DemoPlayerController>();
            var combat = player.GetComponent<DemoPlayerCombatController>(); var health = player.GetComponent<DemoPlayerHealthController>();
            var body = player.GetComponent<Rigidbody2D>(); var flow = player.GetComponent<DemoRunFlowController>();
            int playerId = player.GetComponent<DemoSessionEntity>().Id;
            Require(playerId > 0 && player.GetComponents<DemoSessionEntity>().Length == 1 && GF.Entity.HasEntity(playerId), "玩家实体逻辑重复或ID无效。");
            _movementEnabled = movement.enabled; _combatEnabled = combat.enabled; movement.enabled = combat.enabled = false;
            var waves = ReadArray<DemoWaveSpawner>(_world, "waves"); Array.Sort(waves, (a, b) => a.Wave.CompareTo(b.Wave));
            var encounters = ReadArray<DemoEncounterController>(_world, "encounters");
            Array.Sort(encounters, (a, b) => a.transform.position.y.CompareTo(b.transform.position.y));
            foreach (DemoWaveSpawner wave in waves)
            {
                body.position = wave.transform.position; Physics2D.SyncTransforms();
                while (wave.SpawnedCount != wave.ConfiguredCount) yield return null;
                Require(wave.ConfiguredCount == 4 && _world.OwnedEntityCount == 5, "波次数量或所有权错误。");
                if (wave == waves[0])
                {
                    var fields = new SerializedObject(wave);
                    var config = (DemoGameplayConfig)fields.FindProperty("gameplayConfig").objectReferenceValue;
                    string id = config.FindSpawn(fields.FindProperty("spawnId").stringValue).enemyId;
                    bool shown = false;
                    _testLease = _world.ShowEnemy(config, id, new Vector3(100, 100), Vector3.zero, Vector3.one, _ => shown = true, error => { throw new InvalidOperationException(error); });
                    Require(GF.Entity.IsLoadingEntity(_testLease.Id), "取消用例没有进入真实加载态。");
                    _testLease.Dispose(); int frame = Time.frameCount + 3;
                    while (Time.frameCount < frame) yield return null;
                    Require(!shown && _world.OwnedEntityCount == 5 && !GF.Entity.HasEntity(_testLease.Id), "取消后出现迟到实体。");
                    _testLease = null;
                }
                for (int i = 0; i < wave.ConfiguredCount; i++)
                {
                    var enemy = wave.EnemyAt(i); var controller = enemy.GetComponent<DemoEnemyController>();
                    Require(enemy.GetComponents<DemoSessionEntity>().Length == 1 && enemy.GetComponent<DemoSessionEntity>().Id > 0, "敌人实体逻辑重复或ID无效。");
                    Require(controller.HasTargetReceiver && controller.HasEnemyConfig, "动态敌人缺少目标或配置。");
                    controller.enabled = false; enemy.GetComponent<DemoDamageReceiver>().ReceiveDamage(new DemoDamageInfo(10000, player));
                }
                while (!wave.IsCompleted) yield return null;
                Require(_world.OwnedEntityCount == 1 && wave.SpawnedCount == 4, "击败回收破坏波次进度。");
            }
            var checkpoint = ReadArray<DemoCheckpointController>(_world, "checkpoints")[0];
            // 使用运行时配置副本验证真实资源失败；立即恢复资产绑定，不改写配置文件。
            var worldFields = new SerializedObject(_world);
            var original = (DemoPresentationConfig)worldFields.FindProperty("presentation").objectReferenceValue;
            var copy = UnityEngine.Object.Instantiate(original);
            var resourceFields = new SerializedObject(copy);
            resourceFields.FindProperty("commonEnemyPrefabAsset").stringValue = "Assets/Game/Prefabs/Entity/QA_MissingEntity.prefab";
            resourceFields.ApplyModifiedPropertiesWithoutUndo();
            worldFields.FindProperty("presentation").objectReferenceValue = copy; worldFields.ApplyModifiedPropertiesWithoutUndo();
            var waveFields = new SerializedObject(waves[0]);
            var gameplay = (DemoGameplayConfig)waveFields.FindProperty("gameplayConfig").objectReferenceValue;
            bool failed = false, unexpectedlyShown = false;
            _testLease = _world.ShowEnemy(gameplay, gameplay.FindSpawn(waveFields.FindProperty("spawnId").stringValue).enemyId,
                new Vector3(100, 100), Vector3.zero, Vector3.one, _ => unexpectedlyShown = true, error => failed = !string.IsNullOrEmpty(error));
            worldFields.FindProperty("presentation").objectReferenceValue = original; worldFields.ApplyModifiedPropertiesWithoutUndo();
            UnityEngine.Object.DestroyImmediate(copy);
            while (!failed) yield return null;
            Require(!unexpectedlyShown && _testLease.IsDisposed && _world.OwnedEntityCount == 1, "资源失败留下实体或票据。");
            _testLease = null;
            var custom = UnityEngine.Object.Instantiate(gameplay);
            string commonId = gameplay.FindSpawn(waveFields.FindProperty("spawnId").stringValue).enemyId;
            custom.FindEnemy(commonId).maxHealth = 17;
            GameObject customInstance = null;
            _testLease = _world.ShowEnemy(custom, commonId, new Vector3(100, 100), Vector3.zero, new Vector3(.8f, .8f, 1f),
                instance => customInstance = instance, error => { throw new InvalidOperationException(error); });
            while (customInstance == null) yield return null;
            Require(customInstance.GetComponent<DemoDamageReceiver>().MaxHealth == 17 && customInstance.transform.localScale.x == .8f,
                "显式配置或生成缩放未应用。");
            var customController = customInstance.GetComponent<DemoEnemyController>();
            _testLease.Dispose(); _testLease = null;
            Require(new SerializedObject(customController).FindProperty("gameplayConfig").objectReferenceValue == null,
                "回收后仍持有会话配置。");
            UnityEngine.Object.DestroyImmediate(custom);
            body.position = checkpoint.transform.position; Physics2D.SyncTransforms();
            while (!checkpoint.IsActivated) yield return null;
            Require(flow.CheckpointSnapshot != null && _world.Ui.RewardChoice.IsBuffChoice, "动态玩家未触发检查点/升级。");
            Require(_world.Ui.RewardChoice.SelectOption(0), "检查点升级选择失败。"); _world.Ui.Hide();
            foreach (var encounter in encounters)
            {
                body.position = encounter.transform.position; Physics2D.SyncTransforms();
                while (encounter.Enemy == null) yield return null;
                var enemy = encounter.Enemy; var controller = enemy.GetComponent<DemoEnemyController>(); controller.enabled = false;
                Require(_world.OwnedEntityCount == 2 && controller.HasTargetReceiver, "动态遭遇所有权/目标错误。");
                var receiver = enemy.GetComponent<DemoDamageReceiver>();
                if (controller.Phase == 1 && new SerializedObject(encounter).FindProperty("grantsBossCurrency").boolValue)
                { receiver.ReceiveDamage(new DemoDamageInfo(receiver.MaxHealth * .5f, player)); Require(controller.Phase == 2, "动态Boss二阶段未生效。"); }
                receiver.ReceiveDamage(new DemoDamageInfo(10000, player));
                while (!encounter.IsCompleted) yield return null;
                Require(_world.OwnedEntityCount == 1 && _world.Ui.RewardChoice.IsOpen, "遭遇回收或奖励入口失败。");
                Require(_world.Ui.RewardChoice.SelectOption(0), "遭遇奖励选择失败。"); _world.Ui.Hide();
            }
            Require(flow.CurrentCurrency > 0, "Boss货币未结算。");
            int ready = Time.frameCount + 2; while (Time.frameCount < ready) yield return null;
            var button = _world.Hud.transform.Find("CompletionPanel/Retry").GetComponent<Button>();
            Canvas.ForceUpdateCanvases(); var pointer = new PointerEventData(EventSystem.current);
            pointer.position = RectTransformUtility.WorldToScreenPoint(null, button.transform.position);
            var hits = new List<RaycastResult>(); EventSystem.current.RaycastAll(pointer, hits);
            Require(hits.Count > 0 && (hits[0].gameObject == button.gameObject || hits[0].gameObject.transform.IsChildOf(button.transform)), "重试按钮没有实际射线命中。");
            ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Require(_world.OwnedEntityCount == 1 && !checkpoint.IsActivated && flow.CurrentCurrency == 0, "按钮重试未复位世界。");
            foreach (var wave in waves) Require(!wave.HasStarted && wave.SpawnedCount == 0, "重试留下旧波次。");
            foreach (var encounter in encounters) Require(!encounter.HasStarted && !encounter.IsCompleted && encounter.Enemy == null, "重试留下旧遭遇。");
            health.ReceiveDamage(new DemoDamageInfo(10000, player));
            Require(health.RevivesRemaining == 0 && !health.IsRetryRequired && health.CurrentHealth > 0 && player.GetComponent<DemoSessionEntity>().Id == playerId,
                "首次复活改变实体或生命状态错误。");
            int retry = flow.RetryCount; health.ReceiveDamage(new DemoDamageInfo(10000, player));
            while (flow.RetryCount == retry) yield return null;
            Require(_world.OwnedEntityCount == 1 && health.CurrentHealth == health.MaxHealth && health.RevivesRemaining == 1 && combat.CurrentEnergy == 0,
                "二次死亡重试未恢复玩家。");
            body.position = waves[0].transform.position; Physics2D.SyncTransforms();
            while (waves[0].SpawnedCount != 4) yield return null;
            for (int i = 0; i < 4; i++) Require(waves[0].EnemyAt(i).GetComponent<DemoEnemyController>().enabled, "复用实体仍保留测试禁用或死亡状态。");
            flow.RestartRun(); Require(_world.OwnedEntityCount == 1, "重试未回收复用实体。");
            Debug.Log("[Demo.EntityQA] PASS：场景无实体/初始仅玩家/两波各4动态敌人/加载取消及资源失败清理/击败回收/检查点/精英奖励/Boss二阶段奖励货币/按钮重试/复活二死重试/实体复用复位。缺失QA资源警告为预期。");
        }
        private static void Tick()
        {
            try
            {
                if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup > _deadline) throw new InvalidOperationException("动态实体专项超时。");
                if (!_routine.MoveNext()) Stop();
            }
            catch (Exception error) { Stop(); Debug.LogException(error); }
        }
        private static void Changed(PlayModeStateChange state) { if (state == PlayModeStateChange.ExitingPlayMode) Stop(); }
        private static void Stop()
        {
            EditorApplication.update -= Tick; EditorApplication.playModeStateChanged -= Changed; AssemblyReloadEvents.beforeAssemblyReload -= Stop;
            _testLease?.Dispose(); _testLease = null;
            if (_world != null && _world.Player != null)
            { _world.Player.GetComponent<DemoPlayerController>().enabled = _movementEnabled; _world.Player.GetComponent<DemoPlayerCombatController>().enabled = _combatEnabled; }
            _world = null; _routine = null;
        }
    }
}
