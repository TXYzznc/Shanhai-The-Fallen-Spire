using Shanhai.Demo.Build;
using Shanhai.Demo.UI;
using UnityEngine;

namespace Shanhai.Demo.QA
{
    public sealed class DemoRewardVisualAcceptanceProbe : MonoBehaviour
    {
        [SerializeField] private DemoRewardChoiceView view;
        [SerializeField] private DemoRewardChoiceController choice;
        public bool Opened { get; private set; }
        public bool HasThreeOptions { get; private set; }

        private void Start()
        {
            if (view == null || choice == null) return;
            UnityEngine.Object.FindObjectOfType<Shanhai.Demo.Player.DemoWorldEntry>().Ui.Open(0);
            Opened = choice.IsOpen;
            HasThreeOptions = choice.OptionCount == 3;
        }
    }
}
