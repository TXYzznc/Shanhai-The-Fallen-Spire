using UnityEngine;

namespace Shanhai.Demo.Input
{
    /// <summary>
    /// 一帧内的语义化输入快照。由输入边界生成，玩法层只读取该结构。
    /// </summary>
    public readonly struct DemoInputState
    {
        public DemoInputState(Vector2 move, Vector2 aimScreenPosition, bool attackPressed, bool dodgePressed, bool skillPressed, bool ultimatePressed, Vector2 aimDirection = default, bool assemblyPressed = false, bool closeUiPressed = false)
        {
            AssemblyPressed = assemblyPressed; CloseUiPressed = closeUiPressed;
            Move = move;
            AimScreenPosition = aimScreenPosition;
            AttackPressed = attackPressed;
            DodgePressed = dodgePressed;
            SkillPressed = skillPressed;
            UltimatePressed = ultimatePressed;
            AimDirection = aimDirection;
        }

        public bool AssemblyPressed { get; }
        public bool CloseUiPressed { get; }
        public Vector2 Move { get; }
        public Vector2 AimScreenPosition { get; }
        public bool AttackPressed { get; }
        public bool DodgePressed { get; }
        public bool SkillPressed { get; }
        public bool UltimatePressed { get; }
        public Vector2 AimDirection { get; }
    }
}
