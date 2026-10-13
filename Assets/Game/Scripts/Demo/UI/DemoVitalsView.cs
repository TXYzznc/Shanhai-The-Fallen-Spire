using Shanhai.Demo.Combat;
using Shanhai.Demo.Configuration;
using Shanhai.Demo.Player;
using TMPro;
using UnityEngine;

namespace Shanhai.Demo.UI
{
    public sealed class DemoVitalsView : MonoBehaviour
    {
        [SerializeField] private TMP_Text summary;
        [SerializeField] private TMP_Text skill;
        [SerializeField] private RectTransform healthFill;
        private DemoPlayerCombatController _combat;
        private DemoPlayerHealthController _health;
        private DemoGameplayConfig _config;
        private float _nextRefresh;
        public void Bind(GameObject player, DemoGameplayConfig config)
        {
            _combat = player != null ? player.GetComponent<DemoPlayerCombatController>() : null;
            _health = player != null ? player.GetComponent<DemoPlayerHealthController>() : null;
            _config = config; _nextRefresh = 0; Refresh();
        }
        private void Update()
        {
            if (_combat == null || Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + .1f; Refresh();
        }
        public void Refresh()
        {
            if (_combat == null || _health == null) { summary.text = skill.text = string.Empty; healthFill.anchorMax = Vector2.up; return; }
            float energyMax = _config != null && _config.Player != null ? _config.Player.energyMax : 100;
            summary.SetText("生命 {0:0}/{1:0} | 能量 {2:0}/{3:0}", _health.CurrentHealth, _health.MaxHealth, _combat.CurrentEnergy, energyMax);
            healthFill.anchorMax = new Vector2(_health.MaxHealth > 0 ? Mathf.Clamp01(_health.CurrentHealth / _health.MaxHealth) : 0, 1);
            if (_health.IsRetryRequired) skill.text = "本局失败，正在重新开始";
            else if (_health.IsHitStunned) skill.text = "受击硬直";
            else if (_combat.SkillCooldownRemaining > 0)
                skill.SetText("技能冷却 {0:1}s | 可复活 {1}次", _combat.SkillCooldownRemaining, _health.RevivesRemaining);
            else skill.SetText("技能就绪（Shift） | 可复活 {0}次", _health.RevivesRemaining);
        }
    }
}
