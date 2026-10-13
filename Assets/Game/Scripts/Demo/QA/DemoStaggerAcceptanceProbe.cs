using System.Collections;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Enemies;
using UnityEngine;

namespace Shanhai.Demo.QA
{
    public sealed class DemoStaggerAcceptanceProbe : MonoBehaviour
    {
        [SerializeField] private DemoDamageReceiver enemy;
        [SerializeField] private DemoEnemyController enemyController;
        public bool HitStunObserved { get; private set; }
        public bool StaggerObserved { get; private set; }
        public bool StaggerExtraDamageObserved { get; private set; }
        public bool Completed { get; private set; }

        private IEnumerator Start()
        {
            if (enemy == null) yield break;
            if (enemyController != null) enemyController.enabled = false;
            enemy.ResetHealth();
            enemy.ReceiveDamage(new DemoDamageInfo(10f, gameObject));
            HitStunObserved = enemy.IsHitStunned;
            yield return new WaitForSeconds(0.2f);
            enemy.ReceiveDamage(new DemoDamageInfo(10f, gameObject));
            enemy.ReceiveDamage(new DemoDamageInfo(10f, gameObject));
            StaggerObserved = enemy.IsStaggered;
            StaggerExtraDamageObserved = enemy.CurrentHealth <= 35.01f;
            Completed = true;
        }
    }
}
