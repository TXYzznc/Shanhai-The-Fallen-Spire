using System.Reflection;
using Shanhai.Demo.Build;
using Shanhai.Demo.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shanhai.Demo.QA
{
    public sealed class DemoRewardSlotAcceptanceProbe : MonoBehaviour
    {
        [SerializeField] private DemoRewardChoiceView view;
        [SerializeField] private DemoRewardChoiceController choice;
        [SerializeField] private DemoMarkLoadout marks;

        private void Start()
        {
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var options = (Button[])typeof(DemoRewardChoiceView).GetField("_buttons", flags).GetValue(view);
            var slots = (Button[])typeof(DemoRewardChoiceView).GetField("_slotButtons", flags).GetValue(view);
            marks.ResetForRetry();
            bool allSlots = true;
            for (int slot = 0; slot < 4; slot++)
            {
                UnityEngine.Object.FindObjectOfType<Shanhai.Demo.Player.DemoWorldEntry>().Ui.Open();
                string id = choice.GetOption(0);
                slots[slot].onClick.Invoke();
                options[0].onClick.Invoke();
                options[0].onClick.Invoke();
                allSlots &= marks.GetMarkId(slot) == id && marks.EquippedCount == 1;
            }
            UnityEngine.Object.FindObjectOfType<Shanhai.Demo.Player.DemoWorldEntry>().Ui.Open();
            options[1].onClick.Invoke();
            slots[2].onClick.Invoke();
            options[1].onClick.Invoke();
            bool changeResetsPreview = choice.IsOpen && !choice.IsConfirmed;
            options[1].onClick.Invoke();
            bool confirmedAfterChange = choice.IsConfirmed && marks.GetMarkId(2) == choice.SelectedMarkId;
            UnityEngine.Object.FindObjectOfType<Shanhai.Demo.Player.DemoWorldEntry>().Ui.OpenBuffs();
            bool hiddenForBuffs = true;
            foreach (var button in slots) hiddenForBuffs &= !button.gameObject.activeSelf;
            Debug.Log($"[DemoRewardSlotAcceptanceProbe] AllSlots={allSlots}, ChangeResetsPreview={changeResetsPreview}, ConfirmedAfterChange={confirmedAfterChange}, HiddenForBuffs={hiddenForBuffs}");
            // Leave the mark preview visible for Game View screenshot review.
            UnityEngine.Object.FindObjectOfType<Shanhai.Demo.Player.DemoWorldEntry>().Ui.Open(2);
            options[1].onClick.Invoke();
        }
    }
}
