## ADDED Requirements

### Requirement: 工作流只规定制品生命周期

框架工作流 MUST 只规定澄清、提案、设计、任务、实现、验证和归档等生命周期，MUST NOT 预设美术、战斗、关卡、玩家测试或其它业务阶段。

#### Scenario: change 不涉及特定内容类型
- **WHEN** 任意框架或工程变更进入 OpenSpec
- **THEN** 工作流 MUST 能在不创建业务专属子目录的情况下完成

### Requirement: 工作流扩展必须由具体项目声明

任何内容生产、玩法设计、业务测试或资源交付子流程 MUST 由具体项目自行添加，MUST NOT 作为框架基线的默认流程。

#### Scenario: 新项目需要业务流程
- **WHEN** 具体项目确定其内容生产或业务测试需求
- **THEN** 项目 MUST 通过自己的 change、agent 或 SKILL 增加流程，而不是修改框架默认基线

### Requirement: 工作流文档不得包含业务样例

工作流、模板和报告格式 MUST 使用领域中性的制品名称与占位符，MUST NOT 携带已实现业务流程的示例正文。

#### Scenario: 提供模板
- **WHEN** 工作流需要说明必填字段
- **THEN** 模板 MUST 仅描述字段契约，不填入玩法或内容样例
