---
name: rework-phase-rx
description: 按质检整改清单返工 TableNine R0-R9 阶段落地：读 R{N} 落地汇报与质检附录、对照 Plan/ArchitectureDesign 修补代码、逐条回填返工记录。Use when the user says 返工、返工 R0/R1/R2/R3/R4/R5/R6/R7/R8/R9、修补质检、整改修复、按质检要求修、处理整改条目。
---

# 返工阶段 Rx

消费 `qa-phase-rx` 产出的整改清单，在代码层逐项修补，并在原落地汇报末尾回填返工证据。不重新推进整阶段，不替代复检。

## 触发与输入

从用户消息提取 `N`（0-9）。常见表述：`返工 R1`、`R2 返工`、`按质检要求修补 R3`。

若未指定阶段号，先询问。

## 阶段 0：定位汇报与质检附录

按顺序查找汇报文件：

1. `Assets/Notes/R{N}落地汇报.md`（**首选**）
2. 若不存在：`Assets/Notes/过程性汇报文档归档/R{N}落地汇报.md`

找不到则告知需先 `advance-phase-rx` 推进或确认阶段号。

**必读段落**（汇报内）：

| 段落 | 用途 |
|------|------|
| §2 文件变更清单 | 改动范围与质检入口 |
| §6 质检路线图 | 优先打开的脚本/测试 |
| `## 质检记录` 至文末 | 判定、问题摘要、**整改条目**、建议返工顺序、重新质检入口 |

若文末无 `## 质检记录` 或判定为 ✅ 通过，告知无需返工或应走 `qa-phase-rx` 复检；不要凭空造整改项。

## 阶段 1：加载返工基准

| 顺序 | 文件 | 读什么 |
|------|------|--------|
| 1 | [AGENTS.md](../../../AGENTS.md) | QFramework 约束、项目入口 |
| 2 | [rules.md](../../../rules.md) | 四层架构、文件保护 |
| 3 | `R{N}落地汇报.md` | 全文 + 质检附录（已读） |
| 4 | [Assets/Notes/Plan.md](../../../Assets/Notes/Plan.md) | `## {N+2}. R{N}：` 任务与验收 |
| 5 | [Assets/Notes/ArchitectureDesign.md](../../../Assets/Notes/ArchitectureDesign.md) | 质检附录引用的相关章节 |

Plan 章节：`R0`→§2 … `R9`→§11。

执行原则（Plan §0，全程遵守）：与 `advance-phase-rx` 相同（编译、EditMode、红线语义等）。

## 阶段 2：解析整改清单

从 `## 整改条目` 表格提取每一行：

- `#`、类别、严重度、**要求**、**当前实现现状**、建议落点

工作规则：

1. **严格按「建议返工顺序」**执行；P0 → P1 → P2。
2. 每条以 **要求** 为验收标准，以 **当前实现现状** 为起点；不要扩大为整阶段重写。
3. 保留 **符合项（可保留）** 中的正确改动，避免误删。
4. 用 **codegraph** 定位与评估：`codegraph_context` → `codegraph_explore`；改前 `codegraph_impact`。
5. **QFramework**：`AbstractCommand` / `AbstractSystem` / `IController` 内架构访问须 `this.` 扩展方法。

## 阶段 3：实施修补

1. **最小正确 diff**：只改整改条目与建议落点涉及的范围。
2. **测试同步**：每条 P0/P1 尽量有 EditMode 覆盖；P2 测试缺口条目必须补断言。
3. **不手改** `.unity`（见 rules.md）。
4. **不提交 git**（除非用户明确要求）。

## 阶段 4：验证

与 `advance-phase-rx` 相同闭环：

```
1. Unity MCP：editor/state → isCompiling == false
2. read_console(types=["error"]) → 0 error
3. 跑「重新质检入口」列出的测试 + 本阶段相关 EditMode
4. read_console(types=["error","warning"]) → 记录终态
5. MCP 不可用时：说明原因，用可替代手段，并在返工记录标注
```

通过标准：编译 0 error；整改相关测试全过；无新增 error。

## 阶段 5：回填落地汇报

在 **同一** `R{N}落地汇报.md` 末尾追加或更新（勿删除原质检附录）。使用 [rework-record-template.md](rework-record-template.md)。

必做：

1. **Plan 验收复验表**：将「整改后（返工时填）」列从空填为 ✅/❌ + 一句话证据。
2. **返工记录**：按整改条目 `#` **逐条**写修复情况（改了什么文件、测试名、是否满足「要求」）。
3. 若返工新增/修改了文件，在 §2 变更清单 **追加一行** 或脚注说明（勿重写整表）。
4. 文末提示用户：返工完成后执行 **「质检 R{N}」** 触发 `qa-phase-rx` 复检。

未完成全部条目时：在返工记录标明 **部分完成** 与剩余项，不要宣称可归档。

## 阶段映射速查

| 阶段 | Plan 节 |
|------|---------|
| R0 | §2 |
| R1 | §3 |
| R2 | §4 |
| R3 | §5 |
| R4 | §6 |
| R5 | §7 |
| R6 | §8 |
| R7 | §9 |
| R8 | §10 |
| R9 | §11 |

## 禁止事项

- 未读质检附录就开始改代码
- 忽略「符合项」导致回退正确实现
- 返工时跨阶段大范围重构
- 只改代码不回填汇报
- 自行判定通过并归档（归档仅 `qa-phase-rx` 在 ✅ 后执行）
- 提交 git（除非用户明确要求）

## 与相邻技能衔接

| 技能 | 关系 |
|------|------|
| `advance-phase-rx` | 首次推进产出汇报 |
| `qa-phase-rx` | 产出整改附录；返工后由用户再次触发复检 |
| `rework-phase-rx` | 本技能：消费整改附录 → 修补 → 回填返工记录 |

## 附加资源

- 返工记录模板：[rework-record-template.md](rework-record-template.md)
