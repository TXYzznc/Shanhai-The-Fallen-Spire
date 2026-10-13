using Shanhai.Demo.Build;
using Shanhai.Demo.Player;
using TMPro;
using UnityEngine;

namespace Shanhai.Demo.UI
{
    public sealed class DemoRunInfoView : MonoBehaviour
    {
        [SerializeField] private TMP_Text summary;
        [SerializeField] private TMP_Text upgrade;
        [SerializeField] private TMP_Text notice;
        private DemoRunFlowController _run;
        private DemoBuffLoadout _buffs;
        private DemoRewardChoiceController _reward;
        private float _nextRefresh;
        public void Bind(GameObject player, DemoRewardChoiceController reward)
        {
            _run = player != null ? player.GetComponent<DemoRunFlowController>() : null;
            _buffs = player != null ? player.GetComponent<DemoBuffLoadout>() : null;
            _reward = reward; notice.text = string.Empty; _nextRefresh = 0; Refresh();
        }
        public void ShowNotice(string message) => notice.text = message;
        private void Update()
        {
            if (_run == null || Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + .1f; Refresh();
        }
        public void Refresh()
        {
            if (_run == null) { summary.text = upgrade.text = string.Empty; return; }
            summary.SetText("货币 {0:0} | WASD 移动 | 鼠标瞄准", _run.CurrentCurrency);
            if (_reward != null && _reward.IsOpen && _reward.IsBuffChoice) upgrade.text = "检查点升级：等待选择增益";
            else if (_buffs != null && _buffs.ActiveCount > 0) upgrade.SetText("检查点升级：已获增益 {0}项", _buffs.ActiveCount);
            else upgrade.text = "检查点升级：待到达";
        }
    }
}
