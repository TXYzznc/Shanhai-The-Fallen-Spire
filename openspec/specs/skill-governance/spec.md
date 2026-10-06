# skill-governance Specification

## Purpose

确保框架只保留领域无关、可路由、可审计的工程与元工作流 SKILL，并通过统一入口、索引和自动检查阻止具体产品知识、业务路径、历史产物及示例模板重新进入框架基线。

## Requirements

### Requirement: SKILL 必须通过领域无关门槛

保留的 SKILL MUST 属于框架工程、资源技术、质量、交付或元工作流，并且
MUST 在不假设具体产品领域模型时仍具有完整价值。

#### Scenario: SKILL 依赖具体产品领域
- **WHEN** SKILL 的核心目标需要具体内容、机制或业务数据才能成立
- **THEN** 该 SKILL MUST 从源目录、索引和路由删除

### Requirement: SKILL 不得携带业务样例

SKILL MUST NOT 包含具体项目名、固定业务路径、业务数据结构或历史业务产物。
操作说明 MUST 使用参数、占位符和领域中性的输入输出契约。

#### Scenario: 工程型 SKILL 含固定项目输入
- **WHEN** 审计发现项目标识、绝对业务路径或固定业务文件
- **THEN** 审计 MUST 失败并报告文件与命中内容

### Requirement: SKILL 必须可被稳定发现

每个 SKILL MUST 有 `SKILL.md`、有效 frontmatter 和明确触发描述；索引 MUST
与磁盘目录一致。

#### Scenario: 运行 SKILL 审计
- **WHEN** 执行框架纯度审计
- **THEN** 缺入口、缺 frontmatter、缺索引或幽灵索引 MUST 以非零退出码失败

### Requirement: 使用事件必须跨编辑器统一记录

工具使用审计 MUST 记录来源、时间和能力名，MUST NOT 把零使用次数直接作为
删除依据。

#### Scenario: 生成使用审计
- **WHEN** 执行使用事件统计
- **THEN** 报告 MUST 显示来源覆盖和时间范围
