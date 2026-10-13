## Why

用户确认移动与攻击期间允许闪避并具有无敌帧。当前攻击锁在TryDodge中拒绝闪避，与新规则冲突；原有配置已提供无敌时间与免伤判定。

## What Changes

移除攻击锁对闪避的限制；成功闪避取消未结算普攻、解除后摇并复位连段，保留冷却与已结算结果。同帧以成功闪避优先，一次输入快照只执行一次位移。同步旧文档限制、暂定处理与单元测试。

## Capabilities

### New Capabilities
- `player-dodge-priority`: 移动/攻击中闪避、无敌时间与同帧优先级。

### Modified Capabilities
无框架变更。

## Impact

DemoPlayerController与DemoPlayerCombatController、测试、项目设计文档及任务表。无新包，保留已有输入与暂停边界；旧Player需要重新构建。
