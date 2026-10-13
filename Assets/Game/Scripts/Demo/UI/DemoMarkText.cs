using Shanhai.Demo.Configuration;
namespace Shanhai.Demo.UI
{
    internal static class DemoMarkText
    {
        public static string SlotName(int slot)
        {
            switch (slot) { case 0: return "普攻"; case 1: return "技能"; case 2: return "大招"; default: return "属性"; }
        }
        public static string Effect(DemoBeastMarkConfig mark, int slot)
        {
            if (mark == null) return "无兽印效果";
            switch (slot)
            {
                case 0:
                    if (mark.normalAttackMultiplier != 1f) return $"二段普攻伤害 +{(mark.normalAttackMultiplier - 1f) * 100:0}%";
                    if (mark.attackDamageReductionPercent > 0) return $"命中后减伤 {mark.attackDamageReductionPercent:0}%，持续 {mark.normalHitReductionDuration:0.##}秒";
                    return $"普攻开始时移速 +{mark.normalAttackMoveSpeedPercent:0}%，持续 {mark.normalAttackMoveSpeedDuration:0.##}秒";
                case 1:
                    if (mark.skillImbalanceMultiplier != 1f) return $"技能失衡累计 +{(mark.skillImbalanceMultiplier - 1f) * 100:0}%";
                    if (mark.skillRangePercent > 0) return $"技能范围 +{mark.skillRangePercent:0}%";
                    return $"技能冷却 -{mark.skillCooldownReductionPercent:0}%";
                case 2:
                    if (mark.ultimateDamageMultiplier != 1f) return $"大招伤害 +{(mark.ultimateDamageMultiplier - 1f) * 100:0}%";
                    if (mark.ultimateDamageReductionPercent > 0) return $"大招减伤 {mark.ultimateDamageReductionPercent:0}%，持续 {mark.ultimateProtectionDuration:0.##}秒";
                    return $"能量恢复 +{mark.ultimateEnergyGainPercent:0}%";
                default:
                    if (mark.attributeAttackPercent > 0) return $"攻击 +{mark.attributeAttackPercent:0}%";
                    if (mark.attributeHealthPercent > 0) return $"生命上限 +{mark.attributeHealthPercent:0}%";
                    return $"移速 +{mark.attributeMoveSpeedPercent:0}%";
            }
        }
    }
}
