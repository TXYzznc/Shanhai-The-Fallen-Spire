using System;
using Shanhai.Demo.Player;
using Shanhai.Demo.QA;
using UnityEditor;
using UnityEngine;

namespace Shanhai.Demo.Editor
{
    internal static class DemoContinuousCombatRunner
    {
        private static DemoContinuousCombatAcceptanceProbe _probe;
        private static double _deadline;
        [MenuItem("Shanhai/Demo/Verify continuous dynamic combat")]
        private static void Begin()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("先进入游戏。");
            var world = UnityEngine.Object.FindObjectOfType<DemoWorldEntry>();
            if (world == null || !world.IsReady || _probe != null) throw new InvalidOperationException("本局未准备好或已有连续验收。");
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            _probe = world.Player.AddComponent<DemoContinuousCombatAcceptanceProbe>(); _deadline = EditorApplication.timeSinceStartup + 180;
            EditorApplication.update += Tick; EditorApplication.playModeStateChanged += Changed;
            AssemblyReloadEvents.beforeAssemblyReload += Stop;
        }
        private static void Tick()
        {
            if (_probe == null) { Stop(); return; }
            if (_probe.Finished)
            {
                if (_probe.Completed) Debug.Log("[Demo.ContinuousDynamicQA] PASS 12项默认数值/AI连续语义输入验收；" + _probe.Details);
                else Debug.LogError("[Demo.ContinuousDynamicQA] FAIL " + $"start={_probe.StartsAtMapStart},waves={_probe.FirstWaveCleared}/{_probe.SecondWaveCleared},checkpoint={_probe.CheckpointUpgradeByUi}/{_probe.CheckpointContainsCompletedWaves},elite={_probe.EliteDefeated}/{_probe.EliteMarkByUi},boss={_probe.BossDefeated}/{_probe.BossPhaseTwoObserved}/{_probe.BossMarkByUi},completion={_probe.CompletionAndCurrency},retry={_probe.RetryButtonRestoresWorld}" + "; " + _probe.Details);
                Stop();
            }
            else if (EditorApplication.timeSinceStartup > _deadline) { Debug.LogError("[Demo.ContinuousDynamicQA] 超时 stage=" + _probe.Stage); Stop(); }
        }
        private static void Changed(PlayModeStateChange state) { if (state == PlayModeStateChange.ExitingPlayMode) Stop(); }
        private static void Stop()
        {
            EditorApplication.update -= Tick; EditorApplication.playModeStateChanged -= Changed; AssemblyReloadEvents.beforeAssemblyReload -= Stop;
            if (_probe != null) UnityEngine.Object.DestroyImmediate(_probe); _probe = null;
        }
    }
}
