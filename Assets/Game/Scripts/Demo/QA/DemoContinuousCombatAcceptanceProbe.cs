using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Shanhai.Demo.Build;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Enemies;
using Shanhai.Demo.Input;
using Shanhai.Demo.Player;
using Shanhai.Demo.UI;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Shanhai.Demo.QA
{
    /// <summary>同一局从起点步行通关，默认数值/AI；奖励通过实际UGUI射线和点击事件确认。</summary>
    public sealed class DemoContinuousCombatAcceptanceProbe : MonoBehaviour, IDemoInputSource
    {
        public bool StartsAtMapStart { get; private set; }
        public bool FirstWaveCleared { get; private set; }
        public bool SecondWaveCleared { get; private set; }
        public bool CheckpointUpgradeByUi { get; private set; }
        public bool CheckpointContainsCompletedWaves { get; private set; }
        public bool EliteDefeated { get; private set; }
        public bool EliteMarkByUi { get; private set; }
        public bool BossDefeated { get; private set; }
        public bool BossPhaseTwoObserved { get; private set; }
        public bool CompletionAndCurrency { get; private set; }
        public bool BossMarkByUi { get; private set; }
        public bool RetryButtonRestoresWorld { get; private set; }
        public bool Completed { get; private set; }
        public bool Finished { get; private set; }
        public string Stage { get; private set; }
        public string Details { get; private set; }
        private DemoWorldEntry _world;
        private DemoPlayerController _movement;
        private DemoPlayerCombatController _combat;
        private DemoPlayerHealthController _health;
        private DemoRunFlowController _flow;
        private DemoWaveSpawner[] _waves;
        private DemoEncounterController[] _encounters;
        private DemoCheckpointController _checkpoint;
        private DemoEnemyController[] _enemies;
        private DemoDamageReceiver[] _receivers;
        private DemoRewardChoiceController _choice;
        private DemoMarkLoadout _marks;
        private DemoBuffLoadout _buffs;
        private DemoCompletionView _completion;
        private Rigidbody2D _body;
        private object _oldMovementInput, _oldCombatInput;
        private float _captureDelta, _begin, _nextNormal, _nextDodge;
        private int _inputFrame = -1, _initialRetries, _normalInputs, _skillInputs, _ultimateInputs;
        private bool _captured, _running;
        private Vector2 _destination;
        private DemoInputState _input;
        private PointerEventData _pointer;
        private readonly List<RaycastResult> _hits = new List<RaycastResult>();
        private Transform _hud;
        private string _observations;
        private static FieldInfo Field(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        public DemoInputState ReadState()
        {
            if (_inputFrame == Time.frameCount) return _input;
            _inputFrame = Time.frameCount;
            Vector2 move = Vector2.zero, aim = Vector2.up;
            bool attack = false, dodge = false, skill = false, ultimate = false;
            if (_running)
            {
                RefreshEnemies();
                Vector2 path = _destination - _body.position;
                move = path.sqrMagnitude > 0.04f ? path.normalized : Vector2.zero;
                aim = move.sqrMagnitude > 0f ? move : (Vector2)transform.up;
                int target = -1; float distanceSquared = float.MaxValue;
                for (int i = 0; i < _enemies.Length; i++)
                {
                    if (_enemies[i] == null || _receivers[i] == null || !_enemies[i].gameObject.activeInHierarchy || _receivers[i].IsDefeated) continue;
                    float distance = ((Vector2)_enemies[i].transform.position - _body.position).sqrMagnitude;
                    if (distance >= distanceSquared) continue;
                    target = i; distanceSquared = distance;
                }
                if (target >= 0)
                {
                    DemoEnemyController enemy = _enemies[target];
                    Vector2 offset = (Vector2)enemy.transform.position - _body.position;
                    aim = offset.sqrMagnitude > 0.001f ? offset.normalized : Vector2.up;
                    Vector2 tangent = new Vector2(-aim.y, aim.x);
                    if (enemy.IsCharging || enemy.IsDashing)
                    {
                        // 精英从4单位触发冲撞：持续后退会永远停在玩家近战范围外。
                        // 侧向闪避锁定冲撞方向后接近；Boss范围攻击仍向外退。
                        move = target == _enemies.Length - 2 ? tangent : -aim;
                        if (!_combat.IsAttackLocked && Time.time >= _nextDodge) { dodge = true; _nextDodge = Time.time + 1.25f; }
                    }
                    else
                    {
                        // Boss保持攻击边缘距离，避免一个移动步长把玩家送进范围攻击退避的盲区。
                        float orbitRadiusSquared = target == _enemies.Length - 1 ? 2.56f : 1.69f;
                        move = distanceSquared > orbitRadiusSquared ? (aim + tangent * 0.5f).normalized : tangent;
                        if (!_combat.IsAttackLocked && distanceSquared < 4f)
                        {
                            if (_combat.CurrentEnergy >= 100f) { ultimate = true; _ultimateInputs++; }
                            else if (_combat.SkillCooldownRemaining <= 0f) { skill = true; _skillInputs++; }
                            else if (Time.time >= _nextNormal) { attack = true; _nextNormal = Time.time + 0.4f; _normalInputs++; }
                        }
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
            _health = GetComponent<DemoPlayerHealthController>(); _flow = GetComponent<DemoRunFlowController>();
            _body = GetComponent<Rigidbody2D>(); _marks = GetComponent<DemoMarkLoadout>(); _buffs = GetComponent<DemoBuffLoadout>();
            _waves = (DemoWaveSpawner[])Field(_flow, "waves").GetValue(_flow);
            _encounters = (DemoEncounterController[])Field(_flow, "encounters").GetValue(_flow);
            _checkpoint = ((DemoCheckpointController[])Field(_flow, "checkpoints").GetValue(_flow))[0];
            _choice = (DemoRewardChoiceController)Field(_flow, "rewardChoice").GetValue(_flow);
            _hud = _choice.transform; _completion = _hud.GetComponent<DemoCompletionView>();
            _world = UnityEngine.Object.FindObjectOfType<DemoWorldEntry>();
            System.Array.Sort(_waves, (a, b) => a.Wave.CompareTo(b.Wave));
            System.Array.Sort(_encounters, (a, b) => a.transform.position.y.CompareTo(b.transform.position.y));
            int capacity = _encounters.Length;
            foreach (var wave in _waves) capacity += wave.ConfiguredCount;
            _enemies = new DemoEnemyController[capacity]; _receivers = new DemoDamageReceiver[capacity];
            RefreshEnemies();
            _oldMovementInput = Field(_movement, "_inputSource").GetValue(_movement); _oldCombatInput = Field(_combat, "_inputSource").GetValue(_combat);
            _captureDelta = Time.captureDeltaTime; _captured = true; Time.captureDeltaTime = 0.05f;
            Field(_movement, "_inputSource").SetValue(_movement, this); Field(_combat, "_inputSource").SetValue(_combat, this);
            _pointer = new PointerEventData(EventSystem.current); _initialRetries = _flow.RetryCount; _begin = Time.time;
            StartsAtMapStart = _body.position.sqrMagnitude < 0.01f && _marks.EquippedCount == 0 && _buffs.ActiveCount == 0;
            StartsAtMapStart &= _world.OwnedEntityCount == 1;
            foreach (var enemy in _enemies) StartsAtMapStart &= enemy == null;
            _running = true;
            Stage = "第一波"; _destination = new Vector2(0f, 8f);
            yield return WaitUntilWave(_waves[0], 40f); FirstWaveCleared = _waves[0].IsCompleted && AliveRun(); Record();
            if (!FirstWaveCleared) { Finish(); yield break; }
            Stage = "第二波"; _destination = new Vector2(0f, 18f);
            yield return WaitUntilWave(_waves[1], 40f); SecondWaveCleared = _waves[1].IsCompleted && AliveRun(); Record();
            if (!SecondWaveCleared) { Finish(); yield break; }
            Stage = "检查点"; yield return WalkTo(new Vector2(0f, 24f), 10f); yield return new WaitForSeconds(0.2f);
            _running = false;
            if (_checkpoint.IsActivated && _choice.IsOpen && _choice.IsBuffChoice)
            {
                int option = 0;
                for (int i = 0; i < _choice.OptionCount; i++) if (_choice.GetOption(i) == "buff_attack") option = i;
                CheckpointUpgradeByUi = ConfirmOption(option) && _buffs.ActiveCount == 1;
            }
            yield return null;
            var snapshot = _flow.CheckpointSnapshot;
            CheckpointContainsCompletedWaves = snapshot != null && snapshot.UpgradeConfirmed && snapshot.Waves.Count == 2
                && snapshot.Waves[0].IsCompleted && snapshot.Waves[1].IsCompleted;
            Record(); if (!CheckpointUpgradeByUi) { Finish(); yield break; }
            _running = true; Stage = "精英"; _destination = new Vector2(0f, 36f);
            yield return WaitUntilEncounter(_encounters[0], 75f); EliteDefeated = _encounters[0].IsCompleted && AliveRun(); Record();
            if (!EliteDefeated) { Finish(); yield break; }
            _running = false; yield return new WaitForSeconds(0.2f);
            EliteMarkByUi = SelectSlot(3) && ConfirmOption(2) && _marks.EquippedCount == 1 && _marks.GetMarkId(3) == "beast_03_shadow";
            if (!EliteMarkByUi) { Finish(); yield break; }
            _running = true; Stage = "Boss"; _destination = new Vector2(0f, 48f);
            yield return WaitUntilEncounter(_encounters[1], 180f); BossDefeated = _encounters[1].IsCompleted && AliveRun(); Record();
            _running = false; if (!BossDefeated) { Finish(); yield break; }
            yield return new WaitForSeconds(0.2f); CompletionAndCurrency = _completion.IsOpen && _choice.IsOpen && _flow.CurrentCurrency == 300;
            BossMarkByUi = SelectSlot(1) && ConfirmOption(1) && _marks.EquippedCount == 2;
            yield return null; Canvas.ForceUpdateCanvases();
            bool retry = Click(_hud.Find("CompletionPanel/Retry").gameObject); yield return null;
            RetryButtonRestoresWorld = retry && _body.position.sqrMagnitude < 0.01f && _marks.EquippedCount == 0 && _buffs.ActiveCount == 0
                && _flow.CurrentCurrency == 0 && _flow.CheckpointSnapshot == null && !_completion.IsOpen && !_choice.IsOpen;
            foreach (var wave in _waves) RetryButtonRestoresWorld &= !wave.HasStarted && !wave.IsCompleted;
            foreach (var encounter in _encounters) RetryButtonRestoresWorld &= !encounter.HasStarted && !encounter.IsCompleted;
            RefreshEnemies();
            RetryButtonRestoresWorld &= _world.OwnedEntityCount == 1 && _world.Feedback.ActiveCount == 0;
            foreach (var enemy in _enemies) RetryButtonRestoresWorld &= enemy == null;
            Completed = StartsAtMapStart && FirstWaveCleared && SecondWaveCleared && CheckpointUpgradeByUi && CheckpointContainsCompletedWaves
                && EliteDefeated && EliteMarkByUi && BossDefeated && BossPhaseTwoObserved && CompletionAndCurrency && BossMarkByUi && RetryButtonRestoresWorld;
            Stage = "已结束"; Finish();
        }
        private void RefreshEnemies()
        {
            int index = 0;
            foreach (var wave in _waves)
                for (int i = 0; i < wave.ConfiguredCount; i++) SetEnemy(index++, wave.EnemyAt(i));
            foreach (var encounter in _encounters) SetEnemy(index++, encounter.Enemy);
        }
        private void SetEnemy(int index, GameObject enemy)
        {
            if (_enemies[index] != null && _enemies[index].gameObject == enemy) return;
            _enemies[index] = enemy != null ? enemy.GetComponent<DemoEnemyController>() : null;
            _receivers[index] = enemy != null ? enemy.GetComponent<DemoDamageReceiver>() : null;
        }
        private bool AliveRun() => _flow.RetryCount == _initialRetries && !_health.IsRetryRequired;
        private IEnumerator WaitUntilWave(DemoWaveSpawner wave, float timeout)
        {
            float until = Time.time + timeout; while (!wave.IsCompleted && Time.time < until && AliveRun()) yield return null;
        }
        private IEnumerator WaitUntilEncounter(DemoEncounterController encounter, float timeout)
        {
            float until = Time.time + timeout;
            while (!encounter.IsCompleted && Time.time < until && AliveRun())
            { RefreshEnemies(); if (encounter == _encounters[1] && _enemies[_enemies.Length - 1] != null) BossPhaseTwoObserved |= _enemies[_enemies.Length - 1].Phase == 2; yield return null; }
        }
        private IEnumerator WalkTo(Vector2 point, float timeout)
        {
            _destination = point; float until = Time.time + timeout;
            while (Vector2.Distance(_body.position, point) > 0.2f && Time.time < until && AliveRun()) yield return null;
        }
        private bool SelectSlot(int slot)
        {
            Transform button = _hud.Find("RewardChoicePanel/RewardSlot_" + slot);
            return button != null && Click(button.gameObject);
        }
        private bool ConfirmOption(int index)
        {
            Canvas.ForceUpdateCanvases(); Transform option = _hud.Find("RewardChoicePanel/RewardOption_" + (index + 1));
            return option != null && Click(option.gameObject) && Click(option.gameObject) && _choice.IsConfirmed && !_choice.IsOpen;
        }
        private bool Click(GameObject button)
        {
            _pointer.position = RectTransformUtility.WorldToScreenPoint(null, button.transform.position); _hits.Clear(); EventSystem.current.RaycastAll(_pointer, _hits);
            if (_hits.Count == 0 || !(_hits[0].gameObject == button || _hits[0].gameObject.transform.IsChildOf(button.transform))) return false;
            return ExecuteEvents.ExecuteHierarchy(_hits[0].gameObject, _pointer, ExecuteEvents.pointerClickHandler) != null;
        }
        private void Record() => _observations += $"{Stage}: HP={_health.CurrentHealth}/{_health.MaxHealth},revives={_health.RevivesRemaining},retry={_flow.RetryCount},pos={_body.position},eliteDone={_encounters[0].IsCompleted},bossDone={_encounters[1].IsCompleted}; ";
        private void Finish() { Details = _observations + $"stage={Stage}, seconds={Time.time - _begin}, input={_normalInputs}/{_skillInputs}/{_ultimateInputs}"; Finished = true; Restore(); }
        private void OnDisable() => Restore();
        private void Restore()
        {
            if (!_captured) return; _running = false;
            if (_movement != null) Field(_movement, "_inputSource").SetValue(_movement, _oldMovementInput);
            if (_combat != null) Field(_combat, "_inputSource").SetValue(_combat, _oldCombatInput);
            Time.captureDeltaTime = _captureDelta; _captured = false;
        }
    }
}
