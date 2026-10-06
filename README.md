# AI Friendly Frame

面向任意游戏类型的 Unity 开发框架。它提供一个可直接打开、可直接运行、但不绑定玩法和内容资产的项目起点：先完成启动、配置、资源、UI、实体、音频与工具链的通用接入，再由具体项目添加玩法、关卡、美术和线上服务。

框架基线不包含任何特定游戏的角色、地图、数值、任务、音效、演示资源或产品规格。可选教学内容通过独立安装包提供，安装与卸载不会改变框架基线。

## 包含的能力

| 领域 | 已具备的内容 | 项目需要自行补充的内容 |
| --- | --- | --- |
| 启动流程 | `Launch` 场景、资源准备、配置加载、数据表加载、语言加载、分组初始化、框架就绪流程 | 登录、选服、热更新策略、首个业务场景与玩法流程 |
| 配置与数据 | `AppConfigs`、DataTable、Config、Language 的加载与 Excel 生成工具；实体、UI、音频分组基础表 | 游戏领域表、数值、关卡数据、运营配置与本地化内容 |
| UI 与实体 | UI 基类、UI 分组、实体基类、实体分组和对象池接入 | 具体界面、交互、角色、战斗对象和表现逻辑 |
| 音频 | `SoundExtension`、音乐/音效分组、音量与静音设置、无资源时的安全降级 | 音频资源、播放时机、混音与动态音频规则 |
| 资源与构建 | 资源构建入口、AssetBundle 工作流、资源规则编辑、热更新程序集与混淆配置入口 | 发布地址、版本策略、内容分包、下载与回滚策略 |
| 编辑器工具 | AppConfigs 面板、表格生成、资源构建、诊断、可选示例包管理器 | 项目专用检查、批处理工具与发布自动化 |
| AI 协作 | Agent 职责、SKILL 白名单、结构化变更记录、框架纯度审计与 Unity 自动化接口 | 项目专属 Agent、领域 SKILL、产品知识库与 CI 规则 |

## 快速开始

### 1. 打开项目

使用 Unity Hub 的 **Unity 2022.3.62f3c1** 打开项目。首次导入时请保持网络可用，等待 Package Manager 解析依赖、资源导入和程序集编译完成。

```powershell
git clone --recurse-submodules git@github.com:TXYzznc/AI-Friendly-Project.git
```

### 2. 从 Launch 场景运行

打开 [`Assets/Game/Scene/Launch.unity`](Assets/Game/Scene/Launch.unity) 并进入 Play Mode。这是框架的唯一标准入口；它会完成启动链、读取配置、加载表格和语言、创建基础分组，最后进入 `FrameworkReadyProcedure`。

不要将业务或示例场景替换为默认启动场景。需要进入业务内容时，应在框架就绪后由 Procedure 切换场景。

### 3. 配置启动内容

选择 [`Assets/Game/ScriptableAssets/Core/AppConfigs.asset`](Assets/Game/ScriptableAssets/Core/AppConfigs.asset)，在 Inspector 的 **App Configs** 面板中维护：

- **Procedures**：启动链、业务入口和场景切换流程；
- **DataTable**：需要加载的数据表；
- **Config**：项目配置；
- **Language**：语言文件；
- **Load from bytes**：按项目资源策略决定文本或二进制加载。

每次新增表或流程后，都应同时检查源文件、生成代码、输出资源和 `AppConfigs` 的加载列表。

## 编辑器 Play Mode 热重载

项目内置本地 UPM 形式的 Fast Script Reload（FSR），用于在 Unity Editor 的 Play Mode 中迭代已有 C# 方法体而不退出运行会话。首次使用、日常流程、回调、限制与排查见 [FSR 开发指南](Documentation/Development/FastScriptReload.md)。FSR 不用于已发布 Player 的热更新，也不替代 HybridCLR。

## 核心用法

### 新增业务流程

1. 在 `Assets/Game/Scripts/Procedures/` 创建一个继承 `ProcedureBase` 的流程。
2. 在 `AppConfigs.asset` 的 Procedures 中注册它。
3. 从现有流程使用 `ChangeState<TProcedure>()` 进入新流程。
4. 将登录、选角、主界面、关卡、战斗等项目逻辑放在项目层 Procedure 中，不改写通用启动语义。

如果需要在通用预加载完成后自动进入某个流程，让该流程实现 `IFrameworkStartupProcedure`，并确保 `AppConfigs` 中至多注册一个这样的流程。

### 新增数据表、配置与语言

1. 将 Excel 源文件分别放入 `GameData/DataTables/`、`GameData/Configs/` 或 `GameData/Languages/` 下的项目命名空间。
2. 使用 **App Configs** 面板或 `Game Framework/GameTools/Refresh All Excels【刷新所有数据表】` 生成输出文件与 DataTable 代码。
3. 将生成结果登记到 `AppConfigs.asset`。
4. 通过 `GF.DataTable`、`GF.Config` 和 `GF.Localization` 在运行时访问。

`Core/` 目录只保存框架自身所需的基础分组表。任何游戏领域的数据都应使用单独目录，避免污染框架基线。

### 新增 UI、实体与音频

- UI：继承 `UIFormBase` 或 `UIItemBase`，在 UI 表中声明资源与分组，再通过 `GF.UI` 打开。
- 实体：继承 `EntityBase`，在实体分组表中声明对象池策略。
- 音频：先在音频分组表配置分组，再调用 `GF.Sound.PlayEffect("资源名")` 等扩展接口；框架不会提供固定音效资源。
- 输入：通过框架输入抽象接入，不要在业务代码中散落平台相关的直接输入调用。

## 可选示例包

仓库根目录的 `Samples~/` 保存可安装的示例源包。它不由 Unity 直接导入，因此新项目保持干净。打开 Unity 菜单 `Tools > AI Friendly Frame > Samples`，可对每个包执行：

- **安装**：仅复制清单声明的文件；需要启动配置的包会先备份当前 `AppConfigs` 和 Build Settings。
- **打开**：用于查看或编辑包的入口场景，不等同于标准启动方式。
- **校验 / 修复**：检查已安装文件是否被改动，并按源包恢复。
- **卸载**：删除包记录的文件，恢复安装前的 `AppConfigs` 和 Build Settings；若用户在安装期间修改了共享配置，管理器会停止自动操作以保护改动。

当前提供两个可选包：

- **基础 UI**：独立 UGUI 预览，可直接打开 `BasicUiSample.unity` 查看，不经过完整启动链。
- **电路拼图**：展示启动 Procedure、配置表、语言、UI 和运行时交互。安装后请始终从 `Launch.unity` 播放，由框架预加载完成后自动加载样例场景。

安装副本位于 `Assets/Sample/`、`Assets/Game/**/Sample/` 与 `GameData/**/Sample/`，均已被 Git 忽略；可提交的示例源只在 `Samples~/` 中。

## 让 AI 在此框架中开发

框架将 AI 协作规则随仓库提供，使 AI 可以先理解边界，再进行实现和验证。

### 入口与规则

- [AGENTS.md](AGENTS.md)：Codex 的项目入口，包含职责路由、目录边界和安全约束。
- [.claude/CLAUDE.md](.claude/CLAUDE.md)：Claude Code 的等价入口。
- `.claude/agents/*.md`：Agent 配置的唯一来源；`.codex/agents/*.toml` 由 `python tools/sync-agents.py` 生成，不直接编辑。
- `.claude/skills/<name>/SKILL.md`：项目 SKILL 的唯一来源；`.claude/SKILL_MATRIX.md` 定义 Agent 可使用的 SKILL。
- `openspec/`：设计、架构、重构和中大型变更的结构化记录。

### 推荐协作方式

1. 先告诉 AI 目标、目标平台、已有资产、验收标准和不希望改动的边界。
2. 对局部实现、缺陷修复和代码检查，可直接要求 AI 阅读相关代码后修改并验证。
3. 对设计、架构、重构、新系统或大型改动，要求 AI 先完成需求澄清和方案收敛；必要时在 `openspec/` 中创建变更记录。
4. 要求 AI 修改 Unity 代码时，明确要求它遵守 `.claude/conventions.md`、避免高频 GC、保持资源路径由配置提供，并执行编译、引用检查或测试。
5. 使用 Unity 自动化服务时，可让 AI 查询 Console、编译状态、测试结果和场景健康状态；不应把自动化结果当作替代实际 Play Mode 验证。

### 纯度与提交前检查

框架基线不应包含项目玩法、产品数据、演示资源、固定本机路径或业务生成物。安装 Python 依赖后可运行：

```powershell
python tools/audit_framework_purity.py
```

提交前至少确认：

- Unity Console 没有新增编译错误；
- `Launch.unity` 可进入 `FrameworkReadyProcedure`；
- 新增表、流程和资源分组已登记到相应配置；
- 未提交 `Library/`、`Temp/`、`Logs/`、`HybridCLRData/`、`AB/` 或本机安装的示例副本；
- 未把项目领域内容回写到 `Assets/Game/ScriptsBuiltin/`。

## 目录约定

```text
Assets/Game/
├─ Animations/            动画片段、控制器和 Avatar Mask
├─ Audio/                 通用音频配置与音频资源
├─ Config/                运行时配置输出
├─ DataTable/             运行时数据表输出
├─ Font/                  通用字体资源
├─ Language/              运行时语言资源
├─ Materials/             材质资源
├─ Models/                模型源资产
├─ Prefabs/
│  ├─ Core/               框架通用 Prefab
│  ├─ Entity/             实体系统 Prefab
│  └─ UI/                 UI 系统 Prefab
├─ Scene/                 Launch 启动场景
├─ ScriptableAssets/      AppConfigs 等配置载体
├─ Shaders/               Shader、Shader Graph 与渲染代码
├─ Sprites/               Sprite 与 SpriteAtlas 源资源
├─ Textures/              非 Sprite 纹理资源
├─ Timeline/              Timeline 与 Playable 资源
├─ VFX/                   粒子和视觉效果资源
├─ Video/                 视频资源
├─ ScriptsBuiltin/        框架核心、通用启动流程和 Editor 工具
└─ Scripts/               通用扩展与项目接入边界

GameData/                 DataTable / Config / Language 的 Excel 源文件
Samples~/                 可安装示例的版本化源包
Packages/                 Unity Package Manager 依赖定义
ProjectSettings/          Unity、URP、构建与运行时项目设置
tools/                    审计、构建、资源处理与协作辅助工具
.claude/                  AI 工作流、SKILL 与 Agent 源配置
.codex/                   由脚本同步生成的 Codex Agent 配置
openspec/                 中大型结构化变更记录
```

资源目录采用按类型划分的浅层骨架。具体项目可在各根目录下继续细分，但框架基线
不预置角色、玩法、关卡或产品模块目录。材质目录统一使用复数 `Materials/`；
`HotfixDlls/`、`AB/` 和 `HybridCLRData/` 是可再生构建产物，不作为空目录提交。

## 版本要求

| 项目 | 版本 / 说明 |
| --- | --- |
| Unity | **2022.3.62f3c1** |
| 渲染管线 | Universal Render Pipeline 14.0.12 |
| 平台基线 | Windows 10；其他平台应由具体项目完成验证与构建配置 |
| 关键扩展 | HybridCLR、Obfuz、Cinemachine、TextMeshPro、UGUI、UniTask、R3 等 |

## 致谢

本项目的底层能力建立在 [sunsvip/GF_X](https://github.com/sunsvip/GF_X) 的基础之上，感谢其开发者和贡献者提供的工作与积累。

## 许可证

本项目自身拥有版权的代码、文档和示例内容采用 [MIT License](LICENSE)，版权所有者为 `TXYzznc`。该授权不覆盖第三方软件、Unity Package Manager 包、字体或其他资产；它们继续遵循各自的许可证和使用条款，详见 [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md)。
