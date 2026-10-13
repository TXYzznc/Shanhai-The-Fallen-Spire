using System.Collections;
using System.Reflection;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Enemies;
using Shanhai.Demo.Input;
using Shanhai.Demo.Player;
using UnityEngine;

namespace Shanhai.Demo.QA
{
    /// <summary>使用正式输入、攻击与敌人AI清除两波；不传送、不注入伤害、不修改战斗数值。</summary>
    public sealed class DemoWaveCombatInputAcceptanceProbe : MonoBehaviour, IDemoInputSource
    {
        [SerializeField] private DemoWaveSpawner firstWave;
        [SerializeField] private DemoWaveSpawner secondWave;
        [SerializeField] private DemoEnemyController[] firstEnemies;
        [SerializeField] private DemoEnemyController[] secondEnemies;
        public bool StartsWithoutEnemies { get; private set; }
        public bool FirstConfiguredWaveSpawned { get; private set; }
        public bool AllFourAiChased { get; private set; }
        public bool PlayerTookActualDamage { get; private set; }
        public bool FirstWaveClearedByCombat { get; private set; }
        public bool SecondConfiguredWaveSpawned { get; private set; }
        public bool SecondWaveClearedByCombat { get; private set; }
        public bool DeadEnemiesReleaseCollision { get; private set; }
        public bool NoRespawnOnReturn { get; private set; }
        public bool RetryRestoresBothWaves { get; private set; }
        public bool Completed { get; private set; }
        public bool Finished { get; private set; }
        public string Details { get; private set; }
        public string FirstWaveCombatDetails { get; private set; }
        public string SecondWaveCombatDetails { get; private set; }
        private DemoPlayerController _movement;
        private DemoPlayerCombatController _combat;
        private DemoPlayerHealthController _health;
        private DemoRunFlowController _flow;
        private Rigidbody2D _body;
        private DemoEnemyController[] _targets;
        private DemoDamageReceiver[] _firstReceivers;
        private DemoDamageReceiver[] _secondReceivers;
        private object _movementInput;
        private object _combatInput;
        private float _captureDelta;
        private bool _captured;
        private bool _fight;
        private Vector2 _walk;
        private float _nextAttack;
        private float _nextSkill;
        private float _nextDodge;
        private int _inputFrame = -1;
        private DemoInputState _input;

        public DemoInputState ReadState()
        {
            if (_inputFrame == Time.frameCount) return _input;
            _inputFrame = Time.frameCount;
            Vector2 move = _walk;
            Vector2 aim = _walk.sqrMagnitude > 0f ? _walk : (Vector2)transform.up;
            bool attack = false, skill = false, dodge = false;
            if (_fight)
            {
                int closest = -1;
                float distanceSquared = float.MaxValue;
                for (int i = 0; i < _targets.Length; i++)
                {
                    DemoDamageReceiver receiver = _targets == firstEnemies ? _firstReceivers[i] : _secondReceivers[i];
                    if (receiver.IsDefeated) continue;
                    float distance = ((Vector2)_targets[i].transform.position - _body.position).sqrMagnitude;
                    if (distance >= distanceSquared) continue;
                    closest = i;
                    distanceSquared = distance;
                }
                if (closest >= 0)
                {
                    aim = ((Vector2)_targets[closest].transform.position - _body.position).normalized;
                    move = distanceSquared > 2.1f ? aim : Vector2.zero;
                    if (!_combat.IsAttackLocked && distanceSquared < 6.25f)
                    {
                        if (Time.time >= _nextSkill && _combat.SkillCooldownRemaining <= 0f)
                        {
                            skill = true;
                            _nextSkill = Time.time + 5.05f;
                        }
                        else if (Time.time >= _nextAttack)
                        {
                            attack = true;
                            _nextAttack = Time.time + 0.4f;
                        }
                        if (Time.time >= _nextDodge)
                        {
                            dodge = true;
                            _nextDodge = Time.time + 1.3f;
                        }
                    }
                }
            }
            _input = new DemoInputState(move, Vector2.zero, attack, dodge, skill, false, aim);
            return _input;
        }

        private IEnumerator Start()
        {
            yield return null;
            _movement = GetComponent<DemoPlayerController>();
            _combat = GetComponent<DemoPlayerCombatController>();
            _health = GetComponent<DemoPlayerHealthController>();
            _flow = GetComponent<DemoRunFlowController>();
            _body = GetComponent<Rigidbody2D>();
            _movementInput = InputField(typeof(DemoPlayerController)).GetValue(_movement);
            _combatInput = InputField(typeof(DemoPlayerCombatController)).GetValue(_combat);
            _captureDelta = Time.captureDeltaTime;
            _captured = true;
            Time.captureDeltaTime = 0.05f;
            InputField(typeof(DemoPlayerController)).SetValue(_movement, this);
            InputField(typeof(DemoPlayerCombatController)).SetValue(_combat, this);
            _firstReceivers = CaptureReceivers(firstEnemies);
            _secondReceivers = CaptureReceivers(secondEnemies);
            var firstPositions = new Vector2[firstEnemies.Length];
            for (int i = 0; i < firstEnemies.Length; i++) firstPositions[i] = firstEnemies[i].transform.position;
            StartsWithoutEnemies = !firstWave.HasStarted && !secondWave.HasStarted;
            for (int i = 0; i < firstEnemies.Length; i++) StartsWithoutEnemies &= !firstEnemies[i].gameObject.activeSelf;
            for (int i = 0; i < secondEnemies.Length; i++) StartsWithoutEnemies &= !secondEnemies[i].gameObject.activeSelf;
            yield return WalkTo(5f, 3f);
            FirstConfiguredWaveSpawned = firstWave.HasStarted && firstWave.SpawnedCount == 4 && firstWave.ConfiguredCount == 4;
            float until = Time.time + 5f;
            while (Time.time < until && _flow.RetryCount == 0) yield return null;
            AllFourAiChased = true;
            for (int i = 0; i < firstEnemies.Length; i++)
                AllFourAiChased &= firstEnemies[i].enabled && firstEnemies[i].HasTargetReceiver
                    && Vector2.Distance(firstPositions[i], firstEnemies[i].transform.position) > 0.5f;
            PlayerTookActualDamage = _health.CurrentHealth < _health.MaxHealth || _health.RevivesRemaining == 0;
            yield return Fight(firstWave, firstEnemies, 25f);
            FirstWaveClearedByCombat = firstWave.IsCompleted && _flow.RetryCount == 0;
            FirstWaveCombatDetails = CombatDetails(firstEnemies);
            if (!FirstWaveClearedByCombat) { Finish(); yield break; }
            yield return WalkTo(15f, 5f);
            SecondConfiguredWaveSpawned = secondWave.HasStarted && secondWave.SpawnedCount == 4 && secondWave.ConfiguredCount == 4;
            yield return Fight(secondWave, secondEnemies, 25f);
            SecondWaveClearedByCombat = secondWave.IsCompleted && _flow.RetryCount == 0;
            SecondWaveCombatDetails = CombatDetails(secondEnemies);
            if (!SecondWaveClearedByCombat) { Finish(); yield break; }
            yield return null;
            DeadEnemiesReleaseCollision = true;
            for (int i = 0; i < firstEnemies.Length; i++) DeadEnemiesReleaseCollision &= !firstEnemies[i].GetComponent<Collider2D>().enabled;
            for (int i = 0; i < secondEnemies.Length; i++) DeadEnemiesReleaseCollision &= !secondEnemies[i].GetComponent<Collider2D>().enabled;
            yield return WalkTo(0f, 6f);
            NoRespawnOnReturn = _body.position.y < 0.3f && firstWave.IsCompleted && secondWave.IsCompleted
                && firstWave.SpawnedCount == 4 && secondWave.SpawnedCount == 4;
            _flow.RestartRun();
            yield return null;
            RetryRestoresBothWaves = !firstWave.HasStarted && !secondWave.HasStarted
                && firstWave.SpawnedCount == 0 && secondWave.SpawnedCount == 0;
            for (int i = 0; i < firstEnemies.Length; i++) RetryRestoresBothWaves &= Restored(firstEnemies[i], _firstReceivers[i]);
            for (int i = 0; i < secondEnemies.Length; i++) RetryRestoresBothWaves &= Restored(secondEnemies[i], _secondReceivers[i]);
            Completed = StartsWithoutEnemies && FirstConfiguredWaveSpawned && AllFourAiChased && PlayerTookActualDamage
                && FirstWaveClearedByCombat && SecondConfiguredWaveSpawned && SecondWaveClearedByCombat
                && DeadEnemiesReleaseCollision && NoRespawnOnReturn && RetryRestoresBothWaves;
            Finish();
        }

        private IEnumerator WalkTo(float y, float timeout)
        {
            float until = Time.time + timeout;
            while (Mathf.Abs(_body.position.y - y) > 0.25f && Time.time < until && _flow.RetryCount == 0)
            {
                _walk = _body.position.y < y ? Vector2.up : Vector2.down;
                yield return null;
            }
            _walk = Vector2.zero;
        }
        private IEnumerator Fight(DemoWaveSpawner wave, DemoEnemyController[] targets, float timeout)
        {
            _targets = targets;
            _fight = true;
            float until = Time.time + timeout;
            while (!wave.IsCompleted && Time.time < until && _flow.RetryCount == 0) yield return null;
            _fight = false;
        }
        private static FieldInfo InputField(System.Type type) => type.GetField("_inputSource", BindingFlags.Instance | BindingFlags.NonPublic);
        private static DemoDamageReceiver[] CaptureReceivers(DemoEnemyController[] enemies)
        {
            var receivers = new DemoDamageReceiver[enemies.Length];
            for (int i = 0; i < enemies.Length; i++) receivers[i] = enemies[i].GetComponent<DemoDamageReceiver>();
            return receivers;
        }
        private string CombatDetails(DemoEnemyController[] enemies)
        {
            string details = $"health={_health.CurrentHealth}, revives={_health.RevivesRemaining}, retries={_flow.RetryCount}";
            for (int i = 0; i < enemies.Length; i++)
                details += $", {enemies[i].name}: attacks={enemies[i].AttackCount}, health={enemies[i].GetComponent<DemoDamageReceiver>().CurrentHealth}, position={enemies[i].transform.position}";
            return details;
        }
        private static bool Restored(DemoEnemyController enemy, DemoDamageReceiver receiver) =>
            !enemy.gameObject.activeSelf && enemy.enabled && receiver.CurrentHealth == receiver.MaxHealth && enemy.GetComponent<Collider2D>().enabled;
        private void Finish()
        {
            Details = $"first={firstWave.IsCompleted}/{firstWave.SpawnedCount}, second={secondWave.IsCompleted}/{secondWave.SpawnedCount}, health={_health.CurrentHealth}, revives={_health.RevivesRemaining}, retries={_flow.RetryCount}, position={_body.position}";
            Finished = true;
            Restore();
        }
        private void OnDisable() => Restore();
        private void Restore()
        {
            if (!_captured) return;
            _fight = false;
            _walk = Vector2.zero;
            InputField(typeof(DemoPlayerController)).SetValue(_movement, _movementInput);
            InputField(typeof(DemoPlayerCombatController)).SetValue(_combat, _combatInput);
            Time.captureDeltaTime = _captureDelta;
            _captured = false;
        }
    }
}
