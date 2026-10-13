using System;
using System.Collections.Generic;

namespace Game.State
{
    public sealed class GameStateChangedEventArgs : EventArgs
    {
        public GameStateChangedEventArgs(bool paused, int count) { IsPaused = paused; PauseCount = count; }
        public bool IsPaused { get; }
        public int PauseCount { get; }
    }

    /// <summary>窗口生命周期信号；同一窗口重复开关幂等。</summary>
    public sealed class GameWindowState
    {
        public event EventHandler Changed;
        public bool IsOpen { get; private set; }
        public void SetOpen(bool open)
        {
            if (IsOpen == open) return;
            IsOpen = open; Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>通用暂停所有权；登记数与正在暂停的窗口数分离。</summary>
    public sealed class GameStateManager : IDisposable
    {
        private readonly Dictionary<GameWindowState, Registration> _windows = new Dictionary<GameWindowState, Registration>();
        private bool _disposed;
        public int PauseCount { get; private set; }
        public bool IsPaused => PauseCount > 0;
        public event EventHandler<GameStateChangedEventArgs> StateChanged;

        public IDisposable RegisterWindow(GameWindowState window)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(GameStateManager));
            if (window == null) throw new ArgumentNullException(nameof(window));
            if (_windows.ContainsKey(window)) throw new InvalidOperationException("同一窗口不能重复注册。");
            var registration = new Registration(this, window);
            _windows.Add(window, registration); registration.Connect(); return registration;
        }
        private void Adjust(int delta)
        {
            bool wasPaused = IsPaused; PauseCount += delta;
            if (wasPaused != IsPaused) StateChanged?.Invoke(this, new GameStateChangedEventArgs(IsPaused, PauseCount));
        }
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            // Registration.Dispose removes itself; snapshot only during teardown.
            var registrations = new List<Registration>(_windows.Values);
            foreach (var registration in registrations) registration.Dispose();
            StateChanged = null;
        }
        private sealed class Registration : IDisposable
        {
            private GameStateManager _manager;
            private readonly GameWindowState _window;
            private bool _held;
            public Registration(GameStateManager manager, GameWindowState window) { _manager = manager; _window = window; }
            public void Connect() { _window.Changed += Changed; Sync(); }
            private void Changed(object sender, EventArgs args) => Sync();
            private void Sync()
            {
                if (_manager == null || _held == _window.IsOpen) return;
                _held = _window.IsOpen; _manager.Adjust(_held ? 1 : -1);
            }
            public void Dispose()
            {
                if (_manager == null) return;
                var manager = _manager; _manager = null;
                _window.Changed -= Changed; manager._windows.Remove(_window);
                if (_held) { _held = false; manager.Adjust(-1); }
            }
        }
    }
}
