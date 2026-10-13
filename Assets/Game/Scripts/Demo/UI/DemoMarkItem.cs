using Shanhai.Demo.Configuration;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Shanhai.Demo.UI
{
    /// <summary>列表卡及槽位共用的纯展示Item。</summary>
    public sealed class DemoMarkItem : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image icon;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text status;
        [SerializeField] private TMP_Text empty;
        public Button Button => button;
        public Sprite ShownIcon => icon.sprite;
        public bool IsEmpty => !icon.enabled;
        public void Render(DemoBeastMarkConfig mark, string heading, string state, bool selected = false)
        {
            title.text = heading; status.text = state;
            icon.sprite = mark != null ? mark.icon : null; icon.enabled = icon.sprite != null;
            empty.gameObject.SetActive(mark == null);
            button.image.color = selected ? new Color(.18f, .42f, .38f, 1) : new Color(.08f, .14f, .2f, 1);
        }
    }
}
