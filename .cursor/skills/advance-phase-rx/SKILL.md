---
name: advance-phase-rx
description: 按 Plan.md 的 R0-R9 阶段推进 TableNine 改造落地：读架构文档、实现阶段任务、跑测试、写阶段性汇报。Use when the user says 推进阶段、推进 R0/R1/R2/R3/R4/R5/R6/R7/R8/R9、落地 Rx、完成 R 阶段、阶段 R+数字。
---

# 推进阶段 Rx

将用户指定的 `R{N}` 阶段从 Plan 落地到可编译、测试通过的代码，并产出供质检入场的阶段性汇报。

## 触发与输入解析

从用户消息提取目标阶段号 `N`（0-9）。常见表述：

- `推进阶段：R1`
- `推进 R2`
- `落地 R3`

若未指定阶段号，先询问；不要猜测。

## 阶段 0：上下文加载（必读）

按顺序阅读，只读与目标阶段相关的章节，避免通读全文：

| 顺序 | 文件 | 读什么 |
|------|------|--------|
| 1 | [AGENTS.md](../../../AGENTS.md) | 项目背景、QFramework 入口、协作约定 |
| 2 | [rules.md](../../../rules.md) | 架构四层规范、文件保护规则 |
| 3 | [Assets/Notes/Plan.md](../../../Assets/Notes/Plan.md) | `## {N+2}. R{N}：` 整节（目标、任务、产出、验收） |
| 4 | [Assets/Notes/ArchitectureDesign.md](../../../Assets/Notes/ArchitectureDesign.md) | 与本阶段任务相关的架构落点章节 |
| 5 | `Assets/Docs/` | 仅当 Plan/Architecture 引用具体玩法规则时再查 |

Plan 章节对照：`R0`→§2，`R1`→§3，…，`R9`→§11。

执行原则（Plan §0，全程遵守）：

1. 每阶段结束必须能编译。
2. 规则改动必须补 EditMode 测试。
3. UI/动画/音频不能作为规则正确性证据。
4. 不扩大 `UseHelpCardCommand` 硬编码 `switch`。
5. 不用「攻击 - 防御」作战斗伤害。
6. 不用旧帮助卡默认临时移除语义。

## 阶段 1：现状勘探

用 **codegraph** 加速，禁止盲目全库 grep：

```
1. codegraph_context(query="<本阶段核心关键词>")   # 如 DamageContext、CurrentArmor
2. codegraph_explore(symbols=[...])               # 一次拉取相关符号源码
3. 需要调用链时：codegraph_trace(from=..., to=...)
4. 改前评估：codegraph_impact(symbol=...)
```

同时扫一眼同阶段或前序汇报（若存在）：`Assets/Notes/R*-落地汇报.md`。

记录与 Plan「重点核查项」的偏差，作为实施清单。

## 阶段 2：实施

1. **最小正确 diff**：只改本阶段任务范围；复用现有 Model/Command/System 组织。
2. **QFramework 约束**：`AbstractCommand` / `AbstractSystem` / `IController` 内访问架构能力必须用 `this.` 扩展方法（见 AGENTS.md）。
3. **测试同步**：按 Plan 要求新增或更新 EditMode 测试；旧语义测试改期望或标 `LegacyRuleTests`，勿新增依赖旧语义的测试。
4. **文档**：本阶段不要求改 Plan；实现假设写入汇报「待确认项」。

## 阶段 3：验证（Unity MCP + 测试）

脚本改动后**必须**走编译与测试闭环：

```
1. 读 mcpforunity://editor/state → 等待 isCompiling == false
2. read_console(types=["error"], ...) → 编译错误归零再继续
3. 跑 EditMode 测试（优先本阶段新增/改动相关用例）
4. read_console(types=["error","warning"]) → 记录终态
5. 若 MCP 不可用：说明原因，改用 dotnet/本地能跑的测试手段，并在汇报标注「未走 Unity MCP 验证」
```

Unity MCP 脚本编辑后不要多余 `refresh_unity`；`create_script` / `script_apply_edits` 已触发编译。

**通过标准**（与 Plan 一致）：

- 编译 0 error
- 本阶段相关 EditMode 测试全过
- Console 无新增 error（warning 需在汇报说明）

## 阶段 4：阶段性汇报

完成后在 `Assets/Notes/` 落地：

```
Assets/Notes/R{N}落地汇报.md
```

使用 [report-template.md](report-template.md) 结构。汇报核心目的：**让后续质检快速知道改了什么、新增了什么、删了什么、下一步查哪里**。

必填侧重：

- **改动清单**：文件路径 + 一句话目的（按 新增 / 修改 / 删除 分类）
- **测试证据**：EditMode/PlayMode 通过数、Console 终态
- **Plan 对照**：任务 checkbox + 验收项是否满足
- **质检入口**：建议优先打开的脚本、测试类、场景路径
- **风险与待确认**：实现假设、未覆盖边界、已知 warning

不要只写「已完成」；质检靠 diff 级信息入场。

## 阶段映射速查

| 阶段 | Plan 节 | 核心关键词 |
|------|---------|------------|
| R0 | §2 | 基线、回归测试、LegacyRuleTests |
| R1 | §3 | RestoreAfterNode、CurrentArmor、DamageContext、Phase |
| R2 | §4 | 护甲、伤害公式、帮助卡永久移除、通关顺序 |
| R3 | §5 | CombatContext、并行伤害、死亡预防 |
| R4 | §6 | EffectSystem、EffectGraph、Atom |
| R5 | §7 | SkillSystem、Relic、触发队列 |
| R6 | §8 | 帮助奖励、房间、节点结算 |
| R7 | §9 | EasySave、重放、Debug |
| R8 | §10 | CardView、DOTween、AudioKit、表现层 |
| R9 | §11 | 全 playtest、27 节点闭环 |

## 禁止事项

- 不要跨阶段做大范围「顺手重构」
- 不要在 R2-R5 完成前大规模做 R8 表现层
- 不要宣称阶段完成但测试未跑或未写入汇报
- 不要提交 git（除非用户明确要求）

## 附加资源

- 汇报模板：[report-template.md](report-template.md)
