# SKILL 索引

`.claude/skills/<name>/SKILL.md` 是项目 SKILL 的唯一来源。本索引只登记
框架工程、资源技术、质量、交付和元工作流能力；具体项目所需的产品领域
SKILL 应在项目建立后另行添加。

## 使用规则

- 触发 SKILL 前必须完整读取其 `SKILL.md`。
- agent 只能使用 `.claude/SKILL_MATRIX.md` 中的白名单。
- 路径、类型和产物名称必须来自任务上下文或配置。
- SKILL 不得携带具体项目名、业务数据结构、业务样例或历史产物。
- `python tools/audit_framework_purity.py` 用于检查索引、路由与纯度。

## 当前 SKILL

### 元工作流与项目治理

`agent-browser`, `competitive-analysis`, `deep-research`,
`dev-tools`, `document-tools`, `find-skills`, `grill-me`, `grill-with-docs`,
`milestone-tracker`, `moai-docs-generation`, `openspec`, `project-management`,
`risk-assessment`, `skill-creator`, `sprint-retrospective`, `task-estimation`,
`xlsx`

### Unity 与客户端工程

`addressables-hotfix`, `localization-i18n`, `physics-collision`,
`save-serialization`, `state-machine`, `uloop-execute-dynamic-code`,
`uloop-run-tests`, `unity-animation`, `unity-architecture-di`,
`unity-async-patterns`, `unity-build-pipeline`, `unity-dev`,
`unity-ecs-patterns`, `unity-editor-scripting`, `unity-foundations`,
`unity-input-correctness`, `unity-networking`, `unity-rect-transform`,
`unity-skills`, `unity-ui`

### 渲染、资源与内容技术

`3d-modeling`, `agency-technical-artist`,
`agency-unity-shader-graph-artist`, `ai-art`, `animation-systems`,
`art-direction`, `blender-mcp`, `codex-image-gen`, `font-pairing-suggester`,
`font-selection-cjk`, `font-subsetting`, `game-art`, `game-ui-design`,
`gpt-image-2-style-library`, `image-compression`, `pixel-font-rendering`,
`rigging`, `shader-effects`, `texture-art`, `typeset`, `ui-asset-splitting`,
`unity-lighting-vfx`, `unity-shaders-rendering`, `vfx-realtime`

### 服务端、数据与网络工程

`arch-api`, `backend-testing`, `database-schema-design`, `game-networking`,
`jwt-auth`, `k6`, `kafka-development`,
`oauth-implementation`, `opentelemetry`, `prometheus`, `redis-best-practices`,
`redis-specialist`

### 质量、构建与发布

`ab-testing`, `asc-submission-health`, `cdn-setup`, `crash-analytics`,
`deploy-checklist`, `devops-deployment`, `feature-flags`,
`github-actions-docs`, `mobile-cicd`, `mobile-device-testing`,
`secrets-management`, `semver`, `setup-fastlane`, `steam-deploy`,
`testing-strategies`

索引必须与目录实际内容一致；不设置候选淘汰区，未通过保留门槛的 SKILL
直接删除。
