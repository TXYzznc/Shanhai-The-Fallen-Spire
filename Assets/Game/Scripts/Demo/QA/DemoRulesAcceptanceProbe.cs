using System.Collections;
using System.Reflection;
using Shanhai.Demo.Build;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Enemies;
using Shanhai.Demo.Player;
using Shanhai.Demo.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Shanhai.Demo.QA
{
    public sealed class DemoRulesAcceptanceProbe : MonoBehaviour
    {
        [SerializeField] private DemoMarkLoadout loadout;
        [SerializeField] private Rigidbody2D playerBody;
        [SerializeField] private DemoEncounterController eliteEncounter;
        [SerializeField] private DemoDamageReceiver eliteEnemy;
        [SerializeField] private GameObject eliteGate;
        [SerializeField] private DemoEncounterController bossEncounter;
        [SerializeField] private DemoDamageReceiver bossEnemy;
        [SerializeField] private GameObject bossGate;
        [SerializeField] private DemoCompletionView completionView;

        public bool AttackMarkApplied { get; private set; }
        public bool ReplacementClearsOldEffect { get; private set; }
        public bool AttributeMarkApplied { get; private set; }
        public bool EliteGateOpened { get; private set; }
        public bool BossGateOpened { get; private set; }
        public bool CompletionCloseWorked { get; private set; }
        public bool Completed { get; private set; }

        private IEnumerator Start()
        {
            yield return null;
            if (loadout == null || playerBody == null || eliteEncounter == null || eliteEnemy == null || eliteGate == null || bossEncounter == null || bossEnemy == null || bossGate == null || completionView == null)
                yield break;

            loadout.ResetForRetry();
            AttackMarkApplied = loadout.TryEquip("beast_01_crack", 0) && loadout.NormalAttackMultiplier > 1f;
            ReplacementClearsOldEffect = loadout.TryEquip("beast_03_shadow", 0) && Mathf.Approximately(loadout.NormalAttackMultiplier, 1f);
            AttributeMarkApplied = loadout.TryEquip("beast_02_mountain", 3) && loadout.AttributeHealthMultiplier > 1f;

            MovePlayer(36f);
            yield return new WaitForSeconds(0.2f);
            bool eliteStarted = eliteEncounter.HasStarted;
            eliteEnemy.ReceiveDamage(new DemoDamageInfo(10000f, gameObject));
            yield return new WaitForSeconds(0.2f);
            EliteGateOpened = eliteStarted && eliteEncounter.IsCompleted && !eliteGate.activeSelf;

            MovePlayer(48f);
            yield return new WaitForSeconds(0.2f);
            bool bossStarted = bossEncounter.HasStarted;
            bossEnemy.ReceiveDamage(new DemoDamageInfo(10000f, gameObject));
            yield return new WaitForSeconds(0.4f);
            BossGateOpened = bossStarted && bossEncounter.IsCompleted && !bossGate.activeSelf;
            if (completionView.IsOpen)
            {
                GameObject panel = typeof(DemoCompletionView).GetField("_panel", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(completionView) as GameObject;
                Transform close = panel != null ? panel.transform.Find("Close") : null;
                Button button = close != null ? close.GetComponent<Button>() : null;
                button?.onClick.Invoke();
                CompletionCloseWorked = !completionView.IsOpen;
            }
            Completed = AttackMarkApplied && ReplacementClearsOldEffect && AttributeMarkApplied && EliteGateOpened && BossGateOpened && CompletionCloseWorked;
        }

        private void MovePlayer(float y)
        {
            playerBody.position = new Vector2(0f, y);
            Physics2D.SyncTransforms();
        }
    }
}
