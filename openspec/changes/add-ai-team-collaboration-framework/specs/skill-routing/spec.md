## ADDED Requirements

### Requirement: 长期工作窗口与子 agent 分层路由
长期用户可见工作窗口 MUST 声明职能、优先 agent 职责和优先 SKILL 方向，但 MUST NOT 因此被 `.claude/SKILL_MATRIX.md` 限制当前环境的可用能力。只有真正显式委派的子 agent MUST 遵守 agent 配置和 SKILL 白名单。

#### Scenario: 长期窗口执行必要检查
- **WHEN** 长期工作窗口需要跨职能的轻量检查
- **THEN** 它 MAY 使用当前环境可用能力，但 MUST 不得绕过派发范围、锁或需要制作人决策的边界
