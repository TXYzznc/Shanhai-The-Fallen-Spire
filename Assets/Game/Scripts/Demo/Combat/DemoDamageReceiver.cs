using UnityEngine;

namespace Shanhai.Demo.Combat
{
    /// <summary>灰盒实体可复用的生命值接收端；具体敌人行为在后续任务接入。</summary>
    public sealed class DemoDamageReceiver : MonoBehaviour, IDemoDamageReceiver
    {
        private Game.State.GamePauseController _pause;
        public void BindPause(Game.State.GamePauseController pause) => _pause = pause;
        private bool GameplayBlocked => _pause != null && _pause.BlocksGameplay;
        [SerializeField, Min(1f)] private float maxHealth = 60f;
        [SerializeField, Min(0f)] private float hitStunDuration = 0.15f;
        [SerializeField, Min(0f)] private float staggerWindow = 3f;
        [SerializeField, Min(0f)] private float staggerDuration = 2f;
        [SerializeField, Min(1f)] private float staggerDamageMultiplier = 1.5f;
        private float _currentHealth;
        private float _hitStunUntil;
        private float _staggerUntil;
        private float _staggerWindowUntil;
        private float _staggerDamage;
        private float _staggerThreshold;
        private bool _staggerEnabled;

        public event System.Action<DemoCombatPresentationEvent> DamageApplied;

        public float CurrentHealth => _currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDefeated => _currentHealth <= 0f;
        public bool IsHitStunned => Time.time < _hitStunUntil;
        public bool IsStaggered => Time.time < _staggerUntil;
        public bool CanStagger => _staggerEnabled;
        public float StaggerProgress => _staggerEnabled && Time.time <= _staggerWindowUntil
            ? Mathf.Clamp01(_staggerDamage / _staggerThreshold) : 0f;
        public float StaggerRecovery => IsStaggered && staggerDuration > 0f
            ? Mathf.Clamp01((_staggerUntil - Time.time) / staggerDuration) : 0f;
        public float DamageTakenMultiplier => IsStaggered ? staggerDamageMultiplier : 1f;

        private void Awake() => _currentHealth = maxHealth;

        internal void ConfigureHealth(float health, float hitStun)
        {
            maxHealth = Mathf.Max(1f, health);
            hitStunDuration = Mathf.Max(0f, hitStun);
            ResetHealth();
        }

        public void ResetHealth()
        {
            _currentHealth = maxHealth;
            _hitStunUntil = 0f;
            _staggerUntil = 0f;
            _staggerWindowUntil = 0f;
            _staggerDamage = 0f;
        }

        public void ConfigureStagger(float threshold, float window, float duration, float damageMultiplier)
        {
            _staggerEnabled = threshold > 0f && window > 0f && duration > 0f;
            _staggerThreshold = Mathf.Max(0f, threshold);
            staggerWindow = Mathf.Max(0f, window);
            staggerDuration = Mathf.Max(0f, duration);
            staggerDamageMultiplier = Mathf.Max(1f, damageMultiplier);
        }

        public void ReceiveDamage(DemoDamageInfo damage)
        {
            if (GameplayBlocked) return;
            if (IsDefeated || damage.Amount <= 0f)
            {
                return;
            }

            _hitStunUntil = Time.time + hitStunDuration;
            if (_staggerEnabled)
            {
                if (_staggerDamage <= 0f || Time.time > _staggerWindowUntil)
                {
                    _staggerDamage = 0f;
                    // 累计窗口从第一击开始；后续命中不能无限续期。
                    _staggerWindowUntil = Time.time + staggerWindow;
                }
                _staggerDamage += damage.Amount * Mathf.Max(0f, damage.ImbalanceMultiplier);
                if (_staggerDamage >= _staggerThreshold)
                {
                    _staggerUntil = Time.time + staggerDuration;
                    _staggerDamage = 0f;
                    _staggerWindowUntil = 0f;
                }
            }

            float previousHealth = _currentHealth;
            _currentHealth = Mathf.Max(0f, _currentHealth - damage.Amount * DamageTakenMultiplier);
            DemoCombatPresentationEvent.Damage(DamageApplied, previousHealth - _currentHealth, transform.position, false);
        }
    }
}
