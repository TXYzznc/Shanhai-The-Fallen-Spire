# skill-routing Specification

## Purpose

约束 agent 只调用已登记、实际存在且领域无关的 SKILL，并确保源 agent、Codex 镜像、SKILL 索引与白名单保持一致，使路由失败能够被自动发现而不是在执行时静默降级。

## Requirements

### Requirement: SKILL 白名单是硬边界

agent MUST 仅调用其 frontmatter 中登记的 SKILL；白名单外能力 MUST 立即交回
主对话。

#### Scenario: 请求白名单外能力
- **WHEN** agent 判断任务需要未登记 SKILL
- **THEN** agent MUST 停止当前职责并向主对话说明缺口

### Requirement: 路由不得绑定产品领域

agent 职责 MUST 以架构、实现、工具、质量、交付或资源技术为边界，MUST NOT
把具体内容类型、产品机制或业务数据作为常驻职责。

#### Scenario: agent 依赖具体领域模型
- **WHEN** agent prompt 的核心职责只有在某类产品中才成立
- **THEN** 该 agent MUST 删除或收敛为领域无关职责

### Requirement: 源与镜像必须一致

`.claude/agents/*.md` MUST 是唯一来源，`.codex/agents/*.toml` MUST 由同步工具
生成，并且两者的名称、职责和 SKILL 白名单 MUST 一致。

#### Scenario: 运行 agent 同步检查
- **WHEN** 执行 `python tools/sync-agents.py --check`
- **THEN** 缺失、额外或内容不一致 MUST 以非零退出码失败

### Requirement: 路由不得硬编码业务路径

agent prompt MUST 从任务上下文或配置获取路径和产物名，MUST NOT 固定业务目录、
业务 catalog、业务场景或历史项目名。

#### Scenario: agent 需要操作文件
- **WHEN** prompt 描述输入或输出位置
- **THEN** 位置 MUST 使用配置来源或领域中性占位符

### Requirement: 长期工作窗口与子 agent 分层路由

长期用户可见工作窗口 MUST 声明职能、优先 agent 职责和优先 SKILL 方向，但 MUST NOT 因此被 `.claude/SKILL_MATRIX.md` 限制当前环境的可用能力。只有真正显式委派的子 agent MUST 遵守 agent 配置和 SKILL 白名单。

#### Scenario: 长期窗口执行必要检查
- **WHEN** 长期工作窗口需要跨职能的轻量检查
- **THEN** 它 MAY 使用当前环境可用能力，但 MUST 不得绕过派发范围、锁或需要制作人决策的边界
