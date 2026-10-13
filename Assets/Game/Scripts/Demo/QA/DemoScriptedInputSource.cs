using Shanhai.Demo.Input;
using UnityEngine;

namespace Shanhai.Demo.QA
{
    /// <summary>仅用于验收的可替换语义输入 provider；不挂在正式场景。</summary>
    public sealed class DemoScriptedInputSource : MonoBehaviour, IDemoInputSource
    {
        [SerializeField] private int moveDurationFrames = 50;
        [SerializeField] private Vector2 moveDirection = Vector2.right;
        private int _frame = -1;
        private DemoInputState _state;

        public DemoInputState ReadState()
        {
            if (_frame == Time.frameCount) return _state;
            _frame = Time.frameCount;
            int phase = Time.frameCount % 120;
            Vector2 move = phase < moveDurationFrames ? moveDirection : Vector2.zero;
            bool attack = phase == 8 || phase == 32;
            bool dodge = phase == 70;
            bool skill = phase == 90;
            _state = new DemoInputState(move, Vector2.zero, attack, dodge, skill, false, Vector2.up);
            return _state;
        }
    }
}
