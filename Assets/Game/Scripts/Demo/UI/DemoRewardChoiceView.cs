using Shanhai.Demo.Build;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace Shanhai.Demo.UI
{
    public sealed class DemoRewardNoticeEventArgs : System.EventArgs
    {
        public readonly string Message;
        public DemoRewardNoticeEventArgs(string message) { Message = message; }
    }
    /// <summary>三选一奖励的临时 UGUI 视图；数据和确认逻辑由 DemoRewardChoiceController 持有。</summary>
    public sealed class DemoRewardChoiceView : MonoBehaviour
    {
        [SerializeField] private DemoRewardChoiceController rewardChoice;
        [SerializeField] private GameObject _panel;
        [SerializeField] private Button[] _buttons;
        [SerializeField] private TMP_Text[] _labels;
        [SerializeField] private TMP_Text[] _iconLabels;
        [SerializeField] private TMP_Text _tooltip;
        private int _pendingIndex = -1;
        [SerializeField] private Image[] _markIcons;

        public event System.Action CloseRequested;
        public event System.Action<DemoRewardNoticeEventArgs> NoticeProduced;
        public void Bind(DemoRewardChoiceController choice) { rewardChoice = choice; _pendingIndex = -1; ClearTooltip(); }
        public void Unbind() => Bind(null);

        public void PresentOptions()
        {
            _pendingIndex = -1;
            if (!rewardChoice.IsOpen)
            {
                Hide();
                return;
            }
            ClearTooltip();
            for (int i = 0; i < _buttons.Length; i++)
            {
                int optionIndex = i;
                bool available = i < rewardChoice.OptionCount;
                _buttons[i].gameObject.SetActive(available);
                _buttons[i].GetComponent<Image>().color = new Color(0.12f, 0.2f, 0.3f, 1f);
                if (!available) continue;
                _markIcons[i].gameObject.SetActive(!rewardChoice.IsBuffChoice);
                _iconLabels[i].gameObject.SetActive(rewardChoice.IsBuffChoice);
                _labels[i].SetText(rewardChoice.GetOption(i));
                Shanhai.Demo.Configuration.DemoBeastMarkConfig option = rewardChoice.GetOptionConfig(i);
                if (option != null)
                {
                    _labels[i].text = option.displayName + (rewardChoice.IsOwnedOption(i) ? $"\n已拥有，兑换 {rewardChoice.DuplicateCurrency} 货币" : "\n获得兽印");
                    _markIcons[i].sprite = option.icon;
                    _iconLabels[i].text = IconGlyph(option.iconKey);
                    _iconLabels[i].color = IconColor(option.iconKey);
                }
                var buff = rewardChoice.GetBuffOptionConfig(i);
                if (buff != null)
                {
                    _labels[i].text = $"{buff.displayName} Lv.{buff.level}";
                    _iconLabels[i].text = "+";
                    _iconLabels[i].color = new Color(0.4f, 1f, 0.6f);
                }
                _buttons[i].onClick.RemoveAllListeners();
                _buttons[i].onClick.AddListener(() =>
                {
                    HandleOptionClick(optionIndex);
                });
                EventTrigger trigger = _buttons[i].GetComponent<EventTrigger>();
                if (trigger == null) throw new System.InvalidOperationException("奖励卡预制体缺少EventTrigger。");
                trigger.triggers.Clear();
                int hoverIndex = i;
                EventTrigger.Entry enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                enter.callback.AddListener(_ => ShowTooltip(hoverIndex));
                trigger.triggers.Add(enter);
                EventTrigger.Entry exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                exit.callback.AddListener(_ => ClearTooltip());
                trigger.triggers.Add(exit);
            }
            _panel.transform.SetAsLastSibling();
            _panel.SetActive(true);
        }

        public void Hide()
        {
            _pendingIndex = -1;
            CloseRequested?.Invoke();
            ClearTooltip();
        }

        private void HandleOptionClick(int index)
        {
            if (rewardChoice == null || !rewardChoice.IsOpen) return;
            if (_pendingIndex == index)
            {
                bool duplicate = !rewardChoice.IsBuffChoice && rewardChoice.IsOwnedOption(index);
                bool buff = rewardChoice.IsBuffChoice;
                if (rewardChoice.SelectOption(index))
                {
                    NoticeProduced?.Invoke(new DemoRewardNoticeEventArgs(buff ? "已获得增益" : duplicate
                        ? $"重复兽印已兑换 {rewardChoice.DuplicateCurrency} 货币" : "已获得兽印，可按Tab装配"));
                    Hide();
                }
                return;
            }
            _pendingIndex = index;
            for (int i = 0; i < _buttons.Length; i++)
                _buttons[i].GetComponent<Image>().color = i == index
                    ? new Color(0.25f, 0.45f, 0.3f, 1f)
                    : new Color(0.12f, 0.2f, 0.3f, 1f);
            ShowTooltip(index);
            _tooltip.text += "  再次点击该项确认";
        }

        private static string IconGlyph(string key)
        {
            if (string.IsNullOrEmpty(key)) return "◆";
            if (key.Contains("01")) return "A";
            if (key.Contains("02")) return "S";
            if (key.Contains("03")) return "U";
            return "◆";
        }

        private static Color IconColor(string key)
        {
            if (key != null && key.Contains("01")) return new Color(1f, 0.45f, 0.2f);
            if (key != null && key.Contains("02")) return new Color(0.35f, 0.8f, 1f);
            if (key != null && key.Contains("03")) return new Color(0.8f, 0.45f, 1f);
            return Color.white;
        }

        private void ShowTooltip(int index)
        {
            if (_tooltip == null || rewardChoice == null) return;
            Shanhai.Demo.Configuration.DemoBeastMarkConfig option = rewardChoice.GetOptionConfig(index);
            var buff = rewardChoice.GetBuffOptionConfig(index);
            _tooltip.text = buff != null ? buff.description : option == null ? string.Empty : option.description
                + (rewardChoice.IsOwnedOption(index) ? $"\n已拥有：选择后兑换 {rewardChoice.DuplicateCurrency} 货币，不改变装备" : "\n获得后按Tab手动装配");
        }

        private void ClearTooltip()
        {
            if (_tooltip == null) return;
            if (_pendingIndex >= 0 && rewardChoice != null && rewardChoice.IsOpen)
            {
                ShowTooltip(_pendingIndex);
                _tooltip.text += "  再次点击该项确认";
            }
            else _tooltip.text = string.Empty;
        }
    }
}
