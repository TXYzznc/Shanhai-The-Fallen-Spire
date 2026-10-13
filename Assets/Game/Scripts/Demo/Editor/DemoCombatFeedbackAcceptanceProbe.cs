using System;
using System.Collections;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Configuration;
using Shanhai.Demo.Enemies;
using Shanhai.Demo.Input;
using Shanhai.Demo.Player;
using Shanhai.Demo.Presentation;
using Shanhai.Demo.UI;
using UnityEditor;
using UnityEngine;

namespace Shanhai.Demo.Editor
{
    /// <summary>展示专项用直接伤害验证数值边界，不替代连续语义战斗验收。</summary>
    internal static class DemoCombatFeedbackAcceptanceProbe
    {
        private static IEnumerator _routine;
        private static double _deadline;
        private static DemoWorldEntry _world;
        private static DemoEntityLease _lease;
        private static float _timeScale;
        [MenuItem("Shanhai/Demo/Verify combat feedback")]
        private static void Begin()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("先从Launch进入游戏。");
            Stop(); _timeScale = Time.timeScale; _deadline = EditorApplication.timeSinceStartup + 45;
            _routine = Run(); EditorApplication.update += Tick; EditorApplication.playModeStateChanged += Changed;
            AssemblyReloadEvents.beforeAssemblyReload += Stop;
        }
        private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        private static IEnumerator Frames(int count)
        { int target = Time.frameCount + count; while (Time.frameCount < target) yield return null; }
        private static IEnumerator Run()
        {
            do { yield return null; _world = UnityEngine.Object.FindObjectOfType<DemoWorldEntry>(); } while (_world == null || !_world.IsReady);
            var player = _world.Player; var combat = player.GetComponent<DemoPlayerCombatController>();
            var movement = player.GetComponent<DemoPlayerController>(); var health = player.GetComponent<DemoPlayerHealthController>();
            var flow = player.GetComponent<DemoRunFlowController>(); var feedback = _world.Feedback;
            movement.enabled = combat.enabled = false; flow.RestartRun();
            Require(combat.TryStartAttack(), "有效普攻被拒绝。");
            Require(!combat.TryStartAttack() && !combat.TryUseSkill() && !combat.TryUseUltimate() && feedback.AttackCount == 1, "锁定动作产生假反馈。");
            while (combat.IsAttackLocked) yield return null;
            Require(combat.TryUseSkill() && !combat.TryUseSkill() && feedback.AttackCount == 2, "技能冷却产生假反馈。");
            Require(!combat.TryUseUltimate() && feedback.AttackCount == 2, "空能量产生假反馈。");
            float expires = Time.time + 1f; while (Time.time < expires) yield return null;
            Require(feedback.ActiveCount == 0, "展示寿命结束后未回收。");
            var worldFields = new SerializedObject(_world); var wave = (DemoWaveSpawner)worldFields.FindProperty("waves").GetArrayElementAtIndex(0).objectReferenceValue;
            var waveFields = new SerializedObject(wave); var config = (DemoGameplayConfig)waveFields.FindProperty("gameplayConfig").objectReferenceValue;
            string id = config.FindSpawn(waveFields.FindProperty("spawnId").stringValue).enemyId;
            GameObject enemy = null;
            _lease = _world.ShowEnemy(config, id, player.transform.position + Vector3.up, Vector3.zero, Vector3.one,
                instance => enemy = instance, error => { throw new InvalidOperationException(error); });
            while (enemy == null) yield return null;
            enemy.GetComponent<DemoEnemyController>().enabled = false;
            var receiver = enemy.GetComponent<DemoDamageReceiver>(); receiver.ConfigureStagger(10f, 3f, 2f, 1.5f);
            float previous = receiver.CurrentHealth; int hits = feedback.HitCount;
            receiver.ReceiveDamage(new DemoDamageInfo(10f, player));
            Require(Mathf.Approximately(receiver.CurrentHealth, previous - 15f) && feedback.HitCount == hits + 1 && Mathf.Approximately(feedback.LastDamage, 15f), "失衡易伤或实际伤害显示改变。");
            receiver.ReceiveDamage(new DemoDamageInfo(0f, player)); Require(feedback.HitCount == hits + 1, "无效伤害产生反馈。");
            combat.ResetForRetry(); combat.TryStartAttack();
            yield return Frames(1); Time.timeScale = 0f; yield return Frames(2);
            Require(enemy.GetComponent<DemoSpriteFeedback>().CurrentState == DemoSpriteState.Hit, "失衡状态掩盖受击闪烁。");
            var particles = feedback.GetComponentsInChildren<ParticleSystem>(false);
            Require(particles.Length > 0 && particles[0].particleCount > 0, "命中粒子未发射。");
            var buffer = new ParticleSystem.Particle[32];
            foreach (var particle in particles)
            {
                int count = particle.GetParticles(buffer);
                for (int i = 0; i < count; i++) Require(Mathf.Abs(buffer[i].position.z) < .001f && Mathf.Abs(buffer[i].velocity.z) < .001f, "粒子离开2D平面。");
            }
            var texts = _world.Ui.Overlay.DamageLayer.GetComponentsInChildren<DemoDamageTextItem>(false);
            Require(texts.Length == 1 && Mathf.Approximately(texts[0].Amount, 15f), "飘字Item未显示真实伤害。");
            texts[0].GetComponent<TMPro.TMP_Text>().ForceMeshUpdate();
            string number = texts[0].GetComponent<TMPro.TMP_Text>().GetParsedText();
            Require(float.TryParse(number, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float shownAmount)
                && Mathf.Approximately(shownAmount, 15f), "飘字数字格式错误：" + number);
            Require(enemy.GetComponent<SpriteRenderer>().sharedMaterial.HasProperty("_FlashAmount") && enemy.GetComponent<DemoSpriteFeedback>().FlashAmount == 1f,
                "受击闪白Shader没有应用。");
            ScreenCapture.CaptureScreenshot("Assets/Screenshots/DemoCombatFeedback.png"); yield return Frames(3); Time.timeScale = _timeScale;
            previous = receiver.CurrentHealth; receiver.ReceiveDamage(new DemoDamageInfo(10000f, player));
            Require(Mathf.Approximately(feedback.LastDamage, previous), "过量伤害显示了请求量。");
            hits = feedback.HitCount; receiver.ReceiveDamage(new DemoDamageInfo(10f, player)); Require(feedback.HitCount == hits, "尸体产生伤害反馈。");
            receiver.ResetHealth(); receiver.ConfigureStagger(0f, 0f, 0f, 1f);
            for (int i = 0; i < 80; i++) { receiver.ResetHealth(); receiver.ReceiveDamage(new DemoDamageInfo(1f, player)); }
            var presentation = (DemoPresentationConfig)worldFields.FindProperty("presentation").objectReferenceValue;
            Require(feedback.GetComponentsInChildren<ParticleSystem>(true).Length == presentation.HitCapacity
                && _world.Ui.Overlay.DamageLayer.GetComponentsInChildren<DemoDamageTextItem>(true).Length == presentation.TextCapacity, "展示池饱和后容量增长。");
            flow.RestartRun(); hits = feedback.HitCount;
            Require(movement.TryDodge(new DemoInputState(Vector2.right, Vector2.zero, false, true, false, false)), "闪避未成功。");
            health.ReceiveDamage(new DemoDamageInfo(10f, enemy)); Require(feedback.HitCount == hits, "无敌拒绝产生反馈。");
            movement.ResetForRetry(); previous = health.CurrentHealth; health.ReceiveDamage(new DemoDamageInfo(10000f, enemy));
            Require(health.RevivesRemaining == 0 && health.CurrentHealth > 0f && Mathf.Approximately(feedback.LastDamage, previous), "复活同帧实际伤害丢失。");
            yield return Frames(1); Require(player.GetComponent<DemoSpriteFeedback>().CurrentState == DemoSpriteState.Hit, "复活命中未闪白。");
            _lease.Dispose(); _lease = null; hits = feedback.HitCount;
            receiver.ResetHealth(); receiver.ReceiveDamage(new DemoDamageInfo(10f, player));
            Require(feedback.HitCount == hits && !enemy.GetComponent<DemoSpriteFeedback>().enabled, "回收实体仍订阅旧会话。");
            flow.RestartRun(); Require(feedback.ActiveCount == 0 && feedback.HitCount == 0 && feedback.AttackCount == 0, "重试有表现残留。");
            Debug.Log("[Demo.CombatFeedbackQA] PASS 有效动作/冷却锁定/空能量、实际失衡伤害15、过量封顶、尸体/零伤害/无敌拒绝、复活闪白、XY粒子、80次命中固定容量和重试清空。");
        }
        private static void Tick()
        {
            try
            {
                if (EditorApplication.timeSinceStartup > _deadline) throw new TimeoutException("战斗表现验收超时。");
                if (!_routine.MoveNext()) Stop();
                else if (_routine.Current is IEnumerator nested) _routine = Flatten(_routine, nested);
            }
            catch (Exception error) { Debug.LogException(error); Stop(); }
        }
        private static IEnumerator Flatten(IEnumerator parent, IEnumerator nested)
        { while (nested.MoveNext()) yield return nested.Current; while (parent.MoveNext()) yield return parent.Current; }
        private static void Changed(PlayModeStateChange state) { if (state == PlayModeStateChange.ExitingPlayMode) Stop(); }
        private static void Stop()
        {
            EditorApplication.update -= Tick; EditorApplication.playModeStateChanged -= Changed; AssemblyReloadEvents.beforeAssemblyReload -= Stop;
            if (_routine == null) return;
            _routine = null; Time.timeScale = _timeScale; _lease?.Dispose(); _lease = null;
            if (_world != null && _world.Player != null)
            { _world.Player.GetComponent<DemoRunFlowController>().RestartRun(); _world.Player.GetComponent<DemoPlayerController>().enabled = true; _world.Player.GetComponent<DemoPlayerCombatController>().enabled = true; }
            _world = null;
        }
    }
}
