---
name: doc-task-rework
description: 按质检整改清单返工 TableNine 文档任务落地：读落地汇报与质检附录、对照任务规格修补代码、逐条回填返工记录。Use when the user says 返工、修补质检、整改修复、按质检要求修、处理整改条目，或 shorthand 返工 R0-R9 / 返工某落地汇报。
---

# 文档任务返工

消费 `doc-task-qa` 产出的整改清单，在代码层逐项修补，并在原落地汇报末尾回填返工证据。不重新推进整任务，不替代复检。

与 `doc-task-develop`、`doc-task-qa` 组成三件套：**落地 → 质检 → 返工**。

## 触发与任务目标解析

从用户消息解析 `report_slug`（及可选 `spec_doc` / `spec_scope`）。

常见表述：`返工 R1`、`R2 返工`、`按质检要求修补 CombatRules-护甲`、`处理 R3 整改条目`。

若未指定 `report_slug`，先询问。

## 阶段 0：定位汇报与质检附录

按顺序查找汇报文件：

1. `Assets/Notes/{report_slug}落地汇报.md`（**首选**）
2. 若不存在：`Assets/Notes/过程性汇报文档归档/{report_slug}落地汇报.md`

找不到则告知需先 `doc-task-develop` 落地或确认 `report_slug`。

**必读段落**（汇报内）：

| 段落 | 用途 |
|------|------|
| §1 任务来源 | `spec_doc` + `spec_scope` |
| §2 文件变更清单 | 改动范围与质检入口 |
| §6 质检路线图 | 优先打开的脚本/测试 |
| `## 质检记录` 至文末 | 判定、问题摘要、**整改条目**、建议返工顺序、重新质检入口 |

若文末无 `## 质检记录` 或判定为 ✅ 通过，告知无需返工或应走 `doc-task-qa` 复检；不要凭空造整改项。

## 阶段 1：加载返工基准

| 顺序 | 文件 | 读什么 |
|------|------|--------|
| 1 | [AGENTS.md](../../../AGENTS.md) | QFramework 约束、项目入口 |
| 2 | [rules.md](../../../rules.md) | 四层架构、文件保护 |
| 3 | 落地汇报 | 全文 + 质检附录 |
| 4 | `spec_doc` | `spec_scope` 任务与验收（从汇报 §1/§3 取路径） |
| 5 | 规格引用的文档 | 质检附录引用的 ArchitectureDesign 等 |

**项目全局红线**：与 `doc-task-develop` 相同（编译、EditMode、Plan §0 语义等）。

## 阶段 2：解析整改清单

从 `## 整改条目` 表格提取每一行：

- `#`、类别、严重度、**要求**、**当前实现现状**、建议落点

工作规则：

1. **严格按「建议返工顺序」**执行；P0 → P1 → P2。
2. 每条以 **要求** 为验收标准，以 **当前实现现状** 为起点；不要扩大为整任务重写。
3. 保留 **符合项（可保留）** 中的正确改动，避免误删。
4. 用 **codegraph** 定位与评估：`codegraph_context` → `codegraph_explore`；改前 `codegraph_impact`。
5. **QFramework**：`AbstractCommand` / `AbstractSystem` / `IController` 内架构访问须 `this.` 扩展方法。

## 阶段 3：实施修补

1. **最小正确 diff**：只改整改条目与建议落点涉及的范围。
2. **测试同步**：每条 P0/P1 尽量有 EditMode 覆盖；P2 测试缺口条目必须补断言。
3. **分层文档**：若返工触及注册表，同步 QframeworkNotes 分层文档。
4. **不手改** `.unity`（见 rules.md）。
5. **不提交 git**（除非用户明确要求）。

## 阶段 4：验证

与 `doc-task-develop` 相同闭环：

```
1. Unity MCP：editor/state → isCompiling == false
2. read_console(types=["error"]) → 0 error
3. 跑「重新质检入口」列出的测试 + 本任务相关 EditMode
4. read_console(types=["error","warning"]) → 记录终态
5. MCP 不可用时：说明原因，用可替代手段，并在返工记录标注
```

通过标准：编译 0 error；整改相关测试全过；无新增 error。

## 阶段 5：回填落地汇报

在 **同一** `{report_slug}落地汇报.md` 末尾追加或更新（勿删除原质检附录）。使用 [rework-record-template.md](rework-record-template.md)。

必做：

1. **验收复验表**：将「整改后（返工时填）」列从空填为 ✅/❌ + 一句话证据。
2. **返工记录**：按整改条目 `#` **逐条**写修复情况（改了什么文件、测试名、是否满足「要求」）。
3. 若返工新增/修改了文件，在 §2 变更清单 **追加一行** 或脚注说明（勿重写整表）。
4. 文末提示用户：返工完成后执行 **「质检 {report_slug}」** 触发 `doc-task-qa` 复检。

未完成全部条目时：在返工记录标明 **部分完成** 与剩余项，不要宣称可归档。

## 禁止事项

- 未读质检附录就开始改代码
- 忽略「符合项」导致回退正确实现
- 返工时超出整改范围大范围重构
- 只改代码不回填汇报
- 自行判定通过并归档（归档仅 `doc-task-qa` 在 ✅ 后执行）
- 提交 git（除非用户明确要求）

## 三件套衔接

| 技能 | 关系 |
|------|------|
| `doc-task-develop` | 首次落地产出汇报 |
| `doc-task-qa` | 产出整改附录；返工后由用户再次触发复检 |
| `doc-task-rework` | 本技能：消费整改附录 → 修补 → 回填返工记录 |

## 附加资源

- 返工记录模板：[rework-record-template.md](rework-record-template.md)
