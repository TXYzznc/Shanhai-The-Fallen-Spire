## 1. 基线审计

- [x] 1.1 生成受管目录的文件、哈希、大小和 Git 状态清单
- [x] 1.2 建立框架允许清单、业务删除清单、生成物清单与待判定清单
- [x] 1.3 检查 Unity GUID、asmdef、脚本、配置和文档引用，锁定每个删除批次的影响范围

## 2. Agent 与 SKILL 治理清理

- [x] 2.1 删除领域型 SKILL 及其 references/scripts/assets
- [x] 2.2 清理保留 SKILL 中的项目名、业务路径、业务结构和玩法案例
- [x] 2.3 收敛或删除依赖玩法领域的 agent，并更新 `.claude/SKILL_MATRIX.md`
- [x] 2.4 更新 `.claude/skills/SKILLS_INDEX.md`、顶层指引、hooks 和审计规则
- [x] 2.5 运行 `tools/sync-agents.py` 重新生成 `.codex/agents/*.toml`

## 3. OpenSpec 与工作流清理

- [x] 3.1 删除业务主规格、业务 change archive 和业务专属工作流制品
- [x] 3.2 将保留的 workflow、skill-governance、skill-routing 和 unity-skills-routing 规则通用化
- [x] 3.3 验证 OpenSpec 结构和所有保留规格

## 4. 工具与历史产物清理

- [x] 4.1 删除业务 playtest 报告、截图、测试结果和业务模板
- [x] 4.2 删除绑定旧业务数据、目录或 catalog 的工具与文档，通用工具改为参数化输入
- [x] 4.3 清理根目录和 AI 协作文档中的业务历史与样例

## 5. Unity 项目清理

- [x] 5.1 删除业务热更 DLL、HybridCLR 业务生成物和其它可再生业务输出
- [x] 5.2 审计并删除未被框架引用的字体、材质、Shader、语言、配置和资源资产
- [x] 5.3 清理 Build Settings、资源规则、link 配置和项目设置中的悬空业务引用
- [x] 5.4 确认 `Assets/Game/ScriptsBuiltin/` 与保留的 `Assets/Game/Scripts/` 不含业务类型或业务常量

## 6. 自动纯度审计

- [x] 6.1 实现严格允许清单与禁止内容扫描
- [x] 6.2 实现 agent↔SKILL、OpenSpec、Unity GUID/asmdef 和生成物一致性检查
- [x] 6.3 为审计脚本增加无业务样例的自动测试

## 7. 验证

- [x] 7.1 运行 Python 工具测试、SKILL 审计、agent 同步一致性和 OpenSpec validate
- [x] 7.2 触发 Unity 重新导入与编译并检查 Console
- [x] 7.3 运行不依赖业务场景的 GF_X 全量诊断并记录不适用项
- [x] 7.4 输出最终保留/删除/通用化统计与已知限制
