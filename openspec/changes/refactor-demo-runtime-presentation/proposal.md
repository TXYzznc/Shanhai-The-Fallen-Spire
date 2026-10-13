## Why

当前Demo从Launch预加载后直接入地图，HUD和实体依赖场景实例，UI在Awake创建节点。用户已明确要求开始界面、可编辑UI/实体预制体、动态生命周期和完整战斗视觉反馈。

## What Changes

- 启动链改为Launch→标题/开始游戏/退出→Demo游戏场景。
- UI及复用子项保存至Prefabs/UI与Prefabs/UI/Item，运行时不再拼建界面结构。
- 玩家、普通敌人、精英和Boss保存至Prefabs/Entity，由GF_X动态生成、绑定、回收。
- 保留战斗规则，增加可替换2D临时攻击、粒子、伤害飘字和受击闪烁预制体。
- 更新权威任务表、设计文档、布局及完整生命周期验收。

## Capabilities

### New Capabilities
- `demo-runtime-presentation`: 启动菜单、预制体组成和战斗反馈的运行时合同。

### Modified Capabilities
- 无现有主spec覆盖本项目Demo；新增项目级spec，不回写框架基线。

## Impact

项目Demo代码、启动Procedure、Demo场景、项目预制体/配置及资源包、Docs/GameDesign与任务表；不修改ScriptsBuiltin核心、不提交或推送Git。不将临时资源或暂定数值宣称正式确认。
