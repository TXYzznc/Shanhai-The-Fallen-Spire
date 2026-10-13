using UnityEngine;

namespace Shanhai.Demo.UI
{
    public sealed class DemoCompletionForm : DemoSessionWindowForm
    {
        [SerializeField] private DemoCompletionView view;
        public DemoCompletionView View => view;
        protected override void BindContent(DemoUiSession session)
        { view.CloseRequested += RequestClose; view.Bind(session.Run, null); view.Open(); }
        protected override void UnbindContent()
        { view.CloseRequested -= RequestClose; view.Bind(null, null); }
    }
}
