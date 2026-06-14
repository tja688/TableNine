---
name: doc-task-qa
description: 质检 TableNine 文档任务落地成果：读落地汇报与任务规格文档，核查变更合规性、架构符合性与隐性风险，小修归档或大问题时续写整改清单。Use when the user says 质检、验收、检查落地、review 落地汇报，或 shorthand 质检 R0-R9 / 质检某落地汇报 / 验收某文档任务。
---

# 文档任务质检

对用户指定任务目标的**落地汇报**做独立质检，给出通过/整改判定，并按问题规模分流处理。

与 `doc-task-develop`、`doc-task-rework` 组成三件套：**落地 → 质检 → 返工**。

## 触发与任务目标解析

从用户消息解析：

| 字段 | 含义 | 解析规则 |
|------|------|----------|
| `report_slug` | 被检汇报标识 | 「质检 R3」→ `R3`；或用户给出的 slug / 汇报文件名 |
| `report_path` | 汇报路径 | `Assets/Notes/{report_slug}落地汇报.md`（首选） |
| `spec_doc` / `spec_scope` | 质检基准 | 从汇报 §1/§3 读取；缺失则按 slug 反推（R{N} → Plan R{N} 节） |

常见表述：`质检 R1`、`验收 R2 落地`、`检查 CombatRules-护甲 落地汇报`、`质检 {report_slug}`。

若未指定 `report_slug`，先询问。

**前置条件**：`report_path` 必须存在。不存在则告知需先执行 `doc-task-develop` 落地任务。

## 阶段 0：加载质检基准

| 顺序 | 文件 | 用途 |
|------|------|------|
| 1 | [AGENTS.md](../../../AGENTS.md) | 项目约束、QFramework 入口 |
| 2 | [rules.md](../../../rules.md) | 四层架构、Command 边界、文件保护 |
| 3 | `Assets/Scripts/TableNine.cs` + codegraph | 注册表、分层、核心流程 |
| 4 | [QFramework_Docs_用户手册.md](../../../Assets/Notes/QframeworkNotes/QFramework_Docs_用户手册.md) | QFramework 分层与用法（按目录查找） |
| 5 | `report_path` | **被检对象**：变更清单、测试证据、质检路线图、任务来源 |
| 6 | `spec_doc` | `spec_scope` 范围内的目标、任务、验收 |
| 7 | 规格引用的文档 | 汇报或规格引用的 [ArchitectureDesign.md](../../../Assets/Notes/过程性汇报文档归档/ArchitectureDesign.md)、`Assets/Docs/` 等 |

## 阶段 1：核查变更（按汇报路线图）

以汇报 **§2 文件变更清单** 和 **§6 质检路线图** 为索引，不要只看摘要。

### 1.1 代码与 diff

```
1. 对照汇报列出的每个路径，git diff / Read 实读改动
2. codegraph_context(query="<任务关键词>") 补全汇报未列的关联符号
3. codegraph_impact(symbol=...) 评估违规改动的爆炸半径
4. 需要链路时：codegraph_trace(from=..., to=...)
```

### 1.2 必查项（全任务通用）

| 类别 | 查什么 | 违规信号 |
|------|--------|----------|
| QFramework 分层 | IController 改状态是否只走 Command；System/Model 是否裸调 | 裸 `GetModel`/`GetSystem`（须 `this.`）；UI 直写 Model |
| 项目红线 | Plan §0 或 ArchitectureDesign 不变量 | `Attack - Defense` 战斗减伤；默认临时移除；大段新 switch |
| 规格符合度 | `spec_doc` 任务与验收项 | 未覆盖验收项；擅自扩大范围 |
| 架构不变量 | ArchitectureDesign 相关章节 | 防御参与扣伤、状态绕过 Command |
| 测试 | 汇报声称的 EditMode 是否真实覆盖规则 | 只测工程不测规则；旧语义测试未更新 |
| 隐性雷 | 硬编码、占位未标注、抢做无关任务 | magic number 业务规则；表现层侵入规则层；静默吞异常 |
| 文件保护 | rules.md | 手改 `.unity` |

### 1.3 运行验证

```
1. Unity MCP：editor/state → 等编译完成
2. read_console(types=["error","warning"])
3. 跑汇报 §5 建议的 EditMode 测试（及任务相关 PlayMode，若适用）
4. MCP 不可用时在汇报/回复中标注，用可替代手段验证
```

## 阶段 2：判定分流

完成核查后，归入 **二选一**（不要模棱两可）：

### A. 通过或小瑕疵 → 自行收尾并归档

**适用**：无架构违规；无规格/Architecture 核心违背；整改量小（通常 ≤3 处、单文件可收敛）。

1. **自行修复**小瑕疵（格式、漏测、注释、明显笔误、轻微规范偏离）。
2. 重跑相关测试，确认 Console 无新增 error。
3. 在 `report_path` 末尾追加简短 **质检通过** stamp（见下）。
4. **归档**：将汇报移至 `Assets/Notes/过程性汇报文档归档/{report_slug}落地汇报.md`（连同 `.meta` 若存在）；`Assets/Notes/` 下原文件删除。
5. 回复用户：**简要**说明结论、修了啥、测试数、已归档路径。不要长篇重复汇报正文。

**质检通过 stamp**：

```markdown
---

## 质检记录

- **日期**：{YYYY-MM-DD}
- **判定**：✅ 通过（{无修复 / 已修复 N 处小瑕疵}）
- **任务来源**：{spec_doc} — {spec_scope}
- **验证**：EditMode {p}/{t}；Console {0 error / 摘要}
- **归档**：已移至 `Assets/Notes/过程性汇报文档归档/`
```

### B. 问题较大 → 续写整改清单，不归档

**适用**（满足任一即选 B）：

- 违反 `rules.md` 分层或 Command 边界
- 违背 ArchitectureDesign / 规格核心不变量
- 验收项未达标且需多处逻辑重做
- 存在隐性硬编码 / 技术债地雷
- 预估整改涉及 **多文件、多子系统** 或需重做任务主干

1. **不要**自行大规模重写（避免质检代理越权改坏）。
2. 在 `report_path` **末尾续写**质检附录，使用 [remediation-appendix.md](remediation-appendix.md) 模板。
3. 整改条目须 **逐条** 写清：**要求**（引用规格/架构原文）vs **当前实现现状**（文件+行为）。
4. 回复用户：摘要问题规模、条目数、为何未归档、建议触发 `doc-task-rework` 后 **重新质检**。

## 阶段 3：评价维度（写入附录或回复）

1. **规格符合度**：任务项与验收项逐条 ✅/⚠️/❌
2. **架构符合度**：ArchitectureDesign 不变量
3. **规范符合度**：rules.md + AGENTS.md 约束
4. **可维护性**：命名、分层、测试可续接性
5. **风险**：硬编码、占位、跨任务耦合、未文档化假设

## 禁止事项

- 未读汇报和规格对应范围，不要凭印象判定
- 问题较大时不要「顺手大改」代替整改清单
- 不要归档未通过质检的汇报
- 不要提交 git（除非用户明确要求）
- 不要手改 `.unity`

## 三件套衔接

| 技能 | 关系 |
|------|------|
| `doc-task-develop` | 产出 `{report_slug}落地汇报.md` |
| `doc-task-qa` | 本技能：消费汇报 → 通过则归档 / 不通过则续写整改 |
| `doc-task-rework` | 消费整改附录 → 修补 → 回填；完成后用户再次触发本技能复检 |

## 附加资源

- 整改附录模板：[remediation-appendix.md](remediation-appendix.md)
