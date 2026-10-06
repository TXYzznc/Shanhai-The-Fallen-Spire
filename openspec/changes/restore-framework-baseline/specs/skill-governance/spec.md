## ADDED Requirements

### Requirement: SKILL 必须满足领域无关保留门槛

保留的 SKILL MUST 属于框架、工程基础设施或元工作流，并 MUST 在不假设具体玩法、角色、关卡、经济或内容模型的情况下具有完整价值。

#### Scenario: SKILL 依赖玩法领域
- **WHEN** 一个 SKILL 的核心目标需要具体玩法领域概念才能成立
- **THEN** 该 SKILL MUST 从源目录、索引和路由矩阵删除

### Requirement: 领域型 SKILL 不设候选淘汰期

已确认属于业务设计知识的 SKILL MUST 直接删除，MUST NOT 继续登记在候选淘汰区或以低频状态保留。

#### Scenario: 审计确认领域型 SKILL
- **WHEN** SKILL 被归类为玩法、内容或业务设计能力
- **THEN** 当前变更 MUST 删除该 SKILL 及其全部引用

### Requirement: 保留 SKILL 不得携带业务样例

保留的 SKILL MUST NOT 包含具体项目名、业务目录、固定业务文件、业务数据结构或玩法教学案例。必要的操作契约 MUST 使用参数、占位符和领域中性的输入输出描述。

#### Scenario: 工程型 SKILL 含业务案例
- **WHEN** 审计发现工程型 SKILL 中存在业务案例或固定业务路径
- **THEN** 该内容 MUST 删除或改写为参数化契约
