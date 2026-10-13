using System;
using System.Collections;
using GameFramework;
using GameFramework.Event;
using Shanhai.Demo.Configuration;
using Shanhai.Demo.UI;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace Shanhai.Demo.Player
{
    /// <summary>开始界面的会话入口；明确持有UI请求和场景加载事件。</summary>
    public sealed class DemoMenuEntry : MonoBehaviour
    {
        [SerializeField] private DemoPresentationConfig presentation;
        private DemoStartMenuForm _menu;
        private UIParams _pendingParams;
        private int _menuSerial = -1;
        private bool _subscribed, _leaving;
        private DemoMenuLoadTicket _loadTicket;
        public bool IsStarting { get; private set; }
        public bool IsReady => _menu != null && _menu.Available;
        public int AcceptedStarts { get; private set; }

        private IEnumerator Start()
        {
            while (GF.UI == null || GF.Scene == null || GF.Event == null) yield return null;
            if (presentation == null || !presentation.HasMenuConfiguration)
            {
                Debug.LogError("[Demo.Menu] 展示资源配置缺失，未打开菜单。", this);
                yield break;
            }
            GF.Event.Subscribe(OpenUIFormFailureEventArgs.EventId, UiFailed);
            _subscribed = true;
            _pendingParams = UIParams.Create(false, 10);
            _pendingParams.OpenCallback = MenuOpened;
            try { _menuSerial = GF.UI.OpenUIForm(presentation.MenuPrefabAsset, presentation.UiGroup, false, _pendingParams); }
            catch (Exception error) { ReleasePending(); Debug.LogException(error, this); }
        }
        private void MenuOpened(UIFormLogic logic)
        {
            _pendingParams = null; // 成功打开后由UIFormBase.OnClose释放。
            _menu = logic as DemoStartMenuForm;
            if (_menu == null) throw new InvalidOperationException("开始界面缺少DemoStartMenuForm。");
            _menu.Bind(this);
            Debug.Log("[Demo.Menu] Ready：标题/开始游戏/退出，尚未开始本局。", this);
        }
        public bool RequestStart()
        {
            if (_leaving || IsStarting || !IsReady) return false;
            IsStarting = true; AcceptedStarts++;
            _menu.SetLoading(true);
            try
            {
                _loadTicket = new DemoMenuLoadTicket(this, presentation.GameSceneAsset);
                _loadTicket.Begin();
            }
            catch (Exception error) { _loadTicket = null; IsStarting = false; _menu.ShowFailure(); Debug.LogException(error, this); return false; }
            return true;
        }
        public void RequestQuit()
        {
            if (_leaving) return;
            _leaving = true;
            _loadTicket?.Cancel();
            Application.Quit();
        }
        internal void SceneLoaded()
        {
            _loadTicket = null;
            _leaving = true;
            IsStarting = false;
            CloseMenu();
            GF.Scene.UnloadScene(presentation.MenuSceneAsset);
            Debug.Log("[Demo.Menu] Started：本局场景加载成功，菜单已关闭。", this);
        }
        internal void SceneFailed(string error)
        {
            _loadTicket = null;
            IsStarting = false;
            _menu?.ShowFailure();
            Debug.LogError("[Demo.Menu] 进入场景失败：" + error, this);
        }
        private void UiFailed(object sender, GameEventArgs e)
        {
            var args = (OpenUIFormFailureEventArgs)e;
            if (_pendingParams == null || args.UserData != _pendingParams) return;
            ReleasePending();
            Debug.LogError("[Demo.Menu] 开始界面加载失败：" + args.ErrorMessage, this);
        }
        private void CloseMenu()
        {
            if (_menuSerial >= 0 && GF.UI != null && (GF.UI.IsLoadingUIForm(_menuSerial) || GF.UI.HasUIForm(_menuSerial)))
                GF.UI.CloseUIForm(_menuSerial);
            _menuSerial = -1; _menu = null; ReleasePending();
        }
        private void ReleasePending()
        {
            if (_pendingParams == null) return;
            ReferencePool.Release(_pendingParams); _pendingParams = null;
        }
        private void OnDestroy()
        {
            if (_subscribed && GF.Event != null)
            {
                GF.Event.Unsubscribe(OpenUIFormFailureEventArgs.EventId, UiFailed);
            }
            _subscribed = false;
            CloseMenu();
            _loadTicket?.Cancel(); _loadTicket = null;
        }
    }

    /// <summary>GF无加载取消API；取消后等待终态并卸载迟到结果，票据不持有已销毁的UI。</summary>
    internal sealed class DemoMenuLoadTicket
    {
        private DemoMenuEntry _owner;
        private readonly string _asset;
        private bool _subscribed;
        public DemoMenuLoadTicket(DemoMenuEntry owner, string asset) { _owner = owner; _asset = asset; }
        public void Begin()
        {
            GF.Event.Subscribe(LoadSceneSuccessEventArgs.EventId, Loaded);
            GF.Event.Subscribe(LoadSceneFailureEventArgs.EventId, Failed);
            _subscribed = true;
            try { GF.Scene.LoadScene(_asset, this); }
            catch { Detach(); throw; }
        }
        public void Cancel() { _owner = null; }
        private void Loaded(object sender, GameEventArgs e)
        {
            var args = (LoadSceneSuccessEventArgs)e;
            if (!ReferenceEquals(args.UserData, this)) return;
            DemoMenuEntry owner = _owner; Detach();
            if (owner != null) owner.SceneLoaded();
            else if (GF.Scene != null && GF.Scene.SceneIsLoaded(_asset) && !GF.Scene.SceneIsUnloading(_asset))
                GF.Scene.UnloadScene(_asset);
        }
        private void Failed(object sender, GameEventArgs e)
        {
            var args = (LoadSceneFailureEventArgs)e;
            if (!ReferenceEquals(args.UserData, this)) return;
            DemoMenuEntry owner = _owner; string error = args.ErrorMessage; Detach();
            if (owner != null) owner.SceneFailed(error);
        }
        private void Detach()
        {
            if (_subscribed && GF.Event != null)
            {
                GF.Event.Unsubscribe(LoadSceneSuccessEventArgs.EventId, Loaded);
                GF.Event.Unsubscribe(LoadSceneFailureEventArgs.EventId, Failed);
            }
            _subscribed = false; _owner = null;
        }
    }
}
