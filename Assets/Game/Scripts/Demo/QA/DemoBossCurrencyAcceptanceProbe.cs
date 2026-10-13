using System.Collections;
using Shanhai.Demo.Build;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Configuration;
using Shanhai.Demo.Enemies;
using Shanhai.Demo.Player;
using Shanhai.Demo.UI;
using TMPro;
using UnityEngine;

namespace Shanhai.Demo.QA
{
    public sealed class DemoBossCurrencyAcceptanceProbe : MonoBehaviour
    {
        [SerializeField] private DemoGameplayConfig gameplayConfig;
        [SerializeField] private DemoEncounterController eliteEncounter;
        [SerializeField] private DemoDamageReceiver eliteEnemy;
        [SerializeField] private DemoEncounterController bossEncounter;
        [SerializeField] private DemoDamageReceiver bossEnemy;
        [SerializeField] private DemoRewardChoiceController choice;
        [SerializeField] private DemoRewardChoiceView rewardView;
        [SerializeField] private TMP_Text statusText;

        private IEnumerator Start()
        {
            yield return null;
            foreach (var enemy in FindObjectsOfType<DemoEnemyController>(true)) enemy.enabled = false;
            var flow = GetComponent<DemoRunFlowController>();
            var health = GetComponent<DemoPlayerHealthController>();
            var body = GetComponent<Rigidbody2D>();
            GetComponent<DemoPlayerController>().enabled = false;
            flow.RestartRun();
            body.position = new Vector2(0f, 36f);
            yield return new WaitForSeconds(0.2f);
            eliteEnemy.ReceiveDamage(new DemoDamageInfo(10000f, gameObject));
            yield return new WaitForSeconds(0.2f);
            bool eliteNoCurrency = eliteEncounter.IsCompleted && flow.CurrentCurrency == 0;
            rewardView.Hide();
            choice.ResetForRetry();
            body.position = new Vector2(0f, 48f);
            yield return new WaitForSeconds(0.2f);
            bossEnemy.ReceiveDamage(new DemoDamageInfo(10000f, gameObject));
            yield return new WaitForSeconds(0.3f);
            int expected = gameplayConfig.BossCurrencyReward;
            bool granted = bossEncounter.IsCompleted && flow.CurrentCurrency == expected;
            bool markReward = choice.IsOpen && choice.OptionCount == 3;
            bossEnemy.ReceiveDamage(new DemoDamageInfo(10000f, gameObject));
            yield return new WaitForSeconds(0.3f);
            bool onceOnly = flow.CurrentCurrency == expected;
            bool hudVisible = statusText.text.Contains("货币 " + expected);
            health.ReceiveDamage(new DemoDamageInfo(10000f, gameObject));
            yield return new WaitForSeconds(0.2f);
            bool reviveRetains = health.CurrentHealth > 0f && flow.CurrentCurrency == expected;
            flow.RestartRun();
            bool retryClears = flow.CurrentCurrency == 0 && !bossEncounter.IsCompleted;
            body.position = new Vector2(0f, 48f);
            yield return new WaitForSeconds(0.2f);
            bossEnemy.ReceiveDamage(new DemoDamageInfo(10000f, gameObject));
            yield return new WaitForSeconds(0.3f);
            bool nextRunGrants = flow.CurrentCurrency == expected;
            Debug.Log($"[DemoBossCurrencyAcceptanceProbe] EliteNoCurrency={eliteNoCurrency}, Granted={granted}, MarkReward={markReward}, OnceOnly={onceOnly}, HudVisible={hudVisible}, ReviveRetains={reviveRetains}, RetryClears={retryClears}, NextRunGrants={nextRunGrants}, Amount={expected}");
        }
    }
}
