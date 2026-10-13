using System.Collections;
using Shanhai.Demo.Build;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Input;
using Shanhai.Demo.Player;
using Shanhai.Demo.Enemies;
using UnityEngine;

namespace Shanhai.Demo.QA
{
    /// <summary>一次性 2D 灰盒验收探针；只通过正式运行时入口执行，不参与正式游戏流程。</summary>
    public sealed class DemoRuntimeAcceptanceProbe : MonoBehaviour
    {
        [SerializeField] private DemoPlayerCombatController combat;
        [SerializeField] private DemoPlayerController movement;
        [SerializeField] private DemoDamageReceiver enemy;
        [SerializeField] private DemoPlayerHealthController health;
        [SerializeField] private DemoMarkLoadout loadout;
        [SerializeField] private DemoRewardChoiceController rewardChoice;
        [SerializeField] private DemoRunFlowController runFlow;

        public bool Completed { get; private set; }
        public bool FirstAttackHit { get; private set; }
        public bool SecondAttackHit { get; private set; }
        public bool SkillStarted { get; private set; }
        public bool DodgeStarted { get; private set; }
        public bool RewardConfirmed { get; private set; }
        public bool ReviveObserved { get; private set; }
        public bool RetryObserved { get; private set; }

        private IEnumerator Start()
        {
            yield return null;
            if (combat == null || movement == null || enemy == null || health == null || loadout == null || rewardChoice == null || runFlow == null)
            {
                yield break;
            }

            enemy.gameObject.SetActive(true);
            DemoEnemyController enemyController = enemy.GetComponent<DemoEnemyController>();
            if (enemyController != null) enemyController.enabled = false;
            health.ResetForRetry();
            combat.transform.rotation = Quaternion.identity;
            enemy.transform.position = combat.transform.position + Vector3.up;
            float initialEnemyHealth = enemy.CurrentHealth;

            bool firstStarted = combat.TryStartAttack();
            yield return null;
            enemy.transform.position = combat.transform.position + combat.transform.up;
            yield return new WaitForSeconds(0.2f);
            FirstAttackHit = firstStarted && enemy.CurrentHealth < initialEnemyHealth;

            yield return new WaitForSeconds(0.25f);
            bool secondStarted = combat.TryStartAttack();
            yield return null;
            enemy.transform.position = combat.transform.position + combat.transform.up;
            yield return new WaitForSeconds(0.2f);
            SecondAttackHit = secondStarted && enemy.CurrentHealth < initialEnemyHealth - 10f;

            yield return new WaitForSeconds(0.25f);
            SkillStarted = combat.TryUseSkill();
            DodgeStarted = movement.TryDodge(new DemoInputState(Vector2.right, Vector2.zero, false, true, false, false));

            rewardChoice.Open(0);
            RewardConfirmed = rewardChoice.IsOpen && rewardChoice.SelectOption(0) && loadout.EquippedCount == 1;

            yield return new WaitForSeconds(0.3f);
            health.ReceiveDamage(new DemoDamageInfo(100f, gameObject));
            ReviveObserved = health.CurrentHealth > 0f && health.RevivesRemaining == 0;
            health.ReceiveDamage(new DemoDamageInfo(100f, gameObject));
            yield return new WaitForSeconds(0.75f);
            RetryObserved = runFlow.RetryCount > 0 && health.IsRetryRequired == false;
            Completed = FirstAttackHit && SecondAttackHit && SkillStarted && DodgeStarted
                && RewardConfirmed && ReviveObserved && RetryObserved;
        }
    }
}
