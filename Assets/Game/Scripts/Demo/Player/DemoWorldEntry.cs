using System;
using System.Collections;
using System.Collections.Generic;
using GameFramework;
using GameFramework.Event;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Configuration;
using Shanhai.Demo.Enemies;
using Shanhai.Demo.UI;
using Shanhai.Demo.Presentation;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Shanhai.Demo.Player
{
    /// <summary>本局显式接入点，先打开HUD再启用玩法；后续实体请求也由此会话持有。</summary>
    public sealed class DemoWorldEntry : MonoBehaviour
    {
        [SerializeField] private DemoPresentationConfig presentation;
        [SerializeField] private DemoFeedbackSession feedback;
        public DemoFeedbackSession Feedback => feedback;
        private GameObject player;
        [SerializeField] private MonoBehaviour inputSource;
        [SerializeField] private Camera gameCamera;
        [SerializeField] private Vector3 playerStart;
        [SerializeField] private Vector3 playerScale = Vector3.one;
        [SerializeField] private DemoEncounterController boss;
        [SerializeField] private DemoEncounterController[] encounters;
        [SerializeField] private DemoWaveSpawner[] waves;
        [SerializeField] private DemoCheckpointController[] checkpoints;
        private bool _closing;
        private readonly List<DemoEntityLease> _entities = new List<DemoEntityLease>(12);
        public int OwnedEntityCount => _entities.Count;
        public bool IsReady { get; private set; }
        public DemoUiSession Ui { get; private set; }
        public DemoHudForm Hud => Ui != null ? Ui.Hud : null;
        public GameObject Player => player;
        private void Awake() { SetGameplay(false); }
        private Game.State.GamePauseController _pause;
        public Game.State.GamePauseController Pause => _pause;
        private IEnumerator Start()
        {
            while (GF.UI == null || GF.Event == null || GF.Entity == null) yield return null;
            if (presentation == null || string.IsNullOrEmpty(presentation.HudPrefabAsset))
            { Debug.LogError("[Demo.World] HUD资源配置缺失。", this); yield break; }

            Show(presentation.PlayerPrefabAsset, playerStart, Vector3.zero, playerScale, PlayerShown, SpawnFailed);
        }
        private void PlayerShown(GameObject instance)
        {
            player = instance;
            _pause = gameObject.AddComponent<Game.State.GamePauseController>();
            player.GetComponent<DemoPlayerController>().BindPause(_pause);
            player.GetComponent<DemoPlayerCombatController>().BindPause(_pause);
            player.GetComponent<DemoPlayerHealthController>().BindPause(_pause);
            player.GetComponent<DemoRunFlowController>().BindPause(_pause);
            player.GetComponent<Rigidbody2D>().position = playerStart;
            player.GetComponent<DemoPlayerController>().BindInput(inputSource, gameCamera);
            player.GetComponent<DemoPlayerCombatController>().BindInput(inputSource);
            player.GetComponent<DemoSpriteFeedback>().BindBaseScale(playerScale);
            gameCamera.GetComponent<DemoCameraFollow>().BindTarget(player.transform);
            SetGameplay(false);
            Ui = gameObject.AddComponent<DemoUiSession>();
            Ui.Initialize(presentation, player, boss, _pause, inputSource, UiReady);
        }
        private void UiReady()
        {
            player.GetComponent<DemoRunFlowController>().BindViews(Ui.RewardChoice, Ui);
            var flow = player.GetComponent<DemoRunFlowController>();
            flow.BindWorld(encounters, waves, checkpoints);
            foreach (var encounter in encounters) encounter.BindSession(this, player.transform, Ui, flow);
            foreach (var wave in waves) wave.BindSession(this, player.transform);
            foreach (var checkpoint in checkpoints) { checkpoint.BindViews(Ui.RewardChoice, Ui); checkpoint.BindRun(flow); }
            feedback.Initialize(presentation, Ui.Overlay.DamageLayer, gameCamera);
            flow.WorldReset += feedback.ResetFeedback;
            player.GetComponent<DemoCombatFeedbackBinding>().Bind(feedback);
            flow.ResetForNewSession();
            player.GetComponent<DemoSpriteFeedback>().ResetPresentation();
            IsReady = true; SetGameplay(true);
            Debug.Log("[Demo.World] HUD已由GF.UI动态加载并绑定当前会话。", this);
        }
        private void SetGameplay(bool enabled)
        {
            if (player != null)
            {
                player.GetComponent<DemoPlayerController>().enabled = enabled;
                player.GetComponent<DemoPlayerCombatController>().enabled = enabled;
                player.GetComponent<DemoRunFlowController>().enabled = enabled;
                player.GetComponent<DemoSpriteFeedback>().enabled = enabled;
            }
            foreach (var encounter in encounters) if (encounter != null) encounter.enabled = enabled;
            foreach (var wave in waves) if (wave != null) wave.enabled = enabled;
            foreach (var checkpoint in checkpoints)
                if (checkpoint != null) { checkpoint.enabled = enabled; checkpoint.GetComponent<Collider2D>().enabled = enabled; }
        }
        private DemoEntityLease Show(string asset, Vector3 position, Vector3 euler, Vector3 scale, Action<GameObject> shown, Action<string> failed)
        {
            if (_closing) throw new InvalidOperationException("会话正在退出，禁止生成实体。");
            var lease = new DemoEntityLease(shown, failed, Released);
            _entities.Add(lease); lease.Begin(asset, position, euler, scale); return lease;
        }
        private void Released(DemoEntityLease lease) { _entities.Remove(lease); }
        public DemoEntityLease ShowEnemy(DemoGameplayConfig config, string id, Vector3 position, Vector3 euler, Vector3 scale, Action<GameObject> shown, Action<string> failed)
        {
            DemoEnemyConfig enemy = config.FindEnemy(id);
            return Show(presentation.EnemyPrefabAsset(enemy), position, euler, scale, instance =>
            {
                var controller = instance.GetComponent<DemoEnemyController>();
                controller.BindPause(_pause);
                instance.GetComponent<DemoDamageReceiver>().BindPause(_pause);
                controller.BindConfiguration(config, id);
                var body = instance.GetComponent<Rigidbody2D>(); body.position = position; body.rotation = euler.z;
                controller.BindTarget(player.transform);
                instance.GetComponent<DemoSpriteFeedback>().BindBaseScale(scale);
                instance.GetComponent<DemoSpriteFeedback>().ResetPresentation();
                instance.GetComponent<DemoCombatFeedbackBinding>().Bind(feedback);
                instance.GetComponent<DemoEnemyStatusBinding>().Bind(Ui.Overlay.EnemyStatusLayer, gameCamera);
                controller.enabled = true; instance.GetComponent<DemoSpriteFeedback>().enabled = true;
                shown(instance);
            }, failed);
        }
        private void SpawnFailed(string error) { Debug.LogError("[Demo.World] 实体加载失败：" + error, this); }
        private void OnDestroy()
        {
            _closing = true; IsReady = false; SetGameplay(false);
            if (Ui != null) Ui.Shutdown();
            foreach (var encounter in encounters) if (encounter != null) encounter.BindView(null);
            foreach (var checkpoint in checkpoints) if (checkpoint != null) checkpoint.BindViews(null, null);
            if (player != null) player.GetComponent<DemoRunFlowController>().BindViews(null, null);
            foreach (var encounter in encounters) if (encounter != null) encounter.ResetForRetry();
            foreach (var wave in waves) if (wave != null) wave.ResetForRetry();
            if (gameCamera != null) gameCamera.GetComponent<DemoCameraFollow>().BindTarget(null);
            if (player != null && !ReferenceEquals(feedback, null)) player.GetComponent<DemoRunFlowController>().WorldReset -= feedback.ResetFeedback;
            while (_entities.Count > 0) _entities[_entities.Count - 1].Dispose();
            if (feedback != null) feedback.Release();
            player = null;
            Ui = null;
        }
    }
}
