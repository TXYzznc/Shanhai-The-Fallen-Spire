using Shanhai.Demo.Combat;
using Shanhai.Demo.Enemies;
using Shanhai.Demo.Player;
using UnityEngine;

namespace Shanhai.Demo.Presentation
{
    public enum DemoSpriteState { Idle, Moving, Attacking, Charging, Dashing, Hit, Staggered, Dead, Skill, Ultimate }
    /// <summary>2D 灰盒临时反馈：攻击/蓄力、受击闪白和死亡状态均由正式运行时组件驱动。</summary>
    public sealed class DemoSpriteFeedback : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private DemoDamageReceiver damageReceiver;
        [SerializeField] private DemoPlayerCombatController playerCombat;
        [SerializeField] private DemoPlayerHealthController playerHealth;
        [SerializeField] private DemoEnemyController enemyController;
        [SerializeField, Min(0.01f)] private float hitFlashDuration = 0.12f;
        [SerializeField, Min(0f)] private float attackPulse = 0.08f;
        [SerializeField, Min(0f)] private float chargePulse = 0.14f;

        private static readonly int FlashId = Shader.PropertyToID("_FlashAmount");
        private MaterialPropertyBlock _properties;
        private float _flashAmount = -1f;
        public float FlashAmount => Mathf.Max(0f, _flashAmount);
        private Color _baseColor;
        private Vector3 _baseScale;

        private float _hitUntil;
        private float _enemyAttackUntil;
        private int _lastAttackCount;
        private Vector3 _lastPosition;
        public DemoSpriteState CurrentState { get; private set; }

        private void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (damageReceiver == null) damageReceiver = GetComponent<DemoDamageReceiver>();
            _properties = new MaterialPropertyBlock();
            _baseColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
            _baseScale = transform.localScale;
            _lastPosition = transform.position;
        }

        private void OnEnable()
        {
            if (damageReceiver != null) damageReceiver.DamageApplied += Damaged;
            if (playerHealth != null) playerHealth.DamageApplied += Damaged;
        }
        private void OnDisable()
        {
            if (damageReceiver != null) damageReceiver.DamageApplied -= Damaged;
            if (playerHealth != null) playerHealth.DamageApplied -= Damaged;
            ResetPresentation();
        }
        private void Damaged(DemoCombatPresentationEvent args) { _hitUntil = Time.time + hitFlashDuration; }

        public void BindBaseScale(Vector3 scale) { _baseScale = scale; }

        public void ResetPresentation()
        {
            _hitUntil = 0f; _enemyAttackUntil = 0f; _lastAttackCount = 0;
            _lastPosition = transform.position; CurrentState = DemoSpriteState.Idle;
            ApplyVisual(_baseColor, 1f);
        }

        private void Update()
        {
            bool moving = (transform.position - _lastPosition).sqrMagnitude > 0.000001f;
            _lastPosition = transform.position;
            if (enemyController != null)
            {
                if (enemyController.AttackCount < _lastAttackCount) { _enemyAttackUntil = 0f; _hitUntil = 0f; }
                else if (enemyController.AttackCount > _lastAttackCount) _enemyAttackUntil = Time.time + 0.18f;
                _lastAttackCount = enemyController.AttackCount;
            }
            if (damageReceiver != null)
            {
                if (damageReceiver.IsDefeated)
                {
                    CurrentState = DemoSpriteState.Dead;
                    ApplyVisual(Color.Lerp(_baseColor, Color.black, 0.45f), 0.82f);
                    return;
                }
                if (damageReceiver.IsStaggered && Time.time >= _hitUntil)
                {
                    CurrentState = DemoSpriteState.Staggered;
                    ApplyVisual(new Color(0.35f, 0.75f, 1f), 0.9f);
                    return;
                }
            }
            else if (playerHealth != null)
            {
                if (playerHealth.IsRetryRequired)
                {
                    CurrentState = DemoSpriteState.Dead;
                    ApplyVisual(Color.Lerp(_baseColor, Color.black, 0.45f), 0.82f);
                    return;
                }
            }

            float scale = 1f;
            Color color = _baseColor;
            CurrentState = moving ? DemoSpriteState.Moving : DemoSpriteState.Idle;
            if (moving) color = Color.Lerp(color, new Color(0.45f, 1f, 0.6f), 0.25f);
            if (playerCombat != null && playerCombat.PresentationAction == DemoPlayerAction.Ultimate)
            {
                CurrentState = DemoSpriteState.Ultimate;
                color = new Color(1f, 0.8f, 0.15f);
            }
            else if (playerCombat != null && playerCombat.PresentationAction == DemoPlayerAction.Skill)
            {
                CurrentState = DemoSpriteState.Skill;
                color = new Color(0.2f, 0.85f, 1f);
            }
            else if (playerCombat != null && playerCombat.PresentationAction == DemoPlayerAction.NormalAttack)
            {
                CurrentState = DemoSpriteState.Attacking;
                float pulse = Mathf.Sin(Time.time * 30f) * attackPulse;
                scale += pulse;
                color = new Color(1f, 0.55f, 0.2f);
            }
            else if (enemyController != null && enemyController.IsCharging)
            {
                CurrentState = DemoSpriteState.Charging;
                float pulse = Mathf.Sin(Time.time * 24f) * chargePulse;
                scale += pulse;
                color = Color.Lerp(color, new Color(1f, 0.55f, 0.12f), 0.65f);
            }
            else if (enemyController != null && (enemyController.IsDashing || Time.time < _enemyAttackUntil))
            {
                CurrentState = enemyController.IsDashing ? DemoSpriteState.Dashing : DemoSpriteState.Attacking;
                color = Color.Lerp(_baseColor, new Color(1f, 0.3f, 0.05f), 0.8f);
            }
            if (Time.time < _hitUntil) { color = Color.white; CurrentState = DemoSpriteState.Hit; }
            ApplyVisual(color, scale);
        }

        private void ApplyVisual(Color color, float scale)
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = color;
                float flash = CurrentState == DemoSpriteState.Hit ? 1f : 0f;
                if (_properties != null && _flashAmount != flash)
                {
                    _flashAmount = flash; spriteRenderer.GetPropertyBlock(_properties);
                    _properties.SetFloat(FlashId, flash); spriteRenderer.SetPropertyBlock(_properties);
                }
            }
            transform.localScale = _baseScale * scale;
        }
    }
}
