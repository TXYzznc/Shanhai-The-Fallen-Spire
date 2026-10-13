using UnityEngine;

namespace Shanhai.Demo.UI
{
    public sealed class DemoRewardForm : DemoSessionWindowForm
    {
        [SerializeField] private DemoRewardChoiceView view;
        private DemoUiSession _session;
        public DemoRewardChoiceView View => view;
        protected override void BindContent(DemoUiSession session)
        {
            view.CloseRequested += RequestClose;
            _session = session; view.NoticeProduced += session.OnRewardNotice;
            view.Bind(session.RewardChoice); view.PresentOptions();
        }
        protected override void UnbindContent()
        {
            view.CloseRequested -= RequestClose;
            if (_session != null) view.NoticeProduced -= _session.OnRewardNotice;
            _session = null;
            view.Unbind();
        }
    }
}
