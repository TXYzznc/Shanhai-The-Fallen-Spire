using System.Collections;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Enemies;
using Shanhai.Demo.UI;
using UnityEngine;

namespace Shanhai.Demo.QA
{
    public sealed class DemoCompletionAcceptanceProbe : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private DemoEncounterController bossEncounter;
        [SerializeField] private DemoDamageReceiver bossReceiver;
        [SerializeField] private DemoCompletionView completionView;
        public bool EncounterStarted { get; private set; }
        public bool CompletionShown { get; private set; }
        public bool Completed { get; private set; }

        private IEnumerator Start()
        {
            yield return null;
            if (player == null || bossEncounter == null || bossReceiver == null || completionView == null) yield break;
            player.position = bossEncounter.transform.position;
            yield return new WaitForSeconds(0.5f);
            EncounterStarted = bossEncounter.HasStarted;
            bossReceiver.ReceiveDamage(new DemoDamageInfo(5000f, gameObject));
            yield return new WaitForSeconds(0.5f);
            CompletionShown = completionView.IsOpen;
            Completed = true;
        }
    }
}
