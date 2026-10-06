## ADDED Requirements

### Requirement: Unity 自动化路由不得预设业务对象

Unity 自动化 SKILL 和调用文档 MUST 以编辑器、资产、场景、组件、构建与诊断操作为边界，MUST NOT 预设玩法对象、业务场景、业务资源或项目专属类型。

#### Scenario: 文档描述自动化调用
- **WHEN** Unity SKILL 文档定义参数和返回值
- **THEN** 契约 MUST 使用 API 类型或领域中性占位符，不包含业务对象样例

### Requirement: 项目寻址不得包含历史项目名

Unity 自动化的 registry、CLI、测试和文档 MUST 通过参数化项目标识寻址，MUST NOT 硬编码历史项目名称或路径。

#### Scenario: 验证多项目寻址
- **WHEN** 测试需要构造两个项目条目
- **THEN** 测试 MUST 使用临时生成的标识和路径，并在测试结束后清理
