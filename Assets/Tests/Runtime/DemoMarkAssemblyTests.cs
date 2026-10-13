#if UNITY_EDITOR
using System.Collections;
using Game.State;
using NUnit.Framework;
using Shanhai.Demo.Build;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Input;
using Shanhai.Demo.Player;
using Shanhai.Demo.UI;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class DemoMarkAssemblyTests
{
    private DemoWorldEntry _world;
    private DemoMarkLoadout _marks;
    [UnitySetUp]
    public IEnumerator Prepare()
    {
        yield return new DemoGameplayAcceptanceTests().LoadDemoScene();
        _world = Object.FindObjectOfType<DemoWorldEntry>(); _marks = _world.Player.GetComponent<DemoMarkLoadout>();
    }
    [UnityTearDown]
    public IEnumerator Cleanup()
    {
        if (_world != null) _world.Player.GetComponent<DemoRunFlowController>().RestartRun();
        yield return null;
    }
    [Test]
    public void AcquisitionDoesNotEquipAndDuplicateOnlyAwardsCurrencyOnce()
    {
        var choice = _world.Ui.RewardChoice; var flow = _world.Player.GetComponent<DemoRunFlowController>();
        choice.Open(); string id = choice.GetOption(0);
        Assert.That(choice.SelectOption(0), Is.True); Assert.That(_marks.HasMark(id), Is.True);
        Assert.That(_marks.EquippedCount, Is.Zero); Assert.That(flow.CurrentCurrency, Is.Zero);
        choice.Open(); Assert.That(choice.IsOwnedOption(0), Is.True); Assert.That(choice.SelectOption(0), Is.True);
        Assert.That(flow.CurrentCurrency, Is.EqualTo(choice.DuplicateCurrency)); Assert.That(_marks.OwnedCount, Is.EqualTo(1));
        Assert.That(choice.SelectOption(0), Is.False); Assert.That(flow.CurrentCurrency, Is.EqualTo(choice.DuplicateCurrency));
    }
    [Test]
    public void EquipRequiresOwnershipAndReplaceMoveUnequipRetainMarks()
    {
        Assert.That(_marks.TryEquip("beast_01_crack", 0), Is.False);
        _marks.TryAcquire("beast_01_crack"); _marks.TryAcquire("beast_02_mountain");
        Assert.That(_marks.TryEquip("beast_01_crack", 0), Is.True);
        Assert.That(_marks.TryEquip("beast_02_mountain", 1), Is.True);
        int changes = 0; System.Action changed = () => changes++; _marks.LoadoutChanged += changed;
        try { Assert.That(_marks.TryEquip("beast_02_mountain", 0), Is.True); Assert.That(changes, Is.EqualTo(1)); }
        finally { _marks.LoadoutChanged -= changed; }
        Assert.That(_marks.GetMarkId(1), Is.Null.Or.Empty); Assert.That(_marks.HasMark("beast_01_crack"), Is.True);
        _marks.Remove(0); Assert.That(_marks.OwnedCount, Is.EqualTo(2)); Assert.That(_marks.EquippedCount, Is.Zero);
    }
    [Test]
    public void RevivalRetainsOwnershipAndRestartClearsBoth()
    {
        _marks.TryAcquire("beast_01_crack"); _marks.TryEquip("beast_01_crack", 0);
        var health = _world.Player.GetComponent<DemoPlayerHealthController>(); health.ReceiveDamage(new DemoDamageInfo(100, null));
        Assert.That(health.RevivesRemaining, Is.Zero); Assert.That(_marks.HasMark("beast_01_crack"), Is.True);
        Assert.That(_marks.EquippedCount, Is.EqualTo(1)); _world.Player.GetComponent<DemoRunFlowController>().RestartRun();
        Assert.That(_marks.OwnedCount, Is.Zero); Assert.That(_marks.EquippedCount, Is.Zero);
    }
    [UnityTest]
    public IEnumerator AssemblySynchronizesHudAndPausesCombatAndCooldown()
    {
        _marks.TryAcquire("beast_01_crack"); _marks.TryAcquire("beast_02_mountain");
        var combat = _world.Player.GetComponent<DemoPlayerCombatController>();
        Assert.That(combat.TryUseSkill(), Is.True); float cooldown = combat.SkillCooldownRemaining;
        _world.Ui.ToggleAssembly();
        yield return DemoUiCompositionTests.WaitFor(() => _world.Ui.Assembly != null);
        var view = _world.Ui.Assembly;
        Assert.That(view.IsOpen, Is.True); Assert.That(_world.Pause.State.PauseCount, Is.EqualTo(1));
        view.SelectMark("beast_01_crack"); view.SelectSlot(0); view.Equip();
        Assert.That(view.GetSlot(0).ShownIcon, Is.SameAs(_marks.GetConfig("beast_01_crack").icon));
        var hudItem = _world.Hud.Display.Marks.GetSlot(0);
        Assert.That(hudItem.ShownIcon, Is.SameAs(view.GetSlot(0).ShownIcon));
        float health = _world.Player.GetComponent<DemoPlayerHealthController>().CurrentHealth;
        _world.Player.GetComponent<DemoPlayerHealthController>().ReceiveDamage(new DemoDamageInfo(10, null));
        Assert.That(_world.Player.GetComponent<DemoPlayerHealthController>().CurrentHealth, Is.EqualTo(health));
        Assert.That(combat.TryStartAttack(), Is.False);
        Assert.That(_world.Player.GetComponent<DemoPlayerController>().TryDodge(new DemoInputState(Vector2.up, Vector2.zero, false, true, false, false)), Is.False);
        yield return new WaitForSecondsRealtime(.12f);
        Assert.That(combat.SkillCooldownRemaining, Is.EqualTo(cooldown).Within(.001));
        view.Unequip(); Assert.That(hudItem.IsEmpty, Is.True); Assert.That(_marks.OwnedCount, Is.EqualTo(2));
        view.Hide(); Assert.That(_world.Pause.State.PauseCount, Is.Zero); Assert.That(Time.timeScale, Is.EqualTo(1));
    }
    [UnityTest]
    public IEnumerator RewardWindowPausesAndModalTabInputCannotOpenAssembly()
    {
        _world.Ui.Open(); Assert.That(Time.timeScale, Is.Zero);
        Assert.That(_world.Pause.State.PauseCount, Is.EqualTo(1));
        _world.Ui.ToggleAssembly(); Assert.That(_world.Ui.IsAssemblyRequested, Is.False);
        yield return DemoUiCompositionTests.WaitFor(() => _world.Ui.RewardView != null);
        Assert.That(_world.Ui.RewardView.transform.Find("RewardChoicePanel/RewardSlot_0"), Is.Null);
        _world.Ui.Hide(); _world.Ui.RewardChoice.ResetForRetry();
        Assert.That(_world.Pause.State.PauseCount, Is.Zero);
        _world.Ui.OpenBuffs(); Assert.That(Time.timeScale, Is.Zero);
        _world.Player.GetComponent<DemoRunFlowController>().RestartRun(); yield return null;
        Assert.That(_world.Pause.State.PauseCount, Is.Zero); Assert.That(Time.timeScale, Is.EqualTo(1));
    }
    [UnityTest]
    public IEnumerator SemanticTabAndEscapeOperateWhilePaused()
    {
        var provider = _world.gameObject.AddComponent<AssemblyInputFixture>();
        _world.Ui.BindInput(provider);
        provider.State = new DemoInputState(Vector2.zero, Vector2.zero, false, false, false, false, default, true);
        yield return null; yield return null;
        yield return DemoUiCompositionTests.WaitFor(() => _world.Ui.Assembly != null);
        Assert.That(_world.Ui.Assembly.IsOpen, Is.True); Assert.That(Time.timeScale, Is.Zero);
        provider.State = new DemoInputState(Vector2.zero, Vector2.zero, false, false, false, false, default, false, true);
        yield return null; yield return null;
        Assert.That(_world.Ui.IsAssemblyRequested, Is.False); Assert.That(Time.timeScale, Is.EqualTo(1));
        _world.Ui.BindInput(null); Object.Destroy(provider);
    }
}
public sealed class AssemblyInputFixture : MonoBehaviour, IDemoInputSource
{
    public DemoInputState State;
    public DemoInputState ReadState() { var state = State; State = default; return state; }
}
#endif
