using UnityEngine;

namespace AiFriendlyFrame.Sample.BasicUi
{
    /// <summary>
    /// Minimal scene-local controller used by the Basic UI sample.
    /// It deliberately has no dependency on business code, resources, or the framework bootstrap flow.
    /// </summary>
    public sealed class BasicUiSampleController : MonoBehaviour
    {
        /// <summary>
        /// Invoked by the sample button's persistent UnityEvent.
        /// </summary>
        public void ReportButtonClick()
        {
            Debug.Log("[AI Friendly Frame 示例] 已点击基础 UI 按钮。");
        }
    }
}
