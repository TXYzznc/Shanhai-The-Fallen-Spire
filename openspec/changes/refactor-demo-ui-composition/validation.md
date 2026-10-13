# HUD拆分验证

## 结果

Unity 2022.3.62f3c1、指定端口8092。编译通过，最终PlayMode自动测试27项通过、0失败，UTC 2026-10-07 12:08:14—12:08:19。证据：[playmode-results.xml](playmode-results.xml)。前两轮新增测试分别22/22与25/25通过，原始记录保留。本批没有控制电脑，没有运行用户实机Player验收，也没有重新出Windows包。

测试后控制台保留1条MissingUiTest.prefab加载失败错误，这是失败恢复用例用LogAssert.Expect显式接收的预期日志；不能将它解释为编译失败或意外错误。测试结束返回干净Launch场景。

## 完整性与规格映射

| 规格/场景 | 实现 | 验证 |
|---|---|---|
| HUD职责与资源独立 | DemoHudForm只绑定显示；DemoHudController只组合Vitals、RunInfo、Marks；三者为嵌套Item资产 | SourcePrefabsHaveExplicitReferencesAndIndependentBoundaries；HUD无奖励模型/面板和表现层；三个源Prefab关联正确、八个Prefab无Missing Script |
| 独立窗口按需打开 | DemoUiSession + DemoUiWindowLease；奖励、装配、结算独立GF Form，表现层独立常驻Form | HudContainsOnlyDisplayAreasAndWindowsAreIndependent；实际GF资源加载，弹窗不属于HUD树 |
| 加载取消与重试 | 请求立即暂停，GF取消路径释放未交接参数与暂停 | LoadingCanBeCancelledAndPooledFormsRebindWithoutDuplicatePauses；加载/已打开均能重试关闭，迟到请求不重开 |
| 失败与重试 | 匹配GF失败事件，释放加载登记，重置奖励选择 | FailedWindowLoadReleasesPauseAndCanRetry；缺失资源失败后恢复Time.timeScale，修复路径可重新打开 |
| 关闭及池复用 | Form解绑玩法引用和会话监听，释放GamePauseWindow登记；UIParams由GF关闭回收 | 重复开关三次计数始终1→0；SessionExitClosesFormsAndReentryUsesFreshBindings覆盖实际场景卸载与菜单重进 |
| 保持显示和玩法 | 四槽只发布按钮意图，输入/互斥移至会话；奖励获得与装配保持分离 | 原20项回归全部通过，含所有权、兑换、Tab/Esc、暂停冷却和120个实际UGUI射线悬停采样 |
| 常驻显示内容 | Vitals与RunInfo独立投影，重试清空通知 | DisplayAreasTrackVitalsCurrencyBuffsAndRestart：75/100及75%血条、技能冷却/就绪、加成后115生命上限、55货币、增益数与重试复位 |
| Boss奖励后结算 | 会话等待奖励完成后打开结算 | BossRewardFinishesBeforeCompletionAndRetryClosesAllWindows：进入真实遭遇、对实体直接结算伤害、两次奖励按钮确认、结算暂停、重试全部关闭 |

## 连贯性与资源维护

- 采用用户第三轮确认的功能区拆分和GF.UI独立窗口方案，沿用UGUI、现有输入抽象与通用暂停类。未新增第三方包，未修改ScriptsBuiltin。
- 新路径均在既有Assets/Game/Prefabs/UI及UI/Item内，通过DemoPresentationConfig索引。源资产由PrefabUtility生成及绑定，没有手改Unity YAML。
- HUD资源从3411行降为552行（只是资源组合边界的证据，不代表性能测量）；组合脚本19行、Form17行。
- 单行生命/货币文字各保留24高度，明确间隔；技能与检查点信息位置相应调整。实际窗口交互由自动射线测试覆盖，最终外观由用户实机确认。
- 原整体兽印生成器对已拆分HUD明确拒绝写入；悬停修复、表现层工具和交付资源验证已更新。迁移再次执行不覆盖已编辑资源。
- 原灰盒QA探针中的旧路径/旧直接装配规则属于历史证据；本批使用Runtime测试入口。维护说明见Documentation/Development/DemoUIComposition.md。

## 框架纯度审计与限制

按AGENTS.md运行python tools/audit_framework_purity.py，返回11项：DemoMain/DemoMenu作为构建场景、Assets/Game/HotfixDlls，以及已有DataTable/Demo、Demo、Materials/Demo、Scene/Demo、ScriptableAssets/Demo、Scripts/Demo、Shaders/Demo、Sprites/Demo、VFX/Demo目录。

这些是当前具体游戏的业务内容/生成物，不符合纯框架基线；本批保留用户授权的游戏资源，没有为了通过基线审计删除内容或修改审计器。应在项目自身变更中维护，不回写纯框架基线。此项审计未通过，不能把自动测试通过描述为所有检查通过。

旧Windows Player不包含本批更新；需要重新构建后由用户实机验收。
