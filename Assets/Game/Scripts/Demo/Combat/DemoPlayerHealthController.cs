using Shanhai.Demo.Configuration;
using Shanhai.Demo.Player;
using Shanhai.Demo.Build;
using UnityEngine;

namespace Shanhai.Demo.Combat
{
    /// <summary>玩家生命、一次复活和二次失败状态；重试流程由关卡会话控制器接管。</summary>
    public sealed class DemoPlayerHealthController : MonoBehaviour, IDemoDamageReceiver
    {
        private Game.State.GamePauseController _pause;
        public void BindPause(Game.State.GamePauseController pause) => _pause = pause;
        private bool GameplayBlocked => _pause != null && _pause.BlocksGameplay;
        [SerializeField] private DemoGameplayConfig gameplayConfig;
        [SerializeField] private DemoPlayerController movementController;
        [SerializeField] private DemoMarkLoadout markLoadout;
        [SerializeField] private DemoBuffLoadout buffLoadout;

        private float _currentHealth;
        private int _revivesRemaining;
        private bool _retryRequired;
        [SerializeField, Min(0f)] private float hitStunDuration = 0.15f;
        private float _hitStunUntil;

        public event System.Action<DemoCombatPresentationEvent> DamageApplied;

        public float CurrentHealth => _currentHealth;
        public int RevivesRemaining => _revivesRemaining;
        public bool IsRetryRequired => _retryRequired;
        public float MaxHealth => GetMaxHealth();
        public bool IsHitStunned => Time.time < _hitStunUntil;

        public void ResetForRetry()
        {
            DemoPlayerConfig player = gameplayConfig != null ? gameplayConfig.Player : null;
            _currentHealth = GetMaxHealth();
            _revivesRemaining = player != null ? player.reviveCount : 1;
            _retryRequired = false;
            _hitStunUntil = 0f;
        }

        private void Awake()
        {
            DemoPlayerConfig player = gameplayConfig != null ? gameplayConfig.Player : null;
            _currentHealth = GetMaxHealth();
            _revivesRemaining = player != null ? player.reviveCount : 1;
            _hitStunUntil = 0f;
        }

        private void OnEnable()
        {
            if (markLoadout != null) markLoadout.LoadoutChanged += ClampHealthToMaximum;
            if (buffLoadout != null) buffLoadout.BuffsChanged += ClampHealthToMaximum;
            ClampHealthToMaximum();
        }

        private void OnDisable()
        {
            if (markLoadout != null) markLoadout.LoadoutChanged -= ClampHealthToMaximum;
            if (buffLoadout != null) buffLoadout.BuffsChanged -= ClampHealthToMaximum;
        }

        private void ClampHealthToMaximum()
        {
            // Increasing the limit does not heal; removing bonuses discards excess health.
            _currentHealth = Mathf.Clamp(_currentHealth, 0f, GetMaxHealth());
        }

        public void ReceiveDamage(DemoDamageInfo damage)
        {
            if (GameplayBlocked) return;
            if (_retryRequired || damage.Amount <= 0f || movementController != null && movementController.IsInvulnerable)
            {
                return;
            }

            float amount = damage.Amount * (markLoadout != null ? markLoadout.DamageTakenMultiplier : 1f);
            float duration = gameplayConfig != null && gameplayConfig.Player != null
                ? gameplayConfig.Player.hitStunDuration : hitStunDuration;
            _hitStunUntil = Time.time + Mathf.Max(0f, duration);
            float previousHealth = _currentHealth;
            _currentHealth = Mathf.Max(0f, _currentHealth - amount);
            // 先发布实际扣血，再处理复活回血，避免同帧净生命变化掩盖致命命中。
            DemoCombatPresentationEvent.Damage(DamageApplied, previousHealth - _currentHealth, transform.position, true);
            if (_currentHealth > 0f)
            {
                return;
            }

            DemoPlayerConfig player = gameplayConfig != null ? gameplayConfig.Player : null;
            if (_revivesRemaining > 0)
            {
                _revivesRemaining--;
                float maxHealth = GetMaxHealth();
                float restorePercent = player != null ? player.reviveHealthPercent : 0.5f;
                _currentHealth = maxHealth * restorePercent;
                return;
            }

            _retryRequired = true;
        }

        private float GetMaxHealth()
        {
            DemoPlayerConfig player = gameplayConfig != null ? gameplayConfig.Player : null;
            float baseHealth = player != null ? player.maxHealth : 100f;
            float multiplier = markLoadout != null ? markLoadout.AttributeHealthMultiplier : 1f;
            if (buffLoadout != null) multiplier *= buffLoadout.HealthMultiplier;
            return baseHealth * multiplier;
        }
    }
}
