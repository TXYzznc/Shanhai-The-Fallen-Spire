## Context

目标框架含旧版事件记录协议和 Claude/Codex Hook。新版已经在上游项目及框架模板中通过测试，本次迁移需要保留此仓库更精简的领域无关指令，同时更新公共工具与五端适配。

## Goals / Non-Goals

**Goals:**

- 同步经过验证的公共 CLI、隐私过滤和五端配置。
- 保留目标仓库现有框架指令与其他 Hook。
- Codex 只通过原生 app-server 精确信任当前项目 Hook 哈希。

**Non-Goals:**

- 自动绕过宿主安全确认。
- 修改 Unity 业务或框架运行时代码。
- 上传事件或采集 Prompt、代码、参数及完整路径。

## Decisions

- 工具、测试和文档采用已验证版本整体同步，避免两个框架副本继续漂移。
- `AGENTS.md` 与 `.claude/CLAUDE.md` 只合并首次激活段落，不整体覆盖。
- `.claude/settings.json` 结构化补入会话记录并扩大工具匹配，其余 Hook 保留。
- Cursor、Kiro、TRAE 使用各自官方项目配置路径；不能程序化授权时由宿主 UI 完成。

## Risks / Trade-offs

- [目标指令被上游完整覆盖] → 只做局部合并并在同步后审查 diff。
- [编辑器版本差异] → 通过 `doctor` 区分配置、信任与实时证据，不伪报激活。
- [旧事件影响判断] → 只把最近 24 小时非推断、非迁移事件作为实时证据。

## Migration Plan

1. 同步工具、测试、文档和五端适配文件。
2. 局部合并首次激活指令和 Claude Hook。
3. 运行单测、`doctor`、报告及严格规格校验。
4. 归档变更；失败时按文件 diff 回退本次迁移。

## Open Questions

- 无。
