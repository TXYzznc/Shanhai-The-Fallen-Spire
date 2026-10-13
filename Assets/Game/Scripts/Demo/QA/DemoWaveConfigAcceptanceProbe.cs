using System.Collections;
using System.Reflection;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Configuration;
using Shanhai.Demo.Enemies;
using UnityEngine;

namespace Shanhai.Demo.QA
{
    /// <summary>临时挂载；用独立配置副本检查数量、XY触发边界及一波重试。</summary>
    public sealed class DemoWaveConfigAcceptanceProbe : MonoBehaviour
    {
        [SerializeField] private DemoWaveSpawner sceneWave;
        [SerializeField] private DemoGameplayConfig gameplayConfig;
        [SerializeField] private GameObject[] enemies;
        public bool SceneBound { get; private set; }
        public bool OutsideRadius { get; private set; }
        public bool ConfigCountAndRadius { get; private set; }
        public bool PartialDefeat { get; private set; }
        public bool CompletedOnce { get; private set; }
        public bool RetryRestored { get; private set; }
        public bool RetryBeforeFirstSpawn { get; private set; }
        public bool SecondRun { get; private set; }
        public bool CapacityRejected { get; private set; }
        public bool DuplicateRejected { get; private set; }
        public bool MissingConfigRejected { get; private set; }
        public bool Completed { get; private set; }

        private IEnumerator Start()
        {
            yield return null;
            DemoSpawnConfig actual = gameplayConfig.FindSpawn("common_wave_01");
            SceneBound = sceneWave.HasValidConfiguration && sceneWave.ConfiguredCount == actual.count
                && sceneWave.ActivationRadius == actual.activationRadius && sceneWave.Wave == actual.wave && !sceneWave.HasStarted;
            sceneWave.enabled = false;
            for (int i = 0; i < enemies.Length; i++) enemies[i].GetComponent<DemoEnemyController>().enabled = false;
            var config = Instantiate(gameplayConfig);
            var firstPosition = enemies[0].transform.position;
            for (int i = 0; i < enemies.Length; i++) Set(enemies[i].GetComponent<DemoEnemyController>(), "gameplayConfig", config);
            config.FindSpawn("common_wave_01").count = 2;
            config.FindSpawn("common_wave_01").activationRadius = 2f;
            var target = new GameObject("QA_WaveTarget");
            target.transform.position = new Vector3(0f, 2.1f, 100f);
            DemoWaveSpawner wave = CreateWave(config, target.transform, enemies, "common_wave_01");
            yield return null;
            OutsideRadius = wave.HasValidConfiguration && !wave.HasStarted;
            enemies[0].transform.position += Vector3.right * 3f;
            enemies[0].transform.rotation = Quaternion.Euler(0f, 0f, 73f);
            wave.ResetForRetry();
            RetryBeforeFirstSpawn = Vector3.Distance(enemies[0].transform.position, firstPosition) < 0.01f;
            target.transform.position = new Vector3(0f, 1.9f, 100f);
            yield return null;
            ConfigCountAndRadius = wave.HasStarted && wave.SpawnedCount == 2 && wave.ConfiguredCount == 2
                && wave.ActivationRadius == 2f && enemies[0].activeSelf && enemies[1].activeSelf && !enemies[2].activeSelf;
            enemies[0].GetComponent<DemoDamageReceiver>().ReceiveDamage(new DemoDamageInfo(10000f, gameObject));
            yield return null;
            PartialDefeat = !wave.IsCompleted;
            enemies[1].GetComponent<DemoDamageReceiver>().ReceiveDamage(new DemoDamageInfo(10000f, gameObject));
            yield return null;
            target.transform.position = Vector3.up * 3f;
            yield return null;
            target.transform.position = Vector3.zero;
            yield return null;
            CompletedOnce = wave.IsCompleted && wave.SpawnedCount == 2;
            target.transform.position = Vector3.up * 3f;
            var initialPosition = enemies[0].transform.position;
            var initialRotation = enemies[0].transform.rotation;
            enemies[0].transform.position += Vector3.right * 2f;
            enemies[0].transform.rotation = Quaternion.Euler(0f, 0f, 90f);
            enemies[0].GetComponent<Rigidbody2D>().velocity = Vector2.one;
            wave.ResetForRetry();
            var receiver = enemies[0].GetComponent<DemoDamageReceiver>();
            RetryRestored = !wave.HasStarted && !wave.IsCompleted && wave.SpawnedCount == 0
                && !enemies[0].activeSelf && receiver.CurrentHealth == receiver.MaxHealth
                && Vector3.Distance(enemies[0].transform.position, initialPosition) < 0.01f
                && Quaternion.Angle(enemies[0].transform.rotation, initialRotation) < 0.01f
                && enemies[0].GetComponent<Rigidbody2D>().velocity == Vector2.zero;
            target.transform.position = Vector3.zero;
            yield return null;
            SecondRun = wave.HasStarted && !wave.IsCompleted && wave.SpawnedCount == 2;
            wave.ResetForRetry();
            wave.enabled = false;
            config.FindSpawn("common_wave_01").count = enemies.Length + 1;
            var invalid = CreateWave(config, target.transform, enemies, "common_wave_01");
            yield return null;
            CapacityRejected = !invalid.HasValidConfiguration && !invalid.HasStarted && !invalid.IsCompleted;
            Destroy(invalid.gameObject);
            config.FindSpawn("common_wave_01").count = 2;
            invalid = CreateWave(config, target.transform, new[] { enemies[0], enemies[0] }, "common_wave_01");
            yield return null;
            DuplicateRejected = !invalid.HasValidConfiguration && !invalid.HasStarted && !invalid.IsCompleted;
            Destroy(invalid.gameObject);
            invalid = CreateWave(config, target.transform, enemies, "missing_spawn");
            yield return null;
            MissingConfigRejected = !invalid.HasValidConfiguration && !invalid.HasStarted && !invalid.IsCompleted;
            Destroy(invalid.gameObject);
            Destroy(wave.gameObject);
            Destroy(target);
            Destroy(config);
            Completed = SceneBound && OutsideRadius && ConfigCountAndRadius && PartialDefeat && CompletedOnce
                && RetryRestored && RetryBeforeFirstSpawn && SecondRun && CapacityRejected && DuplicateRejected && MissingConfigRejected;
            Debug.Log($"[Demo.QA.Wave] Completed={Completed}，配置边界与重试验证完成；三条 Demo.Wave 错误为预期无效配置用例。");
        }

        private static DemoWaveSpawner CreateWave(DemoGameplayConfig config, Transform target, GameObject[] pool, string id)
        {
            var root = new GameObject("QA_WaveFixture");
            root.SetActive(false);
            var wave = root.AddComponent<DemoWaveSpawner>();
            Set(wave, "gameplayConfig", config);
            Set(wave, "spawnId", id);
            Set(wave, "player", target);
            Set(wave, "enemies", pool);
            root.SetActive(true);
            return wave;
        }

        private static void Set(object target, string field, object value) => target.GetType()
            .GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }
}
