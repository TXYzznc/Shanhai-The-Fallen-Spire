using Shanhai.Demo.Combat;
using Shanhai.Demo.Configuration;
using UnityEngine;

namespace Shanhai.Demo.Enemies
{
    /// <summary>普通敌人的 2D 追击和近战伤害行为，参数来自 Demo 配置资产。</summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class DemoEnemyController : MonoBehaviour
    {
        private Game.State.GamePauseController _pause;
        public void BindPause(Game.State.GamePauseController pause) => _pause = pause;
        private bool GameplayBlocked => _pause != null && _pause.BlocksGameplay;
        [SerializeField] private DemoGameplayConfig gameplayConfig;
        [SerializeField] private string enemyId = "common_beast";
        [SerializeField] private Transform target;
        [SerializeField] private LayerMask movementCollisionMask = ~0;
        [SerializeField, Min(0f)] private float movementCollisionSkin = 0.02f;

        private Rigidbody2D _body;
        private DemoEnemyConfig _enemyConfig;
        private IDemoDamageReceiver _targetReceiver;
        private DemoDamageReceiver _selfReceiver;
        private float _nextAttackAt;
        private float _attackAt;
        private int _attackCount;
        private Vector2 _startPosition;
        private float _startRotation;
        private readonly RaycastHit2D[] _movementHits = new RaycastHit2D[16];
        private ContactFilter2D _movementFilter;
        private Collider2D[] _colliders;
        private bool[] _colliderEnabledStates;
        private bool _deathCollisionReleased;
        private Vector2 _chargeDirection;
        private float _dashRemaining;

        public bool HasTargetReceiver => _targetReceiver != null;
        public bool HasEnemyConfig => _enemyConfig != null;
        public float DistanceToTarget => target != null ? Vector2.Distance(transform.position, target.position) : -1f;
        public bool IsEliteOrBoss => _enemyConfig != null && (_enemyConfig.isElite || _enemyConfig.isBoss);
        public bool IsCharging => IsEliteOrBoss && Time.time < _attackAt;
        public event System.Action<DemoCombatPresentationEvent> ActionStarted;
        public int AttackCount => _attackCount;
        public int Phase => _enemyConfig != null && _enemyConfig.isBoss && _selfReceiver != null
            && _selfReceiver.CurrentHealth / _selfReceiver.MaxHealth < _enemyConfig.secondPhaseHealthRatio - 0.000001f ? 2 : 1;
        public int AreaAttackCount { get; private set; }
        public float AttackRange => _enemyConfig != null ? _enemyConfig.attackRange : 0f;
        public float ChargeDuration => _enemyConfig != null ? _enemyConfig.chargeDuration : 0f;
        public bool IsDashing => _dashRemaining > 0f;
        public float ChargeProgress => IsCharging && ChargeDuration > 0f
            ? Mathf.Clamp01(1f - (_attackAt - Time.time) / ChargeDuration) : 0f;

        internal bool MatchesConfiguration(DemoGameplayConfig config, string id) => gameplayConfig == config && enemyId == id;

        public void BindTarget(Transform player)
        {
            target = player;
            _targetReceiver = player != null ? player.GetComponent<DemoPlayerHealthController>() : null;
            if (player != null && _body != null)
            {
                _startPosition = _body.position; _startRotation = _body.rotation;
                ResetForRetry();
                if (_selfReceiver != null) _selfReceiver.ResetHealth();
            }
        }

        public void BindConfiguration(DemoGameplayConfig config, string id)
        {
            gameplayConfig = config; enemyId = id;
            _enemyConfig = config != null ? config.FindEnemy(id) : null;
            if (config == null) return;
            if (_enemyConfig == null) throw new System.InvalidOperationException("敌人配置缺失：" + id);
            _selfReceiver.ConfigureHealth(_enemyConfig.maxHealth, _enemyConfig.hitStunDuration);
            _selfReceiver.ConfigureStagger(_enemyConfig.canStagger ? _enemyConfig.staggerThreshold : 0f,
                _enemyConfig.staggerWindow, _enemyConfig.staggerDuration, _enemyConfig.staggerDamageMultiplier);
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _startPosition = _body.position;
            _startRotation = _body.rotation;
            _body.gravityScale = 0f;
            _body.freezeRotation = true;
            _movementFilter.SetLayerMask(movementCollisionMask);
            _movementFilter.useTriggers = false;
            _colliders = GetComponentsInChildren<Collider2D>(true);
            _colliderEnabledStates = new bool[_colliders.Length];
            for (int i = 0; i < _colliders.Length; i++) _colliderEnabledStates[i] = _colliders[i].enabled;
            _selfReceiver = GetComponent<DemoDamageReceiver>();
            _enemyConfig = gameplayConfig != null ? gameplayConfig.FindEnemy(enemyId) : null;
            if (_selfReceiver != null && _enemyConfig != null)
            {
                _selfReceiver.ConfigureHealth(_enemyConfig.maxHealth, _enemyConfig.hitStunDuration);
                _selfReceiver.ConfigureStagger(_enemyConfig.canStagger ? _enemyConfig.staggerThreshold : 0f,
                    _enemyConfig.staggerWindow, _enemyConfig.staggerDuration, _enemyConfig.staggerDamageMultiplier);
            }
            // 目标是玩家生命控制器；使用具体组件绑定，确保 2D 场景中接口查找不会丢失。
            _targetReceiver = target != null ? target.GetComponentInParent<DemoPlayerHealthController>() : null;
        }

        private void Update()
        {
            if (GameplayBlocked) return;
            if (target == null || _targetReceiver == null || _enemyConfig == null) return;

            Vector2 offset = (Vector2)target.position - _body.position;
            if (_selfReceiver != null && _selfReceiver.IsDefeated)
            {
                CancelCharge();
                if (!_deathCollisionReleased)
                {
                    for (int i = 0; i < _colliders.Length; i++)
                        if (_colliders[i] != null) _colliders[i].enabled = false;
                    _deathCollisionReleased = true;
                    _body.velocity = Vector2.zero;
                    _body.angularVelocity = 0f;
                }
                return;
            }
            if (_selfReceiver != null && (_selfReceiver.IsHitStunned || _selfReceiver.IsStaggered))
            {
                if (_enemyConfig.isElite) CancelCharge();
                return;
            }
            if (IsDashing) return;
            float distance = offset.magnitude;
            if (_attackAt > 0f)
            {
                if (Time.time < _attackAt) return;
                _attackAt = 0f;
                if (_enemyConfig.isElite)
                {
                    _dashRemaining = _enemyConfig.chargeDistance;
                    PublishAction(offset, AttackRange, 110f, DemoPlayerAction.Skill);
                    _attackCount++;
                    return;
                }
                PublishAction(offset, AttackRange, 110f, DemoPlayerAction.NormalAttack);
                if (distance <= AttackRange)
                    _targetReceiver.ReceiveDamage(new DemoDamageInfo(_enemyConfig.attackDamage, gameObject));
                _attackCount++;
                if (Phase == 2)
                {
                    AreaAttackCount++;
                    PublishAction(offset, _enemyConfig.secondPhaseAreaRadius, 360f, DemoPlayerAction.Ultimate);
                    if (distance <= _enemyConfig.secondPhaseAreaRadius)
                        _targetReceiver.ReceiveDamage(new DemoDamageInfo(_enemyConfig.attackDamage, gameObject));
                }
                return;
            }
            float startRange = _enemyConfig.isElite ? _enemyConfig.chargeTriggerRange : AttackRange;
            if (distance > startRange)
            {
                Vector2 step = offset / Mathf.Max(distance, 0.001f)
                    * Mathf.Min(_enemyConfig.moveSpeed * Time.deltaTime, distance - startRange);
                MoveWithCollision(step);
                if (step.sqrMagnitude > 0.0001f)
                {
                    float angle = Mathf.Atan2(step.y, step.x) * Mathf.Rad2Deg - 90f;
                    transform.rotation = Quaternion.Euler(0f, 0f, angle);
                }
                return;
            }

            if (IsEliteOrBoss && Time.time < _attackAt) return;
            if (Time.time >= _nextAttackAt)
            {
                float interval = _enemyConfig.attackCooldown;
                if (Phase == 2) interval *= _enemyConfig.secondPhaseCooldownMultiplier;
                _nextAttackAt = Time.time + interval;
                // 普通攻击当帧结算，不能保留待结算时间，否则下一帧会再次扣血。
                _attackAt = IsEliteOrBoss ? Time.time + ChargeDuration : 0f;
                if (_enemyConfig.isElite)
                    _chargeDirection = offset.sqrMagnitude > 0.0001f ? offset.normalized : Vector2.up;
                if (!IsEliteOrBoss)
                {
                    PublishAction(offset, AttackRange, 110f, DemoPlayerAction.NormalAttack);
                    _targetReceiver.ReceiveDamage(new DemoDamageInfo(_enemyConfig.attackDamage, gameObject));
                    _attackCount++;
                }
            }
        }

        private void PublishAction(Vector2 direction, float radius, float fanAngle, DemoPlayerAction action)
        {
            DemoCombatPresentationEvent.Attack(ActionStarted, action, transform.position,
                direction.sqrMagnitude > .0001f ? direction.normalized : Vector2.up, radius, fanAngle, false);
        }

        private void FixedUpdate()
        {
            if (!IsDashing || _enemyConfig == null) return;
            if (_selfReceiver != null && (_selfReceiver.IsDefeated || _selfReceiver.IsHitStunned || _selfReceiver.IsStaggered))
            {
                CancelCharge();
                return;
            }
            float step = Mathf.Min(_dashRemaining, _enemyConfig.chargeSpeed * Time.fixedDeltaTime);
            if (step <= 0f) { CancelCharge(); return; }
            float moved = MoveWithCollision(_chargeDirection * step, true);
            _dashRemaining = Mathf.Max(0f, _dashRemaining - moved);
            // 距离累计可能留下浮点尾差；耗尽后必须退出冲撞，避免永久阻止追击。
            if (_dashRemaining <= 0.0001f || moved < step - 0.0001f) CancelCharge();
        }

        private void CancelCharge() { _attackAt = 0f; _dashRemaining = 0f; }
        private void OnDisable() => CancelCharge();

        public void ResetForRetry()
        {
            if (_body != null)
            {
                _body.position = _startPosition;
                _body.rotation = _startRotation;
                _body.velocity = Vector2.zero;
                _body.angularVelocity = 0f;
            }
            _attackAt = 0f;
            _dashRemaining = 0f;
            _chargeDirection = Vector2.zero;
            _nextAttackAt = 0f;
            _attackCount = 0;
            AreaAttackCount = 0;
            _deathCollisionReleased = false;
            if (_colliders != null)
                for (int i = 0; i < _colliders.Length; i++)
                    if (_colliders[i] != null) _colliders[i].enabled = _colliderEnabledStates[i];
        }

        private float MoveWithCollision(Vector2 delta, bool chargeHit = false)
        {
            if (delta.sqrMagnitude < 0.000001f) return 0f;
            float distance = delta.magnitude;
            Vector2 direction = delta / distance;
            int count = _body.Cast(direction, _movementFilter, _movementHits, distance + movementCollisionSkin);
            Collider2D blocker = null;
            for (int i = 0; i < count; i++)
            {
                RaycastHit2D hit = _movementHits[i];
                if (hit.collider == null || hit.collider.transform.IsChildOf(transform)) continue;
                float allowed = Mathf.Max(0f, hit.distance - movementCollisionSkin);
                if (allowed < distance) { distance = allowed; blocker = hit.collider; }
            }
            _body.position += direction * distance;
            if (chargeHit && blocker != null && (IDemoDamageReceiver)blocker.GetComponentInParent<DemoPlayerHealthController>() == _targetReceiver)
                _targetReceiver.ReceiveDamage(new DemoDamageInfo(_enemyConfig.attackDamage, gameObject));
            return distance;
        }
    }
}
