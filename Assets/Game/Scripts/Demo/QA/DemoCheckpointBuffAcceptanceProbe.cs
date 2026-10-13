using System.Collections;
using Shanhai.Demo.Build;
using Shanhai.Demo.Player;
using Shanhai.Demo.UI;
using UnityEngine;

namespace Shanhai.Demo.QA
{
    public sealed class DemoCheckpointBuffAcceptanceProbe : MonoBehaviour
    {
        [SerializeField] private DemoRewardChoiceController choice;
        [SerializeField] private DemoCheckpointController checkpoint;
        [SerializeField] private DemoRewardChoiceView view;

        private IEnumerator Start()
        {
            yield return null;
            var body = GetComponent<Rigidbody2D>();
            var buffs = GetComponent<DemoBuffLoadout>();
            body.position = new Vector2(0f, 24f);
            Physics2D.SyncTransforms();
            yield return new WaitForSeconds(0.2f);
            bool triggered = checkpoint.IsActivated && choice.IsBuffChoice && choice.OptionCount == 3;
            string id = choice.GetOption(0);
            bool described = choice.GetBuffOptionConfig(0) != null && !string.IsNullOrEmpty(choice.GetBuffOptionConfig(0).description);
            bool applied = choice.SelectOption(0) && buffs.HasBuff(id) && buffs.ActiveCount == 1;
            UnityEngine.Object.FindObjectOfType<Shanhai.Demo.Player.DemoWorldEntry>().Ui.OpenBuffs();
            bool excludesOwned = true;
            for (int i = 0; i < choice.OptionCount; i++) excludesOwned &= choice.GetOption(i) != id;
            view.Hide();
            GetComponent<DemoRunFlowController>().RestartRun();
            bool reset = buffs.ActiveCount == 0 && !checkpoint.IsActivated && !choice.IsOpen;
            Debug.Log($"[DemoCheckpointBuffAcceptanceProbe] Triggered={triggered}, Described={described}, Applied={applied}, ExcludesOwned={excludesOwned}, RetryCleared={reset}");
        }
    }
}
