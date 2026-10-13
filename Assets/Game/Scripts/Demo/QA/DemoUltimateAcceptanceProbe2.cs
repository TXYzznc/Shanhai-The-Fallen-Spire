using System.Collections;
using System.Reflection;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Enemies;
using UnityEngine;

namespace Shanhai.Demo.QA
{
    public sealed class DemoUltimateAcceptanceProbe2 : MonoBehaviour
    {
        [SerializeField] private DemoPlayerCombatController combat;
        [SerializeField] private DemoDamageReceiver enemy;
        [SerializeField] private DemoEnemyController enemyController;
        public bool UltimateStarted { get; private set; }
        public bool DamageObserved { get; private set; }
        public bool Completed { get; private set; }

        private IEnumerator Start()
        {
            yield return null;
            if (combat == null || enemy == null) yield break;
            if (enemyController != null) enemyController.enabled = false;
            combat.transform.rotation = Quaternion.identity;
            enemy.gameObject.SetActive(true);
            enemy.ResetHealth();
            enemy.transform.position = combat.transform.position + combat.transform.up;
            Physics2D.SyncTransforms();
            FieldInfo energy = typeof(DemoPlayerCombatController).GetField("_energy", BindingFlags.Instance | BindingFlags.NonPublic);
            energy?.SetValue(combat, 100f);
            float before = enemy.CurrentHealth;
            UltimateStarted = combat.TryUseUltimate();
            yield return null;
            DamageObserved = enemy.CurrentHealth < before;
            Completed = UltimateStarted && DamageObserved;
        }
    }
}
