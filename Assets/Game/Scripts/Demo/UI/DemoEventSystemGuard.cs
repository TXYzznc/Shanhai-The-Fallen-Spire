using UnityEngine;
using UnityEngine.EventSystems;

namespace Shanhai.Demo.UI
{
    /// <summary>独立场景保留UI输入；框架入口已有EventSystem时禁用Demo备用实例。</summary>
    [DefaultExecutionOrder(-1000)]
    [RequireComponent(typeof(EventSystem))]
    public sealed class DemoEventSystemGuard : MonoBehaviour
    {
        private void Awake()
        {
            EventSystem ownSystem = GetComponent<EventSystem>();
            bool needsOwnSystem = EventSystem.current == null || EventSystem.current == ownSystem;
            ownSystem.enabled = needsOwnSystem;
            foreach (BaseInputModule module in GetComponents<BaseInputModule>()) module.enabled = needsOwnSystem;
        }
    }
}
