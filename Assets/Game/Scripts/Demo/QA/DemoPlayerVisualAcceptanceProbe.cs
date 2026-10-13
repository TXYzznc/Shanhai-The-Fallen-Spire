using System.Collections;
using System.Reflection;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Input;
using Shanhai.Demo.Player;
using Shanhai.Demo.Presentation;
using UnityEngine;

namespace Shanhai.Demo.QA
{
    /// <summary>实际玩家 Sprite 与语义输入联调；自然充能，失败输入不产生释放提示。</summary>
    public sealed class DemoPlayerVisualAcceptanceProbe : MonoBehaviour, IDemoInputSource
    {
        public bool IdleVisible { get; private set; }
        public bool MovementVisible { get; private set; }
        public bool NormalAttackVisible { get; private set; }
        public bool SkillVisible { get; private set; }
        public bool RejectedSkillHasNoFlash { get; private set; }
        public bool UltimateVisible { get; private set; }
        public bool RejectedUltimateHasNoFlash { get; private set; }
        public bool HitVisible { get; private set; }
        public bool DeathVisible { get; private set; }
        public bool RetryClearsVisual { get; private set; }
        public bool Completed { get; private set; }
        public bool Finished { get; private set; }
        private DemoPlayerController _movement;
        private DemoPlayerCombatController _combat;
        private DemoPlayerHealthController _health;
        private DemoRunFlowController _flow;
        private DemoSpriteFeedback _feedback;
        private SpriteRenderer _sprite;
        private object _movementInput, _combatInput;
        private bool _flowEnabled, _captured;
        private float _captureDelta;
        private Color _baseColor;
        private DemoInputState _input;
        public DemoInputState ReadState() => _input;
        private static FieldInfo Field(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        private void Input(Vector2 move = default, bool attack = false, bool skill = false, bool ultimate = false) =>
            _input = new DemoInputState(move, Vector2.zero, attack, false, skill, ultimate, Vector2.up);
        private IEnumerator Start()
        {
            yield return null;
            _movement = GetComponent<DemoPlayerController>(); _combat = GetComponent<DemoPlayerCombatController>();
            _health = GetComponent<DemoPlayerHealthController>(); _flow = GetComponent<DemoRunFlowController>();
            _feedback = GetComponent<DemoSpriteFeedback>(); _sprite = GetComponent<SpriteRenderer>(); _baseColor = _sprite.color;
            _movementInput = Field(_movement, "_inputSource").GetValue(_movement);
            _combatInput = Field(_combat, "_inputSource").GetValue(_combat);
            _flowEnabled = _flow.enabled; _captureDelta = Time.captureDeltaTime; _captured = true;
            Field(_movement, "_inputSource").SetValue(_movement, this);
            Field(_combat, "_inputSource").SetValue(_combat, this);
            Time.captureDeltaTime = 0.05f; _flow.RestartRun(); _flow.enabled = false; Input();
            yield return new WaitForSeconds(0.3f);
            IdleVisible = State(DemoSpriteState.Idle, _baseColor) && _sprite.sprite != null;
            Vector3 start = transform.position;
            Input(Vector2.right); yield return new WaitForSeconds(0.2f);
            MovementVisible = _feedback.CurrentState == DemoSpriteState.Moving && !Near(_sprite.color, _baseColor)
                && transform.position.x > start.x + 0.5f;
            Input(attack: true); yield return null; Input(); yield return null;
            NormalAttackVisible = _feedback.CurrentState == DemoSpriteState.Attacking && _combat.PresentationAction == DemoPlayerAction.NormalAttack
                && Near(_sprite.color, new Color(1f, 0.55f, 0.2f));
            yield return new WaitForSeconds(0.4f);
            Input(skill: true); yield return null; Input(); yield return null;
            SkillVisible = State(DemoSpriteState.Skill, new Color(0.2f, 0.85f, 1f));
            yield return new WaitForSeconds(0.35f);
            Input(skill: true); yield return null; Input(); yield return null;
            RejectedSkillHasNoFlash = State(DemoSpriteState.Idle, _baseColor) && _combat.SkillCooldownRemaining > 0f;
            float until = Time.time + 21f;
            while (_combat.CurrentEnergy < 100f && Time.time < until) yield return null;
            Input(ultimate: true); yield return null; Input(); yield return null;
            UltimateVisible = State(DemoSpriteState.Ultimate, new Color(1f, 0.8f, 0.15f)) && _combat.CurrentEnergy < 1f;
            yield return new WaitForSeconds(0.5f);
            Input(ultimate: true); yield return null; Input(); yield return null;
            RejectedUltimateHasNoFlash = State(DemoSpriteState.Idle, _baseColor) && _combat.CurrentEnergy < 100f;
            _health.ReceiveDamage(new DemoDamageInfo(1f, gameObject)); yield return null;
            HitVisible = State(DemoSpriteState.Hit, Color.white);
            yield return new WaitForSeconds(0.2f);
            _health.ReceiveDamage(new DemoDamageInfo(10000f, gameObject)); yield return null;
            _health.ReceiveDamage(new DemoDamageInfo(10000f, gameObject)); yield return null;
            DeathVisible = _health.IsRetryRequired && State(DemoSpriteState.Dead, Color.Lerp(_baseColor, Color.black, 0.45f));
            _flow.RestartRun(); Input(); yield return new WaitForSeconds(0.3f);
            RetryClearsVisual = State(DemoSpriteState.Idle, _baseColor) && _combat.PresentationAction == DemoPlayerAction.None
                && !_health.IsRetryRequired && transform.position == start;
            Completed = IdleVisible && MovementVisible && NormalAttackVisible && SkillVisible && RejectedSkillHasNoFlash
                && UltimateVisible && RejectedUltimateHasNoFlash && HitVisible && DeathVisible && RetryClearsVisual;
            Finished = true; Restore();
        }
        private bool State(DemoSpriteState state, Color color) => _feedback.CurrentState == state && Near(_sprite.color, color);
        private static bool Near(Color a, Color b) => Mathf.Abs(a.r-b.r)+Mathf.Abs(a.g-b.g)+Mathf.Abs(a.b-b.b)+Mathf.Abs(a.a-b.a) < 0.01f;
        private void OnDisable() => Restore();
        private void Restore()
        {
            if (!_captured) return;
            Field(_movement, "_inputSource").SetValue(_movement, _movementInput);
            Field(_combat, "_inputSource").SetValue(_combat, _combatInput);
            _flow.enabled = _flowEnabled; Time.captureDeltaTime = _captureDelta; _captured = false;
        }
    }
}
