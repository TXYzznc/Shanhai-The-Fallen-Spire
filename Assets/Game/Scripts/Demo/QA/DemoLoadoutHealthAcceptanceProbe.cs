using System.Collections;
using Shanhai.Demo.Build;
using Shanhai.Demo.Combat;
using UnityEngine;

namespace Shanhai.Demo.QA
{
    /// <summary>兽印/增益变更后生命不得超出新上限，移除效果不能通过再次装备恢复。</summary>
    public sealed class DemoLoadoutHealthAcceptanceProbe : MonoBehaviour
    {
        public bool RemoveClampsHealth { get; private set; }
        public bool ReplacementClampsHealth { get; private set; }
        public bool MoveToOtherSlotClampsHealth { get; private set; }
        public bool ReequipDoesNotHeal { get; private set; }
        public bool DamagedHealthPreserved { get; private set; }
        public bool SameSlotReequipIsAtomic { get; private set; }
        public bool BuffRemovalClampsHealth { get; private set; }
        public bool CombinedRemovalClampsHealth { get; private set; }
        public bool ReenableResynchronizes { get; private set; }
        public bool RetryRestoresBaseHealth { get; private set; }
        public bool Completed { get; private set; }
        public bool Finished { get; private set; }
        public string BaselineDetails { get; private set; }
        private IEnumerator Start()
        {
            yield return null;
            var marks = GetComponent<DemoMarkLoadout>();
            var buffs = GetComponent<DemoBuffLoadout>();
            var health = GetComponent<DemoPlayerHealthController>();
            marks.ResetForRetry();
            buffs.ResetForRetry();
            marks.TryEquip("beast_02_mountain", 3);
            health.ResetForRetry();
            marks.Remove(3);
            BaselineDetails = $"remove={health.CurrentHealth}/{health.MaxHealth}";
            RemoveClampsHealth = Near(health.CurrentHealth, 100f) && Near(health.MaxHealth, 100f);
            marks.TryEquip("beast_02_mountain", 3);
            ReequipDoesNotHeal = Near(health.CurrentHealth, 100f) && Near(health.MaxHealth, 120f);
            health.ResetForRetry();
            marks.TryEquip("beast_02_mountain", 3);
            SameSlotReequipIsAtomic = Near(health.CurrentHealth, 120f);
            marks.TryEquip("beast_01_crack", 3);
            ReplacementClampsHealth = Near(health.CurrentHealth, 100f) && Near(health.MaxHealth, 100f);
            marks.TryEquip("beast_02_mountain", 3);
            health.ResetForRetry();
            marks.TryEquip("beast_02_mountain", 1);
            MoveToOtherSlotClampsHealth = Near(health.CurrentHealth, 100f) && Near(health.MaxHealth, 100f)
                && marks.GetMarkId(3) == string.Empty && marks.GetMarkId(1) == "beast_02_mountain";
            marks.TryEquip("beast_02_mountain", 3);
            health.ResetForRetry();
            health.ReceiveDamage(new DemoDamageInfo(45f, gameObject));
            marks.Remove(3);
            DamagedHealthPreserved = Near(health.CurrentHealth, 75f) && Near(health.MaxHealth, 100f);
            buffs.TryApplyBuff("buff_health");
            health.ResetForRetry();
            buffs.ResetForRetry();
            BuffRemovalClampsHealth = Near(health.CurrentHealth, 100f) && Near(health.MaxHealth, 100f);
            marks.TryEquip("beast_02_mountain", 3);
            buffs.TryApplyBuff("buff_health");
            health.ResetForRetry();
            marks.Remove(3);
            bool firstClamp = Near(health.CurrentHealth, 115f) && Near(health.MaxHealth, 115f);
            buffs.ResetForRetry();
            CombinedRemovalClampsHealth = firstClamp && Near(health.CurrentHealth, 100f) && Near(health.MaxHealth, 100f);
            marks.TryEquip("beast_02_mountain", 3);
            health.ResetForRetry();
            health.enabled = false;
            marks.Remove(3);
            health.enabled = true;
            ReenableResynchronizes = Near(health.CurrentHealth, 100f) && Near(health.MaxHealth, 100f);
            GetComponent<Shanhai.Demo.Player.DemoRunFlowController>().RestartRun();
            RetryRestoresBaseHealth = Near(health.CurrentHealth, 100f) && Near(health.MaxHealth, 100f)
                && marks.EquippedCount == 0 && buffs.ActiveCount == 0 && health.RevivesRemaining == 1;
            Completed = RemoveClampsHealth && ReplacementClampsHealth && MoveToOtherSlotClampsHealth && ReequipDoesNotHeal
                && DamagedHealthPreserved && SameSlotReequipIsAtomic && BuffRemovalClampsHealth && CombinedRemovalClampsHealth
                && ReenableResynchronizes && RetryRestoresBaseHealth;
            Finished = true;
        }
        private static bool Near(float actual, float expected) => Mathf.Abs(actual - expected) < 0.001f;
    }
}
