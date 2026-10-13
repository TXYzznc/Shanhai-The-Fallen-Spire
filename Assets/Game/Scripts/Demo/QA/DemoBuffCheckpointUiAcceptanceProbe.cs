using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Shanhai.Demo.Build;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Configuration;
using Shanhai.Demo.Input;
using Shanhai.Demo.Player;
using Shanhai.Demo.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Shanhai.Demo.QA
{
    /// <summary>六增益UI抽取/实际效果与步行检查点验收，不传送玩家、不注入敌人伤害。</summary>
    public sealed class DemoBuffCheckpointUiAcceptanceProbe : MonoBehaviour, IDemoInputSource
    {
        [SerializeField] private GameObject hud;
        [SerializeField] private DemoGameplayConfig gameplayConfig;
        [SerializeField] private DemoCheckpointController checkpoint;
        public bool SixConfigsValid { get; private set; }
        public bool EligibleOptionsUnique { get; private set; }
        public bool UiConfirmsOnce { get; private set; }
        public bool AllSixApplied { get; private set; }
        public bool BuffCombatEffects { get; private set; }
        public bool BuffMovementSpeed { get; private set; }
        public bool EmptyPoolHidesOldPanel { get; private set; }
        public bool CheckpointReachedByWalking { get; private set; }
        public bool UpgradeConfirmedByUi { get; private set; }
        public bool ReturnDoesNotRewardAgain { get; private set; }
        public bool RevivePreservesCheckpoint { get; private set; }
        public bool RetryClearsCheckpointAndBuffs { get; private set; }
        public bool NextRunCheckpointReopens { get; private set; }
        public bool PreUpgradeSnapshotCaptured { get; private set; }
        public bool UpgradeSnapshotCaptured { get; private set; }
        public bool SnapshotCollectionsReadOnly { get; private set; }
        public bool ReturnPreservesSnapshot { get; private set; }
        public bool RevivePreservesSnapshot { get; private set; }
        public bool RetryClearsSnapshot { get; private set; }
        public bool NewRunCapturesNewSnapshot { get; private set; }
        public bool Completed { get; private set; }
        public bool Finished { get; private set; }
        public string Details { get; private set; }
        private DemoBuffLoadout _buffs;
        private DemoRewardChoiceController _choice;
        private DemoRewardChoiceView _view;
        private DemoPlayerCombatController _combat;
        private DemoPlayerController _movement;
        private DemoPlayerHealthController _health;
        private DemoRunFlowController _flow;
        private Rigidbody2D _body;
        private Transform _panel;
        private PointerEventData _pointer;
        private readonly List<RaycastResult> _hits = new List<RaycastResult>(32);
        private DemoInputState _input;
        private object _oldCombatInput, _oldMovementInput;
        private float _captureDelta;
        private bool _captured;
        private GameObject _target;
        public DemoInputState ReadState() => _input;
        private static FieldInfo Field(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        private void Input(Vector2 move = default, bool attack = false, bool skill = false) =>
            _input = new DemoInputState(move, Vector2.zero, attack, false, skill, false, Vector2.up);

        private IEnumerator Start()
        {
            yield return null;
            _buffs = GetComponent<DemoBuffLoadout>();
            _combat = GetComponent<DemoPlayerCombatController>();
            _movement = GetComponent<DemoPlayerController>();
            _health = GetComponent<DemoPlayerHealthController>();
            _flow = GetComponent<DemoRunFlowController>();
            _body = GetComponent<Rigidbody2D>();
            _choice = hud.GetComponent<DemoRewardChoiceController>();
            _view = hud.GetComponent<DemoRewardChoiceView>();
            _panel = hud.transform.Find("RewardChoicePanel");
            _pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            _oldCombatInput = Field(_combat, "_inputSource").GetValue(_combat);
            _oldMovementInput = Field(_movement, "_inputSource").GetValue(_movement);
            _captureDelta = Time.captureDeltaTime;
            _captured = true;
            Time.captureDeltaTime = 0.05f;
            Field(_combat, "_inputSource").SetValue(_combat, this);
            Field(_movement, "_inputSource").SetValue(_movement, this);
            _flow.RestartRun();
            SixConfigsValid = gameplayConfig.Buffs.Length == 6;
            EligibleOptionsUnique = UiConfirmsOnce = true;
            foreach (DemoBuffConfig config in gameplayConfig.Buffs)
                SixConfigsValid &= config != null && config.level == 1 && !string.IsNullOrEmpty(config.id)
                    && !string.IsNullOrEmpty(config.displayName) && !string.IsNullOrEmpty(config.description) && config.value > 0f;
            for (int count = 0; count < 6; count++)
            {
                UnityEngine.Object.FindObjectOfType<Shanhai.Demo.Player.DemoWorldEntry>().Ui.OpenBuffs();
                yield return null;
                EligibleOptionsUnique &= _choice.OptionCount == Mathf.Min(3, 6 - count);
                for (int i = 0; i < _choice.OptionCount; i++)
                {
                    EligibleOptionsUnique &= !_buffs.HasBuff(_choice.GetOption(i));
                    for (int j = 0; j < i; j++) EligibleOptionsUnique &= _choice.GetOption(i) != _choice.GetOption(j);
                }
                UiConfirmsOnce &= ConfirmBuff() && _buffs.ActiveCount == count + 1;
            }
            AllSixApplied = _buffs.ActiveCount == 6 && Near(_health.MaxHealth, 115f) && Near(_buffs.AttackMultiplier, 1.1f)
                && Near(_buffs.MoveSpeedMultiplier, 1.1f) && Near(_buffs.NormalAttackMultiplier, 1.15f)
                && Near(_buffs.SkillCooldownMultiplier, 0.9f) && Near(_buffs.ImbalanceMultiplier, 1.2f);
            Vector2 beforeMove = _body.position;
            Input(Vector2.right);
            yield return new WaitForSeconds(0.2f);
            Input();
            float moved = _body.position.x - beforeMove.x;
            BuffMovementSpeed = moved >= 1f && moved <= 1.4f;
            _target = new GameObject("QA_BuffCombatTarget");
            _target.transform.position = transform.position + Vector3.up;
            _target.AddComponent<CircleCollider2D>().radius = 0.05f;
            var receiver = _target.AddComponent<DemoDamageReceiver>();
            receiver.ConfigureHealth(200f, 0f);
            receiver.ConfigureStagger(15f, 3f, 2f, 1f);
            Physics2D.SyncTransforms();
            _combat.ResetForRetry();
            Input(attack: true);
            yield return null;
            Input();
            yield return new WaitForSeconds(0.2f);
            bool normal = Near(200f - receiver.CurrentHealth, 12.65f) && receiver.IsStaggered;
            yield return new WaitForSeconds(0.2f);
            float beforeSkill = receiver.CurrentHealth;
            Input(skill: true);
            yield return null;
            Input();
            BuffCombatEffects = normal && Near(beforeSkill - receiver.CurrentHealth, 22f)
                && _combat.SkillCooldownRemaining > 4.3f && _combat.SkillCooldownRemaining <= 4.5f;
            Destroy(_target);
            _target = null;
            yield return null;
            _buffs.ResetForRetry();
            UnityEngine.Object.FindObjectOfType<Shanhai.Demo.Player.DemoWorldEntry>().Ui.OpenBuffs();
            foreach (DemoBuffConfig config in gameplayConfig.Buffs) _buffs.TryApplyBuff(config.id);
            UnityEngine.Object.FindObjectOfType<Shanhai.Demo.Player.DemoWorldEntry>().Ui.OpenBuffs();
            EmptyPoolHidesOldPanel = _choice.OptionCount == 0 && !_choice.IsOpen && !_panel.gameObject.activeSelf;
            _flow.RestartRun();
            GetComponent<DemoMarkLoadout>().TryEquip("beast_02_mountain", 3);
            _flow.AddCurrency(17);
            yield return WalkTo(new Vector2(8f, 0f), 3f);
            yield return WalkTo(new Vector2(8f, 24f), 6f);
            yield return WalkTo(new Vector2(0f, 24f), 3f);
            yield return null;
            CheckpointReachedByWalking = checkpoint.IsActivated && _choice.IsBuffChoice && _choice.OptionCount == 3;
            var beforeUpgrade = _flow.CheckpointSnapshot;
            PreUpgradeSnapshotCaptured = beforeUpgrade != null && beforeUpgrade.CheckpointId == checkpoint.name
                && beforeUpgrade.RunNumber == _flow.RetryCount && _flow.CheckpointSaveRevision == 1
                && !beforeUpgrade.UpgradeConfirmed && beforeUpgrade.Buffs.Count == 0 && beforeUpgrade.Marks.Count == 4
                && beforeUpgrade.Marks[3] == "beast_02_mountain" && beforeUpgrade.Currency == 17
                && Near(beforeUpgrade.Health, 100f) && Near(beforeUpgrade.MaxHealth, 120f) && beforeUpgrade.RevivesRemaining == 1
                && beforeUpgrade.Energy > 0f && beforeUpgrade.Energy <= _combat.CurrentEnergy
                && beforeUpgrade.Waves.Count == 2 && beforeUpgrade.Encounters.Count == 2;
            if (beforeUpgrade != null)
            {
                foreach (var wave in beforeUpgrade.Waves) PreUpgradeSnapshotCaptured &= wave != null && !wave.HasStarted && !wave.IsCompleted && wave.SpawnedCount == 0;
                foreach (var encounter in beforeUpgrade.Encounters) PreUpgradeSnapshotCaptured &= encounter != null && !encounter.HasStarted && !encounter.IsCompleted;
            }
            UpgradeConfirmedByUi = ConfirmBuff() && _buffs.ActiveCount == 1;
            string selected = _choice.SelectedMarkId;
            yield return null;
            var afterUpgrade = _flow.CheckpointSnapshot;
            UpgradeSnapshotCaptured = afterUpgrade != null && !ReferenceEquals(beforeUpgrade, afterUpgrade)
                && afterUpgrade.UpgradeConfirmed && afterUpgrade.UpgradeId == selected && afterUpgrade.Buffs.Count == 1
                && afterUpgrade.Buffs[0] == selected && _flow.CheckpointSaveRevision == 2 && Near(afterUpgrade.MaxHealth, _health.MaxHealth);
            SnapshotCollectionsReadOnly = false;
            if (afterUpgrade != null)
            {
                try { ((IList<string>)afterUpgrade.Buffs)[0] = "invalid"; }
                catch (System.NotSupportedException) { SnapshotCollectionsReadOnly = afterUpgrade.Buffs[0] == selected && _buffs.HasBuff(selected); }
            }
            yield return WalkTo(new Vector2(8f, 24f), 3f);
            yield return WalkTo(new Vector2(8f, 20f), 2f);
            yield return WalkTo(new Vector2(8f, 24f), 2f);
            yield return WalkTo(new Vector2(0f, 24f), 2f);
            ReturnDoesNotRewardAgain = checkpoint.IsActivated && !_choice.IsOpen && _buffs.ActiveCount == 1 && _buffs.HasBuff(selected);
            ReturnPreservesSnapshot = ReferenceEquals(afterUpgrade, _flow.CheckpointSnapshot) && _flow.CheckpointSaveRevision == 2;
            _health.ReceiveDamage(new DemoDamageInfo(_health.MaxHealth * 2f, gameObject));
            RevivePreservesCheckpoint = _health.RevivesRemaining == 0 && !_health.IsRetryRequired
                && checkpoint.IsActivated && _buffs.HasBuff(selected) && _buffs.ActiveCount == 1;
            RevivePreservesSnapshot = ReferenceEquals(afterUpgrade, _flow.CheckpointSnapshot) && afterUpgrade != null
                && afterUpgrade.RevivesRemaining == 1 && afterUpgrade.Currency == 17 && Near(afterUpgrade.Health, 100f);
            _flow.RestartRun();
            RetryClearsCheckpointAndBuffs = !checkpoint.IsActivated && _buffs.ActiveCount == 0 && !_choice.IsOpen
                && _body.position.sqrMagnitude < 0.01f && Near(_health.CurrentHealth, 100f);
            RetryClearsSnapshot = _flow.CheckpointSnapshot == null && _flow.CheckpointSaveRevision == 0
                && afterUpgrade != null && afterUpgrade.Buffs.Count == 1 && afterUpgrade.Currency == 17;
            yield return WalkTo(new Vector2(8f, 0f), 3f);
            yield return WalkTo(new Vector2(8f, 24f), 6f);
            yield return WalkTo(new Vector2(0f, 24f), 3f);
            yield return null;
            NextRunCheckpointReopens = checkpoint.IsActivated && _choice.IsOpen && _choice.IsBuffChoice && _choice.OptionCount == 3;
            var nextSnapshot = _flow.CheckpointSnapshot;
            NewRunCapturesNewSnapshot = nextSnapshot != null && !ReferenceEquals(afterUpgrade, nextSnapshot)
                && nextSnapshot.RunNumber == _flow.RetryCount && nextSnapshot.Currency == 0 && nextSnapshot.Buffs.Count == 0
                && nextSnapshot.Marks.Count == 4 && string.IsNullOrEmpty(nextSnapshot.Marks[3]) && _flow.CheckpointSaveRevision == 1;
            Completed = SixConfigsValid && EligibleOptionsUnique && UiConfirmsOnce && AllSixApplied && BuffCombatEffects
                && BuffMovementSpeed && EmptyPoolHidesOldPanel && CheckpointReachedByWalking && UpgradeConfirmedByUi
                && ReturnDoesNotRewardAgain && RevivePreservesCheckpoint && RetryClearsCheckpointAndBuffs && NextRunCheckpointReopens
                && PreUpgradeSnapshotCaptured && UpgradeSnapshotCaptured && SnapshotCollectionsReadOnly && ReturnPreservesSnapshot
                && RevivePreservesSnapshot && RetryClearsSnapshot && NewRunCapturesNewSnapshot;
            Details = $"movement={moved}, normal12.65/stagger={normal}, emptyPanel={EmptyPoolHidesOldPanel}, checkpoint={checkpoint.IsActivated}, position={_body.position}";
            Finished = true;
            Restore();
        }

        private bool ConfirmBuff()
        {
            if (!_choice.IsOpen || !_choice.IsBuffChoice || _choice.OptionCount < 1) return false;
            string id = _choice.GetOption(0);
            int before = _buffs.ActiveCount;
            var option = _panel.Find("RewardOption_1").gameObject;
            ExecuteEvents.Execute(option, _pointer, ExecuteEvents.pointerEnterHandler);
            bool tooltip = _panel.Find("Tooltip").GetComponent<TMP_Text>().text == _choice.GetBuffOptionConfig(0).description;
            bool preview = Click(option) && _buffs.ActiveCount == before && _choice.IsOpen;
            bool confirm = Click(option) && _buffs.HasBuff(id) && _buffs.ActiveCount == before + 1
                && _choice.IsConfirmed && !_choice.IsOpen && !_panel.gameObject.activeSelf;
            return tooltip && preview && confirm && !_choice.SelectOption(0) && !_buffs.TryApplyBuff(id);
        }
        private bool Click(GameObject button)
        {
            _pointer.position = RectTransformUtility.WorldToScreenPoint(null, button.transform.position);
            _hits.Clear();
            EventSystem.current.RaycastAll(_pointer, _hits);
            if (_hits.Count == 0 || !(_hits[0].gameObject == button || _hits[0].gameObject.transform.IsChildOf(button.transform))) return false;
            return ExecuteEvents.ExecuteHierarchy(_hits[0].gameObject, _pointer, ExecuteEvents.pointerClickHandler) != null;
        }
        private IEnumerator WalkTo(Vector2 target, float timeout)
        {
            float until = Time.time + timeout;
            while (Vector2.Distance(_body.position, target) > 0.2f && Time.time < until)
            {
                Input((target - _body.position).normalized);
                yield return null;
            }
            Input();
        }
        private static bool Near(float actual, float expected) => Mathf.Abs(actual - expected) < 0.01f;
        private void OnDisable() => Restore();
        private void Restore()
        {
            if (!_captured) return;
            Field(_combat, "_inputSource").SetValue(_combat, _oldCombatInput);
            Field(_movement, "_inputSource").SetValue(_movement, _oldMovementInput);
            Time.captureDeltaTime = _captureDelta;
            if (_target != null) Destroy(_target);
            _captured = false;
        }
    }
}
