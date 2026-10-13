using UnityEngine;

namespace Shanhai.Demo.Combat
{
    public interface IDemoAttackLock
    {
        bool IsAttackLocked { get; }
    }

    public readonly struct DemoDamageInfo
    {
        public DemoDamageInfo(float amount, GameObject source, float imbalanceMultiplier = 1f)
        {
            Amount = amount;
            Source = source;
            ImbalanceMultiplier = imbalanceMultiplier;
        }

        public float Amount { get; }
        public GameObject Source { get; }
        public float ImbalanceMultiplier { get; }
    }

    public interface IDemoDamageReceiver
    {
        void ReceiveDamage(DemoDamageInfo damage);
    }
}
