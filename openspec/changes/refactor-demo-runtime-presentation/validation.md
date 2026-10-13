# 验证记录

## 2026-10-07：开始界面接入

环境：Unity 2022.3.62f3c1，项目绑定8092，Editor Play Mode从Launch启动。

- 新增DemoMenu场景、DemoStartMenu预制体和DemoPresentationConfig配置；菜单节点全部在预制体中，两个按钮引用UI/Item/DemoActionButton嵌套预制体。
- DemoStartupProcedure在预加载后进入DemoMenu，由GF.UI加载开始界面；点击开始后GF.Scene加载DemoMain，关闭菜单UI并卸载DemoMenu。
- 正常流程：DemoMenuAcceptanceProbe实际UGUI射线命中开始按钮；双击只接受一次，加载期间按钮禁用；游戏加载后菜单UI和菜单场景均释放。控制台错误0。
- 失败恢复：测试使用运行时复制配置指向不存在的QA_MissingScene，未修改配置资产。失败后菜单保留、按钮恢复、提示非空；恢复真实配置后再次开始成功，防重入仍有效。唯一错误为故意缺失场景的预期错误。
- EditorBuildSettings已包含Launch、DemoMain、DemoMenu，Launch仍为第一场景。
- 加载取消：开始加载后立即销毁菜单入口，独立票据等待终态并卸载迟到的DemoMain；测试确认游戏场景没有残留，菜单UI已关闭，控制台错误0。

退出按钮已绑定Application.Quit；当前证据不包含独立Player进程退出验证。HUD、实体动态迁移和战斗表现仍需继续，不能以开始界面通过代替完整目标验收。

## 2026-10-07：动态HUD接入

- DemoMain已删除场景HUD和重复EventSystem，仅保留DemoWorldEntry显式接入点；HUD由GF.UI打开DemoHudForm，节点在DemoHud及嵌套Item预制体中。
- DemoHudController、DemoLoadoutView、DemoRewardChoiceView、DemoCompletionView不再创建GameObject或运行时添加控件；从预制体读取已编辑的结构。奖励控制器和各视图在打开时绑定当前玩家，关闭时重置奖励及解绑。
- HUD就绪前暂停玩家移动/攻击、波次、遭遇和检查点，绑定完成后启用，避免异步加载期间漏掉奖励和检查点。
- 六分辨率回归：实际分辨率、布局、文字、按钮均6/6；三兽印×四槽位×六分辨率的悬停/预览及装备确认均72/72。首轮两档GameView未实际切换，记录为失败；聚焦GameView后完整重跑，全部通过，没有放宽断言。
- 运行时首次发现装备图标为槽位同级Icon_i，纠正读取路径后重新从Launch进入，控制台错误0。
- DemoWorldUiAcceptanceProbe验证场景无直接UI根节点、奖励面板反复打开不新增节点、退出游戏后HUD不可用且解绑玩家、加载菜单再开始后HUD恢复并绑定新玩家；回归通过，节点数量始终40，控制台错误0。

该阶段玩家/敌人仍是旧场景对象，动态实体、战斗表现及完整玩法迁移回归继续执行。飘字Item尚未制作，D6-002暂不宣称整项完成。

## 2026-10-07：动态实体接入

- DemoMain中预置玩家和普通/精英/Boss已删除。DemoWorldEntry经GF.Entity创建玩家，绑定场景输入和正交相机；HUD准备完成后才启用玩法。
- 波次/遭遇保留配置和生成姿态，触发时创建对应实体；击败后回收实体，保留波次/遭遇进度；重试取消在途请求、清空奖励与检查点，重新按触发创建敌人。
- DemoEntityLease持有每次请求，失败/取消时释放参数并解除订阅，显示成功后参数由EntityBase回收路径持有；DemoSessionEntity在隐藏时解绑输入、目标、配置和会话引用。
- 首轮发现Prefab预挂EntityLogic与GF Entity.OnInit自动添加的逻辑重复，导致未初始化的ID=0组件。已从四类Prefab移除预挂EntityLogic，运行时由GF创建唯一DemoSessionEntity；专项增加唯一组件、正数ID及GF注册断言，重新运行通过。
- DemoEntityAcceptanceProbe完整通过：场景无实体、初始仅玩家、两波各4动态敌人、真实加载态取消无迟到实体、缺失资源失败清理、17生命/0.8缩放配置与回收解绑、击败回收、检查点升级、精英奖励、Boss二阶段/兽印/货币、UGUI按钮重试、首次复活、二次死亡重试、实体复用恢复。故意缺失QA_MissingEntity的资源警告为预期。
- 扩展DemoWorldUiAcceptanceProbe通过：玩家+4个存活敌人退出场景后，全部旧ID不再加载或显示；HUD解绑；重新加载菜单并开始后，仅起点玩家实体及HUD就绪，面板状态复位。本轮节点数45，保持复用，不增加自定义UI结构。
- 部分初次调用因GameView未聚焦或尚未进入玩法而失败，未作为通过证据；通过实际GameView菜单聚焦和等待就绪后重跑。出现过FSR欢迎窗口无效/无父容器错误，增加只关闭该窗口的Editor工具，未修改或重新安装FSR。

该专项使用传送和直接伤害验证生命周期，不代替默认数值/AI下的连续语义战斗。后者将按动态实体接口重跑；攻击/粒子/飘字/实际伤害事件与完整构建继续待办。

## 2026-10-07：2D临时战斗表现接入

- DemoPresentationConfig引用可替换的DemoAttackArc/DemoHitParticle与UI/Item/DemoDamageText预制体；斩击和粒子位于Assets/Game/VFX/Demo，实际生命变化只经原战斗逻辑结算。
- 玩家成功普攻、技能、大招及敌人实际攻击发布同步借用的专用引用事件；事件参数在发布期间不可变，GF ReferencePool回收，订阅在实体隐藏时解除。拒绝动作和拒绝伤害不发布事件。
- 普攻/技能/大招采用橙/青/金色扇形斩击，敌人攻击红色；粒子在XY平面发射，命中位置不依赖攻击源。伤害数字显示实际扣血，过量伤害封顶到扣除前生命；玩家致命命中在复活回血前发布，因此首次复活也有正确飘字与闪白。
- 原失衡当次命中增伤、近战扇形判定、前摇、冷却和能量规则保持。闪白使用保留透明轮廓的SpriteFeedback shader与缓存MaterialPropertyBlock，解决彩色贴图仅设置Color.white不能真正闪白的问题；shader检查hasErrors=False、messageCount=0。
- 展示池容量由配置限定，当前斩击8、粒子24、飘字24，饱和时复用旧展示；结束寿命、重试和退出均清空。配置不保存运行态。
- DemoCombatFeedbackAcceptanceProbe通过：普攻锁定/技能冷却/空能量拒绝无假反馈、10点触发失衡实际15、零伤害/尸体/无敌不显示、过量伤害封顶、复活同帧伤害及闪白、粒子位置与速度Z为0、80次连续命中后容量固定、回收实体解绑、重试无残留。此专项使用直接伤害，仅验证表现与结算边界。
- 视觉复核发现TMP数字格式0.#留下#，改用TMP原生数字格式，增强实际解析文本断言后重跑通过。截图Assets/Screenshots/DemoCombatFeedback.png已查看：15.0飘字、橙色斩击、金色命中粒子、白色受击实体均可见；这是临时灰盒资源。
- 新版DemoWorldUiAcceptanceProbe通过：5个存活实体和活动斩击/粒子/飘字退出时回收、旧ID均消失、HUD飘字池节点销毁和引用解绑；菜单重进后新局仅起点玩家，表现计数/活动数归零。节点66保持稳定，控制台错误0。

默认数值与AI的连续语义战斗正在重跑；新资源包/Windows构建与Player操作验收仍待后续验证。

## 2026-10-07：迁移后连续默认战斗回归

- DemoContinuousCombatAcceptanceProbe改为从动态波次/遭遇接口刷新有界实体缓存，避免旧场景NPC引用；仍通过IDemoInputSource驱动正式移动/战斗Update，奖励和重试按钮使用EventSystem实际射线/点击。完成后移除探针并恢复原provider与时间步。
- 首轮两波和检查点通过，精英阶段88.15秒时失败。定位到冲撞剩余距离7.450581E-09，MoveWithCollision将极小位移视为0，原耗尽判断不成立，使IsDashing永远为True并阻止追击。修复为剩余距离小于等于0.0001时取消冲撞；未改速度、规定距离、冷却、伤害或AI策略。
- 修复后从起点重跑，12项全部合取通过：初始起点/无预置敌人、两波、检查点实际UI升级及完整波次快照、精英击败/实际UI兽印、Boss击败/二阶段、完成界面与300货币、Boss兽印、UGUI重试恢复起点/构筑/门/进度/动态实体/表现。模拟战斗耗时66.05秒；Boss结束生命50、复活次数0，过程中未发生二次死亡重试。未注入敌人伤害或能量，未关闭AI，未传送玩家。控制台错误0。
- 受击事件参数与TMP显示缓冲在会话开启时预热，碰撞/扇形查询继续使用预分配数组。此为代码与容量边界验证，不宣称已完成Profiler性能结论。
- 框架纯度审计返回10项：DemoMain/DemoMenu构建场景、HotfixDlls和8处产品Demo目录（新增Materials/Shaders/VFX）。这是本项目业务工作区，未提交到框架基线；审计未通过，不能宣称纯框架基线通过。

后续仍需新版UI尺寸、Test Runner、资源包/Windows构建证据；独立Player操作、手柄设备及正式资源/暂定设计确认继续保留原边界。

## 2026-10-07：最终Editor迁移回归

- 攻击/扣血参数和24个TMP飘字缓冲预热后，战斗表现专项再次通过，控制台错误0。
- 动态实体生命周期完整专项再次通过；故意缺失QA资源只有预期资源警告，运行错误0。
- 最终实际六分辨率、布局、文字和按钮射线均6/6，三兽印四槽位悬停/预览及确认装备各72/72。检查尺寸为800×600、1024×768、1280×720、1440×900、1920×1080和2560×1440。
- 完整奖励文字/字体材料缓存准备后，5个存活实体与活动表现退出/菜单重进专项再次通过；本轮HUD节点75，前后保持相等，旧飘字节点销毁、旧玩家引用解除、新局仅起点玩家且活动表现/事件计数归零。历史66节点对应文字材料尚未完整生成的早期批次，不能混作同轮比较。
- D6-002/003/004/005按各自单项DoD标完成，D6-001保留独立Player退出，D6-006/007保留后续验证/交付依赖；未将旧设备/正式资源/暂定设计问题自动清除。

## 2026-10-07：PlayMode测试与退出边界

- 旧Test Runner测试改为从Launch进入菜单并等待动态玩家/HUD就绪，每个用例重置本局；跨槽互斥断言改为检查实际“移到新槽、旧槽清空”规则，不误判为拒绝移动。三项原有用例执行Passed=3、Failed=0，XML包含PID3136及实际程序集路径。
- REST测试jobId在Domain Reload后丢失，test_get_last_result返回旧job；未将旧结果视为本次证据，读取当前目标Editor明确记录的TestResults.xml，按PID/时间/程序集核对。
- 首轮测试退出后出现MissingReferenceException：框架关闭UI会先销毁飘字对象，随后会话ResetFeedback访问已销毁引用。补对展示对象的Unity存活检查，使清理支持关闭顺序和部分初始化；需要重新执行测试确认退出错误为0。

- 退出存活检查修复后重新运行三项PlayMode测试：2026-10-07 08:58:05Z～08:58:08Z，Passed=3、Failed=0，完成后Editor已停止且控制台错误0。当前XML的PID=3136和程序集路径匹配本项目，结果摘要归档playmode-test-results.json；未依赖丢失的REST jobId或旧job结果。

- 最终加强托管委托解绑：Unity组件已经销毁时仍按CLR引用解除扣血/动作/重试监听；表现会话自身OnDestroy幂等释放飘字。追加检查池中旧玩家DamageApplied和WorldReset委托均为空。
- 最终源码版本的三项PlayMode测试于09:02:57Z～09:02:59Z通过，退出控制台错误0。随后从Launch重进，首轮GameView布局尚未就绪导致开始按钮射线失败，重进探针也被过早调用；这两条QA前置错误保留记录。重新聚焦实际GameView并检查DemoWorldEntry.IsReady=True后重跑，通过5个存活实体/活动表现退出与菜单重进，以及旧玩家扣血/重试委托为空；本轮65节点前后相等。未放宽射线或生命周期断言。
- 首轮Windows资源与Player构建成功120988422字节；资源清单包含DemoMain/Menu、StartMenu/HUD、四类实体、DamageText以及共享依赖中的AttackArc/HitParticle/材质。最终委托清理修改后重新出包，首轮包不作为最终源码的交付包。
- 通用Launch场景健康扫描报MissingMainCamera（1）、无Light/AudioListener警告（2）及深层级信息；Launch为框架初始化/UI场景，不能据此宣称场景扫描0问题，也未为消除通用扫描提示修改框架核心。玩法2D相机和资产证据见此前专门验收。

## 2026-10-07：最终资源与Windows构建

- 最终源码对应Windows Player构建成功，BuildReport.totalSize=120673759字节，控制台错误0，Editor已停止且不在编译。
- ResourceBuild/Working/Windows64清单确认DemoMain/DemoMenu、DemoHud/DemoStartMenu、四类实体及DemoDamageText；共享依赖清单包括DemoAttackArc、DemoHitParticle、DemoPresentationConfig和表现材质。新资源包/版本文件已复制到StreamingAssets，Launch仍为首个启用构建场景。
- 未启动或注入独立Player的输入，本包实际点击/退出验收仍待完成；先前自动审批拒绝Player操作的边界保留。旧设备、正式资源与暂定设计确认任务不自动关闭。
- 本次没有修改ScriptsBuiltin、暂存/提交或推送Git。框架纯度10项问题与通用Launch健康扫描提示按上文真实记录，不将其宣称通过。

## 2026-10-07：交付资源包解析专项

- 新增只读Editor菜单Verify delivered resource bundles：不实例化实体、不启动Player，检查后仅卸载自己加载的包，结果写入packed-asset-results.json。
- 首轮直接LoadFromFile读取SharedAssets.dat失败。核对构建报告和GF源码后确认LoadType=2（220字节包头快速异或解密），字节长度相同并不代表原始UnityFS；这是检查器未处理GF封装，不能据此判定资源包损坏。检查器改按构建报告的LoadType/CRC调用现有GF解密工具，并验证解密后长度/CRC，再加载交付目录数据。

- 最终专项于2026-10-07 09:23:35Z通过，packed-asset-results.json记录Passed、error为空；加载的是最终Windows交付目录中的7个资源包，按依赖顺序读取菜单/HUD、四类实体、斩击/粒子/飘字以及全部5个独立UI Item。确认模板没有缺失组件、失效序列化引用或QA探针，实体没有预挂EntityLogic；飘字配置指向同一个打包Item。
- 打包字体、图集、材质及四类实体的受击Shader引用有效，Shader可用且包含_FlashAmount属性；菜单与玩法场景均存在于交付包。生成目录、项目StreamingAssets和Player StreamingAssets中的8个文件SHA256逐项一致。
- 检查结束仅卸载检查器自己加载的资源包，Editor停止、控制台错误0。此次仅新增Editor检查工具，未改变运行时代码或最终Player包；包内资产可加载证据不替代独立Player实际点击、完整流程与退出验收，OpenSpec 4.4及D6-001/006/007继续保留该边界。

## 2026-10-07：按用户分工重跑自动化测试

- 用户接管剩余实机测试，要求本对话只运行不控制电脑的测试。本轮仅通过8092 Unity Test Runner接口执行DemoGameplayAcceptanceTests，未使用电脑控制工具或键鼠注入。
- 2026-10-07 17:39:47～17:39:49（北京时间）三项PlayMode测试通过：PlayerHealthRevivesOnceThenRequiresRetry、PlayerDodgeMovesAndSetsInvulnerability、LoadoutKeepsFourSlotsAndMutualExclusion；Passed=3、Failed=0。现有测试包含Launch/动态会话准备，属于PlayMode功能与集成测试，不宣称纯EditMode单元覆盖。
- test_run_by_name接受jobId=6718fde5，进入Play Mode时连接短暂不可用，Domain Reload后job查询返回不存在。核对本轮XML的UTC时间、PID3136及本项目Shanhai.Demo.Tests.dll路径确认真实结果，未再次启动测试或使用旧job结果。
- 测试结束Editor已停止、不在编译，控制台错误0。原始XML和摘要分别归档playmode-test-results.xml及playmode-test-results.json。剩余Player交互由用户实机验收，未据此次自动化测试关闭OpenSpec 4.4或完整交付。

## 2026-10-07：敌人血条与已有状态增量

- 用户要求为敌人增加血条，已有其他状态时显示对应状态。本次使用现有uGUI和动态实体接口局部扩展：DemoEnemyStatus Item位于UI/Item，HUD持有独立EnemyStatusLayer，普通/精英/Boss Prefab持有内部引用和模板引用，显示时显式绑定当前HUD/相机，隐藏时解除会话引用；复用沿用同一Item。未修改框架核心。
- 血条和数字读取实际生命；可失衡敌人显示蓝色积累条，累计窗口结束归零，失衡期间改为剩余恢复进度。精英/Boss蓄力使用黄色进度条，硬直、冲撞和Boss二阶段以文字显示。UI读取既有计时和标志，不改战斗规则；死亡、离屏和解绑后隐藏，全部Graphic.raycastTarget=False。
- 17:52:25～17:52:28（北京时间），PlayMode全量8项通过，失败0、退出控制台错误0。其中4项新测试覆盖实际扣血/拒绝零伤害/死亡复位、不支持失衡时隐藏、失衡窗口与恢复计时、精英真实蓄力和Boss二阶段、离屏/解绑/复用/HUD先销毁；另1项通过GF实际加载普通/精英/Boss并检查HUD层绑定及隐藏解绑；原3项回归继续通过。XML按PID3136、时间和程序集核对，摘要及原始XML已更新。
- 资源与Windows包已重新构建。随后8092断开，原Editor进程退出，未把失联当作复查成功，也未操作用户游戏窗口。使用Unity2022.3无窗口批处理检查交付目录，退出码0；7个包、全部6个UI Item（含EnemyStatus）、三类状态绑定及HUD层引用均通过，三处资源8个文件逐项SHA256一致。最新BuildReport确认Player构建Succeeded、totalErrors=0、120731121字节及正确输出路径。结果见packed-asset-results.json，批处理日志见enemy-status-package-check.log。
- BuildReport原生日期的Kind不明确，初次将其ToUniversalTime造成8小时偏差；改为记录LastBuild.buildreport文件的UTC修改时间，避免把该偏差写入交付时间。当前包指纹已同步实机步骤，旧Player交互记录只证明旧版本；实机复核继续由用户执行。
