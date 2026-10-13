using System.Collections;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Enemies;
using Shanhai.Demo.Player;
using UnityEngine;

namespace Shanhai.Demo.QA
{
    /// <summary>隔离验证 Boss 生命阈值和第二阶段攻击；不挂载到交付场景。</summary>
    public sealed class DemoBossAcceptanceProbe : MonoBehaviour
    {
        [SerializeField] private DemoEnemyController boss;
        [SerializeField] private DemoDamageReceiver receiver;
        [SerializeField] private DemoPlayerController player;
        public bool InitialPhaseValid { get; private set; }
        public bool SecondPhaseValid { get; private set; }
        public bool AreaAttackObserved { get; private set; }
        public bool DeathStopsAttacks { get; private set; }
        public bool Completed { get; private set; }

        private IEnumerator Start()
        {
            yield return null;
            if (boss == null || receiver == null || player == null) yield break;
            boss.gameObject.SetActive(true);
            player.enabled = false;
            player.GetComponent<Rigidbody2D>().position = (Vector2)boss.transform.position + Vector2.down;
            InitialPhaseValid = receiver.MaxHealth == 1000f && boss.Phase == 1;
            receiver.ReceiveDamage(new DemoDamageInfo(400f, gameObject));
            InitialPhaseValid &= boss.Phase == 1;
            receiver.ReceiveDamage(new DemoDamageInfo(1f, gameObject));
            SecondPhaseValid = boss.Phase == 2;
            yield return new WaitForSeconds(3f);
            AreaAttackObserved = boss.AreaAttackCount > 0;
            receiver.ReceiveDamage(new DemoDamageInfo(2000f, gameObject));
            int attacks = boss.AttackCount;
            yield return new WaitForSeconds(2f);
            DeathStopsAttacks = receiver.IsDefeated && boss.AttackCount == attacks;
            Completed = true;
        }
    }
}
