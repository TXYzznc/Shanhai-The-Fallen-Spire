# OpenSpec 批次与并行命名

## Change 范围

一个 change 一般覆盖二至五项紧密相关、可共同验收的工作。任务标识可作为 change 名称的一部分，但不应代替目标、边界、验收和约束描述。实现细节和架构必须先与用户讨论确认，才能写入 OpenSpec artifacts。

## 批次格式

- 连续批次使用 `b<two-digit-sequence>`，例如 `b01`。
- 同一批次的并行 change 共用该前缀，并包含 `parallel` 与职能标识，例如 `b01-parallel-client-<scope>` 和 `b01-parallel-art-<scope>`。
- 每个并行 change 独立完成方案讨论、增量归档、用户确认、锁授予和验收；一个确认不授权另一个。

## 归档与交接

OpenSpec 建立前的派发单位于项目协作区；建立后迁入 change 的 `dispatch.md`。change 归档时派发单和验证证据随其归档，项目中央占用表只保留当前状态，不承担历史记录。
