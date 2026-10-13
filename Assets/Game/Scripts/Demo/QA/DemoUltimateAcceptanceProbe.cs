using System.Collections;
using Shanhai.Demo.Combat;
using UnityEngine;

namespace Shanhai.Demo.QA
{
    public sealed class DemoUltimateAcceptanceProbe : MonoBehaviour
    {
        [SerializeField] private DemoPlayerCombatController combat;
        [SerializeField] private DemoDamageReceiver enemy;
        public bool EnergyReachedMax { get; private set; }
        public bool UltimateStarted { get; private set; }
        public bool DamageObserved { get; private set; }
        public bool Completed { get; private set; }

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(21f);
            if (combat == null || enemy == null) yield break;
            enemy.gameObject.SetActive(true);
            enemy.transform.position = combat.transform.position + combat.transform.up;
            EnergyReachedMax = combat.CurrentEnergy >= 99.9f;
            float before = enemy.CurrentHealth;
            UltimateStarted = combat.TryUseUltimate();
            yield return new WaitForSeconds(0.2f);
            DamageObserved = enemy.CurrentHealth < before;
            Completed = EnergyReachedMax && UltimateStarted && DamageObserved;
        }
    }
}
