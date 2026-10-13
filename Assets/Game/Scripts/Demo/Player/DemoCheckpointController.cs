using Shanhai.Demo.UI;
using Shanhai.Demo.Build;
using UnityEngine;

namespace Shanhai.Demo.Player
{
    /// <summary>2D 检查点灰盒触发器；一次触发后保持已激活状态并打开升级入口。</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class DemoCheckpointController : MonoBehaviour
    {
        private IDemoRewardPresenter rewardView;
        [SerializeField] private DemoRewardChoiceController rewardChoice;
        [SerializeField] private DemoRunFlowController runFlow;
        private bool _awaitingUpgrade;

        public bool IsActivated { get; private set; }

        public void BindViews(DemoRewardChoiceController choice, IDemoRewardPresenter view)
        {
            rewardChoice = choice; rewardView = view;
        }

        public void BindRun(DemoRunFlowController flow) { runFlow = flow; }

        public void ResetForRetry()
        {
            IsActivated = false;
            _awaitingUpgrade = false;
        }

        private void Awake()
        {
            Collider2D trigger = GetComponent<Collider2D>();
            trigger.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!IsActivated && other.GetComponentInParent<DemoPlayerController>() != null)
            {
                IsActivated = true;
                if (runFlow != null) runFlow.CaptureCheckpoint(this, false, string.Empty);
                if (rewardView != null) rewardView.OpenBuffs();
                _awaitingUpgrade = rewardChoice != null && rewardChoice.IsOpen && rewardChoice.IsBuffChoice;
            }
        }

        private void LateUpdate()
        {
            if (!_awaitingUpgrade || !IsActivated || rewardChoice == null) return;
            if (rewardChoice.IsConfirmed && rewardChoice.IsBuffChoice)
            {
                _awaitingUpgrade = false;
                if (runFlow != null) runFlow.CaptureCheckpoint(this, true, rewardChoice.SelectedMarkId);
            }
            else if (!rewardChoice.IsOpen || !rewardChoice.IsBuffChoice) _awaitingUpgrade = false;
        }
    }
}
