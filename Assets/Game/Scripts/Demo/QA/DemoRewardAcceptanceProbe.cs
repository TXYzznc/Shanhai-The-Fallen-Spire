using Shanhai.Demo.Build;
using Shanhai.Demo.Configuration;
using Shanhai.Demo.UI;
using UnityEngine;

namespace Shanhai.Demo.QA
{
    public sealed class DemoRewardAcceptanceProbe : MonoBehaviour
    {
        [SerializeField] private DemoRewardChoiceController choice;
        [SerializeField] private DemoRewardChoiceView view;
        [SerializeField] private DemoMarkLoadout loadout;
        public bool ThreeOptions { get; private set; }
        public bool HasDescriptionAndIcon { get; private set; }
        public bool Confirmed { get; private set; }
        public bool Completed { get; private set; }

        private void Start()
        {
            if (choice == null || view == null || loadout == null) return;
            UnityEngine.Object.FindObjectOfType<Shanhai.Demo.Player.DemoWorldEntry>().Ui.Open(0);
            ThreeOptions = choice.IsOpen && choice.OptionCount == 3;
            DemoBeastMarkConfig option = choice.GetOptionConfig(0);
            HasDescriptionAndIcon = option != null && !string.IsNullOrEmpty(option.description) && !string.IsNullOrEmpty(option.iconKey) && option.level > 0;
            Confirmed = choice.SelectOption(0) && choice.IsConfirmed && loadout.EquippedCount == 1;
            Completed = true;
        }
    }
}
