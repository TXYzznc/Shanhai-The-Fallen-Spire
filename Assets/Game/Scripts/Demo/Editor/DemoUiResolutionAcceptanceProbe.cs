using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Shanhai.Demo.Build;
using Shanhai.Demo.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace Shanhai.Demo.Editor
{
    /// <summary>仅Editor：切换实际GameView固定分辨率，检查布局/文字/按钮射线并恢复窗口设置。</summary>
    public sealed class DemoUiResolutionAcceptanceProbe
    {
        private static DemoUiResolutionAcceptanceProbe _active;
        private IEnumerator _routine;
        private GameObject _hud;
        public int ActualResolutionCases { get; private set; }
        public int BoundsCases { get; private set; }
        public int TextCases { get; private set; }
        public int ButtonRaycastCases { get; private set; }
        public int ContentCases { get; private set; }
        public int EquipmentCases { get; private set; }
        public bool Completed { get; private set; }
        public bool Finished { get; private set; }
        public string Details { get; private set; }
        private EditorWindow _gameView;
        private object _group, _temporarySize;
        private int _originalIndex, _temporaryIndex;
        private MethodInfo _select;
        private DemoRewardChoiceView _view;
        private DemoRewardChoiceController _choice;
        private DemoCompletionView _completion;
        private DemoMarkLoadout _marks;
        private string[] _originalMarks;
        private bool _captured;
        private readonly Vector3[] _corners = new Vector3[4];
        private readonly List<RaycastResult> _hits = new List<RaycastResult>();
        private PointerEventData _pointer;
        private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        [MenuItem("Shanhai/Demo/Check UI resolutions")]
        public static void Begin()
        {
            if (!EditorApplication.isPlaying) { Debug.LogError("[Demo.Ui.Resolution] 需要先进入Play Mode。"); return; }
            Stop(); _active = new DemoUiResolutionAcceptanceProbe(); _active._routine = _active.Start();
            EditorApplication.update += Tick; EditorApplication.playModeStateChanged += Changed;
            AssemblyReloadEvents.beforeAssemblyReload += Stop;
        }
        private static void Tick()
        {
            try { if (_active != null && !_active._routine.MoveNext()) Stop(); }
            catch (Exception error) { Debug.LogException(error); Stop(); }
        }
        private static void Changed(PlayModeStateChange state) { if (state == PlayModeStateChange.ExitingPlayMode) Stop(); }
        private static void Stop()
        {
            EditorApplication.update -= Tick; EditorApplication.playModeStateChanged -= Changed; AssemblyReloadEvents.beforeAssemblyReload -= Stop;
            _active?.Restore(); _active = null;
        }
        private IEnumerator Start()
        {
            yield return null;
            var session = UnityEngine.Object.FindObjectOfType<Shanhai.Demo.Player.DemoWorldEntry>();
            if (session != null && session.IsReady && session.Hud != null) _hud = session.Hud.gameObject;
            if (_hud == null) throw new InvalidOperationException("当前会话HUD尚未准备完成。");
            _view = _hud.GetComponent<DemoRewardChoiceView>(); _choice = _hud.GetComponent<DemoRewardChoiceController>(); _completion = _hud.GetComponent<DemoCompletionView>();
            var hud = _hud.GetComponent<DemoHudController>();
            _marks = (DemoMarkLoadout)hud.GetType().GetField("markLoadout", Flags).GetValue(hud);
            _originalMarks = new string[_marks.SlotCount]; for (int i = 0; i < _originalMarks.Length; i++) _originalMarks[i] = _marks.GetMarkId(i);
            Assembly editor = typeof(EditorWindow).Assembly;
            Type viewType = editor.GetType("UnityEditor.GameView"), sizesType = editor.GetType("UnityEditor.GameViewSizes"), sizeType = editor.GetType("UnityEditor.GameViewSize");
            _gameView = EditorWindow.GetWindow(viewType, false, "Game", false);
            _originalIndex = (int)viewType.GetProperty("selectedSizeIndex", Flags).GetValue(_gameView);
            _select = viewType.GetMethod("SizeSelectionCallback", Flags);
            object sizes = sizesType.BaseType.GetProperty("instance", Flags).GetValue(null);
            _group = sizesType.GetProperty("currentGroup", Flags).GetValue(sizes);
            _temporaryIndex = (int)_group.GetType().GetMethod("GetTotalCount", Flags).Invoke(_group, null);
            Type sizeKind = editor.GetType("UnityEditor.GameViewSizeType");
            _temporarySize = Activator.CreateInstance(sizeType, Flags, null, new[] { Enum.Parse(sizeKind, "FixedResolution"), (object)1280, 720, "Demo QA temporary" }, null);
            _group.GetType().GetMethod("AddCustomSize", Flags).Invoke(_group, new[] { _temporarySize });
            _captured = true;
            _marks.ResetForRetry(); _marks.TryEquip("beast_01_crack", 0); _marks.TryEquip("beast_02_mountain", 1); _marks.TryEquip("beast_03_shadow", 2);
            UnityEngine.Object.FindObjectOfType<Shanhai.Demo.Player.DemoWorldEntry>().Ui.Open(); _completion.Open(); _pointer = new PointerEventData(EventSystem.current);
            foreach (Vector2Int resolution in new[] { new Vector2Int(800,600), new Vector2Int(1024,768), new Vector2Int(1280,720), new Vector2Int(1440,900), new Vector2Int(1920,1080), new Vector2Int(2560,1440) })
            {
                sizeType.GetProperty("width", Flags).SetValue(_temporarySize, resolution.x); sizeType.GetProperty("height", Flags).SetValue(_temporarySize, resolution.y);
                // 切回原尺寸再选临时尺寸，确保走GameView的真实尺寸选择回调。
                _select.Invoke(_gameView, new object[] { _originalIndex, null }); _select.Invoke(_gameView, new object[] { _temporaryIndex, null });
                _gameView.Focus(); _gameView.Repaint();
                float until = Time.realtimeSinceStartup + 5f;
                while ((Screen.width != resolution.x || Screen.height != resolution.y) && Time.realtimeSinceStartup < until) yield return null;
                double settle = EditorApplication.timeSinceStartup + 0.3;
                while (EditorApplication.timeSinceStartup < settle) yield return null;
                Canvas.ForceUpdateCanvases();
                bool actual = Screen.width == resolution.x && Screen.height == resolution.y;
                if (actual) ActualResolutionCases++;
                bool bounds = actual, textFits = actual, buttons = actual;
                string failures = string.Empty;
                foreach (RectTransform rect in _hud.GetComponentsInChildren<RectTransform>(false))
                {
                    if (rect == _hud.transform) continue;
                    rect.GetWorldCorners(_corners);
                    for (int i = 0; i < 4; i++)
                    {
                        Vector2 point = RectTransformUtility.WorldToScreenPoint(null, _corners[i]);
                        if (point.x < -1f || point.y < -1f || point.x > Screen.width + 1f || point.y > Screen.height + 1f) { bounds = false; failures += "bounds:" + rect.name + ";"; break; }
                    }
                }
                foreach (TMP_Text text in _hud.GetComponentsInChildren<TMP_Text>(false))
                {
                    text.ForceMeshUpdate();
                    if (text.preferredHeight > text.rectTransform.rect.height + 1f || !text.enableWordWrapping && text.preferredWidth > text.rectTransform.rect.width + 1f)
                    { textFits = false; failures += "text:" + text.name + $"({text.preferredHeight}/{text.rectTransform.rect.height});"; }
                }
                foreach (Button button in _hud.GetComponentsInChildren<Button>(false))
                {
                    _pointer.position = RectTransformUtility.WorldToScreenPoint(null, button.transform.position); _hits.Clear(); EventSystem.current.RaycastAll(_pointer, _hits);
                    if (_hits.Count == 0 || !(_hits[0].gameObject == button.gameObject || _hits[0].gameObject.transform.IsChildOf(button.transform)))
                    { buttons = false; failures += "raycast:" + button.name + ";"; }
                }
                if (bounds) BoundsCases++; if (textFits) TextCases++; if (buttons) ButtonRaycastCases++;
                Details += $"{resolution.x}x{resolution.y}: actual={Screen.width}x{Screen.height},bounds={bounds},text={textFits},buttons={buttons};{failures}\n";
                if (actual)
                {
                    ScreenCapture.CaptureScreenshot($"Assets/Screenshots/DemoUi{resolution.x}x{resolution.y}.png");
                    double captureUntil = EditorApplication.timeSinceStartup + 0.2;
                    while (EditorApplication.timeSinceStartup < captureUntil) yield return null;
                }
                // 通过真实UGUI悬停/首击/确认，覆盖三兽印四槽位的文字状态。
                Transform panel = _hud.transform.Find("RewardChoicePanel");
                TMP_Text tooltip = panel.Find("Tooltip").GetComponent<TMP_Text>();
                string[] ids = { "beast_01_crack", "beast_02_mountain", "beast_03_shadow" };
                int contentBefore = ContentCases, equipmentBefore = EquipmentCases;
                for (int slot = 0; slot < 4; slot++)
                {
                    foreach (string id in ids)
                    {
                        _marks.ResetForRetry(); UnityEngine.Object.FindObjectOfType<Shanhai.Demo.Player.DemoWorldEntry>().Ui.Open(slot);
                        // EditorApplication.update可能在同一运行帧推进多次，等待真正的Canvas注册/重建帧。
                        int readyFrame = Time.frameCount + 2;
                        while (Time.frameCount < readyFrame) yield return null;
                        int optionIndex = -1;
                        for (int i = 0; i < _choice.OptionCount; i++) if (_choice.GetOption(i) == id) optionIndex = i;
                        if (optionIndex < 0) throw new InvalidOperationException("奖励选项缺失：" + id);
                        GameObject option = panel.Find("RewardOption_" + (optionIndex + 1)).gameObject;
                        var config = _choice.GetOptionConfig(optionIndex);
                        Canvas.ForceUpdateCanvases();
                        ExecuteEvents.Execute(option, _pointer, ExecuteEvents.pointerEnterHandler);
                        bool hover = tooltip.text == config.description && AllTextFits();
                        bool clicked = Click(option);
                        bool preview = clicked && _choice.IsOpen && !_choice.IsConfirmed && _marks.EquippedCount == 0
                            && tooltip.text == config.description + "  再次点击该项确认" && AllTextFits();
                        if (actual && hover && preview) ContentCases++;
                        else Details += $"{resolution.x}x{resolution.y}:{id}/slot{slot},hover={hover},preview={preview},clicked={clicked}\n";
                        bool confirmed = Click(option) && _choice.IsConfirmed && !_choice.IsOpen && _marks.GetMarkId(slot) == id;
                        double refreshUntil = EditorApplication.timeSinceStartup + 0.35;
                        while (EditorApplication.timeSinceStartup < refreshUntil) yield return null;
                        var label = _hud.transform.Find("LoadoutPanel/Slot_" + slot).GetComponent<TMP_Text>();
                        bool equipment = confirmed && label.text.Contains(config.displayName) && label.text.Contains("Lv.1") && AllTextFits();
                        if (actual && equipment) EquipmentCases++;
                        else Details += $"{resolution.x}x{resolution.y}:{id}/slot{slot},equipment={equipment},confirmed={confirmed}\n";
                    }
                }
                Details += $"{resolution.x}x{resolution.y}: content={ContentCases-contentBefore}/12,equipment={EquipmentCases-equipmentBefore}/12\n";
                _marks.ResetForRetry(); _marks.TryEquip(ids[0], 0); _marks.TryEquip(ids[1], 1); _marks.TryEquip(ids[2], 2); UnityEngine.Object.FindObjectOfType<Shanhai.Demo.Player.DemoWorldEntry>().Ui.Open();
            }
            Completed = ActualResolutionCases == 6 && BoundsCases == 6 && TextCases == 6 && ButtonRaycastCases == 6 && ContentCases == 72 && EquipmentCases == 72;
            Finished = true; Restore();
            Debug.Log($"[Demo.Ui.Resolution] Completed={Completed}, actual={ActualResolutionCases}/6, bounds={BoundsCases}/6, text={TextCases}/6, buttons={ButtonRaycastCases}/6, content={ContentCases}/72, equipment={EquipmentCases}/72\n{Details}");
        }
        private bool AllTextFits()
        {
            Canvas.ForceUpdateCanvases();
            foreach (TMP_Text text in _hud.GetComponentsInChildren<TMP_Text>(false))
            {
                text.ForceMeshUpdate();
                if (text.preferredHeight > text.rectTransform.rect.height + 1f
                    || !text.enableWordWrapping && text.preferredWidth > text.rectTransform.rect.width + 1f) return false;
            }
            return true;
        }
        private bool Click(GameObject target)
        {
            Canvas.ForceUpdateCanvases();
            _pointer.button = PointerEventData.InputButton.Left;
            _pointer.position = RectTransformUtility.WorldToScreenPoint(null, target.transform.position);
            _hits.Clear(); EventSystem.current.RaycastAll(_pointer, _hits);
            if (_hits.Count == 0 || !(_hits[0].gameObject == target || _hits[0].gameObject.transform.IsChildOf(target.transform)))
            {
                Details += $"raycast:{target.name}->{(_hits.Count == 0 ? "none" : _hits[0].gameObject.name)}\n";
                return false;
            }
            return ExecuteEvents.Execute(target, _pointer, ExecuteEvents.pointerClickHandler);
        }
        private void Restore()
        {
            if (!_captured) return;
            _select.Invoke(_gameView, new object[] { _originalIndex, null });
            if (ReferenceEquals(_group.GetType().GetMethod("GetGameViewSize", Flags).Invoke(_group, new object[] { _temporaryIndex }), _temporarySize))
                _group.GetType().GetMethod("RemoveCustomSize", Flags).Invoke(_group, new object[] { _temporaryIndex });
            _view.Hide(); _completion.Hide(); _choice.ResetForRetry(); _marks.ResetForRetry();
            for (int i = 0; i < _originalMarks.Length; i++) if (!string.IsNullOrEmpty(_originalMarks[i])) _marks.TryEquip(_originalMarks[i], i);
            _captured = false;
        }
    }
}
