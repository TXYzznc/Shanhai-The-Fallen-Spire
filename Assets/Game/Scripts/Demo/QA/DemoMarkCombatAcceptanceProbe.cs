using System.Collections;
using Shanhai.Demo.Build;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Enemies;
using UnityEngine;

namespace Shanhai.Demo.QA
{
    public sealed class DemoMarkCombatAcceptanceProbe : MonoBehaviour
    {
        private IEnumerator Start()
        {
            yield return null;
            foreach (var enemy in FindObjectsOfType<DemoEnemyController>(true)) enemy.enabled = false;
            var movement = GetComponent<Shanhai.Demo.Player.DemoPlayerController>();
            movement.enabled = false;
            transform.rotation = Quaternion.identity;
            var marks = GetComponent<DemoMarkLoadout>();
            var combat = GetComponent<DemoPlayerCombatController>();
            var buffs = GetComponent<DemoBuffLoadout>();
            buffs.ResetForRetry();
            marks.ResetForRetry();
            var target = new GameObject("MarkCombatAcceptanceTarget");
            target.AddComponent<CircleCollider2D>().radius = 0.05f;
            var receiver = target.AddComponent<DemoDamageReceiver>();
            target.transform.position = transform.position + Vector3.up * 1.75f;
            Physics2D.SyncTransforms();
            combat.ResetForRetry();
            combat.TryUseSkill();
            bool outsideBaseRange = Mathf.Approximately(receiver.CurrentHealth, receiver.MaxHealth);
            marks.TryEquip("beast_02_mountain", 1);
            combat.ResetForRetry();
            combat.TryUseSkill();
            bool rangeApplied = outsideBaseRange && receiver.CurrentHealth < receiver.MaxHealth;
            marks.ResetForRetry();
            marks.TryEquip("beast_01_crack", 3);
            target.transform.position = transform.position + Vector3.up;
            Physics2D.SyncTransforms();
            receiver.ResetHealth();
            combat.ResetForRetry();
            combat.TryUseSkill();
            bool attributeAttack = Mathf.Abs(receiver.MaxHealth - receiver.CurrentHealth - 22f) < 0.01f;
            marks.ResetForRetry();
            marks.TryEquip("beast_01_crack", 1);
            receiver.ResetHealth();
            receiver.ConfigureStagger(22.9f, 3f, 2f, 1f);
            combat.ResetForRetry();
            combat.TryUseSkill();
            bool imbalanceOnly = receiver.IsStaggered && Mathf.Abs(receiver.MaxHealth - receiver.CurrentHealth - 20f) < 0.01f;
            marks.ResetForRetry();
            marks.TryEquip("beast_01_crack", 0);
            receiver.ConfigureStagger(0f, 0f, 0f, 1f);
            receiver.ResetHealth();
            combat.ResetForRetry();
            combat.TryStartAttack();
            yield return new WaitForSeconds(0.2f);
            float firstDamage = receiver.MaxHealth - receiver.CurrentHealth;
            yield return new WaitForSeconds(0.18f);
            combat.TryStartAttack();
            yield return new WaitForSeconds(0.2f);
            float secondDamage = receiver.MaxHealth - receiver.CurrentHealth - firstDamage;
            bool secondOnly = Mathf.Abs(firstDamage - 10f) < 0.01f && Mathf.Abs(secondDamage - 15.6f) < 0.01f;
            Debug.Log($"[DemoMarkCombatAcceptanceProbe] SkillRange={rangeApplied}, AttributeAttack={attributeAttack}, ImbalanceOnly={imbalanceOnly}, SecondOnly={secondOnly}, FirstDamage={firstDamage}, SecondDamage={secondDamage}");
            Destroy(target);
        }
    }
}
