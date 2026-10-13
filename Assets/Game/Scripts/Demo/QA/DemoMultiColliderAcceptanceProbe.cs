using System.Collections;
using System.Reflection;
using Shanhai.Demo.Build;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Enemies;
using Shanhai.Demo.Player;
using UnityEngine;

namespace Shanhai.Demo.QA
{
    public sealed class DemoMultiColliderAcceptanceProbe : MonoBehaviour
    {
        private IEnumerator Start()
        {
            yield return null;
            foreach (var enemy in FindObjectsOfType<DemoEnemyController>(true)) enemy.enabled = false;
            GetComponent<DemoPlayerController>().enabled = false;
            GetComponent<DemoMarkLoadout>().ResetForRetry();
            GetComponent<DemoBuffLoadout>().ResetForRetry();
            GetComponent<DemoPlayerHealthController>().ResetForRetry();
            var combat = GetComponent<DemoPlayerCombatController>();
            transform.rotation = Quaternion.identity;
            var target = new GameObject("MultiColliderAcceptanceTarget");
            target.transform.position = transform.position + Vector3.up;
            var receiver = target.AddComponent<DemoDamageReceiver>();
            target.AddComponent<CircleCollider2D>().radius = 0.05f;
            var child = new GameObject("ExtraHurtbox");
            child.transform.SetParent(target.transform, false);
            child.AddComponent<CircleCollider2D>().radius = 0.05f;
            var other = new GameObject("DistinctAcceptanceTarget");
            other.transform.position = transform.position + new Vector3(0.3f, 1f, 0f);
            var otherReceiver = other.AddComponent<DemoDamageReceiver>();
            other.AddComponent<CircleCollider2D>().radius = 0.05f;
            Physics2D.SyncTransforms();
            combat.ResetForRetry();
            combat.TryStartAttack();
            yield return new WaitForSeconds(0.2f);
            float normalDamage = receiver.MaxHealth - receiver.CurrentHealth;
            bool normalOnce = Mathf.Abs(normalDamage - 10f) < 0.01f;
            bool distinctHit = Mathf.Abs(otherReceiver.MaxHealth - otherReceiver.CurrentHealth - 10f) < 0.01f;
            receiver.ResetHealth();
            combat.ResetForRetry();
            combat.TryUseSkill();
            float skillDamage = receiver.MaxHealth - receiver.CurrentHealth;
            bool skillOnce = Mathf.Abs(skillDamage - 20f) < 0.01f;
            receiver.ResetHealth();
            combat.ResetForRetry();
            typeof(DemoPlayerCombatController).GetField("_energy", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(combat, 100f);
            combat.TryUseUltimate();
            float ultimateDamage = receiver.MaxHealth - receiver.CurrentHealth;
            bool ultimateOnce = Mathf.Abs(ultimateDamage - 50f) < 0.01f;
            Debug.Log($"[DemoMultiColliderAcceptanceProbe] NormalOnce={normalOnce}, SkillOnce={skillOnce}, UltimateOnce={ultimateOnce}, DistinctHit={distinctHit}, NormalDamage={normalDamage}, SkillDamage={skillDamage}, UltimateDamage={ultimateDamage}");
            Destroy(target);
            Destroy(other);
        }
    }
}
