using UnityEngine;

namespace Shanhai.Demo.UI
{
    /// <summary>仅承载战斗表现节点；伤害/敌人状态的绑定和回收由各表现系统负责。</summary>
    public sealed class DemoCombatOverlayForm : UIFormBase
    {
        [SerializeField] private RectTransform damageLayer;
        [SerializeField] private RectTransform enemyStatusLayer;
        public RectTransform DamageLayer => damageLayer;
        public RectTransform EnemyStatusLayer => enemyStatusLayer;
    }
}
