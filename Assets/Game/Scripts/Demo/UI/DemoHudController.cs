using Shanhai.Demo.Build;
using Shanhai.Demo.Configuration;
using UnityEngine;

namespace Shanhai.Demo.UI
{
    /// <summary>组合三个显示区，不持有窗口、输入或玩法状态。</summary>
    public sealed class DemoHudController : MonoBehaviour
    {
        [SerializeField] private DemoVitalsView vitals;
        [SerializeField] private DemoRunInfoView runInfo;
        [SerializeField] private DemoLoadoutView marks;
        public DemoVitalsView Vitals => vitals;
        public DemoRunInfoView RunInfo => runInfo;
        public DemoLoadoutView Marks => marks;
        public void Bind(GameObject player, DemoGameplayConfig config, DemoRewardChoiceController reward)
        { vitals.Bind(player, config); runInfo.Bind(player, reward); marks.BindPlayer(player); }
    }
}
