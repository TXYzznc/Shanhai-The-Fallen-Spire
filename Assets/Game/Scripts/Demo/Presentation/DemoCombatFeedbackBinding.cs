using Shanhai.Demo.Combat;
using UnityEngine;

namespace Shanhai.Demo.Presentation
{
    /// <summary>实体显示期间持有会话订阅；GF隐藏实体时对称解绑。</summary>
    public sealed class DemoCombatFeedbackBinding : MonoBehaviour
    {
        private DemoFeedbackSession _session;
        private DemoDamageReceiver _enemy;
        private Shanhai.Demo.Enemies.DemoEnemyController _enemyCombat;
        private DemoPlayerHealthController _health;
        private DemoPlayerCombatController _combat;
        public void Bind(DemoFeedbackSession session)
        {
            Unbind(); _session = session;
            if (session == null) return;
            _enemy = GetComponent<DemoDamageReceiver>(); _health = GetComponent<DemoPlayerHealthController>();
            _combat = GetComponent<DemoPlayerCombatController>();
            _enemyCombat = GetComponent<Shanhai.Demo.Enemies.DemoEnemyController>();
            if (_enemy != null) _enemy.DamageApplied += session.Damaged;
            if (_health != null) _health.DamageApplied += session.Damaged;
            if (_combat != null) _combat.ActionStarted += session.ActionStarted;
            if (_enemyCombat != null) _enemyCombat.ActionStarted += session.ActionStarted;
        }
        private void OnDisable() { Unbind(); }
        public void Unbind()
        {
            // 即使Unity已销毁会话组件，仍需解除其托管委托，避免池对象保留旧会话。
            if (!ReferenceEquals(_session, null))
            {
                if (_enemy != null) _enemy.DamageApplied -= _session.Damaged;
                if (_health != null) _health.DamageApplied -= _session.Damaged;
                if (_combat != null) _combat.ActionStarted -= _session.ActionStarted;
                if (_enemyCombat != null) _enemyCombat.ActionStarted -= _session.ActionStarted;
            }
            _session = null; _enemy = null; _health = null; _combat = null; _enemyCombat = null;
        }
    }
}
