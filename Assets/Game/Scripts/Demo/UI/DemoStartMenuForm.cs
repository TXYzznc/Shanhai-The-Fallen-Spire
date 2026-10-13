using Shanhai.Demo.Player;
using TMPro;
using UnityEngine;

namespace Shanhai.Demo.UI
{
    /// <summary>节点和控件来自Prefab，生命周期由GF.UI持有。</summary>
    public sealed class DemoStartMenuForm : UIFormBase
    {
        [SerializeField] private UnityEngine.UI.Button startButton;
        [SerializeField] private UnityEngine.UI.Button quitButton;
        [SerializeField] private TMP_Text statusText;
        private DemoMenuEntry _owner;
        public bool StartEnabled => startButton != null && startButton.interactable;
        public string Status => statusText != null ? statusText.text : string.Empty;

        protected override void OnOpen(object userData)
        {
            base.OnOpen(userData);
            startButton.onClick.AddListener(StartGame);
            quitButton.onClick.AddListener(QuitGame);
        }
        public void Bind(DemoMenuEntry owner)
        {
            _owner = owner;
            SetLoading(false);
        }
        public void SetLoading(bool loading)
        {
            startButton.interactable = !loading;
            if (statusText != null) statusText.text = loading ? "正在进入山海……" : string.Empty;
        }
        public void ShowFailure()
        {
            SetLoading(false);
            if (statusText != null) statusText.text = "进入失败，请再次点击开始游戏";
        }
        private void StartGame() { if (_owner != null) _owner.RequestStart(); }
        private void QuitGame() { if (_owner != null) _owner.RequestQuit(); }
        protected override void OnClose(bool isShutdown, object userData)
        {
            startButton.onClick.RemoveListener(StartGame);
            quitButton.onClick.RemoveListener(QuitGame);
            _owner = null;
            if (statusText != null) statusText.text = string.Empty;
            base.OnClose(isShutdown, userData);
        }
    }
}
