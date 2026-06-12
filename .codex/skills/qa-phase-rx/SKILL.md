---
name: qa-phase-rx
description: 质检 TableNine R0-R9 阶段落地成果：读阶段汇报与 Plan/架构文档，核查变更合规性、架构符合性与隐性风险，小修归档或大问题时续写整改清单。Use when the user says 质检、质检 R0/R1/R2/R3/R4/R5/R6/R7/R8/R9、验收 Rx、检查 R 阶段落地、review R+数字 阶段成果。
---

# 质检阶段 Rx

对用户指定的 `R{N}` 落地成果做独立质检，给出通过/整改判定，并按问题规模分流处理。

## 触发与输入

从用户消息提取 `N`（0-9）。常见表述：`质检 R1`、`验收 R2 落地`、`检查 R3 阶段成果`。

若未指定阶段号，先询问。

**前置条件**：`Assets/Notes/R{N}落地汇报.md` 必须存在。不存在则告知需先执行 `advance-phase-rx` 推进阶段。

## 阶段 0：加载质检基准

| 顺序 | 文件 | 用途 |
|------|------|------|
| 1 | [AGENTS.md](../../../AGENTS.md) | 项目约束、QFramework 入口 |
| 2 | [rules.md](../../../rules.md) | 四层架构、Command 边界、文件保护 |
| 3 | `Assets/Notes/R{N}落地汇报.md` | **被检对象**：变更清单、测试证据、质检路线图 |
| 4 | [Assets/Notes/Plan.md](../../../Assets/Notes/Plan.md) | `## {N+2}. R{N}：` 目标、任务、验收 |
| 5 | [Assets/Notes/ArchitectureDesign.md](../../../Assets/Notes/ArchitectureDesign.md) | 本阶段相关不变量与落点 |

Plan 章节：`R0`→§2 … `R9`→§11。

## 阶段 1：核查变更（按汇报路线图）

以汇报 **§2 文件变更清单** 和 **§6 质检路线图** 为索引，不要只看摘要。

### 1.1 代码与 diff

```
1. 对照汇报列出的每个路径，git diff / Read 实读改动
2. codegraph_context(query="<阶段关键词>") 补全汇报未列的关联符号
3. codegraph_impact(symbol=...) 评估违规改动的爆炸半径
4. 需要链路时：codegraph_trace(from=..., to=...)
```

### 1.2 必查项（全阶段通用）

| 类别 | 查什么 | 违规信号 |
|------|--------|----------|
| QFramework 分层 | IController 改状态是否只走 Command；System/Model 是否裸调 | 裸 `GetModel`/`GetSystem`（须 `this.`）；UI 直写 Model |
| Plan §0 红线 | 伤害公式、帮助卡语义、UseHelpCard switch 膨胀 | `Attack - Defense` 战斗减伤；默认临时移除；大段新 switch |
| 架构不变量 | ArchitectureDesign 对应章节 | 防御参与扣伤、通关顺序错、状态绕过 Command |
| 测试 | 汇报声称的 EditMode 是否真实覆盖规则 | 只测工程不测规则；旧语义测试未更新 |
| 隐性雷 | 硬编码、占位未标注、跨阶段抢做 | magic number 业务规则；R8 表现侵入 R2 逻辑；静默吞异常 |
| 文件保护 | rules.md | 手改 `.unity` |

### 1.3 运行验证

```
1. Unity MCP：editor/state → 等编译完成
2. read_console(types=["error","warning"])
3. 跑汇报 §5 建议的 EditMode 测试（及本阶段相关 PlayMode，若适用）
4. MCP 不可用时在汇报/回复中标注，用可替代手段验证
```

## 阶段 2：判定分流

完成核查后，归入 **二选一**（不要模棱两可）：

### A. 通过或小瑕疵 → 自行收尾并归档

**适用**：无架构违规；无 Plan/Architecture 核心违背；整改量小（通常 ≤3 处、单文件可收敛）。

1. **自行修复**小瑕疵（格式、漏测、注释、明显笔误、轻微规范偏离）。
2. 重跑相关测试，确认 Console 无新增 error。
3. 在 `R{N}落地汇报.md` 末尾追加简短 **质检通过** _stamp（见下）。
4. **归档**：将汇报移至 `Assets/Notes/过程性汇报文档归档/R{N}落地汇报.md`（连同 `.meta` 若存在）；`Assets/Notes/` 下原文件删除。
5. 回复用户：**简要**说明结论、修了啥、测试数、已归档路径。不要长篇重复汇报正文。

**质检通过 stamp**（追加到汇报末尾）：

```markdown
---

## 质检记录

- **日期**：{YYYY-MM-DD}
- **判定**：✅ 通过（{无修复 / 已修复 N 处小瑕疵}）
- **验证**：EditMode {p}/{t}；Console {0 error / 摘要}
- **归档**：已移至 `Assets/Notes/过程性汇报文档归档/`
```

### B. 问题较大 → 续写整改清单，不归档

**适用**（满足任一即选 B）：

- 违反 `rules.md` 分层或 Command 边界
- 违背 ArchitectureDesign 核心不变量
- Plan 验收项未达标且需多处逻辑重做
- 存在隐性硬编码 / 技术债地雷，后续阶段必踩
- 预估整改涉及 **多文件、多子系统** 或需重做本阶段主干

1. **不要**自行大规模重写（避免质检代理越权改坏）。
2. 在 `Assets/Notes/R{N}落地汇报.md` **末尾续写**质检附录，使用 [remediation-appendix.md](remediation-appendix.md) 模板。
3. 整改条目须 **逐条** 写清：**要求**（引用 Plan/Architecture 原文或条款）vs **当前实现现状**（文件+行为）。
4. 回复用户：摘要问题规模、条目数、为何未归档、建议由推进代理按清单返工后 **重新质检**。

## 阶段 3：评价维度（写入附录或回复）

质检评价至少覆盖：

1. **Plan 符合度**：任务项与验收项逐条 ✅/⚠️/❌
2. **架构符合度**：ArchitectureDesign 不变量是否满足
3. **规范符合度**：rules.md + AGENTS.md 约束
4. **可维护性**：命名、分层、测试可续接性
5. **风险**：硬编码、占位、跨阶段耦合、未文档化假设

## 禁止事项

- 未读汇报和 Plan 对应节，不要凭印象判定
- 问题较大时不要「顺手大改」代替整改清单
- 不要归档未通过质检的汇报
- 不要提交 git（除非用户明确要求）
- 不要手改 `.unity`

## 与推进技能衔接

| 技能 | 路径 | 关系 |
|------|------|------|
| advance-phase-rx | `.cursor/skills/advance-phase-rx/` | 推进产出 `R{N}落地汇报.md` |
| qa-phase-rx | 本技能 | 消费汇报 → 通过则归档 / 不通过则续写整改 |

整改完成后，用户再次触发本技能对同一 `R{N}` 复检。

## 附加资源

- 整改附录模板：[remediation-appendix.md](remediation-appendix.md)
