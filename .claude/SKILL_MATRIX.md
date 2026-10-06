# Agent ↔ SKILL 白名单

`.claude/agents/*.md` 是 agent 配置的唯一来源，`.codex/agents/*.toml`
由 `python tools/sync-agents.py` 生成。agent 只能调用本表及其
frontmatter 中登记的 SKILL；需要其它能力时必须交回主对话。

本白名单仅约束显式委派的子 agent。长期用户可见工作窗口遵守项目派发范围、占用和
升级规则，并按其职能优先选择能力，但不因本表失去当前环境已提供的必要检查能力。

## 保留门槛

保留的 agent 和 SKILL 必须满足以下条件：

1. 属于框架工程、工具、构建、质量、资源技术或元工作流；
2. 不绑定项目名称、固定业务目录、业务数据模型或内容类型；
3. 不以玩法、角色、关卡、经济、任务或其它产品领域知识为核心；
4. 输入、输出和路径必须从任务上下文或配置获得；
5. 删除 SKILL 时必须同步清理 agent frontmatter、本文和索引。

## 白名单

| Agent | Tier | SKILL |
|---|---|---|
| `art-2d` | impl | `gpt-image-2-style-library`, `ai-art`, `codex-image-gen` |
| `art-3d` | impl | `3d-modeling`, `texture-art`, `blender-mcp` |
| `art-anim` | impl | `animation-systems`, `rigging` |
| `art-director` | lead | `art-direction`, `game-art`, `grill-me`, `ai-art`, `codex-image-gen` |
| `art-font` | impl | `typeset`, `font-pairing-suggester`, `font-selection-cjk`, `font-subsetting`, `pixel-font-rendering` |
| `art-ui` | impl | `game-ui-design`, `art-direction`, `ai-art`, `codex-image-gen`, `unity-rect-transform` |
| `art-vfx` | impl | `vfx-realtime`, `shader-effects` |
| `client-lead` | lead | `unity-foundations`, `unity-architecture-di`, `unity-async-patterns`, `grill-me`, `openspec` |
| `client-ta` | impl | `unity-shaders-rendering`, `unity-lighting-vfx`, `shader-effects`, `agency-unity-shader-graph-artist` |
| `client-unity` | impl | `unity-foundations`, `unity-ui`, `unity-input-correctness`, `save-serialization`, `state-machine`, `physics-collision`, `localization-i18n`, `unity-skills`, `unity-rect-transform` |
| `devops-engineer` | impl | `devops-deployment`, `github-actions-docs`, `mobile-cicd`, `secrets-management`, `deploy-checklist`, `feature-flags` |
| `net-backend` | impl | `arch-api`, `jwt-auth`, `oauth-implementation`, `backend-testing` |
| `net-db` | system | `database-schema-design`, `redis-best-practices` |
| `net-lead` | lead | `arch-api`, `game-networking`, `grill-me`, `openspec` |
| `producer` | lead | `project-management`, `task-estimation`, `risk-assessment`, `milestone-tracker`, `grill-me`, `openspec`, `deep-research` |
| `qa-engineer` | impl | `testing-strategies`, `backend-testing`, `crash-analytics`, `k6` |
| `tools-engineer` | impl | `unity-editor-scripting`, `unity-skills`, `uloop-execute-dynamic-code`, `skill-creator`, `find-skills` |

## 通用交回规则

以下任一条件触发时，agent 必须停止并交回主对话：

- 需要白名单外 SKILL；
- 出现跨职能决策；
- 缺少 MCP、权限或必要输入；
- 任务超出职责边界；
- 三轮内无法收敛；
- 触发设计、架构、重构或大型变更门槛。

主对话负责按需组合能力，不通过扩张常驻白名单来覆盖具体项目需求。
