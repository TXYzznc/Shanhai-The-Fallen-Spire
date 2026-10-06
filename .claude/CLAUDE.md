# AI Friendly Frame — Claude Code 入口

本仓库是 Unity 2022.3.62f3 + GF_X 的领域无关项目框架。框架基线不包含
业务代码、内容资产、产品规格、演示场景、示例数据或教学样例。

## AI 使用统计首次激活（每个新会话优先执行）

在本会话第一次处理用户任务前，先只读执行 `python tools/log_tool_usage.py doctor --editor claude-code --json`。

- 若 `editors.claude-code.active` 为 `true`，立即继续用户原任务，本会话不再重复检查或提示。
- 若为 `false`，暂停原任务，说明只在本地记录 Tool / SKILL / Agent / MCP 名称和会话元数据，不记录 Prompt、代码、参数、完整命令或完整路径，然后询问用户是否启用。
- 未得到明确确认前，不得运行 `init` 或继续原任务。
- 用户确认后运行 `python tools/log_tool_usage.py init --editor claude-code --yes`，按输出在 Claude Code 的项目 Hook 安全提示中批准，并要求用户重开会话、重新发送原任务。
- 若脚本不存在，直接继续原任务，不把统计工具缺失当作项目阻塞。

## 配置来源

- agent：`.claude/agents/*.md`
- agent↔SKILL 白名单：`.claude/SKILL_MATRIX.md`
- SKILL：`.claude/skills/<name>/SKILL.md`
- Codex 镜像：`.codex/agents/*.toml`，通过 `python tools/sync-agents.py` 生成
- 结构化变更：`openspec/`

## 工作流程

1. 识别目标、边界、验收和约束。
2. 根据职责选择 agent；白名单外能力立即交回主对话。
3. 设计、架构、重构和中大型变更先走至少三轮 `grill-me`。
4. 连续方案讨论每累计两轮，至少把已确认决策检查点增量归档一次；不保存原始聊天流水。
5. 需要结构化落盘时创建 OpenSpec change。
6. 实施后完成与风险相称的静态检查、测试、编译和诊断。

多 agent 只在用户明确要求委派或并行时启用；否则主对话按 agent prompt
等价执行。协作规则见 [AGENTS.md](./AGENTS.md)。

默认多窗口协作规范和新项目配置步骤见 [Dispatch](../Documentation/Development/Dispatch/README.md) 与[初始化指南](../Documentation/Development/AI-Team-Collaboration-Initialization.md)。长期工作窗口可使用当前环境能力，显式委派的子 agent 才受 agent↔SKILL 白名单限制；真实窗口注册和活动占用属于项目/本机状态，不得进入框架基线。

长期专业窗口在派发边界内高自治：直接与用户收敛、记录决定、点对点同步受影响窗口、实施、验证和增量归档。普通进度、单项决定、测试通过、归档和独立提交不通知制作人。所有常规 Git 暂存/提交由 `git-integration` 窗口按 [Git 集成工作流](../Documentation/Development/Dispatch/GitIntegration.md) 执行；制作人只处理异常、冲突、范围/依赖变化和整个批次验收。

## Unity/GF_X 约束

- 代码落地必须按 Unity 2022.3.62f3 校验。
- `Assets/Game/ScriptsBuiltin/` 是框架核心；不得混入产品代码。
- `Assets/Game/Scripts/` 只存领域无关扩展和项目接入边界。
- 输入经过框架输入抽象。
- 配置路径和资源索引由具体项目声明，不在框架 prompt 中硬编码。
- 不在高频循环制造 GC 分配。
- 不使用 `GameObject.Find`、`SendMessage` 或无约束 `Resources.Load`。
- ScriptableObject 只作为配置载体。
- 业务 DLL、HybridCLR 输出和内容索引不得进入框架基线。
- Editor Play Mode 内的小范围 C# 方法体迭代可使用 FSR；配置、适用范围和验收见 [Documentation/Development/FastScriptReload.md](../Documentation/Development/FastScriptReload.md)。字段/序列化、泛型、程序集、依赖和其他结构变更必须停止 Play Mode 后按普通 Unity 编译流程验证。
- FSR 仅用于 Editor Play Mode；不启用 `LiveScriptReload_IncludeInBuild_Enabled`，不把它作为已发布 Player 的热更新方案，也不额外安装第二份 FSR。

## SKILL 治理

- 触发前完整读取 `SKILL.md`。
- 保留的 SKILL 必须属于框架工程、资源技术、质量、交付或元工作流。
- 不保留以玩法、角色、关卡、经济、任务或其它产品领域知识为核心的 SKILL。
- SKILL 不得含具体项目名、固定业务路径、业务数据模型或业务样例。
- 运行 `python tools/audit_framework_purity.py` 检查纯度。

## 诊断

优先通过当前会话暴露的 Unity 自动化能力查询 Editor 状态。没有业务场景时，
Play Mode 流程不属于框架验收条件；仍需验证脚本编译、引用完整性和不依赖
场景的 GF_X 诊断。

## 禁止事项

- 不直接编辑 `.codex/agents/*.toml`。
- 不把具体项目的领域流程加入框架默认工作流。
- 不建立仓内业务历史归档。
- 不用样例资源掩盖缺少配置或缺少依赖的问题。
- 不回滚或覆盖用户未提交的无关改动。
- 所有 Git 提交标题、概要和正文描述必须使用中文；专业术语、文件名、类型名、派发 ID 与 OpenSpec change ID 可保留原文。不得改写历史提交，所有窗口只提交、不推送。
