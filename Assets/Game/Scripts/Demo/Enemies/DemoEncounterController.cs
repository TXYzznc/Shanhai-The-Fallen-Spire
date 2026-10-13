using Shanhai.Demo.Combat;
using Shanhai.Demo.UI;
using Shanhai.Demo.Configuration;
using Shanhai.Demo.Player;
using UnityEngine;

namespace Shanhai.Demo.Enemies
{
    /// <summary>2D遭遇触发后动态生成实体，胜利后只保留进度与奖励状态。</summary>
    public sealed class DemoEncounterController : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private GameObject enemy;
        [SerializeField] private DemoDamageReceiver enemyReceiver;
        private IDemoRewardPresenter rewardView;
        [SerializeField] private GameObject closedGate;
        [SerializeField] private string spawnId;
        [SerializeField] private bool sealEntranceOnStart;
        [SerializeField] private bool grantsBossCurrency;
        [SerializeField] private DemoGameplayConfig gameplayConfig;
        [SerializeField] private DemoRunFlowController runFlow;
        [SerializeField] private Vector3 spawnPosition;
        [SerializeField] private Vector3 spawnEuler;
        [SerializeField] private Vector3 spawnScale = Vector3.one;
        private DemoWorldEntry _session;
        private DemoEntityLease _lease;
        private DemoSpawnConfig _spawn;
        private float _radiusSquared;
        private DemoDropConfig _drop;
        private bool _isBoss, _failed;
        public bool HasStarted { get; private set; }
        public bool IsCompleted { get; private set; }
        public bool HasValidConfiguration { get; private set; }
        public float ActivationRadius { get; private set; }
        public string RewardTableId => _drop != null ? _drop.id : string.Empty;
        public GameObject Enemy => enemy;
        public void BindView(IDemoRewardPresenter view) { rewardView = view; }
        public void BindSession(DemoWorldEntry session, Transform target, IDemoRewardPresenter view, DemoRunFlowController flow)
        {
            _session = session; player = target; rewardView = view; runFlow = flow;
            _spawn = gameplayConfig != null ? gameplayConfig.FindSpawn(spawnId) : null;
            DemoEnemyConfig config = _spawn != null ? gameplayConfig.FindEnemy(_spawn.enemyId) : null;
            _drop = config != null ? gameplayConfig.FindDrop(config.dropTableId) : null;
            HasValidConfiguration = _spawn != null && _spawn.count == 1 && _spawn.activationRadius > 0f && player != null
                && config != null && _drop != null && _drop.IsChoiceReward && _drop.amount == 1 && view != null;
            if (!HasValidConfiguration) { Debug.LogError("[Demo.Encounter] 配置无效：" + spawnId, this); return; }
            ActivationRadius = _spawn.activationRadius; _radiusSquared = ActivationRadius * ActivationRadius; _isBoss = config.isBoss;
        }
        private void Awake() { if (closedGate != null) closedGate.SetActive(!sealEntranceOnStart); }
        public void ResetForRetry()
        {
            HasStarted = false; IsCompleted = false; _failed = false;
            _lease?.Dispose(); _lease = null; enemy = null; enemyReceiver = null;
            if (closedGate != null) closedGate.SetActive(!sealEntranceOnStart);
            if (rewardView != null) rewardView.Hide();
        }
        private void Update()
        {
            if (!HasValidConfiguration || _failed || _session == null || !_session.IsReady) return;
            if (!HasStarted && ((Vector2)(player.position - transform.position)).sqrMagnitude <= _radiusSquared)
            {
                HasStarted = true;
                if (sealEntranceOnStart && closedGate != null) closedGate.SetActive(true);
                _lease = _session.ShowEnemy(gameplayConfig, _spawn.enemyId, spawnPosition, spawnEuler, spawnScale,
                    instance => { enemy = instance; enemyReceiver = instance.GetComponent<DemoDamageReceiver>(); }, Failed);
            }
            if (HasStarted && !IsCompleted && enemyReceiver != null && enemyReceiver.IsDefeated)
            {
                IsCompleted = true;
                if (grantsBossCurrency && runFlow != null) runFlow.AddCurrency(gameplayConfig.BossCurrencyReward);
                if (closedGate != null) closedGate.SetActive(false);
                rewardView.OpenDrop(_drop, _isBoss);
                _lease?.Dispose(); _lease = null; enemy = null; enemyReceiver = null;
            }
        }
        private void Failed(string error)
        {
            ResetForRetry(); _failed = true;
            Debug.LogError("[Demo.Encounter] 加载失败，重试前停止遭遇：" + error, this);
        }
        private void OnDestroy()
        {
            ResetForRetry(); _session = null; player = null; rewardView = null; runFlow = null;
        }
    }
}
