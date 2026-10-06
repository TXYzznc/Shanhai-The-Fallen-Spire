## Why

框架基线目前可启动但缺少可验证的端到端使用参考。团队需要一个不依赖外部美术、不会污染默认项目入口、又能集中展示 GF_X 通用能力的可选样例；同时，样例必须能被安全安装和完整移除，保持新项目默认工作区纯净。

## What Changes

- 为声明完整 `AppConfigs` 配置档的样例添加事务式切换：安装前备份完整资产、安装后独占配置档、卸载时恢复并清理项目本地的 Git 忽略状态文件。
- 新增可选 Sample 包机制：样例源文件保存在仓库根目录 `Samples~/`，默认不导入 Unity；编辑器工具按 manifest 安装到已忽略的 `Assets/Sample/<SampleId>/`。
- 新增 Sample Manager 编辑器窗口，提供安装、打开、校验、重装和安全移除操作；它不得修改 `Launch` 或 Build Settings。清单可声明受控的 `AppConfigs` 登记项，并在卸载时按安装前快照恢复。
- 新增 `CircuitPuzzle` 电路拼接小游戏样例：以程序化几何图形和 Unity 内置能力构成画面，不依赖生图模型、下载资源或业务资源。
- 样例覆盖可配置关卡生成、流程状态、UI、事件、设置持久化、本地化、资源加载、实体/对象池和无资源音频安全降级；不实施真实远端下载、资源发布或 HybridCLR 热更新发布链路。
- 将当前基础 UI Sample 纳入同一可选 Sample 包/安装机制，避免 `Assets/Sample/` 直接成为框架基线的一部分。

## Capabilities

### New Capabilities

- `optional-sample-packages`: 可发现、可安装、可校验且可安全移除的 Unity Sample 包机制。
- `circuit-puzzle-sample`: 用于验证框架通用能力的程序化电路拼接小游戏样例。

### Modified Capabilities

无。

## Impact

- 新增仓库根目录 `Samples~/`、编辑器 Sample Manager 和 `Assets/Sample/` 忽略规则。
- 新增或调整 `Assets/Game/ScriptsBuiltin/Editor/` 中的通用编辑器工具，不修改 GF_X 默认启动链。
- 安装后的样例资产、脚本、场景和可选配置位于 `Assets/Sample/` 与受清单约束的 `GameData/*/Sample/` 命名空间；可由工具回收。
- 将补充 README 与 Sample 包说明，明确固定资源路径和 Sample 自有资源路径的边界。
