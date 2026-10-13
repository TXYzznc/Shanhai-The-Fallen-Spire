# AI Friendly Frame — Codex 入口

Unity 2022.3.62f3 + GF_X 的领域无关项目框架。当前基线不包含业务代码、
内容资产、产品规格、演示场景、示例数据或教学样例。

Claude Code 的等价入口为 [.claude/CLAUDE.md](./.claude/CLAUDE.md)。

## Source of truth

- `.claude/agents/*.md` 是 agent 配置源。
- `.codex/agents/*.toml` 由 `python tools/sync-agents.py` 生成，禁止直接编辑。
- `.claude/skills/<name>/SKILL.md` 是项目 SKILL 的唯一来源。
- `.claude/SKILL_MATRIX.md` 定义 agent↔SKILL 白名单。
- `openspec/` 管理中大型结构化变更。

## Codex 执行语义

匹配专业职责时先读取对应 `.codex/agents/<name>.toml`。只有用户明确要求
子 agent、委派或并行 agent 时才使用 sub-agent；否则主对话按对应职责和
白名单等价执行。

轻量的读取、解释和局部修复可以由主对话直接处理。跨职能任务由主对话编排。

## 决策门槛

检测到设计、架构、重构、大改、重写、PRD、系统、范式、方案或思路时：

1. 先使用 `grill-me` 或 `grill-with-docs` 澄清目标、边界、验收和约束；
2. 至少完成三轮关键决策收敛；
3. 连续讨论每累计两轮，至少增量归档一次已确认决策检查点；
4. 评估规模，命中中大型变更时建立 OpenSpec change；
5. 只有共识冲突、不可逆决策或触及框架核心时才中断执行。

## 多窗口协作

默认协作规范、模板与项目初始化见 [Documentation/Development/Dispatch/README.md](./Documentation/Development/Dispatch/README.md) 和 [初始化指南](./Documentation/Development/AI-Team-Collaboration-Initialization.md)。长期用户可见工作窗口按职能优先选择能力，但只有显式委派的子 agent 受 agent↔SKILL 白名单约束；活动派发、真实占用、窗口 ID、端口、项目路径与权威计划属于项目/本机状态，不得提交到框架基线。

专业窗口在已派发任务、可写路径、已确认方案和验收边界内高自治：可直接询问用户、记录决定、点对点同步受影响窗口、自主实施验证与增量归档。普通进度、单项决定、测试通过、归档和独立提交不通知制作人。所有常规暂存和提交由 `git-integration` 窗口按 [Git 集成工作流](./Documentation/Development/Dispatch/GitIntegration.md) 执行；制作人只处理强制升级事件和整个批次验收。

## 路由

| 任务 | Agent |
|---|---|
| 范围、计划、排期、风险、研究 | `producer` |
| Unity 架构、模块边界、性能预算 | `client-lead` |
| Unity C#、输入、序列化、物理、UI 技术接入 | `client-unity` |
| Shader、渲染、光照和 TA | `client-ta` |
| 服务端与网络架构 | `net-lead` |
| API、认证、缓存和消息系统 | `net-backend` |
| 数据库、索引和迁移 | `net-db` |
| 测试、缺陷分析和质量验证 | `qa-engineer` |
| 构建、CI/CD 和发布 | `devops-engineer` |
| Editor、内部工具、代码生成和 SKILL | `tools-engineer` |
| 视觉方向与资源评审 | `art-director` |
| UI、字体、2D、3D、动画、VFX 资源技术 | 对应 `art-*` |

不存在常驻的玩法、内容或产品领域 agent。具体项目需要这些能力时，在项目层
另行添加。

## 项目约束

- 平台：Windows 10。
- Unity：2022.3.62f3；所有实现、工作流与参考资料均以 Unity 2022.3 API 为准。
- 框架核心：`Assets/Game/ScriptsBuiltin/`。
- 通用扩展：`Assets/Game/Scripts/`。
- 代码结构查询优先使用 codebase-memory；不可用时用最小范围 `rg`。
- 修改 Unity 代码前先读 [.claude/conventions.md](./.claude/conventions.md)。
- Editor Play Mode 内的小范围 C# 方法体迭代可使用 FSR；配置、适用范围和验收见 [Documentation/Development/FastScriptReload.md](./Documentation/Development/FastScriptReload.md)。结构变更、泛型、字段/序列化、程序集和依赖变更必须停止 Play Mode 后按普通 Unity 编译流程验证。
- FSR 只用于 Editor Play Mode 开发效率；不启用 `LiveScriptReload_IncludeInBuild_Enabled`，不将其当作已发布 Player 的热更新方案，也不额外安装第二份 FSR。
- 不在高频循环制造 GC 分配。
- ScriptableObject 是配置载体，不是运行时数据库。
- 输入必须经过框架输入抽象。
- 不在框架基线提交业务生成物、演示资源或固定项目路径。
- 所有 Git 提交标题、概要和正文描述必须使用中文；专业术语、文件名、类型名、派发 ID 与 OpenSpec change ID 可保留原文。不得改写历史提交，所有窗口只提交、不推送。

## 框架纯度

运行：

```powershell
python tools/audit_framework_purity.py
```

审计必须覆盖 agent↔SKILL、OpenSpec、禁止内容、业务生成物和固定路径。
具体项目的领域内容应存在于项目自己的变更中，不得回写框架基线。
