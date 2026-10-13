using System;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Shanhai.Demo.Player
{
    /// <summary>仅本局内存进度快照；不保存至磁盘，不修改复活或重试的出生规则。</summary>
    public sealed class DemoCheckpointSnapshot
    {
        public string CheckpointId { get; }
        public int RunNumber { get; }
        public Vector2 PlayerPosition { get; }
        public float Health { get; }
        public float MaxHealth { get; }
        public int RevivesRemaining { get; }
        public float Energy { get; }
        public int Currency { get; }
        public bool UpgradeConfirmed { get; }
        public string UpgradeId { get; }
        public ReadOnlyCollection<string> Marks { get; }
        public ReadOnlyCollection<string> Buffs { get; }
        public ReadOnlyCollection<DemoCheckpointProgress> Waves { get; }
        public ReadOnlyCollection<DemoCheckpointProgress> Encounters { get; }

        internal DemoCheckpointSnapshot(string checkpointId, int runNumber, Vector2 position, float health, float maxHealth,
            int revivesRemaining, float energy, int currency, bool upgradeConfirmed, string upgradeId,
            string[] marks, string[] buffs, DemoCheckpointProgress[] waves, DemoCheckpointProgress[] encounters)
        {
            CheckpointId = checkpointId; RunNumber = runNumber; PlayerPosition = position;
            Health = health; MaxHealth = maxHealth; RevivesRemaining = revivesRemaining; Energy = energy; Currency = currency;
            UpgradeConfirmed = upgradeConfirmed; UpgradeId = upgradeId;
            // Arrays are newly allocated by the capture operation and never shared with live loadouts.
            Marks = Array.AsReadOnly(marks); Buffs = Array.AsReadOnly(buffs);
            Waves = Array.AsReadOnly(waves); Encounters = Array.AsReadOnly(encounters);
        }
    }

    public sealed class DemoCheckpointProgress
    {
        public string Id { get; }
        public bool HasStarted { get; }
        public bool IsCompleted { get; }
        public int SpawnedCount { get; }
        internal DemoCheckpointProgress(string id, bool started, bool completed, int count)
        { Id = id; HasStarted = started; IsCompleted = completed; SpawnedCount = count; }
    }
}
