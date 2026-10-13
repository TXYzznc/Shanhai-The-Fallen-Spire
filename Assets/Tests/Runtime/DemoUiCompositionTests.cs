#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Shanhai.Demo.Build;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Configuration;
using Shanhai.Demo.Player;
using Shanhai.Demo.UI;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEditor;
using TMPro;
using UnityEngine.UI;

public sealed class DemoUiCompositionTests
{
    [UnityTest]
    public IEnumerator DisplayAreasTrackVitalsCurrencyBuffsAndRestart()
    {
        yield return new DemoGameplayAcceptanceTests().LoadDemoScene();
        var ui = UnityEngine.Object.FindObjectOfType<DemoWorldEntry>().Ui;
        var vitals = ui.Hud.Display.Vitals; var runInfo = ui.Hud.Display.RunInfo;
        var combat = ui.Player.GetComponent<DemoPlayerCombatController>();
        Assert.That(combat.TryUseSkill(), Is.True); vitals.Refresh();
        StringAssert.Contains("技能冷却", vitals.transform.Find("SkillStatusText").GetComponent<TMP_Text>().text);
        var health = ui.Player.GetComponent<DemoPlayerHealthController>(); health.ReceiveDamage(new DemoDamageInfo(25, null));
        vitals.Refresh();
        StringAssert.Contains("75/100", vitals.transform.Find("StatusText").GetComponent<TMP_Text>().text);
        Assert.That(((RectTransform)vitals.transform.Find("HealthBar/Fill")).anchorMax.x, Is.EqualTo(.75f).Within(.001f));
        ui.Run.AddCurrency(55); ui.Player.GetComponent<DemoBuffLoadout>().TryApplyBuff("buff_health");
        vitals.Refresh(); runInfo.Refresh();
        StringAssert.Contains("/115", vitals.transform.Find("StatusText").GetComponent<TMP_Text>().text);
        StringAssert.Contains("货币 55", runInfo.transform.Find("RunSummary").GetComponent<TMP_Text>().text);
        StringAssert.Contains("已获增益 1项", runInfo.transform.Find("CheckpointStatusText").GetComponent<TMP_Text>().text);
        ui.ShowNotice("测试通知"); ui.Run.RestartRun(); vitals.Refresh(); runInfo.Refresh();
        StringAssert.Contains("100/100", vitals.transform.Find("StatusText").GetComponent<TMP_Text>().text);
        StringAssert.Contains("技能就绪", vitals.transform.Find("SkillStatusText").GetComponent<TMP_Text>().text);
        StringAssert.Contains("货币 0", runInfo.transform.Find("RunSummary").GetComponent<TMP_Text>().text);
        Assert.That(runInfo.transform.Find("RewardNotice").GetComponent<TMP_Text>().text, Is.Empty);
    }
    [UnityTest]
    public IEnumerator BossRewardFinishesBeforeCompletionAndRetryClosesAllWindows()
    {
        yield return new DemoGameplayAcceptanceTests().LoadDemoScene();
        var world = UnityEngine.Object.FindObjectOfType<DemoWorldEntry>(); var ui = world.Ui;
        var boss = (Shanhai.Demo.Enemies.DemoEncounterController)typeof(DemoUiSession).GetField("_boss", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(ui);
        // Enter the real encounter and defeat its real entity; no OS input is used.
        ui.Player.GetComponent<Rigidbody2D>().position = boss.transform.position; Physics2D.SyncTransforms();
        yield return WaitFor(() => boss.Enemy != null);
        boss.Enemy.GetComponent<DemoDamageReceiver>().ReceiveDamage(new DemoDamageInfo(100000, ui.Player));
        yield return WaitFor(() => ui.RewardView != null);
        Assert.That(ui.IsCompletionRequested, Is.False); Assert.That(ui.Pause.State.PauseCount, Is.EqualTo(1));
        var button = ui.RewardView.transform.Find("RewardChoicePanel/RewardOption_1").GetComponent<Button>();
        button.onClick.Invoke(); Assert.That(ui.RewardChoice.IsOpen, Is.True);
        button.onClick.Invoke(); Assert.That(ui.IsRewardRequested, Is.False);
        yield return WaitFor(() => ui.Completion != null);
        Assert.That(ui.Completion.IsOpen, Is.True); Assert.That(ui.Pause.State.PauseCount, Is.EqualTo(1));
        ui.Completion.transform.Find("CompletionPanel/Retry").GetComponent<Button>().onClick.Invoke();
        Assert.That(ui.IsCompletionRequested, Is.False); Assert.That(ui.IsRewardRequested, Is.False);
        Assert.That(ui.Pause.State.PauseCount, Is.Zero); Assert.That(ui.Run.CurrentCurrency, Is.Zero);
    }
    [UnityTest]
    public IEnumerator FailedWindowLoadReleasesPauseAndCanRetry()
    {
        yield return new DemoGameplayAcceptanceTests().LoadDemoScene();
        var ui = UnityEngine.Object.FindObjectOfType<DemoWorldEntry>().Ui;
        var configField = typeof(DemoUiSession).GetField("_config", BindingFlags.Instance | BindingFlags.NonPublic);
        var original = (DemoPresentationConfig)configField.GetValue(ui);
        var broken = UnityEngine.Object.Instantiate(original);
        var fields = new SerializedObject(broken); fields.FindProperty("rewardPrefabAsset").stringValue = "Assets/Game/Prefabs/UI/MissingUiTest.prefab"; fields.ApplyModifiedProperties();
        try
        {
            configField.SetValue(ui, broken);
            LogAssert.Expect(LogType.Error, new Regex(@"\[Demo.UI\] 窗口加载失败："));
            ui.Open();
            yield return WaitFor(() => !ui.IsRewardRequested);
            Assert.That(ui.Pause.State.PauseCount, Is.Zero); Assert.That(Time.timeScale, Is.EqualTo(1));
            Assert.That(ui.RewardChoice.IsOpen, Is.False);
            configField.SetValue(ui, original); ui.Open();
            yield return WaitFor(() => ui.RewardView != null);
            Assert.That(ui.Pause.State.PauseCount, Is.EqualTo(1));
        }
        finally { configField.SetValue(ui, original); ui.Hide(); ui.RewardChoice.ResetForRetry(); UnityEngine.Object.Destroy(broken); }
    }
    [Test]
    public void SourcePrefabsHaveExplicitReferencesAndIndependentBoundaries()
    {
        const string root = "Assets/Game/Prefabs/UI/";
        foreach (string name in new[] { "DemoHud", "DemoCombatOverlay", "DemoRewardChoice", "DemoMarkAssembly", "DemoCompletion",
            "Item/DemoHudVitals", "Item/DemoHudRunInfo", "Item/DemoHudMarkSummary" })
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(root + name + ".prefab"); Assert.That(prefab, Is.Not.Null, name);
            foreach (var component in prefab.GetComponentsInChildren<Component>(true)) Assert.That(component, Is.Not.Null, name + " Missing Script");
        }
        var hud = AssetDatabase.LoadAssetAtPath<GameObject>(root + "DemoHud.prefab").GetComponent<DemoHudForm>();
        Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(hud.Display.Vitals.gameObject), Is.EqualTo(root + "Item/DemoHudVitals.prefab"));
        Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(hud.Display.RunInfo.gameObject), Is.EqualTo(root + "Item/DemoHudRunInfo.prefab"));
        Assert.That(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(hud.Display.Marks.gameObject), Is.EqualTo(root + "Item/DemoHudMarkSummary.prefab"));
        Assert.That(hud.GetComponent<DemoRewardChoiceView>(), Is.Null); Assert.That(hud.transform.Find("DamageLayer"), Is.Null);
    }
    public static IEnumerator WaitFor(Func<bool> condition)
    {
        float until = Time.realtimeSinceStartup + 10;
        while (!condition() && Time.realtimeSinceStartup < until) yield return null;
        Assert.That(condition(), Is.True, "独立窗口等待超时。");
    }
    [UnityTest]
    public IEnumerator HudContainsOnlyDisplayAreasAndWindowsAreIndependent()
    {
        yield return new DemoGameplayAcceptanceTests().LoadDemoScene();
        var world = UnityEngine.Object.FindObjectOfType<DemoWorldEntry>(); var ui = world.Ui;
        Assert.That(ui.Hud.transform.Find("RewardChoicePanel"), Is.Null);
        Assert.That(ui.Hud.transform.Find("CompletionPanel"), Is.Null);
        Assert.That(ui.Hud.GetComponent<DemoRewardChoiceController>(), Is.Null);
        Assert.That(ui.Hud.Display.Vitals, Is.Not.Null); Assert.That(ui.Hud.Display.RunInfo, Is.Not.Null);
        Assert.That(ui.Hud.Display.Marks, Is.Not.Null); Assert.That(ui.Overlay, Is.Not.Null);
        Assert.That(ui.Overlay.transform.IsChildOf(ui.Hud.transform), Is.False);
        ui.Open(); Assert.That(Time.timeScale, Is.Zero);
        yield return WaitFor(() => ui.RewardView != null);
        Assert.That(ui.RewardView.GetComponent<DemoRewardForm>().Available, Is.True);
        Assert.That(ui.RewardView.transform.IsChildOf(ui.Hud.transform), Is.False);
        Assert.That(world.Pause.State.PauseCount, Is.EqualTo(1)); ui.Hide(); ui.RewardChoice.ResetForRetry();
        Assert.That(world.Pause.State.PauseCount, Is.Zero);
    }
    [UnityTest]
    public IEnumerator LoadingCanBeCancelledAndPooledFormsRebindWithoutDuplicatePauses()
    {
        yield return new DemoGameplayAcceptanceTests().LoadDemoScene();
        var ui = UnityEngine.Object.FindObjectOfType<DemoWorldEntry>().Ui;
        ui.ToggleAssembly(); Assert.That(ui.IsAssemblyRequested, Is.True); Assert.That(Time.timeScale, Is.Zero);
        ui.Run.RestartRun(); Assert.That(ui.IsAssemblyRequested, Is.False); Assert.That(Time.timeScale, Is.EqualTo(1));
        yield return null; yield return null;
        Assert.That(ui.Assembly, Is.Null);
        for (int i = 0; i < 3; i++)
        {
            ui.ToggleAssembly(); yield return WaitFor(() => ui.Assembly != null);
            Assert.That(ui.Pause.State.PauseCount, Is.EqualTo(1));
            ui.Assembly.Hide(); Assert.That(ui.Pause.State.PauseCount, Is.Zero);
        }
        ui.OpenBuffs(); Assert.That(ui.Pause.State.PauseCount, Is.EqualTo(1));
        ui.Run.RestartRun(); yield return null;
        Assert.That(ui.IsRewardRequested, Is.False); Assert.That(ui.RewardChoice.IsOpen, Is.False);
        Assert.That(ui.Pause.State.PauseCount, Is.Zero);
    }
    [UnityTest]
    public IEnumerator SessionExitClosesFormsAndReentryUsesFreshBindings()
    {
        yield return new DemoGameplayAcceptanceTests().LoadDemoScene();
        var world = UnityEngine.Object.FindObjectOfType<DemoWorldEntry>(); var ui = world.Ui;
        ui.Open(); yield return WaitFor(() => ui.RewardView != null);
        var oldReward = ui.RewardView; var oldHud = ui.Hud; var oldOverlay = ui.Overlay;
        var config = AssetDatabase.LoadAssetAtPath<DemoPresentationConfig>("Assets/Game/ScriptableAssets/Demo/DemoPresentationConfig.asset");
        GF.Scene.UnloadScene(config.GameSceneAsset);
        yield return WaitFor(() => world == null);
        Assert.That(oldReward.GetComponent<DemoRewardForm>().Available, Is.False);
        Assert.That(oldHud.Available, Is.False); Assert.That(oldOverlay.Available, Is.False);
        Assert.That(Time.timeScale, Is.EqualTo(1));
        GF.Scene.LoadScene(config.MenuSceneAsset, "Demo.UI.UnitTest");
        yield return WaitFor(() => UnityEngine.Object.FindObjectOfType<DemoMenuEntry>() != null && UnityEngine.Object.FindObjectOfType<DemoMenuEntry>().IsReady);
        Assert.That(UnityEngine.Object.FindObjectOfType<DemoMenuEntry>().RequestStart(), Is.True);
        yield return WaitFor(() => UnityEngine.Object.FindObjectOfType<DemoWorldEntry>() != null && UnityEngine.Object.FindObjectOfType<DemoWorldEntry>().IsReady);
        var next = UnityEngine.Object.FindObjectOfType<DemoWorldEntry>().Ui;
        Assert.That(next, Is.Not.SameAs(ui)); Assert.That(next.RewardChoice.IsOpen, Is.False);
        Assert.That(next.Pause.State.PauseCount, Is.Zero);
        next.ToggleAssembly(); yield return WaitFor(() => next.Assembly != null);
        next.Assembly.Hide(); Assert.That(next.Pause.State.PauseCount, Is.Zero);
    }
}
#endif
