## ADDED Requirements

### Requirement: Launch menu entry
游戏 MUST 从Launch完成框架初始化后显示标题、开始游戏、退出；开始成功后才进入玩法场景。
#### Scenario: Fresh launch
- **WHEN** Launch预加载完成
- **THEN** 显示开始界面且未生成本局玩家/敌人
#### Scenario: Repeated start
- **WHEN** 加载期间重复触发开始
- **THEN** 仅创建一局，失败时解除禁用并可再次开始

### Requirement: Authored prefabs and dynamic entities
UI及Item MUST 位于用户指定UI目录，玩家与各类敌人 MUST 位于Entity目录，并运行时由框架创建与回收。
#### Scenario: Enter and leave a run
- **WHEN** 创建本局、触发波次，再离开或重试
- **THEN** 新实体引用当前会话，旧实体/加载请求/监听按生命周期释放，不保留场景实体与动态拼建UI结构

### Requirement: Presentation preserves combat
攻击、命中粒子、伤害飘字与受击闪烁 MUST 只映射成功动作/有效伤害，不改变战斗规则。
#### Scenario: Effective hit
- **WHEN** 敌人或玩家收到有效伤害
- **THEN** 命中反馈显示实际结算伤害，闪烁恢复正确状态，死亡/回收不残留表现
#### Scenario: Rejected action
- **WHEN** 动作被攻击锁、冷却、能量或无敌规则拒绝
- **THEN** 不产生虚假的攻击或伤害反馈

### Requirement: Enemy health and existing statuses
普通、精英及Boss MUST 使用UI/Item预制体显示实际生命；已有失衡和蓄力使用进度条，硬直、冲撞及二阶段使用文字提示。显示不得改变战斗数据或新增状态规则。
#### Scenario: Status changes and pooling
- **WHEN** 扣血、积累窗口到期、失衡恢复、蓄力或阶段切换
- **THEN** UI反映当前真实状态，无对应状态时隐藏该项
- **WHEN** 死亡、离屏、实体回收、HUD销毁或再次显示
- **THEN** UI隐藏并清理旧会话引用，复用后显示当前实体状态且不阻挡输入
