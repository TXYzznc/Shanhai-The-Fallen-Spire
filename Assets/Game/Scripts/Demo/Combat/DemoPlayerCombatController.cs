using Shanhai.Demo.Configuration;
using Shanhai.Demo.Input;
using Shanhai.Demo.Build;
using Shanhai.Demo.Player;
using UnityEngine;

namespace Shanhai.Demo.Combat
{
    public enum DemoPlayerAction { None, NormalAttack, Skill, Ultimate }
    /// <summary>
    /// 玩家两段普通攻击的时序与扇形命中检测。设备读取仍由 IDemoInputSource 集中提供。
    /// </summary>
    public sealed class DemoPlayerCombatController : MonoBehaviour, IDemoAttackLock
    {
        private Game.State.GamePauseController _pause;
        public void BindPause(Game.State.GamePauseController pause) => _pause = pause;
        private bool GameplayBlocked => _pause != null && _pause.BlocksGameplay;
        [SerializeField] private MonoBehaviour inputSourceBehaviour;
        [SerializeField] private DemoGameplayConfig gameplayConfig;
        [SerializeField] private DemoMarkLoadout markLoadout;
        [SerializeField] private DemoBuffLoadout buffLoadout;
        [SerializeField] private DemoPlayerHealthController healthController;
        private DemoPlayerController _movement;
        [SerializeField] private Transform attackOrigin;
        [SerializeField, Min(0.1f)] private float attackRadius = 1.6f;
        [SerializeField, Range(1f, 180f)] private float attackFanAngle = 110f;
        [SerializeField] private LayerMask attackLayers = ~0;

        private static readonly Collider2D[] HitBuffer = new Collider2D[32];
        private readonly IDemoDamageReceiver[] _damagedReceivers = new IDemoDamageReceiver[32];
        private IDemoInputSource _inputSource;
        private int _comboStep;
        private float _comboExpiresAt;
        private float _nextAttackAt;
        private float _attackHitAt;
        private float _attackLockedUntil;
        private bool _hitApplied;
        private float _nextSkillAt;
        private float _energy;
        private DemoPlayerAction _presentationAction;
        private float _presentationUntil;

        public event System.Action<DemoCombatPresentationEvent> ActionStarted;

        public DemoPlayerAction PresentationAction => Time.time < _presentationUntil ? _presentationAction : DemoPlayerAction.None;
        public bool IsAttackLocked => Time.time < _attackLockedUntil || healthController != null && (healthController.IsHitStunned || healthController.IsRetryRequired);
        public float CurrentEnergy => _energy;
        public float SkillCooldownRemaining => Mathf.Max(0f, _nextSkillAt - Time.time);

        public void ResetForRetry()
        {
            _comboStep = 0;
            _comboExpiresAt = 0f;
            _nextAttackAt = 0f;
            _attackHitAt = 0f;
            _attackLockedUntil = 0f;
            _hitApplied = true;
            _nextSkillAt = 0f;
            _energy = 0f;
            _presentationAction = DemoPlayerAction.None;
            _presentationUntil = 0f;
        }

        public void BindInput(MonoBehaviour input)
        {
            inputSourceBehaviour = input; _inputSource = input as IDemoInputSource;
        }

        private void Awake()
        {
            _movement = GetComponent<DemoPlayerController>();
            _inputSource = inputSourceBehaviour as IDemoInputSource;
            if (attackOrigin == null)
            {
                attackOrigin = transform;
            }
        }

        private void Update()
        {
            if (GameplayBlocked) return;
            if (healthController != null && healthController.IsRetryRequired)
            {
                // Discard any pending windup hit when the run has failed.
                _hitApplied = true;
                return;
            }
            if (_inputSource == null)
            {
                return;
            }

            DemoInputState input = _inputSource.ReadState();
            // Either component may update first; both consume the same semantic snapshot.
            // TryDodge is idempotent within a frame, so movement is never applied twice.
            if (input.DodgePressed && _movement != null) _movement.TryDodge(input);
            DemoPlayerConfig player = gameplayConfig != null ? gameplayConfig.Player : null;
            float energyMax = player != null ? player.energyMax : 100f;
            float energyRegen = player != null ? player.energyRegenPerSecond : 5f;
            if (markLoadout != null) energyRegen *= markLoadout.EnergyRegenMultiplier;
            _energy = Mathf.Min(energyMax, _energy + energyRegen * Time.deltaTime);
            if (_movement != null && _movement.DodgedThisFrame) return;
            if (input.AttackPressed)
            {
                TryStartAttack();
            }

            if (!_hitApplied && _comboStep > 0 && Time.time >= _attackHitAt)
            {
                ApplyAttackHit();
            }

            if (input.SkillPressed)
            {
                TryUseSkill();
            }

            if (input.UltimatePressed) TryUseUltimate();

            if (_comboStep > 0 && Time.time >= _attackLockedUntil && Time.time >= _comboExpiresAt)
            {
                _comboStep = 0;
            }
        }

        public bool TryStartAttack()
        {
            if (GameplayBlocked || _movement != null && _movement.DodgedThisFrame) return false;
            if (Time.time < _nextAttackAt || IsAttackLocked)
            {
                return false;
            }

            DemoPlayerConfig player = gameplayConfig != null ? gameplayConfig.Player : null;
            float cooldown = player != null ? player.attackCooldown : 0.35f;
            float comboWindow = player != null ? player.comboWindow : 0.8f;
            float windup = player != null ? player.attackWindup : 0.12f;
            float recovery = player != null ? player.attackRecovery : 0.2f;

            _comboStep = Time.time <= _comboExpiresAt && _comboStep == 1 ? 2 : 1;
            _comboExpiresAt = Time.time + comboWindow;
            _nextAttackAt = Time.time + cooldown;
            _attackHitAt = Time.time + windup;
            _attackLockedUntil = _attackHitAt + recovery;
            _hitApplied = false;
            _presentationAction = DemoPlayerAction.NormalAttack;
            _presentationUntil = _attackLockedUntil;
            if (markLoadout != null) markLoadout.OnNormalAttackStarted();
            PublishAction(DemoPlayerAction.NormalAttack);
            return true;
        }

        private void ApplyAttackHit()
        {
            _hitApplied = true;
            DemoPlayerConfig player = gameplayConfig != null ? gameplayConfig.Player : null;
            float multiplier = _comboStep == 2
                ? (player != null ? player.comboSecondMultiplier : 1.3f)
                : 1f;
            if (_comboStep == 2 && markLoadout != null) multiplier *= markLoadout.NormalAttackMultiplier;
            if (buffLoadout != null) multiplier *= buffLoadout.NormalAttackMultiplier;
            if (ApplyFanDamage(multiplier) && markLoadout != null) markLoadout.OnNormalAttackHit();
        }

        /// <summary>闪避取消未命中的普攻与后摇，不返还已消耗的冷却或能量。</summary>
        public void CancelForDodge()
        {
            _hitApplied = true;
            _comboStep = 0;
            _comboExpiresAt = _attackHitAt = _attackLockedUntil = 0f;
            _presentationAction = DemoPlayerAction.None;
            _presentationUntil = 0f;
        }

        public bool TryUseSkill()
        {
            if (GameplayBlocked || _movement != null && _movement.DodgedThisFrame) return false;
            if (IsAttackLocked || Time.time < _nextSkillAt)
            {
                return false;
            }

            DemoPlayerConfig player = gameplayConfig != null ? gameplayConfig.Player : null;
            float skillCooldown = player != null ? player.skillCooldown : 5f;
            if (markLoadout != null) skillCooldown *= markLoadout.SkillCooldownMultiplier;
            if (buffLoadout != null) skillCooldown *= buffLoadout.SkillCooldownMultiplier;
            _nextSkillAt = Time.time + skillCooldown;
            _presentationAction = DemoPlayerAction.Skill;
            _presentationUntil = Time.time + 0.25f;
            PublishAction(DemoPlayerAction.Skill, markLoadout != null ? markLoadout.SkillRangeMultiplier : 1f);
            float skillMultiplier = player != null ? player.skillDamageMultiplier : 2f;
            ApplyFanDamage(skillMultiplier,
                markLoadout != null ? markLoadout.SkillRangeMultiplier : 1f,
                markLoadout != null ? markLoadout.SkillImbalanceMultiplier : 1f);
            return true;
        }

        public bool TryUseUltimate()
        {
            if (GameplayBlocked || _movement != null && _movement.DodgedThisFrame) return false;
            DemoPlayerConfig player = gameplayConfig != null ? gameplayConfig.Player : null;
            float energyMax = player != null ? player.energyMax : 100f;
            if (IsAttackLocked || _energy < energyMax) return false;
            _energy = 0f;
            _presentationAction = DemoPlayerAction.Ultimate;
            _presentationUntil = Time.time + 0.4f;
            if (markLoadout != null) markLoadout.OnUltimateStarted();
            float ultimateMultiplier = player != null ? player.ultimateDamageMultiplier : 5f;
            if (markLoadout != null) ultimateMultiplier *= markLoadout.UltimateDamageMultiplier;
            PublishAction(DemoPlayerAction.Ultimate);
            ApplyFanDamage(ultimateMultiplier);
            return true;
        }

        private void PublishAction(DemoPlayerAction action, float rangeMultiplier = 1f)
        {
            DemoCombatPresentationEvent.Attack(ActionStarted, action, attackOrigin.position, attackOrigin.up,
                attackRadius * rangeMultiplier, attackFanAngle);
        }

        private bool ApplyFanDamage(float damageMultiplier, float rangeMultiplier = 1f, float imbalanceMultiplier = 1f)
        {
            DemoPlayerConfig player = gameplayConfig != null ? gameplayConfig.Player : null;
            float damage = (player != null ? player.baseAttackDamage : 10f) * damageMultiplier;
            if (buffLoadout != null) damage *= buffLoadout.AttackMultiplier;
            if (markLoadout != null) damage *= markLoadout.AttributeAttackMultiplier;
            int hitCount = Physics2D.OverlapCircleNonAlloc(attackOrigin.position, attackRadius * rangeMultiplier, HitBuffer, attackLayers);
            Vector2 forward = attackOrigin.up;
            bool hitReceiver = false;
            int receiverCount = 0;
            for (int i = 0; i < hitCount; i++)
            {
                Collider2D hit = HitBuffer[i];
                if (hit == null || hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                Vector2 toTarget = (Vector2)hit.bounds.center - (Vector2)attackOrigin.position;
                if (toTarget.sqrMagnitude < 0.0001f || Vector3.Angle(forward, toTarget) > attackFanAngle * 0.5f)
                {
                    continue;
                }

                IDemoDamageReceiver receiver = hit.GetComponentInParent<IDemoDamageReceiver>();
                if (receiver == null || receiver is DemoDamageReceiver enemy && enemy.IsDefeated) continue;
                bool alreadyHit = false;
                for (int j = 0; j < receiverCount; j++)
                {
                    if (ReferenceEquals(_damagedReceivers[j], receiver))
                    {
                        alreadyHit = true;
                        break;
                    }
                }
                if (alreadyHit) continue;
                _damagedReceivers[receiverCount++] = receiver;
                hitReceiver = true;
                receiver.ReceiveDamage(new DemoDamageInfo(damage, gameObject,
                    imbalanceMultiplier * (buffLoadout != null ? buffLoadout.ImbalanceMultiplier : 1f)));
            }
            for (int i = 0; i < receiverCount; i++) _damagedReceivers[i] = null;
            return hitReceiver;
        }
    }
}
