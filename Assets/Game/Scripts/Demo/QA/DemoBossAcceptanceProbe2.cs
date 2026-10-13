using System.Collections;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Enemies;
using Shanhai.Demo.Player;
using UnityEngine;

namespace Shanhai.Demo.QA
{
    public sealed class DemoBossAcceptanceProbe2 : MonoBehaviour
    {
        [SerializeField] private DemoEnemyController boss;
        [SerializeField] private DemoDamageReceiver receiver;
        [SerializeField] private DemoPlayerController player;
        [SerializeField] private Rigidbody2D playerBody;
        public bool InitialPhaseValid { get; private set; }
        public bool PhaseOneAfterDamageValid { get; private set; }
        public bool SecondPhaseValid { get; private set; }
        public bool AreaAttackObserved { get; private set; }
        public bool DeathStopsAttacks { get; private set; }
        public bool Completed { get; private set; }

        private IEnumerator Start()
        {
            yield return null;
            if (boss == null || receiver == null || player == null || playerBody == null) yield break;
            boss.gameObject.SetActive(true);
            GetComponent<DemoRunFlowController>().enabled = false;
            GetComponent<DemoPlayerCombatController>().enabled = false;
            receiver.ResetHealth();
            // 阶段边界以实际生命比例为准；隔离失衡加伤，避免400点输入变成600点伤害。
            receiver.ConfigureStagger(0f, 3f, 2f, 1.5f);
            player.enabled = false;
            playerBody.position = (Vector2)boss.transform.position + Vector2.down;
            yield return null;
            InitialPhaseValid = receiver.MaxHealth == 1000f && boss.Phase == 1;
            receiver.ReceiveDamage(new DemoDamageInfo(400f, gameObject));
            PhaseOneAfterDamageValid = Mathf.Abs(receiver.CurrentHealth - 600f) < 0.001f && boss.Phase == 1;
            receiver.ReceiveDamage(new DemoDamageInfo(1f, gameObject));
            SecondPhaseValid = boss.Phase == 2;
            yield return new WaitForSeconds(5f);
            AreaAttackObserved = boss.AreaAttackCount > 0;
            receiver.ReceiveDamage(new DemoDamageInfo(2000f, gameObject));
            int attacks = boss.AttackCount;
            yield return new WaitForSeconds(2f);
            DeathStopsAttacks = receiver.IsDefeated && boss.AttackCount == attacks;
            Completed = InitialPhaseValid && PhaseOneAfterDamageValid && SecondPhaseValid && AreaAttackObserved && DeathStopsAttacks;
        }
    }
}
