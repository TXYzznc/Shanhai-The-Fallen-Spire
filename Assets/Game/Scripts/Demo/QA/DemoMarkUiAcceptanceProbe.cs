using System.Collections;
using System.Collections.Generic;
using Shanhai.Demo.Build;
using Shanhai.Demo.Configuration;
using Shanhai.Demo.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Shanhai.Demo.QA
{
    /// <summary>通过UGUI射线及按钮事件验证三兽印四槽位，不替代物理鼠标/手柄设备验收。</summary>
    public sealed class DemoMarkUiAcceptanceProbe : MonoBehaviour
    {
        [SerializeField] private GameObject hud;
        [SerializeField] private DemoGameplayConfig gameplayConfig;
        public int EquippedCases { get; private set; }
        public int MovedCases { get; private set; }
        public bool ThreeConfigsValid { get; private set; }
        public bool ThreeDistinctOptions { get; private set; }
        public bool ButtonsReachableByRaycast { get; private set; }
        public bool HoverShowsCorrectDetails { get; private set; }
        public bool TooltipFits { get; private set; }
        public bool PreviewDoesNotEquip { get; private set; }
        public bool ConfirmationEquipsOnce { get; private set; }
        public bool LoadoutLabelsMatchSlot { get; private set; }
        public bool SlotEffectsAndReplacement { get; private set; }
        public bool MovingClearsOldSlot { get; private set; }
        public bool RemovalClearsEffects { get; private set; }
        public bool InvalidEquipmentRejected { get; private set; }
        public bool Completed { get; private set; }
        public bool Finished { get; private set; }
        private DemoMarkLoadout _marks;
        private DemoRewardChoiceController _choice;
        private DemoRewardChoiceView _view;
        private TMP_Text _tooltip;
        private Transform _panel;
        private readonly List<RaycastResult> _hits = new List<RaycastResult>(32);
        private PointerEventData _pointer;
        private float _captureDelta;
        private bool _captured;
        private static readonly string[] Ids = { "beast_01_crack", "beast_02_mountain", "beast_03_shadow" };
        private static readonly string[,] Effects = {
            { "二段普攻伤害", "技能失衡累计", "大招伤害", "攻击 +" },
            { "命中后减伤", "技能范围", "大招减伤", "生命上限" },
            { "普攻时移速", "技能冷却", "能量恢复", "移速 +" }
        };

        private IEnumerator Start()
        {
            yield return null;
            _captureDelta = Time.captureDeltaTime;
            _captured = true;
            Time.captureDeltaTime = 0.05f;
            _marks = GetComponent<DemoMarkLoadout>();
            _choice = hud.GetComponent<DemoRewardChoiceController>();
            _view = hud.GetComponent<DemoRewardChoiceView>();
            _panel = hud.transform.Find("RewardChoicePanel");
            _tooltip = _panel.Find("Tooltip").GetComponent<TMP_Text>();
            _pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            ThreeConfigsValid = gameplayConfig.BeastMarks.Length == 3;
            foreach (DemoBeastMarkConfig config in gameplayConfig.BeastMarks)
                ThreeConfigsValid &= config != null && !string.IsNullOrEmpty(config.id) && !string.IsNullOrEmpty(config.displayName)
                    && !string.IsNullOrEmpty(config.iconKey) && config.level == 1 && config.slotCost == 1 && !string.IsNullOrEmpty(config.description);
            ThreeDistinctOptions = ButtonsReachableByRaycast = HoverShowsCorrectDetails = PreviewDoesNotEquip = true;
            ConfirmationEquipsOnce = LoadoutLabelsMatchSlot = SlotEffectsAndReplacement = true;
            MovingClearsOldSlot = RemovalClearsEffects = true;
            TooltipFits = true;
            for (int slot = 0; slot < 4; slot++)
            {
                _marks.ResetForRetry();
                for (int mark = 0; mark < 3; mark++)
                {
                    UnityEngine.Object.FindObjectOfType<Shanhai.Demo.Player.DemoWorldEntry>().Ui.Open();
                    yield return null;
                    ButtonsReachableByRaycast &= Click(_panel.Find("RewardSlot_" + slot).gameObject);
                    int index = OptionIndex(Ids[mark]);
                    ThreeDistinctOptions &= _choice.OptionCount == 3 && index >= 0
                        && _choice.GetOption(0) != _choice.GetOption(1) && _choice.GetOption(1) != _choice.GetOption(2)
                        && _choice.GetOption(0) != _choice.GetOption(2);
                    if (index < 0) { Finished = true; Restore(); yield break; }
                    GameObject option = _panel.Find("RewardOption_" + (index + 1)).gameObject;
                    DemoBeastMarkConfig config = _choice.GetOptionConfig(index);
                    ExecuteEvents.Execute(option, _pointer, ExecuteEvents.pointerEnterHandler);
                    HoverShowsCorrectDetails &= _tooltip.text == config.description && !_tooltip.text.Contains(config.iconKey);
                    _tooltip.ForceMeshUpdate();
                    TooltipFits &= _tooltip.preferredHeight <= _tooltip.rectTransform.rect.height + 0.1f;
                    string before = _marks.GetMarkId(slot);
                    ButtonsReachableByRaycast &= Click(option);
                    PreviewDoesNotEquip &= _choice.IsOpen && _marks.GetMarkId(slot) == before && !_choice.IsConfirmed;
                    _tooltip.ForceMeshUpdate();
                    TooltipFits &= _tooltip.preferredHeight <= _tooltip.rectTransform.rect.height + 0.1f;
                    ButtonsReachableByRaycast &= Click(option);
                    bool confirmed = _choice.IsConfirmed && !_choice.IsOpen && !_panel.gameObject.activeSelf
                        && _marks.GetMarkId(slot) == Ids[mark] && _marks.EquippedCount == 1;
                    ConfirmationEquipsOnce &= confirmed && !_choice.SelectOption(index);
                    if (confirmed) EquippedCases++;
                    // The view refreshes on unscaledTime; match that clock even under captureDeltaTime.
                    yield return new WaitForSecondsRealtime(0.3f);
                    var label = hud.transform.Find("LoadoutPanel/Slot_" + slot).GetComponent<TMP_Text>();
                    LoadoutLabelsMatchSlot &= label.text.Contains(config.displayName) && label.text.Contains("Lv.1")
                        && label.text.Contains(Effects[mark, slot]);
                    SlotEffectsAndReplacement &= MatchesEffects(mark, slot);
                    int nextSlot = (slot + 1) % 4;
                    UnityEngine.Object.FindObjectOfType<Shanhai.Demo.Player.DemoWorldEntry>().Ui.Open(nextSlot);
                    yield return null;
                    index = OptionIndex(Ids[mark]);
                    option = _panel.Find("RewardOption_" + (index + 1)).gameObject;
                    ButtonsReachableByRaycast &= Click(option) && Click(option);
                    bool moved = _marks.GetMarkId(slot) == string.Empty && _marks.GetMarkId(nextSlot) == Ids[mark]
                        && _marks.EquippedCount == 1 && MatchesEffects(mark, nextSlot);
                    MovingClearsOldSlot &= moved;
                    if (moved) MovedCases++;
                    // Return through the UI so the next option must replace this same-slot mark.
                    UnityEngine.Object.FindObjectOfType<Shanhai.Demo.Player.DemoWorldEntry>().Ui.Open(slot);
                    yield return null;
                    index = OptionIndex(Ids[mark]);
                    option = _panel.Find("RewardOption_" + (index + 1)).gameObject;
                    ButtonsReachableByRaycast &= Click(option) && Click(option);
                    SlotEffectsAndReplacement &= MatchesEffects(mark, slot);
                }
                _marks.Remove(slot);
                RemovalClearsEffects &= _marks.EquippedCount == 0 && MatchesEffects(-1, -1);
                yield return new WaitForSecondsRealtime(0.3f);
                RemovalClearsEffects &= hud.transform.Find("LoadoutPanel/Slot_" + slot).GetComponent<TMP_Text>().text.Contains("空槽");
            }
            InvalidEquipmentRejected = !_marks.TryEquip("missing", 0) && !_marks.TryEquip(Ids[0], -1)
                && !_marks.TryEquip(Ids[0], 4) && !_marks.TryEquip(string.Empty, 0) && _marks.EquippedCount == 0;
            Completed = EquippedCases == 12 && MovedCases == 12 && ThreeConfigsValid && ThreeDistinctOptions
                && ButtonsReachableByRaycast && HoverShowsCorrectDetails && TooltipFits && PreviewDoesNotEquip && ConfirmationEquipsOnce
                && LoadoutLabelsMatchSlot && SlotEffectsAndReplacement && MovingClearsOldSlot && RemovalClearsEffects && InvalidEquipmentRejected;
            Finished = true;
            // Keep a mixed loadout and a preview visible for screenshot inspection only.
            _marks.TryEquip(Ids[0], 0);
            _marks.TryEquip(Ids[1], 3);
            _marks.TryEquip(Ids[2], 1);
            UnityEngine.Object.FindObjectOfType<Shanhai.Demo.Player.DemoWorldEntry>().Ui.Open(3);
            yield return null;
            Click(_panel.Find("RewardOption_2").gameObject);
            Restore();
        }

        private bool Click(GameObject button)
        {
            _pointer.position = RectTransformUtility.WorldToScreenPoint(null, button.transform.position);
            _hits.Clear();
            EventSystem.current.RaycastAll(_pointer, _hits);
            if (_hits.Count == 0 || !(_hits[0].gameObject == button || _hits[0].gameObject.transform.IsChildOf(button.transform))) return false;
            return ExecuteEvents.ExecuteHierarchy(_hits[0].gameObject, _pointer, ExecuteEvents.pointerClickHandler) != null;
        }
        private int OptionIndex(string id)
        {
            for (int i = 0; i < _choice.OptionCount; i++) if (_choice.GetOption(i) == id) return i;
            return -1;
        }
        private bool MatchesEffects(int mark, int slot)
        {
            if (mark == 1 && slot == 0) _marks.OnNormalAttackHit();
            if (mark == 1 && slot == 2) _marks.OnUltimateStarted();
            if (mark == 2 && slot == 0) _marks.OnNormalAttackStarted();
            return Near(_marks.NormalAttackMultiplier, mark == 0 && slot == 0 ? 1.2f : 1f)
                && Near(_marks.SkillImbalanceMultiplier, mark == 0 && slot == 1 ? 1.15f : 1f)
                && Near(_marks.UltimateDamageMultiplier, mark == 0 && slot == 2 ? 1.25f : 1f)
                && Near(_marks.AttributeAttackMultiplier, mark == 0 && slot == 3 ? 1.1f : 1f)
                && Near(_marks.DamageTakenMultiplier, mark == 1 && slot == 0 ? 0.85f : mark == 1 && slot == 2 ? 0.7f : 1f)
                && Near(_marks.SkillRangeMultiplier, mark == 1 && slot == 1 ? 1.15f : 1f)
                && Near(_marks.AttributeHealthMultiplier, mark == 1 && slot == 3 ? 1.2f : 1f)
                && Near(_marks.TemporaryMoveSpeedMultiplier, mark == 2 && slot == 0 ? 1.1f : 1f)
                && Near(_marks.SkillCooldownMultiplier, mark == 2 && slot == 1 ? 0.8f : 1f)
                && Near(_marks.EnergyRegenMultiplier, mark == 2 && slot == 2 ? 1.25f : 1f)
                && Near(_marks.AttributeMoveSpeedMultiplier, mark == 2 && slot == 3 ? 1.15f : 1f);
        }
        private static bool Near(float actual, float expected) => Mathf.Abs(actual - expected) < 0.001f;
        private void OnDisable() => Restore();
        private void Restore()
        {
            if (!_captured) return;
            Time.captureDeltaTime = _captureDelta;
            _captured = false;
        }
    }
}
