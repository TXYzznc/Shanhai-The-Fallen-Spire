using UnityEngine;
using Shanhai.Demo.Build;

namespace Shanhai.Demo.UI
{
    public sealed class DemoMarkAssemblyForm : DemoSessionWindowForm
    {
        [SerializeField] private DemoMarkAssemblyView view;
        public DemoMarkAssemblyView View => view;
        protected override void BindContent(DemoUiSession session)
        {
            view.CloseRequested += RequestClose;
            view.Bind(session.Player.GetComponent<DemoMarkLoadout>()); view.Open();
        }
        protected override void UnbindContent()
        { view.CloseRequested -= RequestClose; view.Bind(null); }
    }
}
