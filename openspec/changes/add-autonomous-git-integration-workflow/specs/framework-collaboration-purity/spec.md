## MODIFIED Requirements

### Requirement: 协作框架制品可被静态审计
框架纯度审计 MUST 验证协作文档与模板存在、入口引用有效、`.ai/dispatch/` 本机注册表被 Git 忽略，并拒绝受管协作文档中的已知产品标识、固定产品路径、活动派发或真实窗口标识。审计读取 UTF-8 文本时 MUST 正确忽略可选 BOM。

#### Scenario: 运行协作纯度审计
- **WHEN** 执行 `python tools/audit_framework_purity.py`
- **THEN** 缺失协作制品、忽略规则或产品污染 MUST 以非零退出码报告精确依据

#### Scenario: 审计输入带 UTF-8 BOM
- **WHEN** 受管文本文件以 UTF-8 BOM 开始
- **THEN** 审计 MUST 按与无 BOM 文件相同的内容处理其首行
