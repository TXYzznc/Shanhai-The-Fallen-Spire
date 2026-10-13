using System.Collections;
using System.Reflection;
using Shanhai.Demo.Build;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Input;
using Shanhai.Demo.Player;
using UnityEngine;

namespace Shanhai.Demo.QA
{
    public sealed class DemoRetryStateAcceptanceProbe : MonoBehaviour
    {
        [SerializeField] private DemoMarkLoadout loadout;
        [SerializeField] private DemoPlayerHealthController health;
        [SerializeField] private DemoPlayerCombatController combat;
        [SerializeField] private DemoPlayerController movement;
        [SerializeField] private DemoRunFlowController runFlow;
        public bool RevivePreservesLoadout { get; private set; }
        public bool RetryClearsLoadout { get; private set; }
        public bool RetryClearsEnergy { get; private set; }
        public bool RetryRestoresHealth { get; private set; }
        public bool RetryClearsCombatLock { get; private set; }
        public bool RetryClearsDodge { get; private set; }
        public bool Completed { get; private set; }

        private IEnumerator Start()
        {
            yield return null;
            if (loadout == null || health == null || combat == null || movement == null || runFlow == null) yield break;
            if (!loadout.TryEquip("beast_02_mountain", 3)) yield break;
            health.ReceiveDamage(new DemoDamageInfo(health.MaxHealth * 10f, gameObject));
            RevivePreservesLoadout = loadout.EquippedCount == 1 && health.RevivesRemaining == 0 && health.CurrentHealth > 0f;
            yield return new WaitForSeconds(0.2f);
            typeof(DemoPlayerCombatController).GetField("_energy", BindingFlags.NonPublic | BindingFlags.Instance)?.SetValue(combat, 100f);
            combat.TryStartAttack();
            health.ReceiveDamage(new DemoDamageInfo(health.MaxHealth * 10f, gameObject));
            int previousRetries = runFlow.RetryCount;
            while (runFlow.RetryCount == previousRetries) yield return null;
            RetryClearsLoadout = loadout.EquippedCount == 0;
            RetryClearsEnergy = combat.CurrentEnergy < 1f;
            RetryRestoresHealth = Mathf.Approximately(health.CurrentHealth, health.MaxHealth) && health.RevivesRemaining == 1;
            RetryClearsCombatLock = !combat.IsAttackLocked;
            RetryClearsDodge = !movement.IsInvulnerable && movement.TryDodge(new DemoInputState(Vector2.right, Vector2.zero, false, true, false, false));
            Completed = RevivePreservesLoadout && RetryClearsLoadout && RetryClearsEnergy && RetryRestoresHealth
                && RetryClearsCombatLock && RetryClearsDodge;
        }
    }
}
