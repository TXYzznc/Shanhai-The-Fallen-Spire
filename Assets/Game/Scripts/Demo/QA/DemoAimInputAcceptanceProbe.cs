using System.Collections;
using System.Reflection;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Input;
using Shanhai.Demo.Player;
using UnityEngine;

namespace Shanhai.Demo.QA
{
    /// <summary>用实际正交相机投影验证鼠标屏幕坐标分支；不代表物理鼠标或手柄输入实测。</summary>
    public sealed class DemoAimInputAcceptanceProbe : MonoBehaviour, IDemoInputSource
    {
        public int MouseDirectionCases { get; private set; }
        public int StickDirectionCases { get; private set; }
        public bool StickOverridesMouse { get; private set; }
        public bool MouseAtPlayerKeepsFacing { get; private set; }
        public bool AimDoesNotMovePlayer { get; private set; }
        public bool MouseAimSkillSameFrame { get; private set; }
        public bool StickAimSkillSameFrame { get; private set; }
        public bool RetryResetsFacing { get; private set; }
        public bool Completed { get; private set; }
        public bool Finished { get; private set; }
        public string Details { get; private set; }
        private DemoInputState _input;
        private DemoPlayerController _movement;
        private DemoPlayerCombatController _combat;
        private DemoRunFlowController _flow;
        private Rigidbody2D _body;
        private Camera _camera;
        private object _movementInput, _combatInput;
        private float _captureDelta;
        private bool _captured;
        private DemoDamageReceiver _rightTarget, _upTarget;
        public DemoInputState ReadState() => _input;
        private static FieldInfo Field(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        private void Input(Vector2 mouseWorldDirection, Vector2 stick = default, bool skill = false)
        {
            Vector3 screen = _camera.WorldToScreenPoint(transform.position + (Vector3)mouseWorldDirection * 2f);
            _input = new DemoInputState(Vector2.zero, screen, false, false, skill, false, stick);
        }
        private IEnumerator Start()
        {
            yield return null;
            _movement = GetComponent<DemoPlayerController>(); _combat = GetComponent<DemoPlayerCombatController>();
            _flow = GetComponent<DemoRunFlowController>(); _body = GetComponent<Rigidbody2D>();
            _camera = (Camera)Field(_movement, "aimCamera").GetValue(_movement);
            _movementInput = Field(_movement, "_inputSource").GetValue(_movement);
            _combatInput = Field(_combat, "_inputSource").GetValue(_combat);
            _captureDelta = Time.captureDeltaTime; _captured = true;
            Field(_movement, "_inputSource").SetValue(_movement, this);
            Field(_combat, "_inputSource").SetValue(_combat, this);
            Time.captureDeltaTime = 0.05f; _flow.RestartRun(); Input(Vector2.up);
            yield return new WaitForSeconds(0.3f);
            Vector2 start = _body.position;
            foreach (Vector2 direction in new[] { Vector2.up, Vector2.right, Vector2.down, Vector2.left })
            {
                Input(direction); yield return new WaitForSeconds(0.3f);
                if (Vector2.Angle(transform.up, direction) < 0.1f) MouseDirectionCases++;
            }
            foreach (Vector2 direction in new[] { Vector2.up, Vector2.right, Vector2.down, Vector2.left, new Vector2(1f, 1f).normalized })
            {
                Input(-direction, direction); yield return new WaitForSeconds(0.3f);
                if (Vector2.Angle(transform.up, direction) < 0.1f) StickDirectionCases++;
            }
            StickOverridesMouse = StickDirectionCases == 5;
            Vector3 facing = transform.up; Input(Vector2.zero); yield return new WaitForSeconds(0.2f);
            MouseAtPlayerKeepsFacing = Vector2.Angle(transform.up, facing) < 0.1f;
            AimDoesNotMovePlayer = Vector2.Distance(start, _body.position) < 0.001f && Mathf.Abs(transform.position.z) < 0.001f;
            _rightTarget = Target("QA_AimRight", start + Vector2.right);
            _upTarget = Target("QA_AimUp", start + Vector2.up);
            yield return null; Physics2D.SyncTransforms();
            _combat.ResetForRetry(); transform.rotation = Quaternion.identity; _body.rotation = 0f;
            Input(Vector2.right, skill: true); yield return null;
            Input(Vector2.right); yield return null;
            MouseAimSkillSameFrame = Mathf.Abs(_rightTarget.CurrentHealth - 180f) < 0.001f && _upTarget.CurrentHealth == 200f;
            float mouseRight = _rightTarget.CurrentHealth, mouseUp = _upTarget.CurrentHealth;
            _rightTarget.ResetHealth(); _upTarget.ResetHealth();
            _combat.ResetForRetry(); transform.rotation = Quaternion.identity; _body.rotation = 0f;
            Input(Vector2.up, Vector2.right, true); yield return null;
            Input(Vector2.up, Vector2.right); yield return null;
            StickAimSkillSameFrame = Mathf.Abs(_rightTarget.CurrentHealth - 180f) < 0.001f && _upTarget.CurrentHealth == 200f;
            float stickRight = _rightTarget.CurrentHealth, stickUp = _upTarget.CurrentHealth;
            _flow.RestartRun();
            RetryResetsFacing = Quaternion.Angle(transform.rotation, Quaternion.identity) < 0.01f;
            Completed = MouseDirectionCases == 4 && StickDirectionCases == 5 && StickOverridesMouse && MouseAtPlayerKeepsFacing
                && AimDoesNotMovePlayer && MouseAimSkillSameFrame && StickAimSkillSameFrame && RetryResetsFacing;
            Details = $"mouse={MouseDirectionCases}/4, stick={StickDirectionCases}/5, mouseSkillRightUp={mouseRight}/{mouseUp}, stickSkillRightUp={stickRight}/{stickUp}";
            Finished = true; Restore();
        }
        private static DemoDamageReceiver Target(string name, Vector2 position)
        {
            var root = new GameObject(name); root.transform.position = position;
            root.AddComponent<CircleCollider2D>().isTrigger = true;
            var receiver = root.AddComponent<DemoDamageReceiver>(); receiver.ConfigureHealth(200f, 0f); return receiver;
        }
        private void OnDisable() => Restore();
        private void Restore()
        {
            if (!_captured) return;
            Field(_movement, "_inputSource").SetValue(_movement, _movementInput);
            Field(_combat, "_inputSource").SetValue(_combat, _combatInput);
            Time.captureDeltaTime = _captureDelta;
            if (_rightTarget != null) Destroy(_rightTarget.gameObject);
            if (_upTarget != null) Destroy(_upTarget.gameObject);
            _captured = false;
        }
    }
}
