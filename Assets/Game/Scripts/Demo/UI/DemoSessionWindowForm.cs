using System;
using Game.State;

namespace Shanhai.Demo.UI
{
    public abstract class DemoSessionWindowForm : UIFormBase
    {
        private Action _close;
        public void Bind(DemoUiSession session, Action close)
        {
            _close = close;
            GetComponent<GamePauseWindow>().Bind(session.Pause.State);
            BindContent(session);
        }
        protected void RequestClose() => _close?.Invoke();
        protected abstract void BindContent(DemoUiSession session);
        protected abstract void UnbindContent();
        protected override void OnClose(bool isShutdown, object userData)
        {
            UnbindContent(); _close = null;
            GetComponent<GamePauseWindow>().Unbind();
            base.OnClose(isShutdown, userData);
        }
    }
}
