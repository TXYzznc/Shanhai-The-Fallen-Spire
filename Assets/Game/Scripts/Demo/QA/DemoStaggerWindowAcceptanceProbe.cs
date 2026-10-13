using System.Collections;
using Shanhai.Demo.Combat;
using System.Reflection;
using Shanhai.Demo.Configuration;
using Shanhai.Demo.Enemies;
using UnityEngine;

namespace Shanhai.Demo.QA
{
    public sealed class DemoStaggerWindowAcceptanceProbe : MonoBehaviour
    {
        [SerializeField] private DemoEnemyController sceneEnemy;
        [SerializeField] private DemoGameplayConfig gameplayConfig;
        public bool SceneConfigurationBound { get; private set; }
        public bool ThresholdMatchesTable { get; private set; }
        public bool WindowNotRenewed { get; private set; }
        public bool WithinWindowTriggers { get; private set; }
        public bool StaggerBonus { get; private set; }
        public bool DurationExpires { get; private set; }
        public bool ExpiredDamageNormal { get; private set; }
        public bool ResetClearsStateAndAccumulation { get; private set; }
        public bool DisabledStagger { get; private set; }
        public bool ZeroDamageIgnored { get; private set; }
        public bool CustomProfileBound { get; private set; }
        public bool CustomWindowExpires { get; private set; }
        public bool Completed { get; private set; }
        private float _oldCaptureDelta;
        private bool _captured;

        private void OnDisable()
        {
            if (_captured) Time.captureDeltaTime = _oldCaptureDelta;
        }

        private IEnumerator Start()
        {
            yield return null;
            _oldCaptureDelta = Time.captureDeltaTime;
            _captured = true;
            Time.captureDeltaTime = 0.05f;
            sceneEnemy.gameObject.SetActive(true);
            sceneEnemy.enabled = false;
            DemoDamageReceiver actual = sceneEnemy.GetComponent<DemoDamageReceiver>();
            DemoEnemyConfig profile = gameplayConfig.FindEnemy("common_beast");
            SceneConfigurationBound = actual.MaxHealth == profile.maxHealth && profile.staggerThreshold == 30f
                && profile.staggerWindow == 3f && profile.staggerDuration == 2f;
            actual.ReceiveDamage(new DemoDamageInfo(10f, gameObject));
            actual.ReceiveDamage(new DemoDamageInfo(10f, gameObject));
            ThresholdMatchesTable = !actual.IsStaggered;
            actual.ReceiveDamage(new DemoDamageInfo(10f, gameObject));
            ThresholdMatchesTable &= actual.IsStaggered;

            var root = new GameObject("QA_StaggerWindowFixture");
            DemoDamageReceiver receiver = root.AddComponent<DemoDamageReceiver>();
            receiver.ConfigureHealth(500f, 0.15f);
            receiver.ConfigureStagger(30f, 3f, 2f, 1.5f);
            receiver.ReceiveDamage(new DemoDamageInfo(10f, gameObject));
            yield return new WaitForSeconds(2.1f);
            receiver.ReceiveDamage(new DemoDamageInfo(10f, gameObject));
            yield return new WaitForSeconds(1.15f);
            receiver.ReceiveDamage(new DemoDamageInfo(10f, gameObject));
            WindowNotRenewed = !receiver.IsStaggered && Mathf.Abs(receiver.CurrentHealth - 470f) < 0.001f;
            receiver.ReceiveDamage(new DemoDamageInfo(10f, gameObject));
            receiver.ReceiveDamage(new DemoDamageInfo(10f, gameObject));
            WithinWindowTriggers = receiver.IsStaggered;
            float before = receiver.CurrentHealth;
            receiver.ReceiveDamage(new DemoDamageInfo(10f, gameObject));
            StaggerBonus = Mathf.Abs(before - receiver.CurrentHealth - 15f) < 0.001f;
            yield return new WaitForSeconds(2.1f);
            DurationExpires = !receiver.IsStaggered && receiver.DamageTakenMultiplier == 1f;
            before = receiver.CurrentHealth;
            receiver.ReceiveDamage(new DemoDamageInfo(1f, gameObject));
            ExpiredDamageNormal = Mathf.Abs(before - receiver.CurrentHealth - 1f) < 0.001f;
            receiver.ReceiveDamage(new DemoDamageInfo(30f, gameObject));
            receiver.ResetHealth();
            ResetClearsStateAndAccumulation = !receiver.IsStaggered && !receiver.IsHitStunned && receiver.CurrentHealth == 500f;
            receiver.ReceiveDamage(new DemoDamageInfo(10f, gameObject));
            ResetClearsStateAndAccumulation &= !receiver.IsStaggered;
            receiver.ResetHealth();
            receiver.ConfigureStagger(0f, 3f, 2f, 1.5f);
            receiver.ReceiveDamage(new DemoDamageInfo(60f, gameObject));
            DisabledStagger = !receiver.IsStaggered && receiver.CurrentHealth == 440f;
            receiver.ResetHealth();
            receiver.ReceiveDamage(new DemoDamageInfo(0f, gameObject));
            ZeroDamageIgnored = receiver.CurrentHealth == 500f && !receiver.IsHitStunned && !receiver.IsStaggered;
            Destroy(root);
            var customConfig = Instantiate(gameplayConfig);
            DemoEnemyConfig custom = customConfig.FindEnemy("common_beast");
            custom.maxHealth = 120;
            custom.hitStunDuration = 0.35f;
            custom.staggerThreshold = 40f;
            custom.staggerWindow = 0.3f;
            custom.staggerDuration = 0.2f;
            custom.staggerDamageMultiplier = 2f;
            root = new GameObject("QA_CustomEnemyProfile");
            root.SetActive(false);
            root.AddComponent<Rigidbody2D>();
            receiver = root.AddComponent<DemoDamageReceiver>();
            DemoEnemyController controller = root.AddComponent<DemoEnemyController>();
            typeof(DemoEnemyController).GetField("gameplayConfig", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(controller, customConfig);
            typeof(DemoEnemyController).GetField("target", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(controller, transform);
            root.SetActive(true);
            controller.enabled = false;
            receiver.ReceiveDamage(new DemoDamageInfo(20f, gameObject));
            receiver.ReceiveDamage(new DemoDamageInfo(15f, gameObject));
            CustomProfileBound = receiver.MaxHealth == 120f && !receiver.IsStaggered;
            receiver.ReceiveDamage(new DemoDamageInfo(5f, gameObject));
            CustomProfileBound &= receiver.IsStaggered && receiver.CurrentHealth == 75f;
            yield return new WaitForSeconds(0.25f);
            CustomProfileBound &= !receiver.IsStaggered && receiver.IsHitStunned;
            receiver.ResetHealth();
            receiver.ReceiveDamage(new DemoDamageInfo(20f, gameObject));
            yield return new WaitForSeconds(0.35f);
            receiver.ReceiveDamage(new DemoDamageInfo(20f, gameObject));
            CustomWindowExpires = !receiver.IsStaggered && receiver.CurrentHealth == 80f;
            Destroy(root);
            Destroy(customConfig);
            actual.ResetHealth();
            Time.captureDeltaTime = _oldCaptureDelta;
            Completed = SceneConfigurationBound && ThresholdMatchesTable && WindowNotRenewed && WithinWindowTriggers
                && StaggerBonus && DurationExpires && ExpiredDamageNormal && ResetClearsStateAndAccumulation && DisabledStagger && ZeroDamageIgnored
                && CustomProfileBound && CustomWindowExpires;
        }
    }
}
