using Shanhai.Demo.Combat;
using Shanhai.Demo.Enemies;
using TMPro;
using UnityEngine;

namespace Shanhai.Demo.UI
{
    /// <summary>真实Item模板；只显示已有状态，不参与战斗结算。</summary>
    public sealed class DemoEnemyStatusItem : MonoBehaviour
    {
        [SerializeField] private RectTransform healthFill;
        [SerializeField] private RectTransform staggerFill;
        [SerializeField] private GameObject staggerRow;
        [SerializeField] private RectTransform chargeFill;
        [SerializeField] private GameObject chargeRow;
        [SerializeField] private TMP_Text healthLabel;
        [SerializeField] private TMP_Text statusLabel;
        private float _lastHealth = -1f, _lastMax = -1f;
        private int _lastStatus = -1;
        public float HealthRatio { get; private set; }
        public float StaggerRatio { get; private set; }
        public float ChargeRatio { get; private set; }
        public bool ShowsStagger => staggerRow.activeSelf;
        public bool ShowsCharge => chargeRow.activeSelf;
        public string Status => statusLabel.text;

        public void Render(DemoDamageReceiver health, DemoEnemyController enemy, Vector2 position)
        {
            gameObject.SetActive(true);
            ((RectTransform)transform).anchoredPosition = position;
            HealthRatio = Mathf.Clamp01(health.CurrentHealth / health.MaxHealth);
            Fill(healthFill, HealthRatio);
            if (_lastHealth != health.CurrentHealth || _lastMax != health.MaxHealth)
            {
                healthLabel.SetText("{0:0}/{1:0}", health.CurrentHealth, health.MaxHealth);
                _lastHealth = health.CurrentHealth; _lastMax = health.MaxHealth;
            }
            staggerRow.SetActive(health.CanStagger);
            StaggerRatio = health.IsStaggered ? health.StaggerRecovery : health.StaggerProgress;
            Fill(staggerFill, StaggerRatio);
            chargeRow.SetActive(enemy.IsCharging);
            ChargeRatio = enemy.ChargeProgress;
            Fill(chargeFill, ChargeRatio);
            int status = health.IsStaggered ? 1 : health.IsHitStunned ? 2 : enemy.IsCharging ? 3
                : enemy.IsDashing ? 4 : enemy.Phase == 2 ? 5 : health.CanStagger ? 6 : 0;
            if (_lastStatus != status)
            {
                statusLabel.SetText(status == 1 ? "失衡恢复" : status == 2 ? "硬直" : status == 3 ? "蓄力"
                    : status == 4 ? "冲撞" : status == 5 ? "二阶段" : status == 6 ? "失衡积累" : string.Empty);
                _lastStatus = status;
            }
        }
        private static void Fill(RectTransform rect, float ratio)
        {
            Vector2 anchor = rect.anchorMax; anchor.x = ratio; rect.anchorMax = anchor;
        }
        public void Clear()
        {
            HealthRatio = StaggerRatio = ChargeRatio = 0f;
            _lastHealth = _lastMax = -1f; _lastStatus = -1;
            statusLabel.SetText(string.Empty); gameObject.SetActive(false);
        }
    }
}
