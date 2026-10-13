using System;
using Game.State;
using Shanhai.Demo.Build;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Configuration;
using Shanhai.Demo.Enemies;
using Shanhai.Demo.Input;
using Shanhai.Demo.Player;
using UnityEngine;

namespace Shanhai.Demo.UI
{
    /// <summary>一局UI的组合根；持有窗口请求、语义输入与跨窗口规则。</summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class DemoUiSession : MonoBehaviour, IDemoRewardPresenter
    {
        private DemoPresentationConfig _config;
        private DemoEncounterController _boss;
        private DemoPlayerHealthController _health;
        private IDemoInputSource _input;
        private DemoUiWindowLease _hud, _overlay, _reward, _assembly, _completion;
        private bool _disposed, _completionObserved, _ready;
        private Action _onReady;
        public GameObject Player { get; private set; }
        public GamePauseController Pause { get; private set; }
        public DemoRunFlowController Run { get; private set; }
        public DemoRewardChoiceController RewardChoice { get; private set; }
        public DemoHudForm Hud => _hud?.Form as DemoHudForm;
        public DemoCombatOverlayForm Overlay => _overlay?.Form as DemoCombatOverlayForm;
        public DemoRewardChoiceView RewardView => (_reward?.Form as DemoRewardForm)?.View;
        public DemoMarkAssemblyView Assembly => (_assembly?.Form as DemoMarkAssemblyForm)?.View;
        public DemoCompletionView Completion => (_completion?.Form as DemoCompletionForm)?.View;
        public bool IsRewardRequested => _reward != null && _reward.IsRequested;
        public bool IsAssemblyRequested => _assembly != null && _assembly.IsRequested;
        public bool IsCompletionRequested => _completion != null && _completion.IsRequested;
        public void Initialize(DemoPresentationConfig config, GameObject player, DemoEncounterController boss,
            GamePauseController pause, MonoBehaviour input, Action onReady)
        {
            _config = config; Player = player; _boss = boss; Pause = pause; BindInput(input); _onReady = onReady;
            Run = player.GetComponent<DemoRunFlowController>(); _health = player.GetComponent<DemoPlayerHealthController>();
            RewardChoice = gameObject.AddComponent<DemoRewardChoiceController>();
            RewardChoice.BindConfiguration(config.GameplayConfig); RewardChoice.BindPlayer(player);
            Run.WorldReset += ResetWindows;
            _hud = new DemoUiWindowLease(null, HudOpened, Failed);
            _overlay = new DemoUiWindowLease(null, _ => CheckReady(), Failed);
            _reward = new DemoUiWindowLease(pause.State, form => ((DemoRewardForm)form).Bind(this, Hide), RewardFailed);
            _assembly = new DemoUiWindowLease(pause.State, form => ((DemoMarkAssemblyForm)form).Bind(this, CloseAssembly), Failed);
            _completion = new DemoUiWindowLease(pause.State, form => ((DemoCompletionForm)form).Bind(this, CloseCompletion), Failed);
            _hud.Open(config.HudPrefabAsset, config.UiGroup, 20);
            _overlay.Open(config.CombatOverlayPrefabAsset, config.UiGroup, 10);
        }
        private void HudOpened(UIFormBase form)
        {
            Hud.Bind(Player, _config.GameplayConfig, RewardChoice);
            Hud.Display.Marks.OpenRequested += ToggleAssembly; CheckReady();
        }
        private void CheckReady()
        {
            if (_ready || Hud == null || Overlay == null || _disposed) return;
            _ready = true; var ready = _onReady; _onReady = null; ready?.Invoke();
        }
        public void BindInput(MonoBehaviour input) => _input = input as IDemoInputSource;
        private bool CanOpenAssembly => _ready && !_disposed && !IsRewardRequested && !RewardChoice.IsOpen
            && !IsCompletionRequested && !Run.IsRetrying && !_health.IsRetryRequired;
        public void ToggleAssembly()
        {
            if (IsAssemblyRequested) CloseAssembly();
            else if (CanOpenAssembly) _assembly.Open(_config.AssemblyPrefabAsset, _config.UiGroup, 30);
        }
        public void CloseAssembly() => _assembly?.Close();
        public void CloseCompletion() => _completion?.Close();
        public void Open(int slot = 0)
        {
            if (!_ready || _disposed || IsRewardRequested || IsCompletionRequested) return;
            CloseAssembly(); RewardChoice.Open(slot); PresentReward();
        }
        public void OpenBuffs()
        {
            if (!_ready || _disposed || IsRewardRequested || IsCompletionRequested) return;
            CloseAssembly(); RewardChoice.OpenBuffs(); PresentReward();
        }
        public bool OpenDrop(DemoDropConfig drop, bool isBoss)
        {
            if (drop == null || !drop.IsChoiceReward || drop.amount != 1) return false;
            if (!(isBoss && drop.guaranteedForBoss) && (drop.chance <= 0 || (drop.chance < 1 && UnityEngine.Random.value >= drop.chance))) return false;
            if (drop.rewardId == DemoDropConfig.BeastMarkChoice) Open(); else OpenBuffs();
            return RewardChoice != null && RewardChoice.IsOpen;
        }
        private void PresentReward()
        { if (RewardChoice.IsOpen) _reward.Open(_config.RewardPrefabAsset, _config.UiGroup, 40); }
        public void Hide() => _reward?.Close();
        public void ShowNotice(string message) { if (Hud != null) Hud.Display.RunInfo.ShowNotice(message); }
        internal void OnRewardNotice(DemoRewardNoticeEventArgs args) => ShowNotice(args.Message);
        private void Update()
        {
            if (!_ready || _disposed) return;
            Hud.Display.Marks.SetCanOpen(IsAssemblyRequested || CanOpenAssembly);
            if (_input != null)
            {
                var input = _input.ReadState();
                if (input.AssemblyPressed) ToggleAssembly();
                else if (input.CloseUiPressed && IsAssemblyRequested) CloseAssembly();
            }
            if (_boss == null) return;
            if (!_boss.IsCompleted) { _completionObserved = false; return; }
            if (!_completionObserved && !RewardChoice.IsOpen && !IsRewardRequested)
            { _completionObserved = true; CloseAssembly(); _completion.Open(_config.CompletionPrefabAsset, _config.UiGroup, 50); }
        }
        private void ResetWindows()
        {
            Hide(); CloseAssembly(); CloseCompletion(); _completionObserved = false;
            if (Hud != null) Hud.Display.RunInfo.ShowNotice(string.Empty);
        }
        private void RewardFailed(string error) { RewardChoice.ResetForRetry(); Failed(error); }
        private void Failed(string error)
        { if (!_disposed) Debug.LogError("[Demo.UI] 窗口加载失败：" + error, this); }
        public void Shutdown()
        {
            if (_disposed) return; _disposed = true; _ready = false; _onReady = null; _input = null;
            if (Run != null) Run.WorldReset -= ResetWindows;
            if (Hud != null) Hud.Display.Marks.OpenRequested -= ToggleAssembly;
            _reward?.Dispose(); _assembly?.Dispose(); _completion?.Dispose(); _overlay?.Dispose(); _hud?.Dispose();
            if (RewardChoice != null) RewardChoice.BindPlayer(null);
            Player = null; Run = null; _boss = null; _health = null; Pause = null;
        }
        private void OnDestroy() => Shutdown();
    }
}
