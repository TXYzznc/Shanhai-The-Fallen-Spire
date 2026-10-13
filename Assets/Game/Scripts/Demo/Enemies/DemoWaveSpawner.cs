using Shanhai.Demo.Combat;
using Shanhai.Demo.Configuration;
using Shanhai.Demo.Player;
using UnityEngine;

namespace Shanhai.Demo.Enemies
{
    /// <summary>触发时请求GF实体；仅持有本波票据，死亡/重试时回收。</summary>
    public sealed class DemoWaveSpawner : MonoBehaviour
    {
        [SerializeField] private DemoGameplayConfig gameplayConfig;
        [SerializeField] private string spawnId = "common_wave_01";
        [SerializeField] private Transform player;
        [SerializeField] private GameObject[] enemies = new GameObject[0];
        [SerializeField] private Vector3[] spawnPositions;
        [SerializeField] private Vector3[] spawnEuler;
        [SerializeField] private Vector3[] spawnScales;
        private DemoWorldEntry _session;
        private DemoEntityLease[] _leases;
        private DemoDamageReceiver[] _receivers;
        private bool[] _defeated;
        private DemoSpawnConfig _spawn;
        private float _radiusSquared;
        private bool _failed;
        public bool HasValidConfiguration { get; private set; }
        public int ConfiguredCount { get; private set; }
        public int Wave { get; private set; }
        public float ActivationRadius { get; private set; }
        public bool HasStarted { get; private set; }
        public bool IsCompleted { get; private set; }
        public int SpawnedCount { get; private set; }
        public GameObject EnemyAt(int index) => enemies != null && index >= 0 && index < enemies.Length ? enemies[index] : null;
        public void BindSession(DemoWorldEntry session, Transform target)
        {
            _session = session; player = target;
            _spawn = gameplayConfig != null ? gameplayConfig.FindSpawn(spawnId) : null;
            HasValidConfiguration = _spawn != null && player != null && _spawn.count > 0 && _spawn.activationRadius > 0f
                && gameplayConfig.FindEnemy(_spawn.enemyId) != null && spawnPositions != null && spawnPositions.Length >= _spawn.count
                && spawnEuler != null && spawnEuler.Length >= _spawn.count && spawnScales != null && spawnScales.Length >= _spawn.count;
            if (!HasValidConfiguration) { Debug.LogError("[Demo.Wave] 配置无效：" + spawnId, this); return; }
            ConfiguredCount = _spawn.count; Wave = _spawn.wave; ActivationRadius = _spawn.activationRadius;
            _radiusSquared = ActivationRadius * ActivationRadius;
            enemies = new GameObject[ConfiguredCount]; _receivers = new DemoDamageReceiver[ConfiguredCount];
            _leases = new DemoEntityLease[ConfiguredCount]; _defeated = new bool[ConfiguredCount];
        }
        private void Update()
        {
            if (!HasValidConfiguration || _failed || _session == null || !_session.IsReady) return;
            if (!HasStarted && ((Vector2)(player.position - transform.position)).sqrMagnitude <= _radiusSquared)
            {
                HasStarted = true;
                for (int i = 0; i < ConfiguredCount && !_failed; i++)
                {
                    int slot = i;
                    _leases[i] = _session.ShowEnemy(gameplayConfig, _spawn.enemyId, spawnPositions[i], spawnEuler[i], spawnScales[i],
                        instance => { enemies[slot] = instance; _receivers[slot] = instance.GetComponent<DemoDamageReceiver>(); SpawnedCount++; }, Failed);
                }
            }
            if (!HasStarted || IsCompleted || SpawnedCount != ConfiguredCount) return;
            bool completed = true;
            for (int i = 0; i < ConfiguredCount; i++)
            {
                if (!_defeated[i] && _receivers[i] != null && _receivers[i].IsDefeated)
                {
                    _defeated[i] = true; _leases[i]?.Dispose(); _leases[i] = null; enemies[i] = null; _receivers[i] = null;
                }
                if (!_defeated[i]) completed = false;
            }
            IsCompleted = completed;
        }
        private void Failed(string error)
        {
            ResetForRetry(); _failed = true;
            Debug.LogError("[Demo.Wave] 加载失败，重试前停止本波：" + error, this);
        }
        public void ResetForRetry()
        {
            HasStarted = false; IsCompleted = false; SpawnedCount = 0; _failed = false;
            if (_leases == null) return;
            for (int i = 0; i < _leases.Length; i++)
            {
                _leases[i]?.Dispose(); _leases[i] = null; _receivers[i] = null; enemies[i] = null; _defeated[i] = false;
            }
        }
        private void OnDestroy() { ResetForRetry(); player = null; _session = null; }
    }
}
