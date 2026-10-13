#if UNITY_EDITOR
using System.Collections;
using NUnit.Framework;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Configuration;
using Shanhai.Demo.Enemies;
using Shanhai.Demo.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class DemoEnemyStatusTests
{
    private GameObject _actor, _canvas, _cameraObject, _target;
    private DemoEnemyStatusBinding _binding;
    private DemoDamageReceiver _health;
    private DemoEnemyController _enemy;
    private Camera _camera;
    private DemoGameplayConfig _config;

    [SetUp]
    public void SetUp()
    {
        _config = AssetDatabase.LoadAssetAtPath<DemoGameplayConfig>("Assets/Game/DataTable/Demo/DemoGameplayConfig.asset");
        _actor = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Entity/DemoCommonEnemy.prefab"));
        _actor.transform.position = Vector3.zero;
        _health = _actor.GetComponent<DemoDamageReceiver>(); _enemy = _actor.GetComponent<DemoEnemyController>();
        _binding = _actor.GetComponent<DemoEnemyStatusBinding>();
        _enemy.BindConfiguration(_config, "common_beast");
        _cameraObject = new GameObject("StatusTestCamera"); _camera = _cameraObject.AddComponent<Camera>();
        _camera.orthographic = true; _camera.orthographicSize = 10; _camera.transform.position = new Vector3(0, 0, -10);
        _canvas = new GameObject("StatusTestCanvas", typeof(RectTransform), typeof(Canvas));
        _canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        Canvas.ForceUpdateCanvases(); _binding.Bind((RectTransform)_canvas.transform, _camera);
    }
    [TearDown]
    public void TearDown()
    {
        Object.DestroyImmediate(_actor); Object.DestroyImmediate(_canvas);
        Object.DestroyImmediate(_cameraObject); if (_target != null) Object.DestroyImmediate(_target);
    }
    [Test]
    public void HealthTracksActualDamageAndDeadEnemyHides()
    {
        Assert.That(_binding.View.HealthRatio, Is.EqualTo(1f));
        _health.ReceiveDamage(new DemoDamageInfo(10, _actor)); _binding.Refresh();
        Assert.That(_binding.View.HealthRatio, Is.EqualTo(50f / 60f).Within(.001f));
        _health.ReceiveDamage(new DemoDamageInfo(0, _actor)); _binding.Refresh();
        Assert.That(_binding.View.HealthRatio, Is.EqualTo(50f / 60f).Within(.001f));
        _health.ReceiveDamage(new DemoDamageInfo(1000, _actor)); _binding.Refresh();
        Assert.That(_binding.View.gameObject.activeSelf, Is.False);
        _health.ResetHealth(); _binding.Refresh();
        Assert.That(_binding.View.HealthRatio, Is.EqualTo(1f));
        foreach (Graphic graphic in _binding.View.GetComponentsInChildren<Graphic>(true))
            Assert.That(graphic.raycastTarget, Is.False, "状态条不得阻挡奖励或战斗输入。");
    }
    [UnityTest]
    public IEnumerator StaggerWindowAndRecoveryUseExistingTimers()
    {
        _health.ConfigureStagger(30, .06f, .08f, 1.5f);
        _health.ReceiveDamage(new DemoDamageInfo(10, _actor)); _binding.Refresh();
        Assert.That(_binding.View.StaggerRatio, Is.EqualTo(1f / 3f).Within(.001f));
        Assert.That(_binding.View.Status, Is.EqualTo("硬直"));
        yield return new WaitForSeconds(.09f); _binding.Refresh();
        Assert.That(_binding.View.StaggerRatio, Is.EqualTo(0));
        _health.ReceiveDamage(new DemoDamageInfo(30, _actor)); _binding.Refresh();
        Assert.That(_binding.View.Status, Is.EqualTo("失衡恢复"));
        Assert.That(_binding.View.StaggerRatio, Is.GreaterThan(0));
        yield return new WaitForSeconds(.11f); _binding.Refresh();
        Assert.That(_health.IsStaggered, Is.False);
        Assert.That(_binding.View.StaggerRatio, Is.EqualTo(0));
        _health.ConfigureStagger(0, 3, 2, 1.5f); _binding.Refresh();
        Assert.That(_binding.View.ShowsStagger, Is.False);
    }
    [Test]
    public void OffscreenUnbindReuseAndDestroyedHudAreSafe()
    {
        var view = _binding.View;
        _actor.transform.position = new Vector3(1000, 0, 0); _binding.Refresh();
        Assert.That(view.gameObject.activeSelf, Is.False);
        _actor.transform.position = Vector3.zero; _binding.Refresh(); Assert.That(view.gameObject.activeSelf, Is.True);
        _binding.Unbind(); Assert.That(_binding.IsBound, Is.False); Assert.That(view.gameObject.activeSelf, Is.False);
        _binding.Bind((RectTransform)_canvas.transform, _camera); Assert.That(_binding.View, Is.SameAs(view));
        Object.DestroyImmediate(_canvas); _canvas = null;
        Assert.DoesNotThrow(_binding.Unbind); Assert.That(_binding.IsBound, Is.False);
    }
    [UnityTest]
    public IEnumerator EliteChargeAndBossPhaseAreShown()
    {
        _enemy.BindConfiguration(_config, "elite_beast");
        _target = new GameObject("ChargeTarget"); _target.transform.position = Vector3.up * 3f;
        _target.AddComponent<DemoPlayerHealthController>(); _enemy.BindTarget(_target.transform); _enemy.enabled = true;
        yield return null; _binding.Refresh();
        Assert.That(_enemy.IsCharging, Is.True); Assert.That(_binding.View.ShowsCharge, Is.True);
        Assert.That(_binding.View.Status, Is.EqualTo("蓄力"));
        yield return new WaitForSeconds(.12f); _binding.Refresh();
        Assert.That(_binding.View.ChargeRatio, Is.GreaterThan(0));
        _enemy.enabled = false; _enemy.BindTarget(null); _enemy.BindConfiguration(_config, "fallen_spire_guardian");
        _health.ConfigureStagger(0, 3, 2, 1.5f); _health.ReceiveDamage(new DemoDamageInfo(450, _actor));
        yield return new WaitForSeconds(.2f); _binding.Refresh();
        Assert.That(_binding.View.Status, Is.EqualTo("二阶段")); Assert.That(_binding.View.ShowsCharge, Is.False);
    }
}
#endif
