using System;
using UnityEngine;

namespace Game.State
{
    /// <summary>将实际可见窗口的Enable/Disable生命周期注册到暂停管理器。</summary>
    public sealed class GamePauseWindow : MonoBehaviour
    {
        private readonly GameWindowState _window = new GameWindowState();
        private IDisposable _registration;
        public void Bind(GameStateManager manager)
        {
            Unbind();
            _window.SetOpen(isActiveAndEnabled);
            if (manager != null) _registration = manager.RegisterWindow(_window);
        }
        public void Unbind() { _registration?.Dispose(); _registration = null; }
        private void OnEnable() => _window.SetOpen(true);
        private void OnDisable() => _window.SetOpen(false);
        private void OnDestroy() => Unbind();
    }
}
