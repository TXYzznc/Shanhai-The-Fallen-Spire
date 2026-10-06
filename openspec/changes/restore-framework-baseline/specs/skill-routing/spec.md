## ADDED Requirements

### Requirement: 路由角色不得绑定玩法领域

agent 路由 MUST 以框架架构、Unity 实现、工程工具、质量、发布、资源技术和项目治理等领域无关职责为边界，MUST NOT 以具体玩法系统或内容类型作为常驻框架角色。

#### Scenario: agent 的职责依赖玩法概念
- **WHEN** agent prompt 的核心职责需要具体玩法、内容模型或业务数据才能成立
- **THEN** 该 agent MUST 删除或收敛为领域无关职责

### Requirement: 白名单不得引用已删除 SKILL

`.claude/agents/*.md`、`.claude/SKILL_MATRIX.md`、`.claude/skills/SKILLS_INDEX.md` 和生成的 `.codex/agents/*.toml` MUST 仅引用实际存在且通过保留门槛的 SKILL。

#### Scenario: 同步后检查白名单
- **WHEN** 从 `.claude/agents/` 重新生成 Codex agent 镜像
- **THEN** agent↔SKILL 一致性审计 MUST 不报告缺失或领域型 SKILL

### Requirement: 路由不得硬编码业务路径和业务产物

agent prompt MUST NOT 固定引用业务表、业务 catalog、业务 Prefab、业务场景、具体项目名或历史归档路径。

#### Scenario: agent 需要描述输入输出位置
- **WHEN** prompt 定义文件或资源操作
- **THEN** 路径 MUST 来自任务上下文、配置或领域中性占位符
