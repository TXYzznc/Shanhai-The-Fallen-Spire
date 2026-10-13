using System.Collections;
using System.Reflection;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Configuration;
using Shanhai.Demo.Enemies;
using Shanhai.Demo.Input;
using Shanhai.Demo.Player;
using UnityEngine;

namespace Shanhai.Demo.QA
{
    /// <summary>隔离场景的精英冲撞物理专项，闪避方向通过正式输入抽象移动。</summary>
    public sealed class DemoEliteChargeAcceptanceProbe : MonoBehaviour, IDemoInputSource
    {
        [SerializeField] private DemoGameplayConfig gameplayConfig;
        public bool PursuitObserved { get; private set; }
        public bool WindupStationary { get; private set; }
        public bool ChargeMovesAndHitsOnce { get; private set; }
        public bool LockedDirectionCanBeAvoided { get; private set; }
        public bool WallBlocksDamageAndMovement { get; private set; }
        public bool TriggerDoesNotBlock { get; private set; }
        public bool HitStunCancelsWindup { get; private set; }
        public bool StaggerCancelsDash { get; private set; }
        public bool DeathCancelsDash { get; private set; }
        public bool RetryRestoresState { get; private set; }
        public bool NonDefaultDistanceApplied { get; private set; }
        public bool Completed { get; private set; }
        public bool Finished { get; private set; }
        public string Details { get; private set; }
        private DemoEnemyController _enemy;
        private DemoDamageReceiver _receiver;
        private DemoPlayerHealthController _health;
        private DemoPlayerController _movement;
        private DemoPlayerCombatController _combat;
        private DemoRunFlowController _flow;
        private Rigidbody2D _body, _enemyBody;
        private GameObject _enemyObject, _wall;
        private DemoGameplayConfig _copy;
        private DemoInputState _input;
        private object _oldInput;
        private Vector2 _start;
        private bool _combatEnabled, _flowEnabled, _captured;
        private float _captureDelta;
        public DemoInputState ReadState() => _input;
        private static FieldInfo InputField => typeof(DemoPlayerController).GetField("_inputSource", BindingFlags.Instance | BindingFlags.NonPublic);
        private IEnumerator Start()
        {
            yield return null;
            _body = GetComponent<Rigidbody2D>(); _start = _body.position;
            _movement = GetComponent<DemoPlayerController>(); _combat = GetComponent<DemoPlayerCombatController>();
            _flow = GetComponent<DemoRunFlowController>(); _health = GetComponent<DemoPlayerHealthController>();
            _combatEnabled = _combat.enabled; _flowEnabled = _flow.enabled; _oldInput = InputField.GetValue(_movement);
            _captureDelta = Time.captureDeltaTime; _captured = true; Time.captureDeltaTime = 0.05f;
            _combat.enabled = _flow.enabled = false; InputField.SetValue(_movement, this);
            _copy = Instantiate(gameplayConfig); _copy.FindEnemy("elite_beast").attackCooldown = 4f;
            _enemyObject = new GameObject("QA_ChargeElite"); _enemyObject.SetActive(false);
            _enemyObject.transform.position = new Vector3(10f, 3f);
            _enemyBody = _enemyObject.AddComponent<Rigidbody2D>(); _enemyBody.bodyType = RigidbodyType2D.Kinematic;
            _enemyObject.AddComponent<CircleCollider2D>().radius = 0.3f;
            _receiver = _enemyObject.AddComponent<DemoDamageReceiver>(); _enemy = _enemyObject.AddComponent<DemoEnemyController>();
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            typeof(DemoEnemyController).GetField("gameplayConfig", flags).SetValue(_enemy, _copy);
            typeof(DemoEnemyController).GetField("enemyId", flags).SetValue(_enemy, "elite_beast");
            typeof(DemoEnemyController).GetField("target", flags).SetValue(_enemy, transform);
            ResetCase(); _enemyObject.SetActive(true);
            _enemyBody.position = new Vector2(10f, 8f); Physics2D.SyncTransforms();
            yield return WaitCharging();
            PursuitObserved = _enemy.IsCharging && _enemyBody.position.y <= 4.1f && _enemyBody.position.y > 3.8f;
            ResetCase();
            yield return WaitCharging();
            Vector2 before = _enemyBody.position;
            yield return new WaitForSeconds(0.15f);
            WindupStationary = _enemy.IsCharging && Vector2.Distance(before, _enemyBody.position) < 0.01f;
            yield return new WaitForSeconds(0.8f);
            ChargeMovesAndHitsOnce = _enemy.AttackCount == 1 && !_enemy.IsDashing && _health.CurrentHealth == 85f
                && before.y - _enemyBody.position.y > 1.5f;
            yield return new WaitForSeconds(0.3f);
            ChargeMovesAndHitsOnce &= _health.CurrentHealth == 85f && _enemy.AttackCount == 1;
            ResetCase(); yield return WaitCharging();
            _input = new DemoInputState(Vector2.right, Vector2.zero, false, false, false, false, Vector2.right);
            yield return new WaitForSeconds(0.6f); _input = default;
            yield return new WaitForSeconds(0.3f);
            LockedDirectionCanBeAvoided = _body.position.x > 12f && Mathf.Abs(_enemyBody.position.x - 10f) < 0.01f
                && _health.CurrentHealth == 100f && _enemy.AttackCount == 1 && !_enemy.IsDashing;
            ResetCase();
            _wall = new GameObject("QA_ChargeWall"); _wall.transform.position = new Vector3(10f, 1.5f);
            var wallCollider = _wall.AddComponent<BoxCollider2D>(); wallCollider.size = new Vector2(2f, 0.1f);
            Physics2D.SyncTransforms(); yield return new WaitForSeconds(1f);
            WallBlocksDamageAndMovement = _enemyBody.position.y >= 1.84f && _health.CurrentHealth == 100f && !_enemy.IsDashing;
            ResetCase(); wallCollider.isTrigger = true; Physics2D.SyncTransforms();
            yield return new WaitForSeconds(1f);
            TriggerDoesNotBlock = _enemyBody.position.y < 1.5f && _health.CurrentHealth == 85f;
            Destroy(_wall); _wall = null; yield return null;
            ResetCase(); yield return WaitCharging();
            _receiver.ReceiveDamage(new DemoDamageInfo(1f, gameObject));
            yield return new WaitForSeconds(0.8f);
            HitStunCancelsWindup = !_enemy.IsCharging && !_enemy.IsDashing && _enemy.AttackCount == 0 && _health.CurrentHealth == 100f;
            ResetCase(); yield return WaitDashing();
            _receiver.ReceiveDamage(new DemoDamageInfo(100f, gameObject));
            before = _enemyBody.position; yield return new WaitForSeconds(0.3f);
            StaggerCancelsDash = _receiver.IsStaggered && !_enemy.IsDashing && Vector2.Distance(before, _enemyBody.position) < 0.01f && _health.CurrentHealth == 100f;
            ResetCase(); yield return WaitDashing();
            _receiver.ReceiveDamage(new DemoDamageInfo(10000f, gameObject)); before = _enemyBody.position;
            yield return new WaitForSeconds(0.3f);
            DeathCancelsDash = _receiver.IsDefeated && !_enemy.IsDashing && Vector2.Distance(before, _enemyBody.position) < 0.01f
                && !_enemyObject.GetComponent<Collider2D>().enabled && _health.CurrentHealth == 100f;
            ResetCase();
            RetryRestoresState = !_enemy.IsDashing && !_enemy.IsCharging && _enemy.AttackCount == 0
                && _receiver.CurrentHealth == 250f && _enemyObject.GetComponent<Collider2D>().enabled
                && Vector2.Distance(_enemyBody.position, new Vector2(10f, 3f)) < 0.01f;
            _copy.FindEnemy("elite_beast").chargeDistance = 1f;
            yield return new WaitForSeconds(1f);
            NonDefaultDistanceApplied = Mathf.Abs(_enemyBody.position.y - 2f) < 0.05f && _health.CurrentHealth == 100f && !_enemy.IsDashing;
            Completed = PursuitObserved && WindupStationary && ChargeMovesAndHitsOnce && LockedDirectionCanBeAvoided && WallBlocksDamageAndMovement
                && TriggerDoesNotBlock && HitStunCancelsWindup && StaggerCancelsDash && DeathCancelsDash && RetryRestoresState && NonDefaultDistanceApplied;
            Details = $"windup={WindupStationary}, hitOnce={ChargeMovesAndHitsOnce}, dodgeX={LockedDirectionCanBeAvoided}, wall={WallBlocksDamageAndMovement}, customY={_enemyBody.position.y}";
            Finished = true; Restore();
        }
        private void ResetCase()
        {
            _input = default; _body.position = new Vector2(10f, 0f); _body.velocity = Vector2.zero;
            _health.ResetForRetry(); _movement.ResetForRetry(); _receiver.ResetHealth(); _enemy.ResetForRetry();
            Physics2D.SyncTransforms();
        }
        private IEnumerator WaitCharging() { float until = Time.time + 3f; while (!_enemy.IsCharging && Time.time < until) yield return null; }
        private IEnumerator WaitDashing() { float until = Time.time + 2f; while (!_enemy.IsDashing && Time.time < until) yield return null; }
        private void OnDisable() => Restore();
        private void Restore()
        {
            if (!_captured) return;
            _input = default; InputField.SetValue(_movement, _oldInput); _combat.enabled = _combatEnabled; _flow.enabled = _flowEnabled;
            _body.position = _start; _health.ResetForRetry(); Time.captureDeltaTime = _captureDelta;
            if (_enemyObject != null) Destroy(_enemyObject); if (_wall != null) Destroy(_wall); if (_copy != null) Destroy(_copy);
            _captured = false;
        }
    }
}
