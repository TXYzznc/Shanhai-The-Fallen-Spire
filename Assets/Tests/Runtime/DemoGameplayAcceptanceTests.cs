using System.Collections;
using NUnit.Framework;
using Shanhai.Demo.Build;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Input;
using Shanhai.Demo.Player;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
using Shanhai.Demo.Configuration;
using Shanhai.Demo.UI;
#endif

public sealed class DemoGameplayAcceptanceTests
{
    [UnitySetUp]
    public IEnumerator LoadDemoScene()
    {
        var world = Object.FindObjectOfType<DemoWorldEntry>();
        if (world == null)
        {
            yield return SceneManager.LoadSceneAsync("Launch");
            float deadline = Time.realtimeSinceStartup + 30f;
            DemoMenuEntry menu = null;
            while ((menu == null || !menu.IsReady) && Time.realtimeSinceStartup < deadline)
            { yield return null; menu = Object.FindObjectOfType<DemoMenuEntry>(); }
            Assert.That(menu != null && menu.IsReady, Is.True, "Launch未进入开始界面。");
            Assert.That(menu.RequestStart(), Is.True);
            while ((world == null || !world.IsReady) && Time.realtimeSinceStartup < deadline)
            { yield return null; world = Object.FindObjectOfType<DemoWorldEntry>(); }
        }
        Assert.That(world != null && world.IsReady, Is.True, "动态玩家/HUD会话未准备好。");
        var flow = world.Player.GetComponent<DemoRunFlowController>();
        // A preceding death case may still be inside the existing delayed auto-retry.
        // RestartRun deliberately refuses a second restart during that interval.
        float retryDeadline = Time.realtimeSinceStartup + 5f;
        while (flow.IsRetrying && Time.realtimeSinceStartup < retryDeadline) yield return null;
        Assert.That(flow.IsRetrying, Is.False, "前一用例的自动重试尚未结束。");
        flow.RestartRun();
        yield return null;
    }

    [UnityTest]
    public IEnumerator PlayerHealthRevivesOnceThenRequiresRetry()
    {
        DemoPlayerHealthController health = Object.FindObjectOfType<DemoPlayerHealthController>();
        Assert.That(health, Is.Not.Null);
        Assert.That(health.CurrentHealth, Is.EqualTo(100f).Within(0.01f));

        health.ReceiveDamage(new DemoDamageInfo(100f, health.gameObject));
        Assert.That(health.CurrentHealth, Is.EqualTo(50f).Within(0.01f));
        Assert.That(health.RevivesRemaining, Is.EqualTo(0));
        Assert.That(health.IsRetryRequired, Is.False);

        health.ReceiveDamage(new DemoDamageInfo(100f, health.gameObject));
        Assert.That(health.CurrentHealth, Is.EqualTo(0f).Within(0.01f));
        Assert.That(health.IsRetryRequired, Is.True);
        yield return null;
    }

    [UnityTest]
    public IEnumerator PlayerDodgeMovesAndSetsInvulnerability()
    {
        DemoPlayerController player = Object.FindObjectOfType<DemoPlayerController>();
        Assert.That(player, Is.Not.Null);
        Rigidbody2D body = player.GetComponent<Rigidbody2D>();
        Vector2 before = body.position;

        bool started = player.TryDodge(new DemoInputState(Vector2.up, Vector2.zero, false, true, false, false));
        Assert.That(player.IsInvulnerable, Is.True);
        Assert.That(started, Is.True);
        Assert.That(body.position, Is.EqualTo(before), "触发闪避不能直接瞬移。");
        yield return null;
        Assert.That(body.position.y, Is.GreaterThan(before.y));
    }

    [UnityTest]
    public IEnumerator LoadoutKeepsFourSlotsAndMutualExclusion()
    {
        DemoMarkLoadout loadout = Object.FindObjectOfType<DemoMarkLoadout>();
        Assert.That(loadout, Is.Not.Null);
        loadout.TryAcquire("beast_01_crack"); loadout.TryAcquire("beast_02_mountain"); loadout.TryAcquire("beast_03_shadow");
        Assert.That(loadout.TryEquip("beast_01_crack", 0), Is.True);
        Assert.That(loadout.TryEquip("beast_02_mountain", 0), Is.True);
        Assert.That(loadout.GetMarkId(0), Is.EqualTo("beast_02_mountain"));
        Assert.That(loadout.TryEquip("beast_03_shadow", 1), Is.True);
        Assert.That(loadout.TryEquip("beast_01_crack", 2), Is.True);
        Assert.That(loadout.TryEquip("beast_02_mountain", 3), Is.True);
        Assert.That(loadout.TryEquip("beast_03_shadow", 3), Is.True);
        Assert.That(loadout.GetMarkId(1), Is.Null.Or.Empty);
        Assert.That(loadout.GetMarkId(3), Is.EqualTo("beast_03_shadow"));
        Assert.That(loadout.SlotCount, Is.EqualTo(4));
        yield return null;
    }

#if UNITY_EDITOR
    [UnityTest]
    public IEnumerator DynamicEnemyStatusBindsAndClearsWhenHidden()
    {
        var world = Object.FindObjectOfType<DemoWorldEntry>();
        var config = AssetDatabase.LoadAssetAtPath<DemoGameplayConfig>("Assets/Game/DataTable/Demo/DemoGameplayConfig.asset");
        foreach (string id in new[] { "common_beast", "elite_beast", "fallen_spire_guardian" })
        {
            GameObject enemy = null;
            var lease = world.ShowEnemy(config, id, world.Player.transform.position + Vector3.right,
                Vector3.zero, Vector3.one, instance => enemy = instance, error => Assert.Fail(error));
            try
            {
                float deadline = Time.realtimeSinceStartup + 10f;
                while (enemy == null && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.That(enemy, Is.Not.Null, "动态敌人加载超时。");
                var binding = enemy.GetComponent<DemoEnemyStatusBinding>(); binding.Refresh();
                Assert.That(binding.IsBound, Is.True); Assert.That(binding.View, Is.Not.Null);
                Assert.That(binding.View.HealthRatio, Is.EqualTo(1f));
                Assert.That(binding.View.transform.parent, Is.SameAs(world.Ui.Overlay.EnemyStatusLayer));
                var view = binding.View;
                lease.Dispose(); yield return null;
                Assert.That(binding.IsBound, Is.False); Assert.That(view.gameObject.activeSelf, Is.False);
            }
            finally { lease.Dispose(); }
        }
    }
#endif
}
