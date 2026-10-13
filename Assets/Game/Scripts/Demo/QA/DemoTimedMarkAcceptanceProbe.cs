using System.Collections;
using System.Reflection;
using Shanhai.Demo.Build;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Enemies;
using UnityEngine;

namespace Shanhai.Demo.QA
{
    public sealed class DemoTimedMarkAcceptanceProbe : MonoBehaviour
    {
        private IEnumerator Start()
        {
            yield return null;
            foreach (var enemy in FindObjectsOfType<DemoEnemyController>(true)) enemy.enabled = false;
            GetComponent<Shanhai.Demo.Player.DemoPlayerController>().enabled = false;
            var combat = GetComponent<DemoPlayerCombatController>();
            var marks = GetComponent<DemoMarkLoadout>();
            var health = GetComponent<DemoPlayerHealthController>();
            transform.rotation = Quaternion.identity;
            marks.ResetForRetry();
            marks.TryEquip("beast_02_mountain", 0);
            combat.ResetForRetry();
            health.ResetForRetry();
            combat.TryStartAttack();
            yield return new WaitForSeconds(0.2f);
            bool missNoProtection = Mathf.Approximately(marks.DamageTakenMultiplier, 1f);
            var target = new GameObject("TimedMarkAcceptanceTarget");
            target.AddComponent<CircleCollider2D>().radius = 0.05f;
            target.AddComponent<DemoDamageReceiver>();
            target.transform.position = transform.position + Vector3.up;
            Physics2D.SyncTransforms();
            combat.ResetForRetry();
            combat.TryStartAttack();
            yield return new WaitForSeconds(0.2f);
            float before = health.CurrentHealth;
            health.ReceiveDamage(new DemoDamageInfo(10f, gameObject));
            bool normalReduction = Mathf.Abs(before - health.CurrentHealth - 8.5f) < 0.01f;
            marks.TryEquip("beast_03_shadow", 0);
            bool replacementCleared = Mathf.Approximately(marks.DamageTakenMultiplier, 1f);
            health.ResetForRetry();
            combat.ResetForRetry();
            combat.TryStartAttack();
            bool speedApplied = Mathf.Abs(marks.TemporaryMoveSpeedMultiplier - 1.1f) < 0.01f;
            yield return new WaitForSeconds(1.1f);
            bool speedExpired = Mathf.Approximately(marks.TemporaryMoveSpeedMultiplier, 1f);
            marks.TryEquip("beast_02_mountain", 2);
            combat.ResetForRetry();
            health.ResetForRetry();
            typeof(DemoPlayerCombatController).GetField("_energy", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(combat, 100f);
            bool ultimateStarted = combat.TryUseUltimate();
            before = health.CurrentHealth;
            health.ReceiveDamage(new DemoDamageInfo(10f, gameObject));
            bool ultimateReduction = ultimateStarted && Mathf.Abs(before - health.CurrentHealth - 7f) < 0.01f;
            yield return new WaitForSeconds(0.4f);
            bool ultimateExpired = Mathf.Approximately(marks.DamageTakenMultiplier, 1f);
            marks.ResetForRetry();
            bool retryCleared = marks.EquippedCount == 0 && Mathf.Approximately(marks.TemporaryMoveSpeedMultiplier, 1f) && Mathf.Approximately(marks.DamageTakenMultiplier, 1f);
            Debug.Log($"[DemoTimedMarkAcceptanceProbe] MissNoProtection={missNoProtection}, NormalReduction={normalReduction}, ReplacementCleared={replacementCleared}, SpeedApplied={speedApplied}, SpeedExpired={speedExpired}, UltimateReduction={ultimateReduction}, UltimateExpired={ultimateExpired}, RetryCleared={retryCleared}");
            Destroy(target);
        }
    }
}
