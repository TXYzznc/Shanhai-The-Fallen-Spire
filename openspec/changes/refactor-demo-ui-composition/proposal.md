## Why

DemoHud混合常驻显示、奖励/结算、装配窗口创建、输入互斥与表现层。组件通过HUD根节点和名字路径依赖彼此，维护和测试成本增加。用户三轮确认同时拆代码和预制体，独立弹窗走GF.UI，功能区也独立维护。

## What Changes

- HUD保留三个功能区的组合与生命周期绑定；显示组件只投影数据。
- 奖励、兽印装配、结算独立预制体与GF.UI Form；按需加载和关闭。
- 会话协调器管理输入、互斥、加载、暂停交接及退出清理；奖励状态独立于HUD资产。
- 飘字/敌人状态使用独立常驻表现层Form。
- 同步编辑器迁移工具、资源索引和自动验证，保持玩法、视觉布局和操作规则。

## Capabilities

### New Capabilities
- `demo-ui-composition`: 会话UI组合、功能区与窗口生命周期。

### Modified Capabilities

无框架核心变更。

## Impact

项目Demo脚本、UI预制体、展示配置及测试；不改ScriptsBuiltin、不新增包。旧Player须重新构建才包含变更。
