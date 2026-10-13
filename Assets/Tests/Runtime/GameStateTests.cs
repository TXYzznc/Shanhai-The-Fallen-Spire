using System;
using Game.State;
using NUnit.Framework;
using UnityEngine;

public sealed class GameStateTests
{
    [Test]
    public void RegisteredWindowsPauseUntilLastClose()
    {
        using (var state = new GameStateManager())
        {
            var a = new GameWindowState(); var b = new GameWindowState();
            using (state.RegisterWindow(a)) using (state.RegisterWindow(b))
            {
                Assert.That(state.PauseCount, Is.Zero); a.SetOpen(true); b.SetOpen(true);
                Assert.That(state.PauseCount, Is.EqualTo(2)); a.SetOpen(false);
                Assert.That(state.PauseCount, Is.EqualTo(1)); Assert.That(state.IsPaused, Is.True);
                b.SetOpen(false); Assert.That(state.IsPaused, Is.False);
            }
        }
    }
    [Test]
    public void RepeatedOpenCloseAndUnregisterAreIdempotent()
    {
        using (var state = new GameStateManager())
        {
            var window = new GameWindowState(); var registration = state.RegisterWindow(window);
            window.SetOpen(true); window.SetOpen(true); Assert.That(state.PauseCount, Is.EqualTo(1));
            registration.Dispose(); registration.Dispose(); window.SetOpen(false); window.SetOpen(false);
            Assert.That(state.PauseCount, Is.Zero);
            using (state.RegisterWindow(window)) Assert.That(state.IsPaused, Is.False);
        }
    }
    [Test]
    public void StateEventsOnlyPublishTransitionsAndLateOwnersCannotAffectNewSession()
    {
        var state = new GameStateManager(); int events = 0;
        state.StateChanged += (sender, args) => { events++; Assert.That(args.IsPaused, Is.EqualTo(state.IsPaused)); };
        var a = new GameWindowState(); var b = new GameWindowState();
        var first = state.RegisterWindow(a); var second = state.RegisterWindow(b);
        a.SetOpen(true); b.SetOpen(true); a.SetOpen(false); Assert.That(events, Is.EqualTo(1));
        state.Dispose(); Assert.That(events, Is.EqualTo(2)); Assert.That(state.PauseCount, Is.Zero);
        first.Dispose(); second.Dispose(); b.SetOpen(true);
        Assert.That(state.PauseCount, Is.Zero); Assert.Throws<ObjectDisposedException>(() => state.RegisterWindow(a));
    }
    [Test]
    public void DuplicateRegistrationIsRejectedWithoutChangingCount()
    {
        using (var state = new GameStateManager())
        {
            var window = new GameWindowState(); window.SetOpen(true);
            using (state.RegisterWindow(window))
            {
                Assert.Throws<InvalidOperationException>(() => state.RegisterWindow(window));
                Assert.That(state.PauseCount, Is.EqualTo(1));
            }
            Assert.That(state.PauseCount, Is.Zero);
        }
    }
    [Test]
    public void DisabledAndDestroyedWindowRestoreOriginalTimeScale()
    {
        float previous = Time.timeScale; var root = new GameObject("PauseTest"); var windowObject = new GameObject("WindowTest");
        try
        {
            Time.timeScale = .75f; var adapter = root.AddComponent<GamePauseController>();
            var window = windowObject.AddComponent<GamePauseWindow>(); window.Bind(adapter.State);
            Assert.That(adapter.State.PauseCount, Is.EqualTo(1)); Assert.That(Time.timeScale, Is.Zero);
            windowObject.SetActive(false); Assert.That(Time.timeScale, Is.EqualTo(.75f));
            windowObject.SetActive(true); Assert.That(Time.timeScale, Is.Zero);
            UnityEngine.Object.DestroyImmediate(windowObject); Assert.That(adapter.State.PauseCount, Is.Zero);
            Assert.That(Time.timeScale, Is.EqualTo(.75f));
        }
        finally { UnityEngine.Object.DestroyImmediate(windowObject); UnityEngine.Object.DestroyImmediate(root); Time.timeScale = previous; }
    }
}
