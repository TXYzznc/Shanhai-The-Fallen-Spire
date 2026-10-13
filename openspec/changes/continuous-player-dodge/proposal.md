## Why

原闪避一次性设置终点坐标，表现为瞬移。用户确认改为0.2秒内连续快速移动2米。

## What Changes

按帧推进锁定方向的闪避位移，遇障碍或边界停止；普通移动不叠加，暂停冻结剩余进度，重试及禁用清除进度。保留冷却、无敌、攻击取消及同帧优先级；同步配置和测试。

## Capabilities

### New Capabilities
- `continuous-player-dodge`: 连续闪避与进度生命周期。

### Modified Capabilities
无框架核心变更。

## Impact

DemoPlayerController、DemoGameplayConfig、项目配置、现有闪避/玩法测试与项目规格。用户已取消录屏与实玩；本次不增加录屏工具、依赖或UI操作。
