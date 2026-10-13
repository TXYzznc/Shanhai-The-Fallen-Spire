using Shanhai.Demo.Build;
using Shanhai.Demo.Configuration;
using UnityEngine;

namespace Shanhai.Demo.UI
{
    /// <summary>GF管理常驻HUD生命周期；只负责显示区绑定与解绑。</summary>
    public sealed class DemoHudForm : UIFormBase
    {
        [SerializeField] private DemoHudController display;
        public DemoHudController Display => display;
        public void Bind(GameObject player, DemoGameplayConfig config, DemoRewardChoiceController reward)
            => display.Bind(player, config, reward);
        protected override void OnClose(bool isShutdown, object userData)
        { display.Bind(null, null, null); base.OnClose(isShutdown, userData); }
    }
}
