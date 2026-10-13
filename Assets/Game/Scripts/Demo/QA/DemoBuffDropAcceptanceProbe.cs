using Shanhai.Demo.Build;
using Shanhai.Demo.Combat;
using UnityEngine;

namespace Shanhai.Demo.QA
{
    /// <summary>临时验收：验证掉落表 rewardId 能解析到增益并改变玩家运行时属性。</summary>
    public sealed class DemoBuffDropAcceptanceProbe : MonoBehaviour
    {
        private void Start()
        {
            var loadout = GetComponent<DemoBuffLoadout>();
            var health = GetComponent<DemoPlayerHealthController>();
            // 只验普通直接增益条目的解析；精英/Boss三选一由遭遇UI探针验收。
            bool dropResolved = loadout != null && loadout.TryApplyDrop("common_drop") && Mathf.Abs(loadout.AttackMultiplier - 1.1f) < 0.01f;
            bool healthApplied = loadout != null && loadout.TryApplyBuff("buff_health") && health != null && Mathf.Abs(health.MaxHealth - 115f) < 0.01f;
            bool attackApplied = loadout != null && loadout.TryApplyBuff("buff_normal") && Mathf.Abs(loadout.NormalAttackMultiplier - 1.15f) < 0.01f;
            var target = new GameObject("BuffImbalanceAcceptanceTarget");
            var receiver = target.AddComponent<DemoDamageReceiver>();
            receiver.ConfigureStagger(12f, 3f, 2f, 1f);
            receiver.ReceiveDamage(new DemoDamageInfo(10f, gameObject));
            bool baselineNotStaggered = !receiver.IsStaggered;
            receiver.ResetHealth();
            bool imbalanceApplied = loadout != null && loadout.TryApplyBuff("buff_imbalance");
            receiver.ReceiveDamage(new DemoDamageInfo(10f, gameObject, loadout != null ? loadout.ImbalanceMultiplier : 1f));
            imbalanceApplied &= baselineNotStaggered && receiver.IsStaggered;
            Destroy(target);
            Debug.Log($"[DemoBuffDropAcceptanceProbe] ImbalanceApplied={imbalanceApplied}");
            Debug.Log($"[DemoBuffDropAcceptanceProbe] DropResolved={dropResolved}, HealthApplied={healthApplied}, AttackApplied={attackApplied}, ActiveCount={(loadout != null ? loadout.ActiveCount : 0)}");
        }
    }
}
