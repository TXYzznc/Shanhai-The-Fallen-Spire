## Why

框架缺少可复用的多窗口协作契约，导致并行工作时的派发、资源占用、交接和本机窗口注册只能由具体项目临时约定，且难以自动审计。需要将已经验证的协作机制抽象为不携带任何产品内容的框架默认能力。

## What Changes

- 新增领域无关的多窗口调度、派发、占用、暂停恢复和项目初始化文档与模板。
- 明确长期用户可见工作窗口与显式委派子 agent 的分层路由及 SKILL 边界。
- 为方案讨论增量归档、OpenSpec 批次/并行命名和权威计划只读边界建立标准契约。
- 扩展框架纯度审计与测试，校验协作制品完整性、本机注册表忽略规则和产品内容隔离。
- 更新 Codex/Claude 入口，统一引用协作规范；所有窗口继续只提交、不推送。

## Capabilities

### New Capabilities

- `ai-team-collaboration`: 多窗口派发、占用、通信、暂停恢复、归档和初始化的领域无关协作能力。
- `framework-collaboration-purity`: 协作制品、本机注册表忽略和产品内容隔离的可重复审计能力。

### Modified Capabilities

- `workflow`: 增加方案增量归档与 OpenSpec 批次/并行命名要求。
- `skill-routing`: 明确长期工作窗口与子 agent 的分层 SKILL 路由边界。

## Impact

影响 `AGENTS.md`、`.claude/CLAUDE.md`、`.claude/AGENTS.md`、`Documentation/Development/`、`openspec/specs/`、`tools/audit_framework_purity.py` 和对应 Python 测试；不修改 Unity 框架核心、业务代码、资源或现有项目实例。
