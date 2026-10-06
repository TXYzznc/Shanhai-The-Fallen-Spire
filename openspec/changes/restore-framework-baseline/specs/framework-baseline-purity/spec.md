## ADDED Requirements

### Requirement: 框架基线采用严格允许清单

仓库 MUST 仅保留 GF_X/Unity 框架核心、必要依赖、领域无关的工程基础设施和元工作流。无法证明属于允许类别的内容 MUST NOT 留在框架基线。

#### Scenario: 内容无法归入允许类别
- **WHEN** 审计项既不是框架核心、必要依赖、工程基础设施，也不是元工作流
- **THEN** 审计结果 MUST 将其列为删除项

### Requirement: 当前树不保留业务制品和样例

仓库 MUST NOT 包含具体项目、玩法、角色、关卡、战斗、经济、任务、成就、引导等业务代码、资源、配置、规格、测试证据、历史说明或样例制品。

#### Scenario: 扫描到业务制品
- **WHEN** 静态审计在受管目录发现业务专属标识、路径或资产
- **THEN** 验收 MUST 失败并报告精确文件与命中依据

### Requirement: 业务历史不迁入仓内归档

被清理的业务 OpenSpec、报告、截图、测试结果和说明 MUST 从当前树删除，MUST NOT 搬入其它仓内归档目录。

#### Scenario: 清理历史制品
- **WHEN** 业务历史已被归类为删除项
- **THEN** 当前工作树 MUST 不再包含其正文或资源副本

### Requirement: 业务生成物必须清零

由旧业务程序集或业务数据生成的热更 DLL、HybridCLR 输出、资源目录、索引和缓存 MUST 删除；框架生成管线 MUST 在空业务输入下不引用已删除类型。

#### Scenario: 生成物包含旧业务类型
- **WHEN** 生成物或生成清单引用不属于框架允许清单的类型
- **THEN** 该生成物 MUST 删除且引用检查 MUST 不再命中该类型

### Requirement: 纯度验收必须可重复

仓库 MUST 提供可重复运行的审计入口，至少验证允许清单、禁止内容、引用完整性、agent↔SKILL 一致性、OpenSpec 有效性和 Unity 编译健康。

#### Scenario: 在清理后的工作树运行审计
- **WHEN** 执行框架纯度审计
- **THEN** 审计 MUST 以非零退出码报告任何残留或悬空引用，并以零退出码表示全部规则通过

### Requirement: 空业务状态不要求可运行样例

本次框架基线 MUST 不创建启动场景、演示场景、示例 Prefab、示例数据或验证样例；没有可进入的业务 Play Mode 流程 MUST NOT 被视为验收失败。

#### Scenario: 构建设置没有业务场景
- **WHEN** 框架完成编译且 Build Settings 场景列表为空
- **THEN** 纯度验收 MUST 接受该状态
