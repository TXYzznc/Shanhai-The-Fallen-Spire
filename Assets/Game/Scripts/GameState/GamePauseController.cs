using UnityEngine;

namespace Game.State
{
    /// <summary>会话拥有的Unity时钟适配，恢复首次暂停前的倍率。</summary>
    public sealed class GamePauseController : MonoBehaviour
    {
        private readonly GameStateManager _state = new GameStateManager();
        private float _previousScale;
        private bool _ownsClock;
        private int _blockedInputFrame = -1;
        public GameStateManager State => _state;
        public bool BlocksGameplay => _state.IsPaused || _blockedInputFrame == Time.frameCount;
        private void Awake() => _state.StateChanged += Changed;
        private void Changed(object sender, GameStateChangedEventArgs args)
        {
            _blockedInputFrame = Time.frameCount;
            if (args.IsPaused)
            {
                _previousScale = Time.timeScale; _ownsClock = true; Time.timeScale = 0f;
            }
            else RestoreClock();
        }
        private void RestoreClock()
        {
            if (!_ownsClock) return;
            Time.timeScale = _previousScale; _ownsClock = false;
        }
        private void OnDestroy() { _state.Dispose(); _state.StateChanged -= Changed; RestoreClock(); }
    }
}
