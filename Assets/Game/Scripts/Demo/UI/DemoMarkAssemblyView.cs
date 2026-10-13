using Shanhai.Demo.Build;
using Shanhai.Demo.Configuration;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shanhai.Demo.UI
{
    public sealed class DemoMarkAssemblyView : MonoBehaviour
    {
        [SerializeField] private DemoMarkItem ownedItemPrefab;
        [SerializeField] private Transform ownedRoot;
        [SerializeField] private DemoMarkItem[] slots;
        [SerializeField] private TMP_Text detail;
        [SerializeField] private TMP_Text message;
        [SerializeField] private TMP_Text count;
        [SerializeField] private TMP_Text emptyList;
        [SerializeField] private Button equipButton;
        [SerializeField] private Button unequipButton;
        [SerializeField] private Button closeButton;
        private DemoMarkLoadout _loadout;
        private DemoMarkItem[] _ownedItems;
        private string _selected = string.Empty;
        private int _slot = -1;
        public event System.Action CloseRequested;
        public bool IsOpen => gameObject.activeInHierarchy;
        public DemoMarkItem GetSlot(int index) => slots[index];
        private void Awake()
        {
            for (int i = 0; i < slots.Length; i++) { int slot = i; slots[i].Button.onClick.AddListener(() => SelectSlot(slot)); }
            equipButton.onClick.AddListener(Equip); unequipButton.onClick.AddListener(Unequip); closeButton.onClick.AddListener(Hide);
        }
        public void Bind(DemoMarkLoadout loadout)
        {
            if (_loadout != null) _loadout.LoadoutChanged -= Refresh;
            _loadout = loadout;
            if (_loadout != null) _loadout.LoadoutChanged += Refresh;
            _selected = string.Empty; _slot = -1; Refresh();
        }
        public void Open()
        {
            if (_loadout == null) return;
            _selected = string.Empty; _slot = -1; message.text = "选择兽印与目标槽位，再点击装配";
            Refresh(); transform.SetAsLastSibling(); gameObject.SetActive(true);
        }
        public void Hide() => CloseRequested?.Invoke();
        public void SelectMark(string id) { if (_loadout != null && _loadout.HasMark(id)) { _selected = id; Refresh(); } }
        public void SelectSlot(int slot)
        {
            if (slot < 0 || slot >= 4) return;
            _slot = slot;
            if (string.IsNullOrEmpty(_selected) && _loadout != null) _selected = _loadout.GetMarkId(slot);
            Refresh();
        }
        public void Equip()
        {
            if (_loadout == null || !_loadout.TryEquip(_selected, _slot)) return;
            message.text = "装配完成，旧兽印仍保留在已拥有列表"; Refresh();
        }
        public void Unequip()
        {
            if (_loadout == null || _slot < 0 || string.IsNullOrEmpty(_loadout.GetMarkId(_slot))) return;
            _loadout.Remove(_slot); message.text = "已卸下，兽印仍保留"; Refresh();
        }
        private void EnsureItems()
        {
            int needed = _loadout != null ? _loadout.OwnedCount : 0;
            if (_ownedItems != null && _ownedItems.Length >= needed) return;
            int oldCount = _ownedItems != null ? _ownedItems.Length : 0;
            var expanded = new DemoMarkItem[needed];
            if (_ownedItems != null) System.Array.Copy(_ownedItems, expanded, oldCount);
            for (int i = oldCount; i < needed; i++)
            {
                int index = i; var item = Instantiate(ownedItemPrefab, ownedRoot); item.gameObject.SetActive(true);
                item.Button.onClick.AddListener(() => { if (_loadout != null) SelectMark(_loadout.GetOwnedId(index)); });
                expanded[i] = item;
            }
            _ownedItems = expanded;
        }
        public void Refresh()
        {
            EnsureItems();
            if (_loadout == null || !_loadout.HasMark(_selected)) _selected = string.Empty;
            int owned = _loadout != null ? _loadout.OwnedCount : 0;
            count.SetText("已拥有 {0} 个兽印", owned); emptyList.gameObject.SetActive(owned == 0);
            if (_ownedItems != null) for (int i = 0; i < _ownedItems.Length; i++)
            {
                bool visible = i < owned; _ownedItems[i].gameObject.SetActive(visible); if (!visible) continue;
                string id = _loadout.GetOwnedId(i); var mark = _loadout.GetConfig(id); int position = _loadout.FindEquippedSlot(id);
                _ownedItems[i].Render(mark, mark.displayName, position < 0 ? "未装配" : "已装配：" + DemoMarkText.SlotName(position), id == _selected);
            }
            for (int i = 0; i < slots.Length; i++)
            {
                var mark = _loadout != null ? _loadout.GetConfig(_loadout.GetMarkId(i)) : null;
                slots[i].Render(mark, DemoMarkText.SlotName(i), mark != null ? mark.displayName : string.Empty, i == _slot);
            }
            var selected = _loadout != null ? _loadout.GetConfig(_selected) : null;
            bool chosen = selected != null && _slot >= 0;
            string targetId = _slot >= 0 && _loadout != null ? _loadout.GetMarkId(_slot) : string.Empty;
            var target = _loadout != null ? _loadout.GetConfig(targetId) : null;
            equipButton.interactable = chosen && targetId != _selected;
            unequipButton.interactable = _slot >= 0 && target != null;
            string action = "装配";
            if (chosen)
            {
                int current = _loadout.FindEquippedSlot(_selected);
                action = current == _slot ? "已装配" : current >= 0 ? "移动：" + DemoMarkText.SlotName(current) + " → " + DemoMarkText.SlotName(_slot)
                    : target != null ? "替换：" + target.displayName : "装配";
            }
            equipButton.GetComponentInChildren<TMP_Text>().text = action;
            detail.text = selected == null ? "选择已拥有的兽印查看效果\n\n未获得兽印不能装配" : selected.displayName + "\n\n" + selected.description
                + (chosen ? "\n\n目标：" + DemoMarkText.SlotName(_slot) + "\n当前：" + (target != null ? target.displayName : "空") + "\n装配后：" + DemoMarkText.Effect(selected, _slot) : "\n\n请选择目标槽位");
        }
        private void OnDestroy() { if (_loadout != null) _loadout.LoadoutChanged -= Refresh; }
    }
}
