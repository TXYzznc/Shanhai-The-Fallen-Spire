using System;
using Game.State;
using GameFramework;
using GameFramework.Event;
using UnityGameFramework.Runtime;

namespace Shanhai.Demo.UI
{
    /// <summary>持有一次GF窗口请求；加载暂停交接给Form，取消后禁止迟到回调。</summary>
    internal sealed class DemoUiWindowLease : IDisposable
    {
        private readonly GameStateManager _state;
        private readonly Action<UIFormBase> _opened;
        private readonly Action<string> _failed;
        private readonly GameWindowState _loading = new GameWindowState();
        private IDisposable _loadingRegistration;
        private UIParams _pending;
        private int _serial = -1;
        private bool _disposed, _subscribed;
        public bool IsRequested { get; private set; }
        public UIFormBase Form { get; private set; }
        public int SerialId => _serial;
        public DemoUiWindowLease(GameStateManager state, Action<UIFormBase> opened, Action<string> failed)
        { _state = state; _opened = opened; _failed = failed; }
        public void Open(string asset, string group, int order)
        {
            if (_disposed || IsRequested) return;
            IsRequested = true;
            if (_state != null)
            {
                _loadingRegistration = _state.RegisterWindow(_loading); _loading.SetOpen(true);
            }
            _pending = UIParams.Create(false, order);
            _pending.OpenCallback = Opened; _pending.CloseCallback = Closed;
            GF.Event.Subscribe(OpenUIFormFailureEventArgs.EventId, Failed); _subscribed = true;
            try
            {
                int serial = GF.UI.OpenUIForm(asset, group, false, _pending);
                // Pooled forms can complete (or fail) synchronously before OpenUIForm returns.
                if (IsRequested) _serial = serial;
            }
            catch (Exception error) { Fail(error.Message); }
        }
        private void Opened(UIFormLogic logic)
        {
            _pending = null; Unsubscribe();
            _serial = logic.UIForm.SerialId;
            if (_disposed || !IsRequested) { GF.UI.CloseUIForm(_serial); return; }
            Form = logic as UIFormBase;
            try
            {
                if (Form == null) throw new InvalidOperationException("窗口缺少UIFormBase。");
                _opened(Form);
            }
            catch (Exception error) { Close(); _failed(error.Message); }
            finally { ReleaseLoadingPause(); }
        }
        private void Closed(UIFormLogic logic)
        { Form = null; _serial = -1; IsRequested = false; ReleaseLoadingPause(); }
        private void Failed(object sender, GameEventArgs args)
        {
            var failure = (OpenUIFormFailureEventArgs)args;
            if (failure.UserData == _pending) Fail(failure.ErrorMessage);
        }
        private void Fail(string message)
        {
            Close(); _failed(message);
        }
        public void Close()
        {
            IsRequested = false;
            bool openedForm = _serial >= 0 && GF.UI != null && GF.UI.HasUIForm(_serial);
            // Once initialized, UIFormBase.OnClose owns and releases its parameters.
            if (openedForm) _pending = null;
            if (_serial >= 0 && GF.UI != null && (GF.UI.IsLoadingUIForm(_serial) || GF.UI.HasUIForm(_serial)))
                GF.UI.CloseUIForm(_serial);
            _serial = -1; Form = null;
            Unsubscribe();
            // GF cancels before OnInit/OnOpen and never consumes UIParams in the cancelled path.
            if (_pending != null) { ReferencePool.Release(_pending); _pending = null; }
            ReleaseLoadingPause();
        }
        private void ReleaseLoadingPause()
        { _loading.SetOpen(false); _loadingRegistration?.Dispose(); _loadingRegistration = null; }
        private void Unsubscribe()
        {
            if (!_subscribed) return;
            if (GF.Event != null) GF.Event.Unsubscribe(OpenUIFormFailureEventArgs.EventId, Failed);
            _subscribed = false;
        }
        public void Dispose() { if (_disposed) return; _disposed = true; Close(); }
    }
}
