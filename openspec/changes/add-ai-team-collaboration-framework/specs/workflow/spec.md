## ADDED Requirements

### Requirement: 方案讨论按检查点增量归档
设计、架构、重构或中大型变更的方案讨论 MUST 至少进行三轮关键决策收敛，并且连续讨论每累计两轮 MUST 增量归档一次已确认决策、明确排除、待决事项和受影响工作。增量归档 MUST NOT 保存原始聊天流水，也 MUST NOT 代替 OpenSpec artifact 创建前的用户确认。

#### Scenario: 长方案讨论继续到第三轮
- **WHEN** 同一方案已完成两轮关键讨论且仍需继续
- **THEN** 当前窗口 MUST 先更新增量归档及对应权威文档，再继续下一轮讨论

### Requirement: OpenSpec 批次和并行命名可追溯
一个 OpenSpec change SHOULD 一般覆盖二至五项紧密相关工作；change 名称 MAY 对应任务标识。并行 change MUST 共享 `b<两位序号>` 批次前缀，并包含 `parallel` 与职能标识。实现细节和架构 MUST 先经用户确认，再写入 OpenSpec artifacts。

#### Scenario: 同批次存在两个独立职能变更
- **WHEN** 制作人派发可并行的两个职能 change
- **THEN** 两个 change MUST 使用相同批次前缀并分别完成用户确认
