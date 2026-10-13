## ADDED Requirements

### Requirement: UI职责与资源独立
常驻HUD SHALL 只组合Vitals、RunInfo和MarkSummary三个独立子预制体与显示组件。奖励、装配、结算 SHALL 使用独立GF.UI Form；表现层 SHALL 与HUD分离。窗口状态与输入互斥 SHALL 由会话协调器持有。

#### Scenario: 进入与按需开窗
- **WHEN** 进入游戏会话
- **THEN** 加载HUD与表现层，弹窗尚未创建；首次请求后由GF.UI加载，关闭由GF回收。

### Requirement: 生命周期保持暂停与玩法规则
窗口加载、打开、关闭、失败、取消、重试及退出 SHALL 正确维护暂停；旧引用及迟到回调 SHALL 不污染新会话。奖励只获得兽印或重复转币，装配操作、四槽图标和双击确认 SHALL 保持现有行为。

#### Scenario: 加载取消与重试
- **WHEN** 暂停弹窗尚在加载时重试或退出
- **THEN** 取消请求并释放暂停，后续回调不能重新打开窗口或绑定已退出玩家。

#### Scenario: 关闭与池复用
- **WHEN** 关闭后再次打开窗口
- **THEN** 重新绑定当前会话，只有一个暂停登记，输入/按钮监听不重复，提示与选择复位。
