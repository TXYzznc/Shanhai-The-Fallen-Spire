## ADDED Requirements

### Requirement: Git 集成窗口执行常规提交
框架 MUST 提供 `git-integration` 窗口合同。该窗口 MUST 仅处理携带派发/OpenSpec ID、精确路径、验证证据、中文提交说明、排除项和授权依据的请求，并 MUST 显式暂存授权路径、复核暂存清单及提交检查。它 MUST NOT 推送、合并、变基、切换分支、改写历史或修改专业实现。

#### Scenario: 完整提交请求
- **WHEN** 专业窗口提交完整且授权一致的请求
- **THEN** Git 集成窗口 MUST 在无需制作人逐项审批的情况下提交，并且只向请求窗口返回结果

### Requirement: Git 集成窗口升级不可安全处理的异常
路径授权或归属不明、只读内容混入且无法安全分离、仓库异常或需要新用户授权时，Git 集成窗口 MUST 停止 Git 操作并升级制作人或用户。

#### Scenario: 暂存区包含归属不明的内容
- **WHEN** Git 集成窗口无法确定暂存内容归属
- **THEN** 它 MUST 不提交或清理该内容，并 MUST 请求制作人或用户处理
