using System;
using Shanhai.Demo.Build;
using UnityEngine;
using UnityEngine.UI;

namespace Shanhai.Demo.UI
{
    /// <summary>四槽摘要只显示装备并发布按钮意图；窗口和输入由会话管理。</summary>
    public sealed class DemoLoadoutView : MonoBehaviour
    {
        [SerializeField] private DemoMarkItem[] hudSlots;
        [SerializeField] private Button openButton;
        private DemoMarkLoadout _loadout;
        public event Action OpenRequested;
        private void Awake() => openButton.onClick.AddListener(RequestOpen);
        private void RequestOpen() => OpenRequested?.Invoke();
        public DemoMarkItem GetSlot(int index) => hudSlots[index];
        public void SetCanOpen(bool canOpen) => openButton.interactable = canOpen;
        public void BindPlayer(GameObject player)
        {
            if (_loadout != null) _loadout.LoadoutChanged -= Refresh;
            _loadout = player != null ? player.GetComponent<DemoMarkLoadout>() : null;
            if (_loadout != null) _loadout.LoadoutChanged += Refresh;
            Refresh();
        }
        private void Refresh()
        {
            for (int i = 0; i < hudSlots.Length; i++)
            {
                var mark = _loadout != null ? _loadout.GetConfig(_loadout.GetMarkId(i)) : null;
                hudSlots[i].Render(mark, DemoMarkText.SlotName(i), string.Empty);
            }
        }
        private void OnDestroy()
        {
            if (_loadout != null) _loadout.LoadoutChanged -= Refresh;
            openButton.onClick.RemoveListener(RequestOpen);
        }
    }
}
