# unity-skills-routing Specification

## Purpose

确保 Unity 自动化按显式配置和稳定优先级定位目标 Editor，正确处理多项目、端口与字符编码边界，同时保证指引、参数和验证流程不绑定任何历史项目、业务资产或示例场景。

## Requirements

### Requirement: 客户端按确定优先级寻址

客户端 MUST 按显式端口、显式目标、环境目标、当前目录 registry 匹配、默认端口
的顺序定位 Editor，并 MUST 在使用默认值时输出警告。

#### Scenario: 显式端口覆盖其它来源
- **WHEN** CLI 提供合法端口
- **THEN** 客户端 MUST 使用该端口且来源标记为显式端口

#### Scenario: 当前目录命中 registry
- **WHEN** 当前目录位于某个已登记项目路径内
- **THEN** 客户端 MUST 使用该条目的端口且不输出回退警告

### Requirement: 多项目调用不得串线

并行打开的 Unity 项目 MUST 根据各自 registry 条目接收调用，MUST NOT 把一个
项目的请求发送到另一个项目。

#### Scenario: 两个临时项目同时登记
- **WHEN** 测试动态创建两个不同路径和端口的 registry 条目
- **THEN** 每个目录发出的请求 MUST 命中自己的端口

### Requirement: CLI 参数不得污染操作参数

寻址参数 MUST 在操作名和操作参数解析前移除，并 MUST 与标准输入 JSON 和
键值参数共存。

#### Scenario: 寻址参数与标准输入共存
- **WHEN** CLI 同时接收显式目标与标准输入 JSON
- **THEN** 目标 MUST 用于寻址且 JSON MUST 完整传给操作

### Requirement: registry 客户端只读

客户端 MUST 只读 registry，并 MUST 在文件缺失或损坏时安全回退，不得修改
服务端维护的数据。

#### Scenario: registry 无效
- **WHEN** registry 不存在或不是有效 JSON
- **THEN** 客户端 MUST 返回空集合并继续执行回退逻辑

### Requirement: 文档和测试不得硬编码历史项目

Unity 自动化文档和测试 MUST 使用参数、临时目录和动态标识，MUST NOT 固定
历史项目名或历史绝对路径。

#### Scenario: 测试需要项目标识
- **WHEN** 测试构造 registry 条目
- **THEN** 标识与路径 MUST 在测试运行时生成并在结束后清理
