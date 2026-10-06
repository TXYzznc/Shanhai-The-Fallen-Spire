## Context

AI Friendly Frame 是不携带业务内容的 GF_X 框架基线。用户需要一组可运行的参考内容，但默认克隆后的 Unity `Assets/` 仍必须保持无激活 Sample 的状态。当前已有的基础 UI Sample 位于 `Assets/Sample/`，尚未具备安装、卸载和版本校验机制。

本变更以一个程序化电路拼接小游戏验证常用框架接入：状态流转、配置、数据、本地化、设置、事件、UI、对象复用、资源接口和安全音频调用。项目不提供真实的下载服务、发行版本或热更新产物。

## Goals / Non-Goals

**Goals:**

- 将 Sample 源内容保存在仓库根目录 `Samples~/`，使其不被 Unity 默认导入或编译。
- 提供可发现、可安装、可打开、可验证、可重装与可安全卸载的 Editor Sample Manager。
- 安装过程仅写入 manifest 声明的目标路径，并在卸载时恢复“未安装 Sample”的工作区状态。
- 提供 2D 程序化电路拼接游戏，能以独立场景运行，并自然展示 GF_X 的通用 API 和工作流。
- 让每个 Sample 都以可读 manifest 描述版本、入口场景、安装文件和可选固定资源路径。

**Non-Goals:**

- 不在未安装 Sample 时修改 `Launch`、Build Settings 或 AppConfigs 默认加载列表。
- 不实现远端更新、真实 AssetBundle/CDN 发布、真实 HybridCLR 热更新包或 Obfuz 发布验证。
- 不使用生成式图像、美术下载包、业务音效或任何具体游戏 IP。
- 不让卸载操作删除 manifest 以外的用户目录，也不自动覆盖用户修改过的安装副本。

## Decisions

### 1. 采用仓库级 `Samples~/` 作为可选包源

`Samples~/` 位于 Unity 项目根目录而非 `Assets/`，Git 跟踪其中的源包及 `.meta` 文件，Unity 不会导入它。安装器将 payload 复制到 `Assets/` 下的声明目标，并保留 `.meta`，以保证 GUID 和场景/Prefab 引用稳定。

替代方案是把所有 Sample 永久放在 `Assets/Sample/`。该方案会在所有新项目中导入、编译并显示 Sample，违反默认基线纯净要求。另一个替代方案是独立 Git 仓库；其版本与框架不易原子同步，因此不采用。

### 2. 用 manifest 驱动安装目标与回收边界

每个包具有 `sample.json`，至少声明 `id`、`version`、`displayName`、`entryScene` 和 payload 映射。映射允许两类目标：

- Sample 自有内容安装到 `Assets/Sample/<id>/`；
- 必须由 GF_X 固定路径函数解析的内容安装到相应的 `Assets/Game/.../Sample/<id>/`，并由 manifest 列出。

安装器会在目标根创建安装标记和已安装文件清单。卸载时只依据该清单删除；若文件散列或清单不匹配，工具必须停止并让用户选择手动备份、修复或取消。安装和卸载均通过 `AssetDatabase`/Unity Editor API 刷新，避免孤立 `.meta` 和 GUID 破损。

### 3. Sample Manager 是通用 Editor 工具，不包含玩法知识

Sample Manager 位于 `Assets/Game/ScriptsBuiltin/Editor/`，提供 `Tools > AI Friendly Frame > Samples` 菜单和窗口。它扫描 `Samples~/`，只处理通用 manifest 协议；对 `CircuitPuzzle`、`BasicUi` 等具体 id 没有硬编码分支。

窗口提供 Install、Open、Validate、Repair 和 Uninstall。高风险的覆盖与删除显示明确目标列表并要求确认。工具不修改默认 `Launch` 或 Build Settings；清单可以声明样例专属的 DataTable、Config、Language 与 Procedure 注册项。安装器会保存 `AppConfigs` 四个数组的完整快照和资产哈希，卸载时仅在当前状态仍与记录的安装后状态一致时恢复该快照。

当样例需要接管完整启动链时，清单改为声明完整 `AppConfigs` 配置档，而不是在当前项目配置上增量追加。管理器在项目根目录 `.ai-friendly-frame/sample-state/<sample-id>/` 写入被 Git 忽略的原始 `AppConfigs.asset` 备份和恢复状态，再整体替换四个加载列表及二进制加载开关。完整配置档在激活期间独占 `AppConfigs`：其他会修改该资产的样例不能同时安装。卸载前校验安装后哈希和备份哈希；任一不匹配即停止，避免覆盖用户的新配置。若安装在写入安装标记前中断，窗口提供显式恢复入口。

需要从正式 `Launch` 流程加载的样例还可以在 manifest 中显式声明入口场景应加入 Build Settings。安装器只会在场景不存在于当前列表时将其追加，并在安装记录中保存完整的路径、启用状态和顺序快照。卸载或重装前先校验当前列表仍等于安装后快照；通过后恢复安装前列表，若用户已有手工改动则停止而不覆盖。未声明该字段的样例完全不触碰 Build Settings。

### 4. 电路拼接采用独立场景和局部状态机

`CircuitPuzzle` 的入口场景由 Sample Manager 打开。游戏状态为 `Boot → Generating → Playing → Cleared / Paused`，由样例自身控制，不注册到框架默认 Procedure 链。单局使用 6×6 可配置网格：旋转直线、弯线、三通和端点，使能量由入口连至出口；胜利后生成下一局。

玩法对象以运行时绘制的 UI 图形、LineRenderer 或简单 Mesh 表达。节点和能量脉冲采用对象复用，关卡由种子复现。样例通过明确的 Adapter 调用 `GF.Event`、`GF.Setting`、`GF.Localization`、`GF.DataTable`/Config、`GF.Resource` 和 `SoundExtension`；CircuitLevelTable、CircuitPuzzleConfig 和 CircuitPuzzle 语言表分别演示三类 `AppConfigs` 注册数据。样例流程在 `FrameworkReadyProcedure` 之后打开场景；独立场景入口保留作快速预览。当示例没有构建资源或音频文件时，Resource/Sound 路径必须呈现诊断和安全降级，而非抛出未处理异常。

替代方案是把游戏写进框架默认 Procedure；这会将示例行为带进所有项目启动路径，因此不采用。

### 5. 以能力面板和可验证行为展示框架，而非虚假链路

游戏内“Framework Status”面板显示当前关卡种子、配置来源、事件次数、对象池活跃数、语言、设置值和资源/音频可用状态。它只展示实际已调用和已成功/安全降级的能力。真实热更新、远端下载与资源发布以“未配置”的明确状态展示，不伪造成功。

## Risks / Trade-offs

- [安装副本被用户修改后难以安全回收] → manifest 保存清单和散列；不匹配时拒绝自动删除。
- [固定 GF_X 资源路径导致 Sample 写入多个目录] → manifest 显式列出所有目标，卸载严格按清单回收，不扫描或删除宽泛目录。
- [资源模式或未构建 Bundle 影响独立 Sample 场景] → 所有关键玩法使用内置程序化绘制；资源接口演示具有编辑器/未构建的安全诊断路径。
- [示例覆盖能力过多而失去可读性] → 每个能力只映射一个可观察的玩法或状态面板行为，并采用分阶段任务验收。
- [示例源包与安装副本版本漂移] → Sample Manager 显示版本和校验结果，并提供 Repair/Reinstall 而不是静默覆盖。
- [样例修改共享 AppConfigs 后无法安全卸载] → 保存安装前快照及安装后哈希；不匹配时拒绝自动恢复，要求先修复或人工处理。

## Migration Plan

1. 将现有 `Assets/Sample/BasicUi` 迁移至 `Samples~/BasicUi`，并将 `Assets/Sample/` 加入忽略规则。
2. 实现 manifest 模型与 Sample Manager，先支持 BasicUi 的安装、打开和移除。
3. 添加 CircuitPuzzle 源包和独立场景，在未改变默认启动链的情况下验证安装后可运行。
4. 运行卸载、重装、路径冲突、用户修改和 Unity GUID/编译验证。
5. 回滚时删除安装副本、移除 Editor 工具与 `Samples~/` 源包，并还原 `.gitignore`；框架默认入口未被改动，因此无需运行时迁移。

## Open Questions

无。若后续需要将 Sample 集成到实际发布流程，应另建变更来定义资源构建、版本与热更新契约。
