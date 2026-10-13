using System.Collections;
using System.Reflection;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Enemies;
using Shanhai.Demo.Input;
using Shanhai.Demo.Player;
using Shanhai.Demo.UI;
using UnityEngine;

namespace Shanhai.Demo.QA
{
    /// <summary>默认数值与真实Boss AI的输入抽象实战；仅入房初始化传送，战斗不注入伤害或能量。</summary>
    public sealed class DemoBossCombatInputAcceptanceProbe : MonoBehaviour, IDemoInputSource
    {
        [SerializeField] private DemoEnemyController boss;
        [SerializeField] private DemoDamageReceiver receiver;
        [SerializeField] private DemoEncounterController encounter;
        [SerializeField] private GameObject bossGate;
        [SerializeField] private GameObject hud;
        public bool RoomStartsBossAndSeals { get; private set; }
        public bool BossChasedAndAttacked { get; private set; }
        public bool PhaseOneObserved { get; private set; }
        public bool PhaseTwoObserved { get; private set; }
        public bool PhaseTwoAreaObserved { get; private set; }
        public bool BossDefeatedByInput { get; private set; }
        public bool CollisionAndGateReleased { get; private set; }
        public bool RewardAndCompletionShown { get; private set; }
        public bool CurrencyGrantedOnce { get; private set; }
        public bool DeathStopsAttack { get; private set; }
        public bool Completed { get; private set; }
        public bool Finished { get; private set; }
        public string Details { get; private set; }
        public float FightSeconds { get; private set; }
        public int NormalInputs { get; private set; }
        public int SkillInputs { get; private set; }
        public int UltimateInputs { get; private set; }
        public float MinimumBossHealth { get; private set; } = 1000f;
        public float MinimumPlayerHealth { get; private set; } = 100f;
        public int ObservedAttacks { get; private set; }
        public int ObservedAreas { get; private set; }
        private DemoPlayerController _movement;
        private DemoPlayerCombatController _combat;
        private DemoPlayerHealthController _health;
        private DemoRunFlowController _flow;
        private Rigidbody2D _body;
        private object _oldMove, _oldCombat;
        private float _captureDelta, _nextNormal, _nextDodge;
        private int _inputFrame = -1;
        private bool _captured, _fight;
        private DemoInputState _input;
        public DemoInputState ReadState()
        {
            if (_inputFrame == Time.frameCount) return _input;
            _inputFrame = Time.frameCount;
            Vector2 move = Vector2.zero, aim = Vector2.up;
            bool attack = false, dodge = false, skill = false, ultimate = false;
            if (_fight && !receiver.IsDefeated)
            {
                Vector2 offset = (Vector2)boss.transform.position - _body.position;
                float distance = offset.magnitude;
                aim = offset.sqrMagnitude > 0.001f ? offset.normalized : Vector2.up;
                if (boss.IsCharging)
                {
                    move = -aim;
                    if (Time.time >= _nextDodge && !_combat.IsAttackLocked)
                    { dodge = true; _nextDodge = Time.time + 1.25f; }
                }
                else
                {
                    Vector2 tangent = new Vector2(-aim.y, aim.x);
                    move = distance > 1.3f ? (aim + tangent * 0.5f).normalized : tangent;
                    if (!_combat.IsAttackLocked && distance < 2f)
                    {
                        if (_combat.CurrentEnergy >= 100f) { ultimate = true; UltimateInputs++; }
                        else if (_combat.SkillCooldownRemaining <= 0f) { skill = true; SkillInputs++; }
                        else if (Time.time >= _nextNormal) { attack = true; _nextNormal = Time.time + 0.4f; NormalInputs++; }
                    }
                }
            }
            _input = new DemoInputState(move, Vector2.zero, attack, dodge, skill, ultimate, aim);
            return _input;
        }
        private IEnumerator Start()
        {
            yield return null;
            _movement = GetComponent<DemoPlayerController>(); _combat = GetComponent<DemoPlayerCombatController>();
            _health = GetComponent<DemoPlayerHealthController>(); _flow = GetComponent<DemoRunFlowController>(); _body = GetComponent<Rigidbody2D>();
            _oldMove = Field(_movement).GetValue(_movement); _oldCombat = Field(_combat).GetValue(_combat);
            _captureDelta = Time.captureDeltaTime; _captured = true; Time.captureDeltaTime = 0.05f;
            Field(_movement).SetValue(_movement, this); Field(_combat).SetValue(_combat, this);
            _body.position = new Vector2(1.8f, 48f); Physics2D.SyncTransforms();
            yield return null;
            RoomStartsBossAndSeals = encounter.HasStarted && bossGate.activeSelf && boss.enabled && receiver.MaxHealth == 1000f;
            Vector2 before = boss.transform.position;
            int initialRetry = _flow.RetryCount;
            float begin = Time.time;
            _fight = true;
            while (!receiver.IsDefeated && Time.time - begin < 180f && _flow.RetryCount == initialRetry)
            {
                MinimumBossHealth = Mathf.Min(MinimumBossHealth, receiver.CurrentHealth);
                MinimumPlayerHealth = Mathf.Min(MinimumPlayerHealth, _health.CurrentHealth);
                ObservedAttacks = Mathf.Max(ObservedAttacks, boss.AttackCount);
                ObservedAreas = Mathf.Max(ObservedAreas, boss.AreaAttackCount);
                PhaseOneObserved |= boss.Phase == 1 && boss.AttackCount > 0;
                PhaseTwoObserved |= boss.Phase == 2 && receiver.CurrentHealth < 600f;
                PhaseTwoAreaObserved |= boss.AreaAttackCount > 0;
                BossChasedAndAttacked |= Vector2.Distance(before, boss.transform.position) > 0.3f && boss.AttackCount > 0;
                yield return null;
            }
            _fight = false; FightSeconds = Time.time - begin;
            BossDefeatedByInput = receiver.IsDefeated && _flow.RetryCount == initialRetry && NormalInputs > 0 && SkillInputs > 0 && UltimateInputs > 0;
            yield return new WaitForSeconds(0.2f);
            CollisionAndGateReleased = receiver.IsDefeated && !boss.GetComponent<Collider2D>().enabled && !bossGate.activeSelf;
            RewardAndCompletionShown = encounter.IsCompleted && hud.GetComponent<DemoCompletionView>().IsOpen
                && hud.GetComponent<Shanhai.Demo.Build.DemoRewardChoiceController>().IsOpen;
            int attacks = boss.AttackCount;
            yield return new WaitForSeconds(2f);
            CurrencyGrantedOnce = _flow.CurrentCurrency == 300;
            DeathStopsAttack = receiver.IsDefeated && boss.AttackCount == attacks;
            Completed = RoomStartsBossAndSeals && BossChasedAndAttacked && PhaseOneObserved && PhaseTwoObserved
                && PhaseTwoAreaObserved && BossDefeatedByInput && CollisionAndGateReleased && RewardAndCompletionShown && CurrencyGrantedOnce && DeathStopsAttack;
            Details = $"strategy=orbit, seconds={FightSeconds}, bossHP={receiver.CurrentHealth}, minBossHP={MinimumBossHealth}, playerHP={_health.CurrentHealth}, minPlayerHP={MinimumPlayerHealth}, revives={_health.RevivesRemaining}, retries={_flow.RetryCount}, attacks={ObservedAttacks}, areas={ObservedAreas}, input={NormalInputs}/{SkillInputs}/{UltimateInputs}, bossPosition={boss.transform.position}, playerPosition={_body.position}";
            Finished = true; Restore();
        }
        private static FieldInfo Field(object value) => value.GetType().GetField("_inputSource", BindingFlags.Instance | BindingFlags.NonPublic);
        private void OnDisable() => Restore();
        private void Restore()
        {
            if (!_captured) return;
            _fight = false; Field(_movement).SetValue(_movement, _oldMove); Field(_combat).SetValue(_combat, _oldCombat);
            Time.captureDeltaTime = _captureDelta; _captured = false;
        }
    }
}
