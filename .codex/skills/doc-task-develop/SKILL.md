---
name: doc-task-develop
description: 按用户指定的任务规格文档推进 TableNine 开发落地：读架构与规格、实现任务、跑测试、写落地汇报。Use when the user says 落地、推进、实现、按文档开发、开发任务、完成某文档任务，或 shorthand 推进 R0-R9 / 按 Plan.md / 按 ArchitectureDesign / 按 Assets/Docs 某文档。
---

# 文档任务落地

将用户指定的**任务规格文档**（及可选范围）落地为可编译、测试通过的代码，并产出供质检入场的落地汇报。

与 `doc-task-qa`、`doc-task-rework` 组成三件套：**落地 → 质检 → 返工**。

## 触发与任务目标解析

从用户消息解析以下字段；**规格文档**缺失时先询问，不要猜测。

| 字段 | 含义 | 解析规则 |
|------|------|----------|
| `spec_doc` | 任务规格文档路径 | 用户显式给出的 `.md` 路径；或 shorthand「R3」「推进 R2」→ `Assets/Notes/Plan.md` |
| `spec_scope` | 文档内任务范围 | 章节标题、§号、小节名；R{N} → Plan 中 `## {N+2}. R{N}：` 整节 |
| `report_slug` | 汇报文件标识 | 未指定时：Plan R{N} → `R{N}`；其他 → `{文档basename}-{范围缩写}`（简短、无空格） |
| `report_path` | 汇报落盘路径 | `Assets/Notes/{report_slug}落地汇报.md` |

常见触发示例：

- `按 Plan.md 推进 R3` / `推进 R3` / `落地 R3`
- `按 ArchitectureDesign.md §4 实现 EffectSystem`
- `按 Assets/Docs/CombatRules.md 实现护甲规则`
- `实现任务：Assets/Notes/SomeFeaturePlan.md`

若用户只给汇报 slug（如「继续 R3 落地」），反查 `Assets/Notes/` 下是否已有同 slug 汇报或询问 `spec_doc`。

## 阶段 0：上下文加载（必读）

按顺序阅读；只读与 `spec_scope` 相关的章节，避免通读全文：

| 顺序 | 文件 | 读什么 |
|------|------|--------|
| 1 | [AGENTS.md](../../../AGENTS.md) | 项目背景、QFramework 入口、协作约定 |
| 2 | [rules.md](../../../rules.md) | 架构四层规范、文件保护规则 |
| 3 | [Assets/Notes/QframeworkNotes/architecture.md](../../../Assets/Notes/QframeworkNotes/architecture.md) | 注册表、分层、核心流程导航 |
| 4 | `spec_doc` | `spec_scope` 范围内的目标、任务、产出、验收 |
| 5 | 规格引用的文档 | 如 [ArchitectureDesign.md](../../../Assets/Notes/ArchitectureDesign.md)、`Assets/Docs/` 中被引用的章节 |
| 6 | 同 slug 或前序汇报（若存在） | `Assets/Notes/{report_slug}落地汇报.md` 或归档目录中的同名文件 |

**Plan.md 快捷对照**（仅当 `spec_doc` 为 Plan 且范围为 R{N} 时）：

| 范围 | Plan 节 |
|------|---------|
| R0 | §2 |
| R1 | §3 |
| … | … |
| R9 | §11 |

**项目全局红线**（规格未覆盖时仍遵守；Plan §0 为权威来源之一）：

1. 任务结束必须能编译。
2. 规则改动必须补 EditMode 测试。
3. UI/动画/音频不能作为规则正确性证据。
4. 不扩大 `UseHelpCardCommand` 硬编码 `switch`。
5. 不用「攻击 - 防御」作战斗伤害。
6. 不用旧帮助卡默认临时移除语义。

## 阶段 1：现状勘探

用 **codegraph** 加速，禁止盲目全库 grep：

```
1. codegraph_context(query="<本任务核心关键词>")
2. codegraph_explore(symbols=[...])
3. 需要调用链时：codegraph_trace(from=..., to=...)
4. 改前评估：codegraph_impact(symbol=...)
```

记录与规格「重点核查项 / 验收项」的偏差，作为实施清单。

## 阶段 2：实施

1. **最小正确 diff**：只改本任务范围；复用现有 Model/Command/System 组织。
2. **QFramework 约束**：`AbstractCommand` / `AbstractSystem` / `IController` 内访问架构能力必须用 `this.` 扩展方法（见 AGENTS.md）。
3. **测试同步**：按规格要求新增或更新 EditMode 测试；旧语义测试改期望或标 `LegacyRuleTests`，勿新增依赖旧语义的测试。
4. **分层文档**：改动 Command/System/Model/Event/Controller 后同步维护 `Assets/Notes/QframeworkNotes/` 对应分层文档。
5. **规格文档**：本技能默认不改 `spec_doc`；实现假设写入汇报「待确认项」。

## 阶段 3：验证（Unity MCP + 测试）

脚本改动后**必须**走编译与测试闭环：

```
1. 读 mcpforunity://editor/state → 等待 isCompiling == false
2. read_console(types=["error"], ...) → 编译错误归零再继续
3. 跑本任务新增/改动相关 EditMode 测试
4. read_console(types=["error","warning"]) → 记录终态
5. 若 MCP 不可用：说明原因，改用 dotnet/本地能跑的测试手段，并在汇报标注「未走 Unity MCP 验证」
```

Unity MCP 脚本编辑后不要多余 `refresh_unity`；`create_script` / `script_apply_edits` 已触发编译。

**通过标准**：

- 编译 0 error
- 本任务相关 EditMode 测试全过
- Console 无新增 error（warning 需在汇报说明）

## 阶段 4：落地汇报

完成后在 `Assets/Notes/` 落地：

```
Assets/Notes/{report_slug}落地汇报.md
```

使用 [report-template.md](report-template.md) 结构。汇报核心目的：**让后续质检快速知道改了什么、测了什么、对照规格差在哪**。

必填侧重：

- **任务来源**：`spec_doc` + `spec_scope` 可点击链接
- **改动清单**：文件路径 + 一句话目的（按 新增 / 修改 / 删除 分类）
- **测试证据**：EditMode/PlayMode 通过数、Console 终态
- **规格对照**：任务项 + 验收项是否满足
- **质检入口**：建议优先打开的脚本、测试类、场景路径
- **风险与待确认**：实现假设、未覆盖边界、已知 warning

不要只写「已完成」；质检靠 diff 级信息入场。

## 禁止事项

- 不要超出 `spec_scope` 做大范围「顺手重构」
- 不要宣称任务完成但测试未跑或未写入汇报
- 不要提交 git（除非用户明确要求）
- 不要手改 `.unity`（见 rules.md）

## 三件套衔接

| 技能 | 关系 |
|------|------|
| `doc-task-develop` | 本技能：产出 `{report_slug}落地汇报.md` |
| `doc-task-qa` | 用户触发「质检 {report_slug}」→ 通过归档 / 不通过写整改附录 |
| `doc-task-rework` | 消费整改附录 → 修补 → 回填；完成后用户再次触发质检 |

## 附加资源

- 汇报模板：[report-template.md](report-template.md)
