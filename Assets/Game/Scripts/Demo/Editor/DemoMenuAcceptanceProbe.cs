using System;
using System.Collections.Generic;
using Shanhai.Demo.Player;
using Shanhai.Demo.UI;
using Shanhai.Demo.Configuration;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Shanhai.Demo.Editor
{
    internal static class DemoMenuAcceptanceProbe
    {
        private static double _deadline;
        private static int _stage;
        private static int _readyFrame;
        private static bool _failureCase;
        private static bool _cancelCase;
        private static DemoPresentationConfig _original, _temporary;
        private static DemoMenuEntry _entry;
        [MenuItem("Shanhai/Demo/Verify start menu cancellation")]
        private static void RunCancellation()
        {
            Run(); _cancelCase = true;
        }
        [MenuItem("Shanhai/Demo/Verify start menu failure recovery")]
        private static void RunFailure()
        {
            Run();
            _failureCase = true;
        }
        [MenuItem("Shanhai/Demo/Verify start menu flow")]
        private static void Run()
        {
            if (!EditorApplication.isPlaying) throw new InvalidOperationException("从Launch进入Play Mode后执行。");
            _deadline = EditorApplication.timeSinceStartup + 45;
            _stage = 0;
            _readyFrame = -1;
            EditorApplication.ExecuteMenuItem("Window/General/Game");
            _failureCase = false;
            _cancelCase = false;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged -= Changed;
            EditorApplication.playModeStateChanged += Changed;
            AssemblyReloadEvents.beforeAssemblyReload -= Stop;
            AssemblyReloadEvents.beforeAssemblyReload += Stop;
        }
        private static bool Loaded(string name)
        {
            Scene scene = SceneManager.GetSceneByName(name);
            return scene.IsValid() && scene.isLoaded;
        }
        private static void Tick()
        {
            try
            {
                if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup > _deadline)
                    throw new InvalidOperationException("开始界面流程验证超时或Play Mode已停止。");
                if (_stage == 0)
                {
                    DemoMenuEntry entry = UnityEngine.Object.FindObjectOfType<DemoMenuEntry>();
                    DemoStartMenuForm menu = UnityEngine.Object.FindObjectOfType<DemoStartMenuForm>();
                    if (entry == null || menu == null || !entry.IsReady || !menu.Interactable) return;
                    if (_readyFrame < 0) _readyFrame = Time.frameCount + 2;
                    if (Time.frameCount < _readyFrame) return;
                    Canvas.ForceUpdateCanvases();
                    _entry = entry;
                    if (!Loaded("DemoMenu") || Loaded("DemoMain")) throw new InvalidOperationException("点击前场景状态不符合菜单流程。");
                    if (_cancelCase)
                    {
                        if (!entry.RequestStart()) throw new InvalidOperationException("取消用例未提交加载请求。");
                        UnityEngine.Object.DestroyImmediate(entry.gameObject);
                        _stage = 3;
                        return;
                    }
                    if (_failureCase)
                    {
                        var fields = new SerializedObject(entry);
                        _original = (DemoPresentationConfig)fields.FindProperty("presentation").objectReferenceValue;
                        _temporary = UnityEngine.Object.Instantiate(_original);
                        var settings = new SerializedObject(_temporary);
                        settings.FindProperty("gameSceneAsset").stringValue = "Assets/Game/Scene/Demo/QA_MissingScene.unity";
                        settings.ApplyModifiedPropertiesWithoutUndo();
                        fields.FindProperty("presentation").objectReferenceValue = _temporary;
                        fields.ApplyModifiedPropertiesWithoutUndo();
                        if (!entry.RequestStart()) throw new InvalidOperationException("失败用例未提交加载请求。");
                        _stage = 2;
                        return;
                    }
                    Button button = menu.transform.Find("StartGame").GetComponent<Button>();
                    var pointer = new PointerEventData(EventSystem.current);
                    var corners = new Vector3[4];
                    ((RectTransform)button.transform).GetWorldCorners(corners);
                    pointer.position = RectTransformUtility.WorldToScreenPoint(null, (corners[0] + corners[2]) * .5f);
                    var hits = new List<RaycastResult>();
                    EventSystem.current.RaycastAll(pointer, hits);
                    bool hit = hits.Count > 0 && (hits[0].gameObject == button.gameObject || hits[0].gameObject.transform.IsChildOf(button.transform));
                    if (!hit) throw new InvalidOperationException("开始按钮未命中UGUI射线。");
                    ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
                    ExecuteEvents.Execute(button.gameObject, pointer, ExecuteEvents.pointerClickHandler);
                    if (entry.AcceptedStarts != 1 || !entry.IsStarting || button.interactable)
                        throw new InvalidOperationException("重复点击保护或加载反馈失效。");
                    _stage = 1;
                    Debug.Log("[Demo.MenuQA] 菜单先于游戏加载；UGUI射线及双击防重入通过。");
                }
                else if (_stage == 3)
                {
                    const string asset = "Assets/Game/Scene/Demo/DemoMain.unity";
                    if (GF.Scene.SceneIsLoading(asset) || GF.Scene.SceneIsUnloading(asset)) return;
                    if (Loaded("DemoMain") || UnityEngine.Object.FindObjectOfType<DemoStartMenuForm>() != null)
                        throw new InvalidOperationException("取消加载后残留游戏场景或菜单UI。");
                    Stop();
                    Debug.Log("[Demo.MenuQA] PASS：加载中销毁菜单入口，迟到场景卸载，UI已关闭。");
                }
                else if (_stage == 2)
                {
                    var entry = UnityEngine.Object.FindObjectOfType<DemoMenuEntry>();
                    var menu = UnityEngine.Object.FindObjectOfType<DemoStartMenuForm>();
                    if (entry == null || menu == null) throw new InvalidOperationException("加载失败破坏了菜单。");
                    if (entry.IsStarting) return;
                    if (!menu.StartEnabled || string.IsNullOrEmpty(menu.Status) || Loaded("DemoMain"))
                        throw new InvalidOperationException("加载失败未恢复开始按钮和错误提示。");
                    var fields = new SerializedObject(entry);
                    fields.FindProperty("presentation").objectReferenceValue = _original;
                    fields.ApplyModifiedPropertiesWithoutUndo();
                    UnityEngine.Object.DestroyImmediate(_temporary); _temporary = null; _original = null;
                    if (!entry.RequestStart() || entry.RequestStart() || entry.AcceptedStarts != 2)
                        throw new InvalidOperationException("失败后重试或防重入失效。");
                    _stage = 1;
                    Debug.Log("[Demo.MenuQA] 加载失败后恢复按钮/提示，随后重试成功提交；缺失场景错误为本用例预期。");
                }
                else if (Loaded("DemoMain") && !Loaded("DemoMenu"))
                {
                    if (UnityEngine.Object.FindObjectOfType<DemoStartMenuForm>() != null)
                        throw new InvalidOperationException("游戏加载后菜单仍激活。");
                    Stop();
                    Debug.Log("[Demo.MenuQA] PASS：Launch→菜单→游戏，菜单UI及场景已释放。");
                }
            }
            catch (Exception error)
            {
                Stop();
                Debug.LogException(error);
            }
        }
        private static void Changed(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode) Stop();
        }
        private static void Stop()
        {
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= Changed;
            AssemblyReloadEvents.beforeAssemblyReload -= Stop;
            if (_entry != null && _original != null)
            {
                var fields = new SerializedObject(_entry);
                fields.FindProperty("presentation").objectReferenceValue = _original;
                fields.ApplyModifiedPropertiesWithoutUndo();
            }
            if (_temporary != null) UnityEngine.Object.DestroyImmediate(_temporary);
            _temporary = null; _original = null; _entry = null;
        }
    }
}
