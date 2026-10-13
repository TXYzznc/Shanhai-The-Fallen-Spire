using System.Collections;
using System.Reflection;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Enemies;
using Shanhai.Demo.Input;
using Shanhai.Demo.Player;
using UnityEngine;

namespace Shanhai.Demo.QA
{
    public sealed class DemoFailureInputAcceptanceProbe : MonoBehaviour
    {
        private sealed class HeldMove : IDemoInputSource
        {
            public DemoInputState ReadState() => new DemoInputState(Vector2.right, Vector2.zero, false, false, false, false, Vector2.up);
        }

        private IEnumerator Start()
        {
            yield return null;
            foreach (var enemy in FindObjectsOfType<DemoEnemyController>(true)) enemy.enabled = false;
            var combat = GetComponent<DemoPlayerCombatController>();
            var movement = GetComponent<DemoPlayerController>();
            var health = GetComponent<DemoPlayerHealthController>();
            var flow = GetComponent<DemoRunFlowController>();
            var body = GetComponent<Rigidbody2D>();
            flow.enabled = false;
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var inputField = typeof(DemoPlayerController).GetField("_inputSource", flags);
            object originalInput = inputField.GetValue(movement);
            movement.enabled = false;
            health.ResetForRetry();
            health.ReceiveDamage(new DemoDamageInfo(10000f, gameObject));
            yield return new WaitForSeconds(0.2f);
            combat.ResetForRetry();
            transform.rotation = Quaternion.identity;
            var target = new GameObject("FailurePendingHitTarget");
            target.AddComponent<CircleCollider2D>().radius = 0.05f;
            var receiver = target.AddComponent<DemoDamageReceiver>();
            target.transform.position = transform.position + Vector3.up;
            Physics2D.SyncTransforms();
            bool queued = combat.TryStartAttack();
            health.ReceiveDamage(new DemoDamageInfo(10000f, gameObject));
            typeof(DemoPlayerCombatController).GetField("_energy", flags).SetValue(combat, 100f);
            Vector2 before = body.position;
            inputField.SetValue(movement, new HeldMove());
            movement.enabled = true;
            yield return new WaitForSeconds(0.3f);
            bool movementBlocked = Vector2.Distance(before, body.position) < 0.001f;
            bool pendingCancelled = queued && Mathf.Approximately(receiver.CurrentHealth, receiver.MaxHealth);
            bool combatBlocked = !combat.TryStartAttack() && !combat.TryUseSkill() && !combat.TryUseUltimate();
            bool dodgeBlocked = !movement.TryDodge(new DemoInputState(Vector2.right, Vector2.zero, false, true, false, false));
            flow.RestartRun();
            yield return new WaitForSeconds(0.05f);
            bool movementRestored = Vector2.Distance(before, body.position) > 0.001f;
            bool attackRestored = combat.TryStartAttack();
            inputField.SetValue(movement, originalInput);
            flow.enabled = true;
            Destroy(target);
            Debug.Log($"[DemoFailureInputAcceptanceProbe] MovementBlocked={movementBlocked}, PendingCancelled={pendingCancelled}, CombatBlocked={combatBlocked}, DodgeBlocked={dodgeBlocked}, MovementRestored={movementRestored}, AttackRestored={attackRestored}");
        }
    }
}
