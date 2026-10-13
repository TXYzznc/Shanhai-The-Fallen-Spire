using System;
using Shanhai.Demo.Enemies;
using Shanhai.Demo.Player;
using UnityEngine;
using UnityEngine.UI;

namespace Shanhai.Demo.UI
{
    /// <summary>结算窗口内容；完成时机由会话协调器决定。</summary>
    public sealed class DemoCompletionView : MonoBehaviour
    {
        [SerializeField] private GameObject panel;
        [SerializeField] private Button retryButton;
        [SerializeField] private Button closeButton;
        private DemoRunFlowController _run;
        public event Action CloseRequested;
        public bool IsOpen => panel != null && panel.activeInHierarchy;
        private void Awake()
        { retryButton.onClick.AddListener(Retry); closeButton.onClick.AddListener(Hide); }
        public void Open() => panel.SetActive(true);
        public void Hide() => CloseRequested?.Invoke();
        private void Retry() { _run?.RestartRun(); Hide(); }
        public void Bind(DemoRunFlowController flow, DemoEncounterController encounter,
            Shanhai.Demo.Build.DemoRewardChoiceController reward = null) { _run = flow; }
    }
}
