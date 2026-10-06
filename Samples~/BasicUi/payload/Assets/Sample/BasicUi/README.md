# Basic UI Sample

打开 `Scenes/BasicUiSample.unity` 并进入 Play Mode。场景包含一个 Canvas、说明文本和一个已绑定点击事件的按钮；点击按钮后可在 Unity Console 看到确认日志。

本示例只使用 `Assets/Sample/BasicUi/` 内的脚本和场景，不注册到 `Launch` 的默认流程，也不依赖任何业务资源。可以整体删除，或复制后作为新项目功能的起点。

## 资源放置规则

优先将示例自有的场景、脚本、Prefab、贴图、音频和说明放在 `Assets/Sample/<SampleName>/`，使其可以整体移动或删除。

以下资源由框架以约定位置、约定名称或配置表索引加载，不能仅放到根 `Assets/Sample/` 后期待自动生效。应保留框架规定的根目录，并建立名为 `Sample` 的子目录：

| 资源类型 | 应放置的位置 |
| --- | --- |
| 可加载数据表 | `Assets/Game/DataTable/Sample/`，源 Excel 对应 `GameData/DataTables/Sample/` |
| 语言资源 | `Assets/Game/Language/Sample/`，源 Excel 对应 `GameData/Languages/Sample/` |
| 音频（通过 `SoundExtension` 播放） | `Assets/Game/Audio/Sample/`；调用时传入相对名称，如 `Sample/Click.wav` |
| UI Prefab | `Assets/Game/Prefabs/UI/Sample/`；UI 表中的资源名使用 `Sample/<PrefabName>` |
| Entity Prefab | `Assets/Game/Prefabs/Entity/Sample/`；实体表中的资源名使用 `Sample/<PrefabName>` |
| 由框架切换的场景 | `Assets/Game/Scene/Sample/`；调用时使用相对名称 `Sample/<SceneName>` |
| 由 AppConfigs 直接引用的配置 | 保持在 `Assets/Game/ScriptableAssets/` 的约定分类下，例如 `Assets/Game/ScriptableAssets/Sample/` |
| 启动场景和框架运行时组件 | 继续使用 `Assets/Game/Scene/Launch.unity`，不要复制到 Sample 后替换默认入口 |

如果资源仅由示例脚本通过显式引用或项目配置引用，则可以放在 `Assets/Sample/BasicUi/`。新增固定路径前，先检查加载代码、`AppConfigs.asset` 和资源构建规则，避免把路径字符串写死到业务脚本中。
