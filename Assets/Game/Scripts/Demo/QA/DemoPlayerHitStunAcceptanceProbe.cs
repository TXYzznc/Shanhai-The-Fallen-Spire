using System.Collections;
using System.Reflection;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Configuration;
using Shanhai.Demo.Input;
using Shanhai.Demo.Player;
using UnityEngine;

namespace Shanhai.Demo.QA
{
    public sealed class DemoPlayerHitStunAcceptanceProbe : MonoBehaviour, IDemoInputSource
    {
        [SerializeField] private DemoPlayerHealthController health;
        [SerializeField] private DemoPlayerController movement;
        [SerializeField] private DemoPlayerCombatController combat;
        public bool HitStunObserved { get; private set; }
        public bool MovementBlocked { get; private set; }
        public bool DodgeBlocked { get; private set; }
        public bool AttackBlocked { get; private set; }
        public bool MovementRestored { get; private set; }
        public bool AttackRestored { get; private set; }
        public bool CustomDurationBound { get; private set; }
        public bool CustomDurationExpires { get; private set; }
        public bool Completed { get; private set; }
        private DemoInputState _input;
        private object _originalInput;
        private DemoGameplayConfig _originalConfig;
        private float _oldCaptureDelta;
        private bool _captured;
        public DemoInputState ReadState() => _input;

        private static FieldInfo Field(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        private void OnDisable()
        {
            if (!_captured) return;
            if (movement != null) Field(movement, "_inputSource").SetValue(movement, _originalInput);
            if (health != null) Field(health, "gameplayConfig").SetValue(health, _originalConfig);
            Time.captureDeltaTime = _oldCaptureDelta;
        }

        private IEnumerator Start()
        {
            yield return null;
            _originalInput = Field(movement, "_inputSource").GetValue(movement);
            _originalConfig = (DemoGameplayConfig)Field(health, "gameplayConfig").GetValue(health);
            _oldCaptureDelta = Time.captureDeltaTime;
            _captured = true;
            Time.captureDeltaTime = 0.02f;
            Field(movement, "_inputSource").SetValue(movement, this);
            Rigidbody2D body = movement.GetComponent<Rigidbody2D>();
            health.ResetForRetry();
            combat.ResetForRetry();
            health.ReceiveDamage(new DemoDamageInfo(1f, gameObject));
            HitStunObserved = health.IsHitStunned;
            DodgeBlocked = !movement.TryDodge(new DemoInputState(Vector2.right, Vector2.zero, false, true, false, false));
            AttackBlocked = !combat.TryStartAttack();
            _input = new DemoInputState(Vector2.right, Vector2.zero, false, false, false, false, Vector2.up);
            Vector2 initial = body.position;
            yield return null;
            yield return null;
            MovementBlocked = Vector2.Distance(body.position, initial) < 0.001f;
            yield return new WaitForSeconds(0.2f);
            MovementRestored = !health.IsHitStunned && Vector2.Distance(body.position, initial) > 0.1f;
            AttackRestored = combat.TryStartAttack();
            var config = Instantiate(_originalConfig);
            config.Player.hitStunDuration = 0.35f;
            Field(health, "gameplayConfig").SetValue(health, config);
            health.ResetForRetry();
            combat.ResetForRetry();
            health.ReceiveDamage(new DemoDamageInfo(1f, gameObject));
            initial = body.position;
            yield return new WaitForSeconds(0.2f);
            CustomDurationBound = health.IsHitStunned && Vector2.Distance(body.position, initial) < 0.001f;
            yield return new WaitForSeconds(0.2f);
            CustomDurationExpires = !health.IsHitStunned && Vector2.Distance(body.position, initial) > 0.1f;
            _input = default;
            Field(health, "gameplayConfig").SetValue(health, _originalConfig);
            Field(movement, "_inputSource").SetValue(movement, _originalInput);
            Time.captureDeltaTime = _oldCaptureDelta;
            Destroy(config);
            Completed = HitStunObserved && MovementBlocked && DodgeBlocked && AttackBlocked && MovementRestored
                && AttackRestored && CustomDurationBound && CustomDurationExpires;
        }
    }
}
