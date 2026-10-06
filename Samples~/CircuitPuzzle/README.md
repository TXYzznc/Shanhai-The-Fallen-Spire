# 电路拼图

一个无外部美术资源的 6×6 程序化电路拼接小游戏。每次开始时会使用种子生成
新的可解布局；旋转普通节点，让能量从左下角的源点到达右上角的终点。

## 安装与运行

1. 在 Unity 菜单选择 `Tools > AI Friendly Frame > Samples`。
2. 找到 **电路拼图**，点击 **安装**。管理器会复制场景、代码、生成的数据表、配置、语言 Excel 源文件；随后先备份当前完整 `AppConfigs.asset`，再启用本样例的完整启动配置档，并将入口场景登记到 Build Settings。
3. 点击 **打开** 可以直接预览场景；或者打开 `Assets/Game/Scene/Launch.unity` 进入播放模式，样例会在框架预加载完成后通过 `CircuitPuzzleSampleProcedure` 加载。
4. 修改 Excel 后使用 `Game Framework/GameTools/Refresh All Excels【刷新所有数据表】` 重新生成，再执行 **修复 / 重装** 更新安装副本。
5. 需要恢复框架纯净状态时，在同一窗口点击 **卸载**；它会删除样例文件、恢复安装前的完整 `AppConfigs.asset`、恢复安装前的 Build Settings 场景列表，并清理本机备份。样例配置档激活期间不要手工修改 `AppConfigs` 或 Build Settings；若已修改，管理器会先停止并保护当前配置。

## 样例覆盖范围

- 纯代码构建 UGUI Canvas、布局与中文按钮文案；不依赖预制体或图片。
- 确定性种子生成、棋盘连通判定和基础状态流转。
- 节点点击、暂停、重置、下一关、语言切换与运行指标展示。
- UI 脉冲对象池，避免反复创建短生命周期视觉对象。
- GF_X 服务在可用时的 Event、Setting、Localization、Resource 和 SoundExtension 接入；
  在独立场景中安全回退到本地状态、PlayerPrefs 与内置文本。
- `CircuitLevelTable` 数据表驱动固定种子与推荐步数，`CircuitPuzzleConfig` 控制默认关卡与标题色，
  `Sample/CircuitPuzzle` 语言词典提供可本地化文案。

本样例不配置远程更新、HybridCLR、资源包或声音文件。声音调用仅用于演示
`SoundExtension` 在资源不存在时可安全调用的能力。
