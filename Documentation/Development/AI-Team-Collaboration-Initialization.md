# AI 团队开发框架初始化

本指南将框架协作规范配置到具体项目。开始前，项目负责人必须确定权威计划的维护者、项目边界和参与职能；不要将这些项目实例写回框架基线。

## 1. 建立项目协作区

1. 在项目文档区复制 `Dispatch/ActiveAssignments.template.md` 为项目的 `ActiveAssignments.md`，删除示例行，并指定制作人窗口为唯一维护者。
2. 选择项目层的派发目录；OpenSpec 前的方案派发使用 `DispatchTemplate.md` 副本，确认后随对应 `openspec/changes/<change-id>/dispatch.md` 管理并归档，不保留重复历史副本。
3. 为方案讨论复制 `DecisionArchiveTemplate.md`，在项目层文档记录检查点；每连续两轮至少归档一次。

## 2. 配置本机窗口注册

1. 创建 `.ai/dispatch/window-registry.local.json`，以 `Dispatch/WindowRegistry.example.json` 为结构填写真实窗口 ID、主机 ID、项目 ID、标题和路径。
2. 确认 `.ai/dispatch/` 被 Git 忽略；此文件仅是本机/账户状态，不得提交或写入中央占用表。
3. 在首次自动派发前，核对每个窗口的职能、标题和项目绑定；歧义或失效时改用手动派发。

## 3. 声明计划与派发边界

1. 在每份派发单填写人类/外部权威计划和只读输入；AI 只将状态或依赖变化作为建议交给其维护者。
2. 填写 AI 可编辑输入、目录锁、高冲突文件、工具实例和 Git 索引短锁；不要使用固定端口或框架不知情的路径。
3. 一个 change 一般覆盖二至五项紧密相关工作；并行 change 共享 `b<two-digit-sequence>` 前缀，并包含 `parallel` 与职能标识。每个 change 独立完成用户确认。

## 4. 开始与维护

1. 制作人登记派发与占用后，再向已注册窗口发送最小启动消息。
2. 专业窗口按 `RoleRouting.md` 开展工作；长期窗口可使用当前环境能力，显式委派的子 agent 仍受 SKILL 白名单限制。
3. 方案讨论至少三轮，且每两轮更新增量归档；用户确认架构和实现细节后才写入 OpenSpec artifacts。
4. 按 `PauseAndRecovery.md` 处理冲突和恢复。提交使用中文标题、概要和正文，显式暂存授权路径，只提交不推送。

## 初始化验收

- [ ] 中央占用表已建立且由制作人独占维护。
- [ ] 本机注册表存在、被忽略且未进入 Git 状态。
- [ ] 第一份派发单已声明权威计划、范围、锁和回传条件。
- [ ] 项目没有把真实窗口 ID、活动状态、产品任务、端口或绝对路径写入框架基线。
- [ ] `python tools/audit_framework_purity.py` 和相关测试通过。
