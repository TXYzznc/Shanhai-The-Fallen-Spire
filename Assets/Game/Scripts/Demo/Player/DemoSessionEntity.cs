using Shanhai.Demo.Combat;
using Shanhai.Demo.Enemies;
using Shanhai.Demo.Presentation;

namespace Shanhai.Demo.Player
{
    /// <summary>项目实体的GF生命周期桥接；对象池回收后不保留会话引用。</summary>
    public sealed class DemoSessionEntity : EntityBase
    {
        protected override void OnHide(bool isShutdown, object userData)
        {
            var movement = GetComponent<DemoPlayerController>();
            if (movement != null) { movement.enabled = false; movement.BindInput(null, null); }
            var combat = GetComponent<DemoPlayerCombatController>();
            if (combat != null) { combat.enabled = false; combat.BindInput(null); combat.ResetForRetry(); }
            var flow = GetComponent<DemoRunFlowController>();
            if (flow != null) { flow.enabled = false; flow.BindViews(null, null); flow.BindWorld(null, null, null); }
            var enemy = GetComponent<DemoEnemyController>();
            if (enemy != null) { enemy.enabled = false; enemy.BindTarget(null); enemy.BindConfiguration(null, null); }
            var feedback = GetComponent<DemoSpriteFeedback>();
            if (feedback != null) { feedback.enabled = false; feedback.ResetPresentation(); }
            var binding = GetComponent<DemoCombatFeedbackBinding>();
            if (binding != null) binding.Unbind();
            var status = GetComponent<Shanhai.Demo.UI.DemoEnemyStatusBinding>();
            if (status != null) status.Unbind();
            if (movement != null) movement.BindPause(null);
            if (combat != null) combat.BindPause(null);
            if (flow != null) flow.BindPause(null);
            if (enemy != null) enemy.BindPause(null);
            var enemyHealth = GetComponent<DemoDamageReceiver>(); if (enemyHealth != null) enemyHealth.BindPause(null);
            var playerHealth = GetComponent<DemoPlayerHealthController>(); if (playerHealth != null) playerHealth.BindPause(null);
            base.OnHide(isShutdown, userData);
        }
    }
}
