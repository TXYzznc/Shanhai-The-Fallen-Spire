using UnityEngine;

namespace Shanhai.Demo.Input
{
    /// <summary>
    /// 使用 Unity 2022.3 内置输入后端的 Demo provider。每帧只采集一次，避免玩法层重复读取设备。
    /// </summary>
    public sealed class DemoInputSource : MonoBehaviour, IDemoInputSource
    {
        [SerializeField] private string horizontalAxis = "Horizontal";
        [SerializeField] private string verticalAxis = "Vertical";
        [SerializeField] private string aimHorizontalAxis = "Horizontal Aim";
        [SerializeField] private string aimVerticalAxis = "Vertical Aim";
        [SerializeField, Range(0f, 1f)] private float aimDeadZone = 0.2f;
        [SerializeField] private KeyCode attackKey = KeyCode.Mouse0;
        [SerializeField] private KeyCode dodgeKey = KeyCode.Space;
        [SerializeField] private KeyCode skillKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode ultimateKey = KeyCode.Q;

        [SerializeField] private KeyCode assemblyKey = KeyCode.Tab;
        [SerializeField] private KeyCode closeUiKey = KeyCode.Escape;
        private DemoInputState _state;
        private int _stateFrame = -1;
        private bool _focused = true;

        private void OnDisable() => _stateFrame = -1;
        private void OnEnable() => _focused = true;
        private void OnApplicationFocus(bool hasFocus) { _focused = hasFocus; _stateFrame = -1; }

        public DemoInputState ReadState()
        {
            if (_stateFrame == Time.frameCount)
            {
                return _state;
            }

            _stateFrame = Time.frameCount;
            // Unity Editor can report a transient application-focus loss when entering
            // Play Mode even while the Game View is the active input surface. Keep the
            // input abstraction usable during editor play; player builds still honour
            // the real application focus state.
            var canSample = _focused || (Application.isEditor && Application.isPlaying);
            if (!canSample)
            {
                _state = new DemoInputState(Vector2.zero, Vector2.zero, false, false, false, false, Vector2.zero);
                return _state;
            }
            var move = new Vector2(UnityEngine.Input.GetAxisRaw(horizontalAxis), UnityEngine.Input.GetAxisRaw(verticalAxis));
            move = Vector2.ClampMagnitude(move, 1f);
            var aimDirection = new Vector2(UnityEngine.Input.GetAxisRaw(aimHorizontalAxis), UnityEngine.Input.GetAxisRaw(aimVerticalAxis));
            if (aimDirection.sqrMagnitude < aimDeadZone * aimDeadZone) aimDirection = Vector2.zero;
            else aimDirection = Vector2.ClampMagnitude(aimDirection, 1f);
            _state = new DemoInputState(move, UnityEngine.Input.mousePosition, UnityEngine.Input.GetKeyDown(attackKey), UnityEngine.Input.GetKeyDown(dodgeKey), UnityEngine.Input.GetKeyDown(skillKey), UnityEngine.Input.GetKeyDown(ultimateKey), aimDirection, UnityEngine.Input.GetKeyDown(assemblyKey), UnityEngine.Input.GetKeyDown(closeUiKey));
            return _state;
        }
    }
}
