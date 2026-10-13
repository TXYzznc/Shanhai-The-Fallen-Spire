using System.Collections;
using Shanhai.Demo.Build;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Enemies;
using Shanhai.Demo.Player;
using UnityEngine;

namespace Shanhai.Demo.QA
{
    /// <summary>单个真实敌人AI的伤害与生命周期验收；只临时挂载。</summary>
    public sealed class DemoEnemyCycleAcceptanceProbe : MonoBehaviour
    {
        [SerializeField] private DemoEnemyController enemy;
        [SerializeField] private DemoDamageReceiver receiver;
        [SerializeField] private DemoPlayerHealthController health;
        [SerializeField] private Rigidbody2D playerBody;
        [SerializeField] private DemoWaveSpawner wave;
        [SerializeField] private DemoEnemyController[] otherWaveEnemies;
        public bool SpawnedByWave { get; private set; }
        public float FirstAttackDamage { get; private set; }
        public int FirstAttackCount { get; private set; }
        public bool SingleHit { get; private set; }
        public bool CooldownObserved { get; private set; }
        public bool ChaseObserved { get; private set; }
        public bool WallBlocksChase { get; private set; }
        public bool TriggerDoesNotBlock { get; private set; }
        public bool HitStunStopsChase { get; private set; }
        public bool DeathStopsChaseAndAttack { get; private set; }
        public bool DeathReleasesCollision { get; private set; }
        public bool RetryRestoresCollision { get; private set; }
        public bool RetryRestored { get; private set; }
        public bool Completed { get; private set; }
        private float _originalTimeScale;
        private float _originalCaptureDelta;
        private bool _timeSettingsCaptured;

        private void OnDisable()
        {
            if (!_timeSettingsCaptured) return;
            Time.timeScale = _originalTimeScale;
            Time.captureDeltaTime = _originalCaptureDelta;
        }

        private IEnumerator Start()
        {
            _originalTimeScale = Time.timeScale;
            _originalCaptureDelta = Time.captureDeltaTime;
            _timeSettingsCaptured = true;
            yield return null;
            GetComponent<DemoPlayerController>().enabled = false;
            GetComponent<DemoPlayerCombatController>().enabled = false;
            GetComponent<DemoRunFlowController>().enabled = false;
            GetComponent<DemoMarkLoadout>().ResetForRetry();
            GetComponent<DemoBuffLoadout>().ResetForRetry();
            health.ResetForRetry();
            Vector2 initialEnemyPosition = enemy.GetComponent<Rigidbody2D>().position;
            float initialEnemyRotation = enemy.GetComponent<Rigidbody2D>().rotation;
            for (int i = 0; i < otherWaveEnemies.Length; i++) otherWaveEnemies[i].enabled = false;
            playerBody.position = wave.transform.position;
            Physics2D.SyncTransforms();
            while (!wave.HasStarted) yield return null;
            SpawnedByWave = wave.HasValidConfiguration && wave.SpawnedCount == wave.ConfiguredCount
                && wave.SpawnedCount == 4 && enemy.gameObject.activeSelf;
            playerBody.position = Vector2.zero;
            enemy.ResetForRetry();
            receiver.ResetHealth();
            Rigidbody2D body = enemy.GetComponent<Rigidbody2D>();
            body.position = Vector2.up;
            Physics2D.SyncTransforms();
            float initialHealth = health.CurrentHealth;
            while (enemy.AttackCount == 0) yield return null;
            float oldScale = Time.timeScale;
            Time.timeScale = 0f;
            yield return null;
            yield return null;
            FirstAttackDamage = initialHealth - health.CurrentHealth;
            FirstAttackCount = enemy.AttackCount;
            SingleHit = Mathf.Abs(FirstAttackDamage - 8f) < 0.001f && FirstAttackCount == 1;
            yield return null;
            CooldownObserved = enemy.AttackCount == FirstAttackCount;
            Time.timeScale = oldScale;
            float oldCaptureDelta = Time.captureDeltaTime;
            Time.captureDeltaTime = 0.02f;
            for (int i = 0; i < 10; i++) yield return null;
            CooldownObserved &= enemy.AttackCount == FirstAttackCount && health.CurrentHealth == initialHealth - FirstAttackDamage;
            playerBody.position = new Vector2(0f, 3f);
            body.position = Vector2.zero;
            float chaseStartedAt = Time.time;
            while (Time.time - chaseStartedAt < 0.2f) yield return null;
            ChaseObserved = body.position.y > 0.2f && body.position.y < 1.5f;
            var wall = new GameObject("QA_EnemyChaseWall");
            wall.transform.position = new Vector3(0f, 2f, 0f);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(10f, 0.1f);
            var trigger = new GameObject("QA_EnemyChaseTrigger");
            trigger.transform.position = new Vector3(0f, 0.8f, 0f);
            var triggerCollider = trigger.AddComponent<BoxCollider2D>();
            triggerCollider.size = new Vector2(10f, 0.1f);
            triggerCollider.isTrigger = true;
            playerBody.position = new Vector2(0f, 5f);
            body.position = Vector2.zero;
            Physics2D.SyncTransforms();
            Time.captureDeltaTime = 0.1f;
            for (int i = 0; i < 20; i++) yield return null;
            WallBlocksChase = body.position.y > 0.4f && body.position.y < 2f;
            TriggerDoesNotBlock = body.position.y > 1.1f;
            Destroy(wall);
            Destroy(trigger);
            yield return null;
            Time.captureDeltaTime = 0.02f;
            body.position = Vector2.zero;
            playerBody.position = new Vector2(0f, 3f);
            Physics2D.SyncTransforms();
            receiver.ReceiveDamage(new DemoDamageInfo(1f, gameObject));
            Vector2 stunnedPosition = body.position;
            yield return null;
            yield return null;
            HitStunStopsChase = receiver.IsHitStunned && Vector2.Distance(body.position, stunnedPosition) < 0.001f;
            receiver.ReceiveDamage(new DemoDamageInfo(10000f, gameObject));
            int attackCount = enemy.AttackCount;
            yield return null;
            bool deadStoppedMoving = Vector2.Distance(body.position, stunnedPosition) < 0.001f;
            playerBody.position = body.position + Vector2.up;
            yield return null;
            DeathStopsChaseAndAttack = receiver.IsDefeated && enemy.AttackCount == attackCount
                && deadStoppedMoving && Vector2.Distance(body.position, stunnedPosition) < 0.001f;
            DeathReleasesCollision = !enemy.GetComponent<Collider2D>().enabled;
            enemy.ResetForRetry();
            receiver.ResetHealth();
            RetryRestoresCollision = enemy.GetComponent<Collider2D>().enabled;
            RetryRestored = !receiver.IsDefeated && receiver.CurrentHealth == receiver.MaxHealth && enemy.AttackCount == 0
                && Vector2.Distance(body.position, initialEnemyPosition) < 0.001f
                && Mathf.Abs(Mathf.DeltaAngle(body.rotation, initialEnemyRotation)) < 0.001f
                && body.velocity == Vector2.zero && body.angularVelocity == 0f;
            Time.timeScale = oldScale;
            Time.captureDeltaTime = oldCaptureDelta;
            Completed = SpawnedByWave && SingleHit && CooldownObserved && ChaseObserved && HitStunStopsChase && DeathStopsChaseAndAttack
                && WallBlocksChase && TriggerDoesNotBlock && DeathReleasesCollision && RetryRestoresCollision && RetryRestored;
        }
    }
}
