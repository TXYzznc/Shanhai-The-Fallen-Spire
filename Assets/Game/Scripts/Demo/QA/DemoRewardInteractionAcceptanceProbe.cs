using System.Reflection;
using Shanhai.Demo.Build;
using Shanhai.Demo.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Shanhai.Demo.QA
{
    public sealed class DemoRewardInteractionAcceptanceProbe : MonoBehaviour
    {
        [SerializeField] private DemoRewardChoiceView view;
        [SerializeField] private DemoRewardChoiceController choice;
        [SerializeField] private DemoMarkLoadout loadout;
        [SerializeField] private DemoBuffLoadout buffs;
        public bool HoverDescriptionShown { get; private set; }
        public bool ClickConfirmed { get; private set; }
        public bool Completed { get; private set; }

        private void Start()
        {
            if (view == null || choice == null || loadout == null) return;
            UnityEngine.Object.FindObjectOfType<Shanhai.Demo.Player.DemoWorldEntry>().Ui.Open(0);
            Button[] buttons = typeof(DemoRewardChoiceView).GetField("_buttons", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(view) as Button[];
            TMP_Text tooltip = typeof(DemoRewardChoiceView).GetField("_tooltip", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(view) as TMP_Text;
            if (buttons == null || buttons.Length == 0 || tooltip == null) return;
            ExecuteEvents.Execute(buttons[0].gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);
            HoverDescriptionShown = !string.IsNullOrEmpty(tooltip.text) && tooltip.text.Contains("[");
            buttons[0].onClick.Invoke();
            bool previewOnly = choice.IsOpen && !choice.IsConfirmed && loadout.EquippedCount == 0;
            buttons[1].onClick.Invoke();
            bool switchedPreview = choice.IsOpen && !choice.IsConfirmed;
            buttons[1].onClick.Invoke();
            ClickConfirmed = choice.IsConfirmed && !choice.IsOpen && loadout.EquippedCount == 1;
            UnityEngine.Object.FindObjectOfType<Shanhai.Demo.Player.DemoWorldEntry>().Ui.OpenBuffs();
            ExecuteEvents.Execute(buttons[0].gameObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);
            bool buffDescription = !string.IsNullOrEmpty(tooltip.text);
            buttons[0].onClick.Invoke();
            bool buffPreview = choice.IsOpen && buffs != null && buffs.ActiveCount == 0;
            buttons[0].onClick.Invoke();
            bool buffConfirmed = choice.IsConfirmed && !choice.IsOpen && buffs != null && buffs.ActiveCount == 1;
            Completed = HoverDescriptionShown && previewOnly && switchedPreview && ClickConfirmed && buffDescription && buffPreview && buffConfirmed;
            Debug.Log($"[DemoRewardInteractionAcceptanceProbe] PreviewOnly={previewOnly}, SwitchedPreview={switchedPreview}, MarkConfirmed={ClickConfirmed}, BuffPreview={buffPreview}, BuffConfirmed={buffConfirmed}, Completed={Completed}");
        }
    }
}
