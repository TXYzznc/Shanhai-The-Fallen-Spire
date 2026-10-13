# 移动及攻击中闪避验证

Unity 2022.3.62f3c1，用户指定项目端口8092。编译通过；PlayMode全量32项通过、0失败，UTC 2026-10-07 12:23:32—12:23:39（北京时间20:23:32—20:23:39）。原始证据：[playmode-results.xml](playmode-results.xml)。本批通过REST及语义输入自动验证，没有操作实机Player，也未重新构建Windows包。

| 新增用例 | 证明的行为 |
|---|---|
| MovingInputCanDodgeWithoutStoppingAndOnlyMovesOneDodgeDistance | 移动过程中触发闪避，继续移动；同帧再次尝试不重复位移 |
| DodgeCancelsWindupHitButPreservesAttackCooldown | 攻击前摇中可闪避，取消尚未结算伤害；普攻冷却保留，后续从第一段开始；目标仍在可命中范围，排除仅因位移出范围而通过 |
| DodgeDuringRecoveryPreservesAlreadyAppliedDamage | 已命中后的攻击后摇可闪避，解除攻击锁；既有伤害保留且不二次结算 |
| InvulnerabilityRejectsDamageAndHitStunThenExpires | 无敌帧内普通/致命伤害均被拒绝，无受击硬直、伤害事件或复活消耗；无敌到期后实际扣血、硬直与事件恢复 |
| SimultaneousActionsGiveDodgePriorityInEitherUpdateOrderEvenWithZeroCooldown | 两种组件Update顺序下，闪避优先于同帧普攻/技能/大招；即使临时配置零冷却也只发生一次闪避 |

既有27项玩法、敌人状态、暂停计数、奖励所有权、兽印装配、UGUI悬停和独立GF窗口生命周期回归全部通过。源玩家Prefab已经显式引用移动/战斗/健康组件，无需修改资产。当前配置仍为1格距离、1.2秒冷却和0.2秒无敌；具体数值及攻击打断细节仍归档为暂定。

测试套件包含MissingUiTest.prefab故意加载失败用例，其错误由LogAssert.Expect接收，不是本批编译或运行异常。旧DemoAttackDodgeInputAcceptanceProbe包含禁止攻击中闪避断言，属于旧灰盒阶段入口，本批没有运行；当前规则以DemoPlayerDodgeTests为准。

按AGENTS.md运行框架纯度审计，仍有既存11项游戏业务目录、生成物和构建场景问题，结果未通过，与前批一致；未修改审计器或删除用户游戏资产。本次不涉及ScriptsBuiltin，不提交框架基线。

权威任务表更新D1-004当前规则，并增列D9-001记录本批实现及自动验证。用户实机步骤见Docs/GameDesign/04-Demo/04-WindowsPlayer验收步骤.md；旧Player需要重新构建，本批自动通过不代表其已包含更新。
