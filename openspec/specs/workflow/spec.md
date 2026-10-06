# workflow Specification

## Purpose

定义领域无关且可审计的变更生命周期，统一约束提案、设计、任务、验证和归档制品，不预设具体项目的内容生产流程、业务目录、玩法类型、资源命名或示例实现。

## Requirements

### Requirement: OpenSpec 承载结构化变更

中大型变更 MUST 使用 `openspec/changes/<name>/` 承载 proposal、design、specs、
tasks 和验证结果。

#### Scenario: 中大型变更完成澄清
- **WHEN** 任务规模评估命中结构化变更门槛
- **THEN** 实现 MUST 在 OpenSpec artifact 完成后开始

### Requirement: 工作流只规定制品生命周期

默认工作流 MUST 只规定澄清、提案、设计、任务、实现、验证和归档，MUST NOT
预设内容生产、产品机制或业务测试阶段。

#### Scenario: 变更不需要专属子流程
- **WHEN** 任意框架或工程变更进入 OpenSpec
- **THEN** 它 MUST 能仅使用标准 artifact 完成

### Requirement: 项目扩展由具体项目声明

产品领域子流程 MUST 由具体项目的 change、agent 或 SKILL 增加，MUST NOT
回写框架默认工作流。

#### Scenario: 新项目需要附加流程
- **WHEN** 项目确定自己的交付制品
- **THEN** 扩展 MUST 存在于项目层并声明与框架工作流的接口

### Requirement: 模板不得含业务样例

工作流模板 MUST 只描述字段和验收契约，MUST NOT 填入具体项目正文或资源。

#### Scenario: 创建模板
- **WHEN** 工作流提供可复用模板
- **THEN** 模板 MUST 使用空字段或领域中性占位符

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
