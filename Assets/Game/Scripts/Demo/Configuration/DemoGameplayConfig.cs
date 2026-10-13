using System;
using UnityEngine;

namespace Shanhai.Demo.Configuration
{
    /// <summary>
    /// Demo 运行所需的最小配置集合。所有仍未最终确认的数值都通过该资产调整。
    /// </summary>
    [CreateAssetMenu(fileName = "DemoGameplayConfig", menuName = "Shanhai/Demo/Gameplay Config")]
    public sealed class DemoGameplayConfig : ScriptableObject
    {
        [Header("玩家")]
        [SerializeField] private DemoPlayerConfig player = new DemoPlayerConfig();

        [Header("兽印")]
        [SerializeField] private DemoBeastMarkConfig[] beastMarks = Array.Empty<DemoBeastMarkConfig>();

        [Header("增益")]
        [SerializeField] private DemoBuffConfig[] buffs = Array.Empty<DemoBuffConfig>();

        [Header("敌人与 Boss")]
        [SerializeField] private DemoEnemyConfig[] enemies = Array.Empty<DemoEnemyConfig>();

        [Header("刷怪与掉落")]
        [SerializeField] private DemoSpawnConfig[] spawns = Array.Empty<DemoSpawnConfig>();
        [SerializeField] private DemoDropConfig[] drops = Array.Empty<DemoDropConfig>();
        [SerializeField, Min(0)] private int bossCurrencyReward = 300;

        public DemoPlayerConfig Player => player;
        public DemoBeastMarkConfig[] BeastMarks => beastMarks;
        public DemoBuffConfig[] Buffs => buffs;
        public DemoEnemyConfig[] Enemies => enemies;
        public DemoSpawnConfig[] Spawns => spawns;
        public DemoDropConfig[] Drops => drops;
        public int BossCurrencyReward => Mathf.Max(0, bossCurrencyReward);
        [SerializeField, Min(1)] private int duplicateMarkCurrency = 100;
        public int DuplicateMarkCurrency => Mathf.Max(1, duplicateMarkCurrency);

        public DemoSpawnConfig FindSpawn(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < spawns.Length; i++)
            {
                if (spawns[i] != null && spawns[i].id == id) return spawns[i];
            }
            return null;
        }

        public DemoEnemyConfig FindEnemy(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < enemies.Length; i++)
            {
                if (enemies[i] != null && enemies[i].id == id) return enemies[i];
            }
            return null;
        }

        public DemoDropConfig FindDrop(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            for (int i = 0; i < drops.Length; i++)
                if (drops[i] != null && drops[i].id == id) return drops[i];
            return null;
        }
    }

    [Serializable]
    public sealed class DemoPlayerConfig
    {
        [Min(0f)] public float hitStunDuration = 0.15f;
        [Min(1)] public int maxHealth = 100;
        [Min(0f)] public float moveSpeed = 5f;
        [Min(0f)] public float attackCooldown = 0.35f;
        [Min(0f)] public float baseAttackDamage = 10f;
        [Min(0f)] public float comboSecondMultiplier = 1.3f;
        [Min(0f)] public float comboWindow = 0.8f;
        [Min(0f)] public float attackWindup = 0.12f;
        [Min(0f)] public float attackRecovery = 0.2f;
        [Min(0f)] public float skillDamageMultiplier = 2f;
        [Min(0f)] public float skillCooldown = 5f;
        [Min(0f)] public float ultimateDamageMultiplier = 5f;
        [Min(1f)] public float energyMax = 100f;
        [Min(0f)] public float energyRegenPerSecond = 5f;
        [Min(0f)] public float dodgeCooldown = 1.2f;
        [Min(0f)] public float dodgeInvulnerability = 0.2f;
        [Min(0f)] public float dodgeDistance = 2f;
        [Min(0.01f)] public float dodgeDuration = 0.2f;
        [Min(0)] public int reviveCount = 1;
        [Range(0f, 1f)] public float reviveHealthPercent = 0.5f;
    }

    [Serializable]
    public sealed class DemoBeastMarkConfig
    {
        public string id;
        public string displayName;
        public string iconKey;
        public Sprite icon;
        [Min(1)] public int level = 1;
        [Min(0)] public int slotCost = 1;
        [TextArea] public string description;
        public float attackMultiplier = 1f;
        public float defenseMultiplier = 1f;
        [Min(0f)] public float normalAttackMultiplier = 1f;
        [Min(0f)] public float skillImbalanceMultiplier = 1f;
        [Min(0f)] public float ultimateDamageMultiplier = 1f;
        [Min(0f)] public float attributeAttackPercent;
        [Min(0f)] public float attackDamageReductionPercent;
        [Min(0f)] public float skillRangePercent;
        [Min(0f)] public float ultimateDamageReductionPercent;
        [Min(0f)] public float attackSpeedPercent;
        [Min(0f)] public float skillCooldownReductionPercent;
        [Min(0f)] public float ultimateEnergyGainPercent;
        [Min(0f)] public float attributeHealthPercent;
        [Min(0f)] public float attributeMoveSpeedPercent;
        [Min(0f)] public float normalHitReductionDuration;
        [Min(0f)] public float normalAttackMoveSpeedPercent;
        [Min(0f)] public float normalAttackMoveSpeedDuration;
        [Min(0f)] public float ultimateProtectionDuration;
    }

    [Serializable]
    public sealed class DemoBuffConfig
    {
        public string id;
        public string displayName;
        [Min(1)] public int level = 1;
        [TextArea] public string description;
        public float value;
    }

    [Serializable]
    public sealed class DemoEnemyConfig
    {
        [Min(0f)] public float hitStunDuration = 0.15f;
        public bool canStagger = true;
        [Min(0f)] public float staggerThreshold = 30f;
        [Min(0f)] public float staggerWindow = 3f;
        [Min(0f)] public float staggerDuration = 2f;
        [Min(1f)] public float staggerDamageMultiplier = 1.5f;
        public string id;
        public string displayName;
        public bool isElite;
        public bool isBoss;
        [Min(1)] public int maxHealth = 50;
        [Min(0f)] public float moveSpeed = 2f;
        [Min(0f)] public float attackDamage = 10f;
        [Min(0f)] public float attackCooldown = 1.5f;
        [Min(0f)] public float attackRange = 1.5f;
        [Min(0f)] public float chargeDuration = 0.35f;
        [Min(0f)] public float chargeTriggerRange = 4f;
        [Min(0f)] public float chargeSpeed = 8f;
        [Min(0f)] public float chargeDistance = 3f;
        public string dropTableId;
        [Range(0f, 1f)] public float secondPhaseHealthRatio = 0.6f;
        [Range(0.1f, 1f)] public float secondPhaseCooldownMultiplier = 0.7f;
        [Min(0f)] public float secondPhaseAreaRadius = 3f;
        [Tooltip("仅战斗时长设计目标，不作为运行时结束计时器")]
        [Min(0f)] public float estimatedFightMinSeconds;
        [Min(0f)] public float estimatedFightMaxSeconds;
    }

    [Serializable]
    public sealed class DemoSpawnConfig
    {
        public string id;
        public string enemyId;
        public string spawnPointId;
        [Min(1)] public int count = 1;
        [Min(0)] public int wave = 0;
        [Min(0f)] public float activationRadius = 8f;
    }

    [Serializable]
    public sealed class DemoDropConfig
    {
        public const string BeastMarkChoice = "beast_mark_choice";
        public const string BuffChoice = "buff_choice";
        public string id;
        public string rewardId;
        [Range(0f, 1f)] public float chance = 1f;
        [Min(1)] public int amount = 1;
        public bool guaranteedForBoss;
        public bool IsChoiceReward => rewardId == BeastMarkChoice || rewardId == BuffChoice;
    }
}
