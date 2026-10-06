## Why

现有协作规范仍要求制作人处理普通进度、日常交接和 Git 索引短锁，容易形成串行瓶颈。框架需要提供可复用的高自治协作与专用 Git 集成窗口合同，同时保持项目边界和用户授权不被弱化。

## What Changes

- 将专业窗口调整为在派发边界内自主讨论、实施、验证、增量归档及点对点同步。
- 增加 `git-integration` 长期窗口的精确提交请求、门禁、退回和异常升级规范。
- 将制作人职责收敛为批次、异常、未解决跨职能冲突和批次验收。
- 更新 Dispatch 模板、注册示例、暂停恢复、角色路由和入口说明。
- 使框架纯度审计以 UTF-8 BOM 安全方式读取文本，并补充回归测试。

## Capabilities

### New Capabilities

- `git-integration-workflow`: 共享工作区的专用 Git 集成窗口合同与提交门禁。

### Modified Capabilities

- `ai-team-collaboration`: 增加专业窗口自治、点对点同步和制作人强制升级边界。
- `framework-collaboration-purity`: 以 UTF-8 BOM 安全读取审计输入并覆盖该场景。

## Impact

影响协作文档、Codex/Claude/producer 入口、生成的 producer 镜像、框架纯度审计及测试；不修改 Unity 框架核心、业务代码、产品 OpenSpec、本机注册或活动派发。
