# Plan.md

> 项目：TableNine / 当前分支：`dev`  
> 版本：2026-06-11 现阶段改造落地计划。  
> 目标：基于现有 M1-M5 原型代码做修补、改造、优化，而不是推倒重来；先让新版规则稳定，再接 UI、动画、音效、Odin 效果资产和叙事表现。

---

## 0. 当前判断

旧 Plan 的 M1-M5 不能再继续作为开发主线。当前真实状态应回退为：

```text
已具备：QFramework 架构骨架 + 九宫格/发牌/补牌/奖励/配置原型 + 一批 EditMode 测试
未达标：新版属性护甲、伤害公式、帮助卡语义、通关顺序、完整战斗管线、技能/遗物触发、EffectSystem、跨会话存档、可复现重放、表现层事件
```

因此本 Plan 采用新的阶段命名：`R0-R9`。其中 R 表示 Refactor / Repair / Runtime，对应“现有代码改造”而不是旧 M 阶段的“从零建设”。

执行原则：

1. 每个阶段必须能编译。
2. 每个规则阶段必须补 EditMode 测试。
3. 不把 UI、动画、音频当作规则正确性的证据。
4. 不继续扩大 `UseHelpCardCommand` 的硬编码 `switch`。
5. 不继续用“攻击 - 防御”作为战斗伤害。
6. 不继续用旧帮助卡语义：默认临时移除、标注永久移除。
7. 不再宣称 M5 完成，直到全 playtest 运行时、存档、重放、Debug 都达标。

---

## 1. 总体路线图

| 阶段 | 名称 | 核心结果 | 允许并行 |
|---|---|---|---|
| R0 | 基线冻结与回归保护 | 确认现有代码真实能力，建立新版回归测试入口 | 无 |
| R1 | 数据结构迁移 | `restoreAfterNode`、`CurrentArmor`、`DamageContext`、新 Phase/Reason 落地 | 少量文档同步 |
| R2 | 新版基础规则热修 | 护甲、伤害公式、帮助卡默认永久移除、通关顺序修正 | 原型 UI 小修 |
| R3 | 战斗管线重建 | `CombatContext` / `DamageContext` / 并行伤害组 / 死亡预防 | 遗物配置整理 |
| R4 | EffectSystem MVP | 帮助卡从硬编码迁入效果系统，先覆盖已实现卡与关键未实现卡 | Odin 数据结构设计 |
| R5 | SkillSystem + Relic Runtime | 技能/遗物触发队列，荆棘甲、凤凰羽毛、套装、刺皮等上线 | Debug 面板扩充 |
| R6 | 奖励/房间/商店收束 | 通关后帮助奖励→房间按钮→节点结束，房间语义稳定 | UI 结构细化 |
| R7 | 存档、重放、Debug | EasySave、状态 Hash、Command replay、Bug report 可用 | PlayMode 测试 |
| R8 | 表现层接入 | CardView、DOTween、AudioKit、ResKit、Overlay、Yarn 接入边界稳定 | 美术/音频资源导入 |
| R9 | 全 playtest 验收 | 3 层 27 节点，核心卡/怪/技能/遗物、失败/胜利闭环 | 平衡与打磨 |

推荐顺序：**R0 → R1 → R2 → R3 → R4 → R5 → R6 → R7 → R8 → R9**。  
不建议 R8 大规模提前，因为表现层会强依赖 R2-R5 的事件和状态语义。

---

## 2. R0：基线冻结与回归保护

### 目标

让后续 AI 明确“现在能复用什么、不能相信什么”，避免继续沿着旧 M5 结论开发。

### 任务

1. 本地拉取 `dev` 分支，确认 Unity 版本为 2022.3 LTS。
2. 运行当前 EditMode 测试，记录通过/失败数量。
3. 若本地无法运行 Unity 测试，至少执行代码级静态核查：
   - `TableNine.cs` 注册项。
   - `RuntimeCommands.cs` 中战斗、帮助卡、房间流程。
   - `RuntimeSystems.cs` 中 Combat/Stat/Effect/Reward/Relic。
   - `RuntimeModels.cs` 中 CardRuntime、DeckModel、FlowModel。
4. 新建或更新一份阶段状态说明，明确旧 M5 回退为“配置就绪 / 运行时未完成”。
5. 给现有测试分类打标签：
   - `LegacyRuleTests`：旧规则下暂时保留，但后续需要改期望。
   - `NewRuleTests`：新版规则测试。
   - `RegressionTests`：与规则语义无关的工程回归。
6. 禁止在 R0 之后继续新增依赖旧语义的测试。

### 重点核查项

- `CombatSystem.CalculateDamage` 是否仍为 `attacker.Attack - defender.Defense`。
- `ApplyDamageCommand` 是否仍只扣 HP。
- `UseHelpCardCommand` 是否仍大段 `switch(cardId)`。
- `ConsumeHelpCardCommand` 是否仍要求调用方传 `permanentlyRemove`。
- `CheckClearConditionCommand` 是否直接弹房间。
- `ChooseRoomCommand` 是否承担了旧的“房间后帮助奖励”。
- `EffectSystem` 是否仍为空。
- `ISkillSystem` 是否未注册。
- `MemorySaveUtility` 是否仍是默认存档实现。

### 产出

- 当前测试结果记录。
- 新旧规则冲突列表。
- 需要修改/废弃的旧测试列表。
- 后续阶段任务确认。

### 验收

- 团队/AI 明确：旧 M5 不可再作为完成状态。
- 后续开发入口改为本 Plan。
- 不再出现“按旧 ArchitectureDesign 继续补 M6 表现”的任务描述。

---

## 3. R1：数据结构迁移

### 目标

先把新版规则需要的数据字段补齐，让 R2/R3 可以改逻辑而不是边改逻辑边补结构。

### 任务 1：帮助卡配置语义迁移

1. 在 `CardDefinition` 中新增：

```csharp
public bool RestoreAfterNode;
```

2. 保留旧字段兼容期，但所有新逻辑读取 `RestoreAfterNode`。
3. 配置加载时做迁移：

```text
如果旧配置只有 IsPermanentRemoveOnUse：
RestoreAfterNode = !IsPermanentRemoveOnUse
```

4. `ConfigValidator` 增加 warning：新配置不应再依赖旧字段。
5. 所有帮助卡默认 `RestoreAfterNode = false`。
6. 仅卡面标注“使用后复原”的卡设置为 `true`。

### 任务 2：运行态护甲字段

1. 在 `CardRuntime` 中新增：

```csharp
public int CurrentArmor;
```

2. 创建统一修改方法或 Command：

```csharp
SetArmorCommand / ChangeArmorCommand
ApplyStatChangeCommand
```

3. UI/Debug 暂时可以显示 0，但字段必须存在。
4. 存档 DTO 预留 `currentArmor`。

### 任务 3：有效属性扩展

扩展 `EffectiveStats`：

```csharp
public int CurrentArmor;
public int DamageReduction;
```

要求：

- `Defense` 保留，用于显示和护甲生成。
- `DamageReduction` 独立计算，不能复用 Defense。
- 旧测试如果只断言 Attack/Defense/HP，不应被破坏。

### 任务 4：DamageContext 引入

新增：

```csharp
public enum DamageType
{
    Combat,
    HelpCard,
    Reflect,
    Relic,
    Skill,
    Room,
    Debug
}
```

新增 `DamageContext`：

```csharp
public sealed class DamageContext
{
    public CardUid? Source;
    public CardUid Target;
    public string CauseId;
    public DamageType Type;
    public int RawAttack;
    public int DamageReduction;
    public int DamageBeforeArmor;
    public int ArmorAbsorbed;
    public int HpDamage;
    public bool IgnoreArmor;
    public bool Preventable = true;
    public bool WasPrevented;
    public bool WasFatalBeforePrevention;
    public List<string> Tags = new List<string>();
}
```

兼容包装：

```csharp
ApplyDamageCommand(CardUid targetUid, int damage)
```

短期保留，但内部转为 `DamageContext`。

### 任务 5：流程 Phase 补齐

检查并补充：

- `RoomChoosing`
- `RoomResolving`
- `NodeEnding`
- `LayerComplete`

如果已有部分枚举，则对齐命名和含义。

### 任务 6：事件和原因枚举预埋

新增或补齐：

- `RemoveReason`
- `BoardMoveReason`
- `HelpCardConsumeReason`
- `DamageAppliedEvent` 新结构字段。
- `ArmorChangedEvent`
- `StatsDirtyEvent`
- `FlowPhaseChangedEvent`
- `InputLockChangedEvent`

### 产出

- 新字段编译通过。
- 旧逻辑暂未完全改，但新结构可用。
- 配置校验能识别帮助卡消耗语义。

### 验收

- 新建卡实例时 `CurrentArmor == 0`。
- 旧存档/旧配置不会因新增字段崩溃。
- 新测试可以构造 `DamageContext`。
- `CardDefinition.RestoreAfterNode` 是新逻辑唯一语义入口。

---

## 4. R2：新版基础规则热修

### 目标

先修最会误导后续开发的四个核心规则：护甲、伤害公式、帮助卡默认永久移除、通关顺序。

---

### R2-1：节点开始填护甲

#### 任务

1. 在 `StatSystem` 增加：

```csharp
void FillArmorFromDefenseAtNodeStart();
```

2. `StartNodeCommand` 在玩家可交互前调用。
3. 最小版先处理玩家卡；如果怪物也要显示/使用护甲，则在怪物创建后统一填充怪物护甲。
4. 发送：

```csharp
ArmorChangedEvent(uid, oldArmor, newArmor, "node_start_defense")
StatsDirtyEvent(uid)
```

#### 测试

- 玩家基础防御为 2，节点开始后护甲为 2。
- 有木盾/木甲加成时，护甲等于有效防御。
- 下一节点开始会重新按有效防御填护甲，而不是继承上节点剩余护甲。

#### 验收

- `StartNewRunCommand` 进入第一节点后，Debug 可看到玩家护甲。
- 护甲不影响攻击和血量上限。

---

### R2-2：获得防御同步加护甲

#### 任务

1. 所有增加防御的入口改走：

```csharp
ApplyStatChangeCommand(targetUid, StatType.Defense, delta, causeId)
```

2. 当 `delta > 0` 且属性为 Defense：

```text
BaseDefense 或临时 Defense 增加 delta
CurrentArmor 增加 delta
```

3. 当获得血量上限：

```text
MaxHp += delta
CurrentHp += delta
```

4. 所有属性最低 0。

#### 测试

- 属性提升选择防御：防御 +1，护甲也 +1。
- 血量上限 +2：MaxHp +2，CurrentHp +2。
- 负数变化不会让属性低于 0。

#### 验收

- 属性提升卡不再直接写 `playerRuntime.BaseDefense += 1`。

---

### R2-3：伤害先扣护甲再扣血

#### 任务

1. 重写 `ApplyDamageCommand`：

```text
如果 preventable 且被庇佑等免疫：WasPrevented=true，伤害归零
如果 IgnoreArmor：HpDamage = DamageBeforeArmor
否则：ArmorAbsorbed = min(CurrentArmor, DamageBeforeArmor)，剩余扣 Hp
```

2. `CurrentArmor` 与 `CurrentHp` 最低为 0。
3. 发送更完整事件：

```csharp
DamageAppliedEvent(context)
ArmorChangedEvent(...)
StatsDirtyEvent(targetUid)
```

4. 旧 `DamageAppliedEvent(targetUid, damage)` 如果被 UI 使用，保留兼容构造或适配。

#### 测试

- 10 护甲受 3 伤害：护甲 7，血不变。
- 2 护甲受 5 伤害：护甲 0，血 -3。
- ignoreArmor 伤害：护甲不变，血扣伤害。
- 0 或负伤害不扣任何值。

#### 验收

- 所有伤害入口都能走 `DamageContext`。

---

### R2-4：战斗公式热修

#### 任务

1. 将 `CombatSystem.CalculateDamage` 从：

```text
max(0, attacker.Attack - defender.Defense)
```

改为：

```text
max(0, attacker.Attack - defender.DamageReduction)
```

2. 更推荐改名：

```csharp
DamageContext BuildCombatDamage(CardUid source, CardUid target, string causeId)
```

3. Defense 不再作为扣伤害项。

#### 测试

- 怪物 Defense 99、DamageReduction 0，玩家 Attack 5，实际伤害仍为 5，再由护甲吸收。
- 怪物 DamageReduction 2，玩家 Attack 5，实际伤害 3。
- 玩家防御只影响节点开始护甲，不影响怪物攻击公式。

#### 验收

- 全库不再有战斗公式依赖 defender.Defense。

---

### R2-5：帮助卡默认永久移除

#### 任务

1. 改造 `ConsumeHelpCardCommand`：

```text
读取 CardDefinition.RestoreAfterNode
true  -> state.IsTemporarilyRemoved = true
false -> state.IsPermanentlyRemoved = true
```

2. 移除调用方传入 `permanentlyRemove` 的语义责任。
3. 当前硬编码帮助卡调用全部改为：

```csharp
SendCommand(new ConsumeHelpCardCommand(helpUid, HelpCardConsumeReason.Used));
```

4. `RewardSystem.RestoreHelpDeckSnapshot` 改名或改逻辑为：

```csharp
RestoreHelpDeckSnapshotByRestoreAfterNode
```

5. 永久移除卡：节点结束不恢复，并从 Owned/State/Collection 清理。
6. 临时移除卡：节点结束恢复为 Active。
7. 未使用且仍在场上/道具格的卡：节点结束 +10 金币。

#### 测试

- 默认帮助卡使用后永久移除，下一节点不再出现。
- `RestoreAfterNode=true` 的帮助卡使用后节点内移除，下一节点恢复。
- 未使用帮助卡在节点结束提供 +10。
- 节点内新获得帮助卡在节点结束后保留。

#### 验收

- 不再出现 `helpDefinition.IsPermanentRemoveOnUse` 作为新逻辑入口。

---

### R2-6：通关流程顺序重排

#### 任务

1. 改 `CheckClearConditionCommand`：

当前：

```text
无怪物 -> ClearReady -> 生成房间候选 -> RoomChoiceRequestedEvent
```

目标：

```text
无怪物 -> ClearReady -> GenerateHelpRewardCommand
```

2. `GenerateHelpRewardCommand`：

```text
RewardSource = NodeClear
FlowPhase = HelpRewardChoosing
OverlayVisible lock = true
HelpRewardGeneratedEvent
```

3. `PickHelpCardRewardCommand` / `SkipHelpRewardCommand` 完成后：

```text
关闭帮助奖励 Overlay
进入 RoomChoosing
生成 2 个房间候选
发送 RoomChoiceRequestedEvent
```

4. `RoomChoosing` 不应锁住棋盘帮助卡交互。
5. `ChooseRoomCommand` 顺序改为：

```text
FlowPhase = NodeEnding or RoomResolving
SettleNodeEndHelpCardsCommand
ResolveRoomCommand
房间事件完成后 ProceedToNextNodeCommand
```

6. 如果房间会打开商店/宝箱 Overlay，则房间流程暂停，直到选择/关闭后再进入下一节点。

#### 测试

- 清场后第一事件是帮助奖励，不是房间选择。
- 帮助奖励选择后进入 RoomChoosing。
- RoomChoosing 阶段仍可拾取场上帮助卡。
- RoomChoosing 阶段可使用道具帮助卡。
- 点击房间后才结算未用帮助卡金币。
- 房间候选数量为 2。

#### 验收

- 原型 UI 即使暂时粗糙，也必须按新顺序显示。

---

## 5. R3：战斗管线重建

### 目标

把 `StartCombatCommand` 从“直接双方扣血 + 旋转补牌”的大块逻辑改成可插入技能、遗物、帮助卡状态的管线。

### 任务 1：拆 CombatContext

新增：

```csharp
public sealed class CombatContext
{
    public CardUid PlayerUid;
    public CardUid MonsterUid;
    public EffectiveStats PlayerStats;
    public EffectiveStats MonsterStats;
    public bool PlayerActsFirst;
    public List<DamageContext> FirstHitGroup;
    public List<DamageContext> CounterHitGroup;
    public CombatResult Result;
}
```

### 任务 2：改造 StartCombatCommand

目标流程：

```text
检查怪物存在与邻接
Lock CombatResolving
Set Phase CombatResolving
Send CombatStartedEvent
Build CombatContext
Send ResolveCombatCommand(context)
Unlock CombatResolving
如果玩家死亡 -> GameOverCommand
否则 -> CommitPlayerActionCommand
```

### 任务 3：ResolveCombatCommand

管线：

1. 触发 `OnBeforeCombat`。
2. 计算先攻。
3. 构建先手伤害组。
4. 加入并行额外伤害：荆棘甲等。
5. 一次性提交先手伤害组。
6. DeathCheck。
7. 目标存活则构建反击伤害组。
8. 加入同步反伤：刺皮等。
9. 一次性提交反击伤害组。
10. DeathCheck。
11. 触发 `OnAfterCombat`。
12. KillSettlement。

### 任务 4：并行伤害组

并行含义：先计算同一组所有伤害，再统一 Apply。避免“先扣死导致另一段不触发”的串行偏差。

示例：

```text
玩家先手伤害 + 荆棘甲额外伤害 -> 同一 FirstHitGroup
怪物反击伤害 + 刺皮反伤 -> 同一 CounterHitGroup
```

### 任务 5：死亡预防

新增 `ApplyDeathPreventCommand`，处理：

- 凤凰羽毛：致命伤后恢复 50% MaxHp，遗物标记 consumed。
- 未来其他免死效果。

### 任务 6：动作提交统一

新增 `CommitPlayerActionCommand`：

```text
如果当前阶段允许棋盘移动
  -> RotateBoardClockwiseCommand
  -> RequestRefillBoardCommand
否则
  -> CheckClearConditionCommand
```

战斗、拾取帮助卡、点击空格都应走这个统一收尾，而不是每个 Command 自己散写旋转/补牌。

### 测试

- 双方无先攻，玩家先。
- 双方有先攻，玩家先。
- 只有怪物先攻，怪物先。
- 玩家先手击杀怪物，怪物不反击。
- 怪物存活时一定反击。
- 荆棘甲与玩家伤害同组。
- 刺皮与怪物反击同组。
- 玩家被致命伤，凤凰羽毛触发后不 GameOver。
- 玩家无凤凰羽毛且 HP 为 0，进入 GameOverCommand。
- 战斗结束只触发一次 CommitPlayerAction。

### 验收

- `StartCombatCommand` 不再直接包含所有伤害细节。
- `KillMonsterCommand` 只做死亡结算，不负责先攻/反击判断。
- 战斗事件足够驱动 UI/Anim/Audio。

---

## 6. R4：EffectSystem MVP

### 目标

停止帮助卡硬编码扩散，用最小可用 EffectSystem 承接帮助卡、遗物、技能、房间效果。

### 范围控制

R4 不要求完整 Odin 可视化编辑器。先做纯 C# 注册表或 ScriptableObject 简单配置，保证规则入口统一。

### 任务 1：EffectGraph 数据结构

新增：

```csharp
public sealed class EffectGraphDefinition
{
    public string EffectGraphId;
    public List<EffectAtomDefinition> Atoms;
}

public sealed class EffectAtomDefinition
{
    public string AtomType;
    public Dictionary<string, string> Parameters;
}
```

### 任务 2：EffectContext

```csharp
public sealed class EffectContext
{
    public EffectSource Source;
    public CardUid? Caster;
    public List<CardUid> Targets;
    public BoardSlotNo? SourceSlot;
    public Dictionary<string, object> Blackboard;
    public List<string> Tags;
}
```

### 任务 3：第一批 Atom

必须先落地：

1. `DamageAtom`
2. `HealAtom`
3. `AddGoldAtom`
4. `ModifyStatAtom`
5. `ConsumeHelpCardAtom`
6. `OpenChoiceOverlayAtom`
7. `AddCardToHelpDeckAtom`
8. `InjectCardToBattleDeckAtom`

第二批：

1. `MoveBoardAtom`
2. `SwapCardsAtom`
3. `RemoveCardAtom`
4. `ApplyStatusAtom`

### 任务 4：迁移已实现帮助卡

从 `UseHelpCardCommand` 的 `switch` 中迁出：

| 帮助卡 | 迁移目标 |
|---|---|
| `help_potion` | `HealAtom + ConsumeHelpCardAtom` |
| `help_throwing_knife` | `OpenTargeting + DamageAtom + ConsumeHelpCardAtom` |
| `help_attribute_up` | `OpenChoiceOverlayAtom + ModifyStatAtom + ConsumeHelpCardAtom` |
| `help_common_chest/help_chest/help_blue_chest/help_gold_chest` | `OpenChoiceOverlayAtom + ConsumeHelpCardAtom` |
| `help_gold_card` | `AddGoldAtom + ConsumeHelpCardAtom` |
| `help_blessing` | `ApplyStatusAtom + ConsumeHelpCardAtom` |

### 任务 5：补齐关键未实现帮助卡

按对战斗/流程影响排序：

1. `help_fireball`：DamageAtom。
2. `help_bomb`：范围 DamageAtom。
3. `help_swap`：SwapCardsAtom。
4. `help_spin_wheel`：MoveBoardAtom。
5. `help_food`：HealAtom。
6. `help_healing_spring`：Heal/MaxHp。
7. `help_boulder`：RemoveCard/Damage。
8. `help_smasher`：ModifyStat 或 DamageReduction。
9. `help_violence`：临时攻击翻倍状态。
10. `help_watchtower` / `help_multiplier_tower`：塔类持续/位置效果，可放到 R5 技能化。

### 任务 6：UseHelpCardCommand 收束

最终只保留：

```text
校验 uid 和类型
读取 CardDefinition.EffectGraphId
构建 EffectContext
ResolveEffectGraphCommand
```

没有 effectGraph 的卡发配置错误，不再静默 NotImplemented。

### 测试

- 已实现 7 类帮助卡迁移后行为不变，但消耗语义改为新版。
- 未配置 effectGraph 的帮助卡会触发 validator error。
- 飞刀目标选择仍可跨距离。
- 属性提升防御时同步护甲。
- 宝箱卡打开后流程能回到 PlayerControl 或 RoomResolving。

### 验收

- `UseHelpCardCommand` 不再是内容逻辑堆积点。
- 新增帮助卡不需要改 Command，只加配置/Atom。

---

## 7. R5：SkillSystem + Relic Runtime

### 目标

让“配置有、运行时无”的技能和遗物开始真正改变规则，而不是只存在于候选和栏位中。

---

### R5-1：SkillSystem 最小骨架

#### 任务

1. 新增并注册：

```csharp
ISkillSystem
SkillSystem
EffectTriggerQueue
```

2. 最小接口：

```csharp
void Trigger(SkillTrigger trigger, TriggerContext context);
IReadOnlyList<SkillTriggerLog> RecentLogs { get; }
```

3. 加防递归：

```text
每个根 Command 最多触发 N 层
同一 uid + skillId + trigger 在同一阶段可限制次数
```

4. 触发后调用 EffectSystem。

#### 首批 Trigger

- `OnNodeStart`
- `OnBeforeCombat`
- `OnModifyDamage`
- `OnAfterDamage`
- `OnCardMoved`
- `OnMonsterKilled`
- `OnNodeClear`
- `OnNodeEnd`

### R5-2：怪物技能上线优先级

优先做影响战斗管线的技能：

1. `刺皮`：怪物反击时同步对玩家造成反伤。
2. `硬皮/减伤类`：提供 `DamageReduction`。
3. `先攻`：已部分存在，迁入统一系统或保持 StatSystem 查询但加测试。
4. 四幼崽位置技能：短期可继续在 StatSystem，但要补 StatsDirty 和显示刷新。
5. `复仇`：怪物攻击累计变化。
6. `红桃之母`：损血累计洗入红桃。

### R5-3：遗物触发上线

优先做：

| 遗物 | 运行时目标 |
|---|---|
| 木剑 | 常驻攻击加成 |
| 木盾 | 常驻防御加成，并影响节点开始护甲 |
| 木甲 | 常驻防御/护甲或套装逻辑 |
| 木剑/木盾/木甲套装 | `StatSystem` 统一判断 |
| 荆棘甲 | 战斗先手伤害组并行额外伤害 |
| 凤凰羽毛 | 致命伤死亡预防，消费遗物 |
| 活着的肉 | 节点/战斗后治疗类触发 |

### R5-4：导师技能上线

`ChooseTutorSkillCommand` 当前只 `AddSkill`。需要：

- 技能配置有 trigger。
- 获得后能在 `SkillSystem` 中被收集。
- 已有技能不可重复或按 stackPolicy 处理。
- UI 显示技能生效描述。

### 测试

- 荆棘甲拿到后，战斗对怪物造成额外伤害。
- 凤凰羽毛触发后遗物被消费，玩家恢复 50% MaxHp。
- 木盾增加防御后，下一节点护甲增加。
- 刺皮怪物反击时玩家同时受到反伤。
- SkillSystem 触发日志记录 skillId、owner、trigger、effectGraph。
- 防递归上限能阻止无限触发。

### 验收

- “拿了没用”的核心遗物问题解决。
- 怪物技能不再只靠少数 StatSystem if 分支。

---

## 8. R6：奖励、房间、商店流程收束

### 目标

把奖励闭环从旧流程改成新版稳定流程，并让房间系统成为后续表现层可接的清晰状态机。

### 任务 1：帮助奖励流程

1. 清场后自动打开帮助奖励三选一。
2. 可跳过 +10。
3. 选择/跳过后关闭 Overlay。
4. 进入 `RoomChoosing`。
5. 生成 2 个房间按钮。

### 任务 2：房间候选

1. `GenerateRoomCandidates` 改为 2 个候选。
2. 候选来源可先简单随机：商店、金币、宝箱、温泉/属性。
3. 后续可配置权重与去重。

### 任务 3：房间事件

先稳定四类：

| 房间 | R6 最小行为 |
|---|---|
| 金币房 | +50 金币，显示事件，然后下一节点 |
| 宝箱房 | 打开遗物三选一，可跳过 +20，选择后下一节点 |
| 温泉/属性房 | 提高血量上限并回满或注入属性提升效果，完成后下一节点 |
| 商店 | 展示 6 张帮助卡；可购买/删卡；关闭商店后下一节点 |

注意：房间是否“注入战斗牌组”如果策划仍有分歧，R6 先按最新版 `层级系统.md` 的房间类型说明实现；文档冲突另开设计问题，不阻塞主流程。

### 任务 4：节点结束结算

点击房间按钮后统一执行：

```text
SettleUnusedHelpCards
Restore/RemoveHelpDeckSnapshotByRestoreAfterNode
Clear battle/demon/node temp state
ResolveRoom
```

不要在帮助奖励选择时提前结算未用帮助卡。

### 任务 5：商店收束

1. 购买帮助卡受容量和同名 3 张上限。
2. 删除帮助卡 +10 金币。
3. 商店中删除永久移除/临时移除状态卡要明确限制：建议只允许删除 active owned help cards。
4. 关闭商店进入房间完成流程，不再回到帮助奖励。

### 测试

- 清场→帮助奖励→房间按钮→点击房间→节点结束。
- 跳过帮助奖励 +10 后仍进入 RoomChoosing。
- RoomChoosing 阶段使用帮助卡会影响节点结束结算。
- 金币房完成后直接进入下一节点。
- 宝箱房选择遗物后进入下一节点。
- 商店关闭后进入下一节点，不再打开帮助奖励。
- 删除帮助卡金币变化正确。

### 验收

- 奖励/房间顺序完全符合新版设计。
- 原型 UI 至少能表达完整流程。

---

## 9. R7：存档、重放、Debug

### 目标

把 playtest 期最重要的定位能力补上：跨会话存档、可复现重放、可复制 bug report。

### R7-1：EasySaveUtility

1. 实现 `EasySaveUtility : ISaveUtility`。
2. `TableNine.cs` 运行态注册 EasySave，测试态可继续用 MemorySave。
3. 存档 schemaVersion。
4. 存档不保存 UnityEngine.Object 引用。

### R7-2：SaveData 扩展

至少保存：

- seed / random state。
- layer / nodeInLayer / phase。
- player：hp、armor、base stats、gold、skills、relics、consumed relics。
- collection：uid、definitionId、hp、armor、boardSlot、itemSlot、counters。
- help deck：owned uid、state、restoreAfterNode、snapshot。
- battle draw pile。
- board slots。
- item slots。
- reward/overlay 上下文。

### R7-3：重放工厂扩展

Command Replay 至少覆盖：

- `StartNewRunCommand`
- `ClickBoardSlotCommand`
- `ClickItemSlotCommand`
- `UseHelpCardCommand`
- `ResolveTargetingCommand`
- `ResolveAttributeChoiceCommand`
- `PickHelpCardRewardCommand`
- `SkipHelpRewardCommand`
- `ChooseRoomCommand`
- `PickRelicRewardCommand`
- `SkipChestRewardCommand`
- `BuyHelpCardCommand`
- `DeleteHelpCardForGoldCommand`
- `CloseShopCommand`
- `ChooseTutorSkillCommand`

### R7-4：状态 Hash

新增查询：

```csharp
GetBoardSnapshotHashQuery
GetRunSnapshotHashQuery
```

Hash 包含：

- 棋盘 1~9 uid/id/hp/armor。
- 玩家 hp/armor/gold。
- 战斗牌堆 uid/id 顺序。
- 帮助卡状态。
- phase 和锁。

### R7-5：Debug 面板

扩展显示：

- 当前护甲。
- DamageReduction。
- 帮助卡快照明细。
- 临时/永久移除列表。
- 怪物剩余判断详情。
- 最近 50 条事件。
- 最近 50 条 Command。
- 一键复制 bug report。
- 一键保存/读档。
- 一键重放并比较 hash。

### 测试

- 存档后重启进程仍能读取。
- Save/Load 后棋盘 hash 一致。
- 同 seed + command list 重放后棋盘 hash 一致。
- 覆盖层中保存/读档不会锁死输入。

### 验收

- 质检可以用 seed + command log 复现吞卡、并发补牌、战斗异常。

---

## 10. R8：表现层接入

### 目标

在规则稳定后，把美术像素图、DOTween 动效、音效/BGM、ResKit、Yarn 接到明确事件点上。

### 前置条件

必须完成：

- R2 基础规则。
- R3 战斗事件。
- R6 通关/房间流程。
- 核心事件：`CardMovedEvent`、`DamageAppliedEvent`、`ArmorChangedEvent`、`FlowPhaseChangedEvent`、`OverlayOpenedEvent`。

### 任务 1：CardView / BoardSlotView

1. `CardView` 显示：图、名称、攻、防、血、护甲、技能/状态图标。
2. `BoardSlotView` 管 slot 绑定。
3. `CardViewPresenter` 监听事件刷新。
4. PoolKit 可后接，先保证视图状态正确。

### 任务 2：DOTween / Anim

`Anim/` 只监听事件：

- `CardPlacedEvent`：发牌/补牌入场。
- `CardMovedEvent`：移动。
- `BoardRotatedEvent`：旋转节奏。
- `DamageAppliedEvent`：命中、跳字。
- `ArmorChangedEvent`：护甲吸收特效。
- `MonsterKilledEvent`：死亡。
- `EffectResolvedEvent`：帮助卡/技能/遗物特效。

规则等待动画通过 `ISequenceUtility`，不要在动画回调里直接改 Model。

### 任务 3：AudioKit

按事件接：

- 点击。
- 发牌。
- 旋转。
- 攻击命中。
- 护甲吸收。
- 治疗。
- 怪物死亡。
- 奖励选择。
- 商店购买。
- 胜利/失败。

### 任务 4：ResKit

资源 ID 绑定：

- cardId -> sprite/prefab。
- effectId -> effect prefab。
- audioId -> clip。
- overlayId -> panel prefab。

缺资源时必须降级为占位图/静音，不阻塞规则。

### 任务 5：UIKit Overlay

整理：

- HelpRewardPanel。
- RoomChoiceHUD，非阻塞。
- ChestRewardPanel。
- ShopPanel。
- TutorSkillPanel。
- AttributeChoicePanel。
- PausePanel。
- DebugPanel。

### 任务 6：Yarn / NarrativeSystem

1. Yarn 触发只监听事件。
2. Yarn 命令如果改状态，必须转发 QFramework Command。
3. 对话时加 `DialogueRunning` 锁。
4. 首次商店、首次宝箱、首次精英、首次死亡预防可作为教程触发点。

### PlayMode 验收

- 第一节点完整可视化打通。
- 动画期间重复点击不会破坏状态。
- 音频缺失不报错。
- 房间按钮出现时仍能用场上帮助卡。
- Debug 面板可打开且不破坏输入锁。

---

## 11. R9：全 playtest 验收

### 目标

重新定义真正的“全 playtest 完成”。

### 内容验收

必须达标：

- 3 层 27 节点可完整推进。
- 第 5 节点精英、第 9 节点层主正确生成。
- 全部 playtest 帮助卡有运行时效果或明确降级说明。
- 全部核心怪物技能有运行时效果或明确降级说明。
- 全部核心遗物有运行时效果或明确降级说明。
- 失败和胜利都能进入稳定终态。
- 存档/读档跨会话可用。
- 重放能复现棋盘状态。
- Debug 面板能输出 bug report。

### 平衡验收

- 第一层普通节点不会因护甲规则改动导致完全无伤或必死。
- 防御转护甲后，木盾/木甲价值明确。
- 帮助卡默认永久移除后，帮助卡奖励频率和商店价格需要复核。
- 未用帮助卡 +10 的收益不会压过正常使用收益。
- 精英/层主注入奖励卡不会导致流程卡死。

### 表现验收

- 所有核心卡牌有图或占位图。
- 主要动作有动画或可接受的灰盒动效。
- 主要事件有音效或静音降级。
- BGM 切换不影响流程。
- UI 在 16:9 和常用窗口尺寸下可操作。

### 质检验收

- EditMode 全通过。
- PlayMode 垂直切片通过。
- 随机 10 个 seed 至少能跑到第一层结束或给出可复现 bug report。
- 配置校验无 error；warning 有明确记录。
- 旧 M1-M5 质检报告中的 P0 项全部关闭或转为已确认降级。

---

## 12. 阶段间禁止事项

### R2 完成前禁止

- 大规模接 DOTween 动画。
- 大规模重做 UI prefab。
- 继续新增基于 `攻击 - 防御` 的技能/遗物。
- 继续新增 `isPermanentRemoveOnUse` 调用。

### R3 完成前禁止

- 做复杂反伤/免死/多段伤害表现。
- 把荆棘甲、刺皮、凤凰羽毛硬编码到 UI 或单个 Command。

### R4 完成前禁止

- 继续把新帮助卡写进 `UseHelpCardCommand` 的 `switch`。
- 为每张帮助卡单独写一个 UI 流程。

### R7 完成前谨慎

- 宣称“可 playtest 质检”。
- 合入大批内容配置但没有重放定位手段。

---

## 13. 推荐任务切片给后续 AI

### 第一批：R1 + R2 最小闭环

交给 AI 的任务描述建议：

```text
请基于 dev 分支现有代码，只做新版基础规则改造：
1. CardDefinition 增加 restoreAfterNode 并迁移旧 isPermanentRemoveOnUse 语义。
2. CardRuntime 增加 CurrentArmor。
3. EffectiveStats 增加 CurrentArmor 和 DamageReduction。
4. ApplyDamageCommand 改为先扣护甲再扣血。
5. CombatSystem 伤害公式改为 attack - damageReduction，不再减 defense。
6. StartNodeCommand 节点开始按有效防御填护甲。
7. ConsumeHelpCardCommand 改为读取 restoreAfterNode，默认永久移除。
8. CheckClearConditionCommand 改为先弹帮助卡三选一，再进入 RoomChoosing。
请补齐对应 EditMode 测试，暂不接 UI 动画。
```

### 第二批：R3 战斗管线

```text
请重构 StartCombatCommand：引入 CombatContext/DamageContext/ResolveCombatCommand/CommitPlayerActionCommand。
必须支持先攻、存活反击、先手击杀不反击、荆棘甲并行伤害、刺皮与反击同步、凤凰羽毛死亡预防。
请补集成测试，尤其是 ClickBoardSlotCommand 发起战斗的端到端测试。
```

### 第三批：R4 EffectSystem MVP

```text
请实现 EffectSystem MVP 和 EffectAtom 注册表，把 help_potion、help_throwing_knife、help_attribute_up、宝箱卡、help_gold_card、help_blessing 从 UseHelpCardCommand switch 迁移到 EffectGraph/Atom。
UseHelpCardCommand 最终只负责校验、构建 EffectContext、调用 ResolveEffectGraphCommand。
```

### 第四批：R6 流程收束

```text
请收束通关奖励与房间流程：清场后帮助奖励，帮助奖励结束后 2 个房间按钮，RoomChoosing 阶段允许继续拾取/使用帮助卡，点击房间后结算未用帮助卡金币并恢复/移除帮助卡，再处理房间事件和进入下一节点。
```

### 第五批：R8 表现接入

```text
规则阶段完成后，请基于事件系统接入 CardView、BoardSlotView、DOTween 动画、AudioKit 音效和 UIKit Overlay。表现层只能监听事件和发送 Command，不允许直接改 Model。
```

---

## 14. 完成定义

本 Plan 的完成不是“文档写完”，而是：

```text
R0-R7 完成：项目可稳定规则 playtest，可保存，可重放，可定位 bug。
R8 完成：美术、动画、音效、UI 能接入且不污染规则层。
R9 完成：才允许重新宣称全 playtest 达标。
```

在 R9 之前，项目状态建议写作：

```text
TableNine 当前处于“规则主干重构与表现接入前置阶段”。
已完成配置和原型闭环；正在对齐新版设计文档的护甲、伤害、帮助卡生命周期、通关流程、战斗管线和效果系统。
```

---

## 15. 当前最高优先级清单

从明天开始最应该做的 10 件事：

1. 跑一次现有测试，记录基线。
2. 给 `CardRuntime` 加 `CurrentArmor`。
3. 给 `CardDefinition` 加 `RestoreAfterNode`。
4. 给 `EffectiveStats` 加 `DamageReduction`。
5. 改 `ApplyDamageCommand`：护甲优先。
6. 改 `CombatSystem.CalculateDamage`：攻击减伤害减免，不减防御。
7. 改 `ConsumeHelpCardCommand`：默认永久移除。
8. 改 `CheckClearConditionCommand`：先帮助奖励，再房间。
9. 加 R2 对应 EditMode 测试。
10. 暂停所有大规模表现层接入，直到 R2 通过。

完成这 10 件事后，项目方向会从“旧 M5 原型继续膨胀”切回“新版设计可落地的规则主干”。