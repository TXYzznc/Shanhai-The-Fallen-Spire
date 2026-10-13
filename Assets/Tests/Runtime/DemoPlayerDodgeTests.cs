#if UNITY_EDITOR
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Configuration;
using Shanhai.Demo.Input;
using Shanhai.Demo.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class DemoPlayerDodgeTests
{
    private DemoWorldEntry _world;
    private DemoPlayerController _movement;
    private DemoPlayerCombatController _combat;
    private DodgeInputFixture _input;
    private DemoInputSource _originalInput;
    private Camera _camera;
    private GameObject _target;
    [UnitySetUp]
    public IEnumerator Prepare()
    {
        yield return new DemoGameplayAcceptanceTests().LoadDemoScene();
        _world = Object.FindObjectOfType<DemoWorldEntry>();
        _movement = _world.Player.GetComponent<DemoPlayerController>();
        _combat = _world.Player.GetComponent<DemoPlayerCombatController>();
        _originalInput = Object.FindObjectOfType<DemoInputSource>();
        _camera = (Camera)new SerializedObject(_world).FindProperty("gameCamera").objectReferenceValue;
        _input = _world.gameObject.AddComponent<DodgeInputFixture>();
        _input.State = new DemoInputState(Vector2.zero, Vector2.zero, false, false, false, false, Vector2.up);
        _movement.BindInput(_input, _camera); _combat.BindInput(_input);
    }
    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        _movement.enabled = _combat.enabled = true;
        _movement.BindInput(_originalInput, _camera); _combat.BindInput(_originalInput);
        _world.Player.GetComponent<DemoRunFlowController>().RestartRun();
        Object.Destroy(_input); if (_target != null) Object.Destroy(_target);
        yield return null;
    }
    [UnityTest]
    public IEnumerator DodgeMovesContinuouslyForConfiguredDistanceAndLocksDirection()
    {
        var body = _world.Player.GetComponent<Rigidbody2D>(); Vector2 before = body.position;
        Assert.That(_movement.TryDodge(new DemoInputState(Vector2.right, Vector2.zero, false, true, false, false)), Is.True);
        Assert.That(body.position, Is.EqualTo(before), "起步不能瞬移。");
        Assert.That(_movement.IsInvulnerable, Is.True);
        Assert.That(_movement.TryDodge(new DemoInputState(Vector2.left, Vector2.zero, false, true, false, false)), Is.False);
        _input.State = new DemoInputState(Vector2.up, Vector2.zero, false, false, false, false, Vector2.up);
        yield return new WaitForSeconds(.08f);
        Assert.That(body.position.x - before.x, Is.GreaterThan(0f).And.LessThan(2f), "中途应处于起点和终点之间。");
        Assert.That(body.position.y, Is.EqualTo(before.y).Within(.001f), "闪避期间普通移动不得叠加或转向。");
        _input.State = new DemoInputState(Vector2.zero, Vector2.zero, false, false, false, false);
        yield return new WaitForSeconds(.16f);
        Assert.That(_movement.IsDodging, Is.False);
        Assert.That(body.position.x - before.x, Is.EqualTo(2f).Within(.01f), "累计位移必须是配置距离。");
        Assert.That(_movement.TryDodge(new DemoInputState(Vector2.right, Vector2.zero, false, true, false, false)), Is.False, "保留闪避冷却。");
        _input.State = new DemoInputState(Vector2.right, Vector2.zero, false, false, false, false, Vector2.up);
        Vector2 after = body.position; yield return null;
        Assert.That(body.position.x, Is.GreaterThan(after.x));
    }
    [UnityTest]
    public IEnumerator DodgeCancelsWindupHitButPreservesAttackCooldown()
    {
        var receiver = CreateTarget();
        Assert.That(_combat.TryStartAttack(), Is.True); Assert.That(_combat.IsAttackLocked, Is.True);
        Assert.That(_movement.TryDodge(new DemoInputState(Vector2.right, Vector2.zero, false, true, false, false)), Is.True);
        Assert.That(_combat.IsAttackLocked, Is.False); Assert.That(_combat.PresentationAction, Is.EqualTo(DemoPlayerAction.None));
        yield return null;
        Assert.That(_combat.TryStartAttack(), Is.False, "闪避不能返还普攻冷却。");
        yield return new WaitForSeconds(.4f);
        Assert.That(receiver.TotalDamage, Is.Zero, "取消的前摇不能在闪避后结算伤害。");
        _target.transform.position = _world.Player.transform.position + Vector3.up * .9f;
        Physics2D.SyncTransforms();
        Assert.That(_combat.TryStartAttack(), Is.True);
        yield return new WaitForSeconds(.14f);
        Assert.That(receiver.TotalDamage, Is.EqualTo(10f).Within(.001f), "取消后连段从第一段开始，目标确实仍可命中。");
    }
    [UnityTest]
    public IEnumerator DodgeDuringRecoveryPreservesAlreadyAppliedDamage()
    {
        var receiver = CreateTarget(); Assert.That(_combat.TryStartAttack(), Is.True);
        yield return new WaitForSeconds(.14f);
        Assert.That(receiver.TotalDamage, Is.EqualTo(10f).Within(.001f)); Assert.That(_combat.IsAttackLocked, Is.True);
        Assert.That(_movement.TryDodge(new DemoInputState(Vector2.right, Vector2.zero, false, true, false, false)), Is.True);
        Assert.That(_combat.IsAttackLocked, Is.False);
        yield return new WaitForSeconds(.25f);
        Assert.That(receiver.TotalDamage, Is.EqualTo(10f).Within(.001f));
    }
    [UnityTest]
    public IEnumerator InvulnerabilityRejectsDamageAndHitStunThenExpires()
    {
        var health = _world.Player.GetComponent<DemoPlayerHealthController>(); int events = 0;
        System.Action<DemoCombatPresentationEvent> listener = _ => events++;
        health.DamageApplied += listener;
        try
        {
            Assert.That(_movement.TryDodge(new DemoInputState(Vector2.up, Vector2.zero, false, true, false, false)), Is.True);
            health.ReceiveDamage(new DemoDamageInfo(10, null)); health.ReceiveDamage(new DemoDamageInfo(100000, null));
            Assert.That(health.CurrentHealth, Is.EqualTo(100)); Assert.That(health.RevivesRemaining, Is.EqualTo(1));
            Assert.That(health.IsHitStunned, Is.False); Assert.That(events, Is.Zero);
            yield return new WaitForSeconds(.23f);
            Assert.That(_movement.IsInvulnerable, Is.False);
            health.ReceiveDamage(new DemoDamageInfo(10, null));
            Assert.That(health.CurrentHealth, Is.EqualTo(90)); Assert.That(health.IsHitStunned, Is.True); Assert.That(events, Is.EqualTo(1));
            Assert.That(_movement.TryDodge(new DemoInputState(Vector2.up, Vector2.zero, false, true, false, false)), Is.False);
        }
        finally { health.DamageApplied -= listener; }
    }
    [UnityTest]
    public IEnumerator SimultaneousActionsGiveDodgePriorityInEitherUpdateOrderEvenWithZeroCooldown()
    {
        _movement.enabled = _combat.enabled = false;
        var original = AssetDatabase.LoadAssetAtPath<DemoGameplayConfig>("Assets/Game/DataTable/Demo/DemoGameplayConfig.asset");
        var copy = Object.Instantiate(original); copy.Player.dodgeCooldown = 0;
        var fields = new SerializedObject(_movement); fields.FindProperty("gameplayConfig").objectReferenceValue = copy; fields.ApplyModifiedProperties();
        int actions = 0; System.Action<DemoCombatPresentationEvent> listener = _ => actions++; _combat.ActionStarted += listener;
        try
        {
            for (int order = 0; order < 2; order++)
            {
                _world.Player.GetComponent<DemoRunFlowController>().RestartRun();
                var body = _world.Player.GetComponent<Rigidbody2D>(); Vector2 before = body.position;
                _input.State = new DemoInputState(Vector2.right, Vector2.zero, true, true, true, true, Vector2.up);
                if (order == 0) { Tick(_combat); Tick(_movement); } else { Tick(_movement); Tick(_combat); }
                float step = Mathf.Min(Time.deltaTime, copy.Player.dodgeDuration);
                Assert.That(body.position.x - before.x, Is.EqualTo(copy.Player.dodgeDistance * step / copy.Player.dodgeDuration).Within(.02f));
                Assert.That(_movement.IsInvulnerable, Is.True); Assert.That(actions, Is.Zero);
                Assert.That(_combat.TryStartAttack(), Is.False); Assert.That(_combat.TryUseSkill(), Is.False); Assert.That(_combat.TryUseUltimate(), Is.False);
                yield return null;
            }
        }
        finally
        {
            _combat.ActionStarted -= listener;
            fields = new SerializedObject(_movement); fields.FindProperty("gameplayConfig").objectReferenceValue = original; fields.ApplyModifiedProperties();
            Object.Destroy(copy);
        }
    }
    [UnityTest]
    public IEnumerator ObstacleStopsDodgeAndRemovingItDoesNotResumeDodge()
    {
        var body = _world.Player.GetComponent<Rigidbody2D>(); Vector2 before = body.position;
        _target = new GameObject("DodgeObstacle");
        _target.transform.position = (Vector3)(before + Vector2.right);
        var collider = _target.AddComponent<BoxCollider2D>(); collider.size = new Vector2(.1f, 3f);
        Physics2D.SyncTransforms();
        Assert.That(_movement.TryDodge(new DemoInputState(Vector2.right, Vector2.zero, false, true, false, false)), Is.True);
        yield return new WaitForSeconds(.15f);
        Assert.That(_movement.IsDodging, Is.False);
        Assert.That(body.position.x - before.x, Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
        Vector2 stopped = body.position;
        Object.Destroy(_target); _target = null;
        yield return new WaitForSeconds(.1f);
        Assert.That(body.position, Is.EqualTo(stopped));
    }
    [UnityTest]
    public IEnumerator ResetDuringDodgeDiscardsRemainingMovement()
    {
        var body = _world.Player.GetComponent<Rigidbody2D>();
        Assert.That(_movement.TryDodge(new DemoInputState(Vector2.right, Vector2.zero, false, true, false, false)), Is.True);
        yield return new WaitForSeconds(.05f);
        Assert.That(_movement.IsDodging, Is.True);
        _movement.ResetForRetry(); Vector2 stopped = body.position;
        yield return new WaitForSeconds(.25f);
        Assert.That(_movement.IsDodging, Is.False);
        Assert.That(body.position, Is.EqualTo(stopped));
    }
    [UnityTest]
    public IEnumerator PausingMidDodgeFreezesAndResumesRemainingDistance()
    {
        var body = _world.Player.GetComponent<Rigidbody2D>(); Vector2 before = body.position;
        Assert.That(_movement.TryDodge(new DemoInputState(Vector2.right, Vector2.zero, false, true, false, false)), Is.True);
        yield return new WaitForSeconds(.05f);
        var window = new Game.State.GameWindowState();
        var registration = _world.Pause.State.RegisterWindow(window);
        try
        {
            window.SetOpen(true); Vector2 paused = body.position;
            yield return new WaitForSecondsRealtime(.25f);
            Assert.That(body.position, Is.EqualTo(paused));
            Assert.That(_movement.IsDodging, Is.True);
            Assert.That(_movement.IsInvulnerable, Is.True);
            window.SetOpen(false);
            yield return new WaitForSeconds(.25f);
            Assert.That(_movement.IsDodging, Is.False);
            Assert.That(body.position.x - before.x, Is.EqualTo(2f).Within(.01f));
        }
        finally { window.SetOpen(false); registration.Dispose(); }
    }
    private DodgeDamageTarget CreateTarget()
    {
        _target = new GameObject("DodgeDamageTarget");
        _target.transform.position = _world.Player.transform.position + Vector3.up * .9f;
        var collider = _target.AddComponent<BoxCollider2D>(); collider.size = new Vector2(.1f, .1f); collider.isTrigger = true;
        var target = _target.AddComponent<DodgeDamageTarget>(); Physics2D.SyncTransforms(); return target;
    }
    private static void Tick(MonoBehaviour component) => component.GetType().GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(component, null);
}
public sealed class DodgeInputFixture : MonoBehaviour, IDemoInputSource
{
    public DemoInputState State;
    public DemoInputState ReadState() => State;
}
public sealed class DodgeDamageTarget : MonoBehaviour, IDemoDamageReceiver
{
    public float TotalDamage { get; private set; }
    public void ReceiveDamage(DemoDamageInfo damage) => TotalDamage += damage.Amount;
}
#endif
