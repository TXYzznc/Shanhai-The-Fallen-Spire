using UnityEngine;

namespace Shanhai.Demo.Configuration
{
    /// <summary>项目展示资源索引；仅保存静态配置。</summary>
    [CreateAssetMenu(menuName = "Shanhai/Demo Presentation")]
    public sealed class DemoPresentationConfig : ScriptableObject
    {
        [SerializeField] private string menuPrefabAsset;
        [SerializeField] private string menuSceneAsset;
        [SerializeField] private string gameSceneAsset;
        [SerializeField] private string hudPrefabAsset;
        [SerializeField] private string combatOverlayPrefabAsset;
        [SerializeField] private string rewardPrefabAsset;
        [SerializeField] private string assemblyPrefabAsset;
        [SerializeField] private string completionPrefabAsset;
        [SerializeField] private DemoGameplayConfig gameplayConfig;
        [SerializeField] private string playerPrefabAsset;
        [SerializeField] private string commonEnemyPrefabAsset;
        [SerializeField] private string eliteEnemyPrefabAsset;
        [SerializeField] private string bossPrefabAsset;
        [SerializeField] private Shanhai.Demo.Presentation.DemoAttackArc attackArcPrefab;
        [SerializeField] private ParticleSystem hitParticlePrefab;
        [SerializeField] private Shanhai.Demo.UI.DemoDamageTextItem damageTextPrefab;
        [SerializeField, Range(1, 32)] private int attackCapacity = 8;
        [SerializeField, Range(1, 64)] private int hitCapacity = 24;
        [SerializeField, Range(1, 64)] private int textCapacity = 24;
        public Shanhai.Demo.Presentation.DemoAttackArc AttackArcPrefab => attackArcPrefab;
        public ParticleSystem HitParticlePrefab => hitParticlePrefab;
        public Shanhai.Demo.UI.DemoDamageTextItem DamageTextPrefab => damageTextPrefab;
        public int AttackCapacity => Mathf.Clamp(attackCapacity, 1, 32);
        public int HitCapacity => Mathf.Clamp(hitCapacity, 1, 64);
        public int TextCapacity => Mathf.Clamp(textCapacity, 1, 64);
        [SerializeField] private string uiGroup = "Default";
        public string MenuPrefabAsset => menuPrefabAsset;
        public string MenuSceneAsset => menuSceneAsset;
        public string GameSceneAsset => gameSceneAsset;
        public string HudPrefabAsset => hudPrefabAsset;
        public string CombatOverlayPrefabAsset => combatOverlayPrefabAsset;
        public string RewardPrefabAsset => rewardPrefabAsset;
        public string AssemblyPrefabAsset => assemblyPrefabAsset;
        public string CompletionPrefabAsset => completionPrefabAsset;
        public DemoGameplayConfig GameplayConfig => gameplayConfig;
        public string PlayerPrefabAsset => playerPrefabAsset;
        public string EnemyPrefabAsset(DemoEnemyConfig enemy) => enemy == null ? null
            : enemy.isBoss ? bossPrefabAsset : enemy.isElite ? eliteEnemyPrefabAsset : commonEnemyPrefabAsset;
        public string UiGroup => uiGroup;
        public bool HasMenuConfiguration => !string.IsNullOrEmpty(menuPrefabAsset)
            && !string.IsNullOrEmpty(menuSceneAsset) && !string.IsNullOrEmpty(gameSceneAsset) && !string.IsNullOrEmpty(uiGroup);
    }
}
