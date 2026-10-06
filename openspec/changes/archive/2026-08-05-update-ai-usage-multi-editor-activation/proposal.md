## Why

框架仍携带只覆盖 Claude Code 与 Codex 的旧版统计工具，缺少统一 `init`、`doctor`、`report` 入口以及 Cursor、Kiro、TRAE 适配。新项目从该框架创建后不能获得当前已验证的首次对话安全激活体验。

## What Changes

- 升级公共记录器、审计器、测试和文档到统一五编辑器版本。
- 新增 Codex 精确信任辅助程序，以及 Cursor、Kiro、TRAE 项目 Hook。
- 在框架自己的精简 `AGENTS.md` 和 `.claude/CLAUDE.md` 中合并首次对话预检规则，不覆盖其他框架约束。
- 更新 Claude Code Hook，使会话和所有工具调用进入统一日志。
- 保持本地隐私白名单、fail-open 和旧命令兼容性。

## Capabilities

### New Capabilities

<!-- None. -->

### Modified Capabilities

- `ai-tool-usage-telemetry`: 增加五编辑器一等适配、初始化、诊断、报告和首次对话安全确认。

## Impact

- `tools/log_tool_usage.py`、审计器、测试和使用文档。
- `.codex/`、`.claude/`、`.cursor/`、`.kiro/`、`.trae/` 项目配置。
- `AGENTS.md` 与 `openspec/specs/ai-tool-usage-telemetry/spec.md`。
