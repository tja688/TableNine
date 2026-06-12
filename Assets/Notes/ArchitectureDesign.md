# ArchitectureDesign.md

> 项目：TableNine / Unity 2022.3 LTS / Built-in Render Pipeline / 2D 卡牌 Roguelike / QFramework。  
> 分支：`dev`  
> 版本：2026-06-11 重构收束版。  
> 目标：把当前 M1-M5 原型代码修正到新版 `Assets/Docs` 规则，并为后续 UI、动画、音频、Odin 效果资产、Text Animator 文本动效接入留下稳定接口。

---

## 0. 一句话结论

当前项目不应该推倒重做。已有的 QFramework 主干、Model/Command/System 分层、九宫格、发牌、补牌、奖励原型、27 节点配置可以继续复用；但必须把规则核心从旧的“攻击-防御、房间先于帮助奖励、帮助卡默认临时移除”改成新版的“防御生成护甲、攻击-伤害减免、帮助卡默认永久移除、通关先帮助奖励再房间选择”。

本架构文档只回答一个问题：**后续 AI 或程序应该如何在现有代码上改造，而不是重新设计一个理想工程。**

落地原则：

1. **规则先于表现**：任何表现动画、DOTween、Audio、UI 都只订阅事件或发 Command，不直接写 Model。
2. **状态变更只走 Command**：System 负责能力和编排，Command 是状态提交入口。
3. **效果能力下沉到 EffectSystem**：帮助卡、技能、遗物、房间卡、导师卡最终都走 EffectGraph/EffectAtom，不继续堆在 `UseHelpCardCommand` 的 `switch` 里。
4. **战斗管线必须重建**：旧 `CombatSystem.CalculateDamage(attacker, defender) = attacker.Attack - defender.Defense` 不再符合设计；防御只负责生成护甲，伤害减免是独立字段。
5. **通关流程必须重排**：无怪物后先帮助卡三选一，再房间按钮；点击房间后才结算未用帮助卡金币、恢复/移除帮助卡、处理房间事件并进入下一节点。

---

## 1. 新版设计不变量

### 1.1 九宫格状态机

九宫格是整个游戏的主状态机，不是普通卡牌游戏的手牌/费用/回合模型。

- 玩家卡默认出生在格 5。
- 玩家只能与正交相邻格交互。
- 点击空格、拾取帮助卡、战斗等玩家动作提交后，除格 5 外的外圈顺时针旋转一格。
- 产生空格后，从战斗牌堆补牌，牌堆为空时停止补牌。
- 道具牌格中的帮助卡使用不受正交距离限制。

架构落点：

- `BoardModel`：只保存格位占用、玩家格、格 1~9 API。
- `BoardSystem`：旋转、移除、放置、交换、移动事实事件。
- `ClickBoardSlotCommand` / `CommitPlayerActionCommand`：把玩家动作收束为一次可追踪提交。
- `BoardSlotChangedEvent` 继续保留，但表现层应优先监听更语义化的 `CardMovedEvent`、`CardPlacedEvent`、`CardRemovedEvent`、`BoardRotatedEvent`。

### 1.2 属性、护甲、伤害

新版四维属性：攻击、防御、血量、护甲。

| 名称 | 新语义 | 运行态落点 |
|---|---|---|
| 攻击 | 造成伤害的基础值 | `EffectiveStats.Attack` |
| 防御 | 节点开始与获得防御时，转化为等量护甲 | `EffectiveStats.Defense`，不直接参与战斗减伤 |
| 血量 | 角色生存值 | `CardRuntime.CurrentHp / MaxHp` |
| 护甲 | 受伤时优先扣除；为 0 后才扣血 | `CardRuntime.CurrentArmor` |
| 伤害减免 | 技能/遗物/状态提供的独立减伤 | `EffectiveStats.DamageReduction` 或 `DamageContext.damageReduction` |

必须满足：

- 所有属性最低为 0。
- 获得血量上限时，当前血量同步增加等量血量。
- 节点开始：玩家卡与需要护甲的怪物卡执行 `currentArmor = effectiveDefense`。
- 获得防御时：增加防御来源的同时，立即增加等量 `currentArmor`。
- 战斗实际伤害：`damageBeforeArmor = max(0, rawAttack - damageReduction)`。
- 防御不再写入 `CalculateDamage(attacker, defender)` 的扣减项。
- 标记 `ignoreArmor` 的伤害直接扣血，不扣护甲。

### 1.3 帮助卡生命周期

新版帮助卡生命周期与老实现语义相反。

| 场景 | 新规则 |
|---|---|
| 默认使用帮助卡 | 使用后永久移除，不在节点结束恢复 |
| 卡面标注“使用后复原” | 使用后仅节点内临时移除，节点结束从快照恢复 |
| 节点开始 | 对帮助卡组做 uid 快照 |
| 节点结束 | 结算未使用且仍在场上/道具格的帮助卡，每张 +10 金币 |
| 新获得帮助卡 | 节点内新增，节点结束后追加保留 |
| 配置字段 | 使用 `restoreAfterNode`，默认 `false` |

旧字段迁移：

```text
旧：isPermanentRemoveOnUse = true 表示永久移除
新：restoreAfterNode = false 表示永久移除
迁移：restoreAfterNode = !isPermanentRemoveOnUse
```

实现上不要再把 `ConsumeHelpCardCommand(helpUid, bool permanentlyRemove)` 作为长期 API。目标 API 应是：

```csharp
ConsumeHelpCardCommand(CardUid helpUid, HelpCardConsumeReason reason)
```

Command 内部读取 `CardDefinition.RestoreAfterNode` 决定 `TemporaryRemoved` 或 `PermanentlyRemoved`。

### 1.4 通关流程顺序

新版通关流程如下：

```text
战斗牌堆与棋盘均无怪物
  -> ClearReady
  -> HelpRewardChoosing（三选一，可跳过 +10）
  -> RoomChoosing（生成 2 个房间按钮；此阶段仍可拾取/使用场上帮助卡）
  -> ChooseRoom
  -> SettleUnusedHelpCards（未用帮助卡 +10）
  -> RestoreOrRemoveHelpCardsBySnapshot
  -> ResolveRoomEvent
  -> ProceedToNextNode / Victory
```

关键点：

- `CheckClearConditionCommand` 不再直接弹房间按钮。
- `PickHelpCardRewardCommand` / `SkipHelpRewardCommand` 完成后进入 `RoomChoosing`。
- `RoomChoosing` 期间不应该加会阻止棋盘和道具交互的 `OverlayVisible` 锁；房间按钮应是非阻塞 HUD 状态。
- 点击房间按钮才算节点结束，因此未用帮助卡金币结算也必须延迟到 `ChooseRoomCommand` 内或其后的 `SettleNodeEndCommand`。
- 房间候选数量按设计为 2 个，不是当前实现的 4 个全量候选。

### 1.5 战斗管线不变量

战斗不是“双方各扣一次血”的简化函数，而是可插入技能/遗物/帮助卡效果的管线。

标准管线：

```text
ValidateCombat
  -> BuildCombatContext
  -> OnBeforeCombat triggers
  -> CalculateEffectiveStats
  -> DetermineInitiative
  -> FirstHit DamageContext
  -> ParallelExtraDamage: 荆棘甲等与先手伤害并行
  -> ApplyDamageGroup
  -> DeathCheck
  -> CounterHit DamageContext（目标存活才有）
  -> ParallelReflectDamage: 刺皮与反击同步结算
  -> ApplyDamageGroup
  -> Damage prevention / Death prevention
  -> OnAfterCombat triggers
  -> KillSettlement
  -> CommitPlayerAction
  -> BoardMove / Refill / ClearCheck
```

必须支持：

- 双方无先攻：玩家先。
- 双方有先攻：玩家先。
- 只有一方有先攻：先攻方先。
- 先手击杀目标，目标不反击。
- `刺皮` 与怪物反击同步，不是反击后再追加串行伤害。
- `荆棘甲` 与玩家先手伤害并行，对怪物造成额外伤害。
- `庇佑` 免疫一次可预防伤害。
- `凤凰羽毛` 在致命伤时触发死亡预防，恢复 50% 血量并消费遗物。

---

## 2. 当前代码基线

### 2.1 可复用部分

当前 `dev` 分支已经具备以下可复用基础：

- `TableNine` 作为唯一 QFramework Architecture 入口。
- `RunModel`、`PlayerModel`、`BoardModel`、`DeckModel`、`CollectionModel`、`ConfigModel`、`FlowModel`、`RewardModel` 已有运行态骨架。
- `StartNewRunCommand`、`StartNodeCommand`、`DealOpeningCardsCommand`、`ClickBoardSlotCommand`、`RequestRefillBoardCommand`、`RefillBoardCommand` 已经跑通基础循环。
- `RewardSystem`、`RelicSystem`、`ShopSystem` 已有 CRUD 与奖励候选原型。
- 配置层已有 27 节点、帮助卡、怪物、遗物、技能的初版数据。
- EditMode 测试已有较厚基础，可继续作为回归保护。

这些代码不推翻，只按本设计逐步收束。

### 2.2 必须修正的旧语义

| 领域 | 当前倾向 | 新目标 |
|---|---|---|
| 战斗伤害 | `攻击 - 防御` | `攻击 - 伤害减免`，再由护甲吸收 |
| ApplyDamage | 只扣 `CurrentHp` | 先扣 `CurrentArmor`，再扣 `CurrentHp`；支持 `ignoreArmor` |
| 帮助卡消耗 | 调用处传 `permanentlyRemove` | Command 自己读取 `restoreAfterNode` |
| 通关流程 | ClearReady 后先房间选择，房间后帮助奖励 | ClearReady 后先帮助奖励，再房间选择 |
| 房间候选 | 4 个固定候选 | 2 个房间按钮候选 |
| EffectSystem | 空壳 | 帮助卡/技能/遗物/房间统一效果入口 |
| SkillSystem | 未注册 | 触发队列 + 防递归 + 调用 EffectSystem |
| 保存 | MemorySave | EasySaveUtility，跨会话持久化 |
| 重放 | 只覆盖少量 Command | 覆盖主路径并断言棋盘 hash 一致 |
| 表现事件 | 事件粒度不足 | UI/Anim/Audio 可订阅的事实事件 |

### 2.3 迁移原则

1. 不在一个提交里同时改战斗、奖励、UI、动画。
2. 每个阶段必须保持 Unity 编译通过。
3. 每个规则阶段先补 EditMode 测试，再接表现层。
4. 原型 UI 能继续工作，但不能成为规则正确性的依据。
5. 新接口落地后，旧接口可以保留一小段兼容层，但必须标记 `Obsolete` 或在 Plan 中列删除节点。

---

## 3. 目录和 QFramework 分层

保持当前平铺式目录，但用职责约束边界：

```text
Assets/Scripts/
├─ TableNine.cs
├─ Data/          # 枚举、DTO、运行态值对象、SaveData、Effect definitions
├─ Model/         # 运行态状态，不驱动动画
├─ System/        # 规则能力和跨模块编排
├─ Command/       # 所有状态变更入口
├─ Event/         # 表现/Debug/日志订阅的事实事件
├─ Query/         # 不改状态的复杂查询
├─ Utility/       # EasySave、ResKit、AudioKit、Odin effect asset、随机数、序列
├─ UI/            # UIKit Panel / ViewController
├─ Game/          # 场景 Controller、CardView、BoardSlotView、输入 Raycaster
├─ Anim/          # DOTween/ActionKit 表现编排，只监听事件
└─ Tests/         # EditMode、PlayMode、Replay
```

### 3.1 QFramework 规则

| 层 | 允许做 | 禁止做 |
|---|---|---|
| Model | 保存状态、提供受控写入方法 | 调用 System、发动画、做复杂规则编排 |
| System | 规则计算、流程服务、候选生成、事件监听 | 直接响应 UI 点击绕过 Command |
| Command | 提交状态变化、串联 System、发事实事件 | 持有 MonoBehaviour、直接播放音效/动画 |
| Query | 复杂只读计算 | 修改 Model |
| Utility | 外部库适配、存取资源、随机数、保存 | 写业务规则 |
| UI/Game/Anim | 发 Command、读 Model、订阅 Event | 直接改 Model/System 内部状态 |

### 3.2 `TableNine` 注册目标

当前注册表需要逐步补齐到：

```csharp
// Utilities
IRandomUtility
IConfigUtility
ISaveUtility              // EasySaveUtility 替换 MemorySaveUtility
ISequenceUtility          // 测试 Immediate，运行 ActionKit
ICommandTraceUtility
ICommandReplayUtility
IDebugEventLogUtility
IEffectAssetUtility       // Odin EffectGraph 读取
IResourceUtility          // ResKit 资源加载
IAudioUtility             // AudioKit 适配
ITextAnimatorUtility      // Febucci Text Animator 适配

// Models
IRunModel
IPlayerModel
IBoardModel
IDeckModel
ICollectionModel
IConfigModel
IFlowModel
IRewardModel
IOverlayModel             // 覆盖层堆栈和非阻塞房间按钮状态

// Systems
IRunSystem
ILevelFlowSystem
IBoardSystem
IDeckSystem
ICombatSystem
IStatSystem
IEffectSystem
ISkillSystem
IRelicSystem
IRewardSystem
IRoomSystem
IShopSystem
IInputLockSystem
IOverlaySystem
ISaveSystem
IDebugEventSystem
INarrativeSystem
```

`IOverlayModel/IOverlaySystem/INarrativeSystem/ITextAnimatorUtility/IResourceUtility/IAudioUtility` 可以在表现层阶段补齐；`ISkillSystem` 与 `IEffectSystem` 属于规则 P0/P1，不应推迟到美术接入之后。

---

## 4. 数据模型

### 4.1 枚举

```csharp
public enum CardType { Player, Monster, Help, Room, Tutor }
public enum CardQuality { White, Blue, Gold, Red, Initial }
public enum MonsterLevel { Level1, Level2, Level3, Level4, Elite, Boss }
public enum Suit { None, Spade, Heart, Diamond, Club }
public enum FlowPhase
{
    None,
    RunStarting,
    NodeStarting,
    OpeningDeal,
    PlayerControl,
    CombatResolving,
    BoardMoving,
    BoardRefilling,
    ClearReady,
    HelpRewardChoosing,
    RoomChoosing,
    RoomResolving,
    Shop,
    ChestRewardChoosing,
    TutorSkillChoosing,
    NodeEnding,
    LayerComplete,
    GameOver,
    Victory
}
```

`RoomChoosing` 是非阻塞阶段：此时允许棋盘帮助卡和道具帮助卡交互，但不允许战斗，因为已经无怪物。

### 4.2 `CardDefinition`

必须新增或重命名字段：

| 字段 | 说明 |
|---|---|
| `CardId` | 稳定 ID |
| `DisplayName` | 显示名 |
| `CardType` | Player / Monster / Help / Room / Tutor |
| `Quality` | 白/蓝/金/红/初始 |
| `Price` | 商店价格 |
| `Suit` / `MonsterLevel` | 怪物分类 |
| `BaseHp` / `BaseAttack` / `BaseDefense` | 静态基础属性 |
| `SkillIds` | 怪物词条/玩家初始技能 |
| `EffectGraphId` | 帮助卡/房间/导师等效果图 |
| `RestoreAfterNode` | 帮助卡使用后是否节点结束复原；默认 false |
| `SameNameLimit` | 默认 3 |
| `Tags` | Chest、Projectile、Tower、IgnoreArmor 等 |

旧 `IsPermanentRemoveOnUse` 不再作为语义源。兼容期可以保留只读迁移方法，但所有新代码读取 `RestoreAfterNode`。

### 4.3 `CardRuntime`

目标字段：

```csharp
public sealed class CardRuntime
{
    public CardUid Uid;
    public string DefinitionId;
    public string DisplayName;
    public CardType CardType;
    public MonsterLevel MonsterLevel;
    public Suit Suit;

    public BoardSlotNo? BoardSlot;
    public int? ItemSlotIndex;

    public int CurrentHp;
    public int MaxHp;
    public int CurrentArmor;
    public int BaseAttack;
    public int BaseDefense;

    public List<string> SkillIds;
    public RuntimeModifierList Modifiers;
    public Dictionary<string, int> Counters;
}
```

最小迁移第一步只要求加 `CurrentArmor`，其余修饰器可后续接 EffectSystem/SkillSystem 时补齐。

### 4.4 `EffectiveStats`

```csharp
public struct EffectiveStats
{
    public int CurrentHp;
    public int MaxHp;
    public int CurrentArmor;
    public int Attack;
    public int Defense;
    public int DamageReduction;
    public bool HasFirstStrike;
}
```

- `Defense` 用于 UI 展示与护甲生成。
- `DamageReduction` 用于伤害公式。
- `CurrentArmor` 用于 UI 展示，不参与 `rawAttack - damageReduction`。

### 4.5 `DamageContext`

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
    public bool Preventable;
    public bool WasPrevented;
    public bool WasFatalBeforePrevention;

    public List<string> Tags = new();
}
```

`ApplyDamageCommand` 应接收 `DamageContext`，而不是裸 `int damage`。兼容期可以保留：

```csharp
ApplyDamageCommand(CardUid targetUid, int damage)
```

但它只能作为测试/旧代码包装，内部转换为 `DamageContext`。

### 4.6 `HelpCardState`

建议显式状态：

```csharp
public enum HelpCardRemovalState
{
    Active,
    TemporarilyRemoved,
    PermanentlyRemoved
}

public sealed class HelpCardState
{
    public CardUid Uid;
    public string DefinitionId;
    public bool RestoreAfterNode;
    public bool IsOnBoard;
    public bool IsInItemSlot;
    public HelpCardRemovalState RemovalState;
}
```

如果短期不改 enum，也至少保证：

- `IsPermanentlyRemoved = !definition.RestoreAfterNode`。
- `IsTemporarilyRemoved = definition.RestoreAfterNode`。
- 这个判断集中在 `ConsumeHelpCardCommand` 或 `DeckSystem.ConsumeHelpCard`，不能散落在每张帮助卡逻辑里。

---

## 5. Model 职责

### 5.1 `RunModel`

保存整局元信息：

- `Layer`
- `NodeInLayer`
- `GlobalNodeIndex`（建议新增）
- `Seed`
- `IsRunActive`
- `CharacterId`
- 节点结果、是否胜利/失败。

不生成卡、不结算奖励。

### 5.2 `PlayerModel`

保存玩家长期状态：

- 玩家 card uid。
- 金币。
- 技能列表。
- 遗物实例列表。
- 永久计数器，如历战、复仇、剧情标记。

玩家血量、护甲、攻击、防御优先以玩家 `CardRuntime` 为准，`PlayerModel` 只做索引和长期集合。

### 5.3 `BoardModel`

保存 3x3 格位。格 5 不特殊写死到所有逻辑中，只由 `PlayerSlot` 体现。

### 5.4 `DeckModel`

保存：

- `OwnedHelpCards`
- `HelpCardStates`
- `NodeStartSnapshot`
- `DemonDeckQueue`
- `BattleDrawPile`
- `ItemSlots[5]`
- `NextBattleCardPreview`
- 补牌防抖标志
- 待选择帮助卡动作，如飞刀目标、属性选择、庇佑状态等

长期目标是把 `PendingHelpCardAction` 拆成通用 `TargetingModel/OverlayModel`，但 P0 可以保留。

### 5.5 `CollectionModel`

所有运行态卡实例仓库，只做 create/get/remove/restore，不做业务判断。

### 5.6 `FlowModel`

保存阶段和输入锁。阶段变化需要统一发送 `FlowPhaseChangedEvent`，锁变化需要发送 `InputLockChangedEvent`。

### 5.7 `RewardModel`

保存当前候选：帮助奖励、房间按钮、宝箱遗物、导师技能、商店卡牌。

### 5.8 `OverlayModel`

建议新增。职责：

- 当前覆盖层栈。
- 哪些覆盖层阻止底层点击。
- 非阻塞 HUD 提示，比如 `RoomChoosing` 按钮。
- Popup 文案和队列。

---

## 6. System 职责

### 6.1 `LevelFlowSystem`

目标是把散落在 Command 中的 phase 切换收束成流程服务。

核心 API：

```csharp
void EnterNodeStarting(int layer, int node);
void EnterPlayerControl();
void EnterClearReady();
void EnterHelpRewardChoosing(RewardSource source);
void EnterRoomChoosing();
void EnterRoomResolving(string roomId);
void EnterNodeEnding();
void EnterGameOver(string reason);
void EnterVictory();
```

短期可以不用 FSMKit，但必须做到：

- 每次 phase 变化只有一个入口。
- 每次 phase 变化都发 `FlowPhaseChangedEvent`。
- 所有锁的增删与覆盖层开关对应。

### 6.2 `BoardSystem`

目标 API：

```csharp
void ResetBoardWithPlayer(CardUid playerUid);
bool IsOrthogonalAdjacentToPlayer(BoardSlotNo slot);
IReadOnlyList<BoardSlotNo> GetEmptySlots();
void PlaceCard(CardUid uid, BoardSlotNo slot, CardPlacementSource source);
CardUid? RemoveCardAt(BoardSlotNo slot, RemoveReason reason);
void RotateClockwise(BoardMoveReason reason);
void RotateCounterClockwise(BoardMoveReason reason);
void Swap(BoardSlotNo a, BoardSlotNo b, BoardMoveReason reason);
```

事件要求：

- 放置：`CardPlacedEvent` + `BoardSlotChangedEvent`
- 移除：`CardRemovedEvent` + `BoardSlotChangedEvent`
- 移动：`CardMovedEvent`
- 旋转：`BoardRotatedEvent`，并为每张移动的卡发 `CardMovedEvent`

### 6.3 `DeckSystem`

职责：

- 生成每节点恶魔卡组。
- 节点开始帮助卡快照。
- 开局 3+3+2 发牌辅助。
- 战斗牌堆注入。
- 下一张预览。
- 怪物剩余判断。
- 帮助卡消耗、节点结束恢复/移除。

帮助卡生命周期相关 API：

```csharp
void SnapshotHelpDeck();
void ConsumeHelpCard(CardUid uid, string causeId);
NodeEndHelpSettlement SettleUnusedHelpCards();
void RestoreHelpDeckSnapshotByRestoreAfterNode();
```

`RewardSystem` 可以调用这些 API，但不要自己直接改 `HelpCardStates` 内部细节。

### 6.4 `StatSystem`

职责：有效属性计算与护甲填充。

目标 API：

```csharp
EffectiveStats GetEffectivePlayerStats();
EffectiveStats GetEffectiveMonsterStats(CardUid monsterUid);
void FillArmorFromDefenseAtNodeStart();
void AddDefenseAndArmor(CardUid targetUid, int defenseDelta, string causeId);
void AddMaxHpAndCurrentHp(CardUid targetUid, int maxHpDelta, string causeId);
```

规则：

- 节点开始填护甲在 `StartNodeCommand` 中，生成棋盘前后均可，但必须在玩家可交互前完成。
- 如果怪物在创建时也有护甲需求，则怪物创建后填充；如果只有玩家需要护甲，先只实现玩家。
- 属性 UI 展示通过 Query 或 `StatsDirtyEvent` 刷新，不让 UI 自己拼来源。

### 6.5 `CombatSystem`

职责：战斗上下文、先攻、伤害构建、并行伤害组。

目标 API：

```csharp
bool CanStartCombat(CardUid monsterUid, out string reason);
CombatContext BuildCombatContext(CardUid monsterUid);
bool PlayerActsFirst(EffectiveStats player, EffectiveStats monster);
DamageContext BuildCombatDamage(CardUid source, CardUid target, DamageType type, string causeId);
IReadOnlyList<DamageContext> BuildParallelDamageGroup(CombatContext context, CombatStep step);
CombatResult ResolveCombat(CombatContext context);
```

`StartCombatCommand` 不再直接写一整套扣血逻辑，而是：

```text
Validate
Lock
Build context
ResolveCombat / or Send ResolveCombatCommand
Unlock
CommitPlayerActionCommand
```

`ApplyDamageCommand` 是伤害提交点；`CombatSystem` 只构建上下文和结果，不直接扣 Model。

### 6.6 `EffectSystem`

职责：帮助卡、技能、遗物、房间、导师的通用效果入口。

核心接口：

```csharp
public interface IEffectSystem : ISystem
{
    void ResolveEffectGraph(string effectGraphId, EffectContext context);
    void ResolveAtom(EffectAtomDefinition atom, EffectContext context);
}

public interface IEffectAtom
{
    void Resolve(EffectContext context);
}
```

第一批 Atom：

| Atom | 用途 |
|---|---|
| `DamageAtom` | 飞刀、火球术、爆弹、瞭望塔、荆棘甲、刺皮 |
| `HealAtom` | 药水、食物、温泉、治疗泉 |
| `AddGoldAtom` | 金币卡、跳过、未用帮助卡结算 |
| `ModifyStatAtom` | 属性提升、加防转护甲、暴力卡 |
| `ConsumeHelpCardAtom` | 默认永久移除，复原卡临时移除 |
| `AddCardToHelpDeckAtom` | 帮助奖励、商店购买 |
| `InjectCardToBattleDeckAtom` | 精英/层主奖励、红桃之母 |
| `OpenChoiceOverlayAtom` | 宝箱、属性三选一、导师 |
| `MoveBoardAtom` | 顺/逆时针、转盘 |
| `SwapCardsAtom` | 交换卡 |
| `RemoveCardAtom` | 滚石、死亡、删卡 |
| `ApplyStatusAtom` | 庇佑、决斗锁、临时效果 |

P0 目标不是一次做完整 Odin 编辑器，而是先让 `UseHelpCardCommand` 从硬编码 switch 迁移到 EffectSystem 注册表。

### 6.7 `SkillSystem`

职责：技能触发收集、排序、防递归、调用 EffectSystem。

最小触发阶段：

```text
OnNodeStart
OnBeforePlayerAction
OnAfterPlayerAction
OnBeforeCombat
OnModifyDamage
OnAfterDamage
OnCardMoved
OnMonsterKilled
OnNodeClear
OnNodeEnd
```

要求：

- 每个 Command 触发链有最大深度，防止无限递归。
- 每次触发写入 Debug 链日志。
- 怪物位置型技能优先仍可在 `StatSystem` 计算，但触发型技能必须进 `SkillSystem`。
- 玩家技能不可重复堆叠，同名获取按配置处理。

### 6.8 `RelicSystem`

当前 CRUD 可保留，但触发能力要接入战斗和 EffectSystem。

优先级：

1. 常驻属性遗物：木剑、木盾、木甲、套装。
2. 战斗遗物：荆棘甲。
3. 死亡预防：凤凰羽毛。
4. 治疗/节点类：活着的肉等。
5. 一次性遗物消费状态和存档。

### 6.9 `RewardSystem` / `RoomSystem`

建议新增 `IRoomSystem`，从 `ChooseRoomCommand` 中拆出房间事件。

职责划分：

- `RewardSystem`：帮助奖励、宝箱遗物候选、导师候选、跳过奖励。
- `RoomSystem`：房间候选、房间事件解析、房间结束回调。
- `DeckSystem`：未用帮助卡结算和帮助卡恢复/移除。

通关后房间候选必须是 2 个按钮。房间事件可先保持原型：金币房直给金币、宝箱房打开宝箱、属性房加血上限/生成属性效果、商店打开商店；但流程顺序必须符合“先帮助奖励，再房间”。

### 6.10 `SaveSystem`

必须从 `MemorySaveUtility` 迁移到 `EasySaveUtility`。

保存安全点：

- 新局开始。
- 节点开始后。
- 点击房间并完成房间事件后。
- 商店关闭后。
- 宝箱/导师/奖励选择完成后。
- GameOver / Victory。

保存内容只存 ID 和运行态数值，不保存 ScriptableObject、Prefab、Sprite、AudioClip。

### 6.11 `InputLockSystem`

锁原因：

```csharp
public enum InputLockReason
{
    CombatResolving,
    BoardMoving,
    BoardRefillRunning,
    OverlayVisible,
    PopupVisible,
    SequenceRunning,
    DialogueRunning,
    GameEnded
}
```

规则：

- 覆盖层三选一会锁底层。
- `RoomChoosing` 房间按钮默认不锁底层帮助卡交互。
- 战斗和补牌必须锁。
- 锁变化发送 `InputLockChangedEvent`。

---

## 7. Command 设计

### 7.1 流程 Command

| Command | 说明 |
|---|---|
| `StartNewRunCommand` | 初始化配置、玩家、帮助卡组、seed |
| `StartNodeCommand` | 设置节点、清理节点状态、生成恶魔卡组、帮助卡快照、填护甲、重置棋盘、发牌 |
| `DealOpeningCardsCommand` | 3 帮助 + 3 恶魔 + 混洗 + 2 战斗 |
| `CheckClearConditionCommand` | 无怪物时进入 ClearReady 并打开帮助奖励 |
| `EnterRoomChoosingCommand` | 帮助奖励结束后生成 2 个房间按钮 |
| `ChooseRoomCommand` | 点击房间按钮，触发节点结束结算和房间事件 |
| `ResolveRoomCommand` | 执行房间效果，可打开商店/宝箱等 |
| `CompleteRoomCommand` | 房间事件完成后进入下一节点 |
| `ProceedToNextNodeCommand` | 只负责节点编号推进/胜利判断 |
| `GameOverCommand` | 失败收束 |
| `VictoryCommand` | 通关收束 |

### 7.2 棋盘 Command

| Command | 说明 |
|---|---|
| `ClickBoardSlotCommand` | 输入路由，只做合法性和分发 |
| `PickHelpCardToItemSlotCommand` | 拾取帮助卡 |
| `RotateBoardClockwiseCommand` | 顺时针旋转并发移动事件 |
| `RotateBoardCounterClockwiseCommand` | 逆时针旋转 |
| `SwapCardsCommand` | 交换卡效果 |
| `RemoveCardFromBoardCommand` | 带 reason 的移除 |
| `RequestRefillBoardCommand` | 补牌防抖入口 |
| `RefillBoardCommand` | 实际补牌 |
| `CommitPlayerActionCommand` | 一次玩家动作的统一收尾：旋转、补牌、清场检测 |

### 7.3 战斗 Command

| Command | 说明 |
|---|---|
| `StartCombatCommand` | 战斗入口，锁输入，构建上下文 |
| `ResolveCombatCommand` | 管线结算，产出伤害组 |
| `ApplyDamageCommand` | 按 DamageContext 扣护甲/血量 |
| `ApplyHealCommand` | 治疗，受上限限制 |
| `ApplyStatChangeCommand` | 攻/防/血变化，防御变化同步护甲 |
| `ApplyDeathPreventCommand` | 凤凰羽毛等死亡预防 |
| `KillMonsterCommand` | 怪物死亡、金币、奖励注入、触发技能/遗物 |

### 7.4 帮助卡 / 效果 Command

| Command | 说明 |
|---|---|
| `UseHelpCardCommand` | 使用入口，转 EffectSystem |
| `ResolveEffectGraphCommand` | 执行效果图 |
| `ConsumeHelpCardCommand` | 根据 `restoreAfterNode` 决定移除模式 |
| `ResolveTargetingCommand` | 飞刀等目标选择统一入口 |
| `ResolveAttributeChoiceCommand` | 属性选择结果 |
| `OpenChestCommand` | 宝箱卡打开入口 |

### 7.5 奖励 / 房间 Command

| Command | 说明 |
|---|---|
| `GenerateHelpRewardCommand` | 生成帮助卡三选一 |
| `PickHelpCardRewardCommand` | 加入帮助卡组后进入 RoomChoosing |
| `SkipHelpRewardCommand` | +10 金币后进入 RoomChoosing |
| `GenerateRoomCandidatesCommand` | 生成 2 个房间按钮 |
| `SettleNodeEndHelpCardsCommand` | 未用帮助卡金币结算 + 恢复/移除 |
| `PickRelicRewardCommand` | 宝箱遗物选择 |
| `SkipChestRewardCommand` | 宝箱跳过 +20 |
| `ChooseTutorSkillCommand` | 导师技能选择 |
| `CloseShopCommand` | 商店结束，回到房间完成流程 |

---

## 8. Event 设计

事件是 UI、Anim、Audio、Debug 的唯一稳定订阅点。

### 8.1 流程事件

- `RunStartedEvent`
- `NodeStartedEvent`
- `FlowPhaseChangedEvent`
- `InputLockChangedEvent`
- `LevelClearReadyEvent`
- `HelpRewardGeneratedEvent`
- `RoomChoiceRequestedEvent`
- `RoomChosenEvent`
- `RoomResolvedEvent`
- `NodeCompletedEvent`
- `LayerCompletedEvent`
- `GameOverEvent`
- `VictoryEvent`

### 8.2 棋盘事件

- `CardPlacedEvent`
- `CardRemovedEvent`
- `CardMovedEvent`
- `BoardSlotChangedEvent`
- `BoardRotatedEvent`
- `RefillStartedEvent`
- `RefillCompletedEvent`
- `BattleDeckChangedEvent`
- `HelpDeckChangedEvent`
- `ItemSlotChangedEvent`

### 8.3 战斗/效果事件

- `CombatStartedEvent`
- `DamageCalculatedEvent`
- `DamageAppliedEvent`
- `ArmorChangedEvent`
- `HealAppliedEvent`
- `DamagePreventedEvent`
- `DeathPreventedEvent`
- `MonsterKilledEvent`
- `EffectResolvedEvent`
- `SkillTriggeredEvent`
- `RelicTriggeredEvent`
- `StatsDirtyEvent`

### 8.4 覆盖层/UI 事件

- `OverlayOpenedEvent`
- `OverlayClosedEvent`
- `PopupRequestedEvent`
- `GameplayMessageEvent`
- `AttributeChoiceRequestedEvent`
- `ChestRewardGeneratedEvent`
- `TutorSkillChoiceRequestedEvent`
- `ShopOpenedEvent`

命名规则：事件名用过去式或事实描述，不用命令式。事件只通知事实，不承载下一步规则决策。

---

## 9. Query 设计

保留少量真正复杂的只读查询：

| Query | 说明 |
|---|---|
| `CanInteractBoardSlotQuery` | 棋盘点击是否合法 |
| `GetEffectivePlayerStatsQuery` | 玩家有效属性 |
| `GetEffectiveMonsterStatsQuery` | 怪物有效属性 |
| `HasMonsterRemainingQuery` | 棋盘 + 战斗牌堆是否仍有怪物 |
| `GetValidTargetsForHelpCardQuery` | 飞刀/交换/火球等合法目标 |
| `GetHelpDeckCapacityQuery` | 当前层帮助卡上限 |
| `GetBoardSnapshotHashQuery` | 重放与 Debug 校验 |
| `GetRewardCandidatesQuery` | Debug 展示候选来源 |

不要把所有 getter 都 Query 化。简单 Model 值可直接读。

---

## 10. 表现层接入边界

### 10.1 UI

UI 只做三件事：

1. 读 Model/Query 展示状态。
2. 监听 Event 刷新局部视图。
3. 将点击转成 Command。

禁止：

- UI 直接改 `CurrentHp`、金币、牌堆、格位。
- UI 自己判断战斗结算。
- UI 自己维护一份和 Model 不一致的卡牌状态。

### 10.2 Game / CardView

建议拆成：

- `GameplaySceneController`：场景启动和全局视图绑定。
- `BoardSlotView`：格位表现。
- `CardView`：卡牌正面、属性、护甲、状态图标。
- `CardViewPresenter`：监听事件，把 CardRuntime 映射到 CardView。
- `CardInputRaycaster`：点击格位/卡牌后发 Command。

### 10.3 Anim / DOTween

`Anim/` 不改 Model。它监听事件：

- `CardMovedEvent` 播放移动。
- `BoardRotatedEvent` 播放旋转队列。
- `DamageAppliedEvent` 播放受击、护甲吸收、跳字。
- `HealAppliedEvent` 播放治疗。
- `EffectResolvedEvent` 播放帮助卡/技能/遗物特效。
- `OverlayOpenedEvent` 播放 UI 入场。

如果规则需要等待动画结束，由 `ISequenceUtility` 统一抽象：

- EditMode：`ImmediateSequenceUtility` 立即完成。
- Runtime：`ActionKitSequenceUtility` 或 DOTween adapter。

### 10.4 Audio

音频只监听事件或 UI 点击事件：

- 普通点击。
- 战斗命中。
- 护甲吸收。
- 怪物死亡。
- 帮助卡使用。
- 奖励选择。
- 商店购买。
- 胜利/失败。

音频不参与规则决策。

### 10.5 Odin 效果资产

Odin 只负责编辑体验。运行时通过 `IEffectAssetUtility` 把 `EffectGraphDefinition` 转为纯 C# `EffectAtomDefinition`。

规则层依赖：

```text
EffectSystem -> IEffectAssetUtility -> EffectGraphDefinition DTO
```

规则层不依赖 Odin drawer、MonoBehaviour、Prefab。

---

## 11. 存档、重放、Debug

### 11.1 存档

`RunSaveData` 应包含：

- schemaVersion
- seed + random state
- layer / nodeInLayer / phase
- player card runtime：hp、armor、base stats、counters
- help deck states：uid、definitionId、restoreAfterNode、removalState
- battle draw pile uid/id
- board slots uid
- item slots uid
- skills / relics / relic consumed state
- reward context（如果允许覆盖层中保存）

### 11.2 重放

重放目标不是只恢复层数，而是复现状态。

最小验收：

```text
同一 seed + 同一 Command list
  -> 当前节点一致
  -> 棋盘 hash 一致
  -> 帮助卡状态 hash 一致
  -> 玩家 hp/armor/gold 一致
  -> 牌堆 hash 一致
```

### 11.3 Debug 面板

Debug 面板至少显示：

- seed / command index / phase / locks
- 玩家 hp、armor、attack、defense、damageReduction、gold
- 9 格卡牌 uid/id/hp/armor/slot
- battle deck preview 和剩余怪物详情
- help deck 快照、临时移除、永久移除、新增卡
- 最近 50 条 Event
- 最近 50 条 Command
- 一键复制 bug report：seed + command list + board hash + save data 摘要

---

## 12. 测试策略

### 12.1 EditMode 必测

- 防御在节点开始转护甲。
- 获得防御时同步加护甲。
- 伤害先扣护甲再扣血。
- `ignoreArmor` 直接扣血。
- 战斗伤害使用 `damageReduction`，不使用对方 Defense。
- 玩家先攻/怪物先攻/双方先攻/双方无先攻。
- 怪物被先手击杀不反击。
- 怪物存活会反击。
- 刺皮与反击同步。
- 荆棘甲与玩家伤害并行。
- 庇佑免疫一次。
- 凤凰羽毛致命伤触发。
- 帮助卡默认永久移除。
- `restoreAfterNode=true` 帮助卡节点结束恢复。
- 通关先帮助奖励再房间选择。
- RoomChoosing 阶段仍可拾取/使用帮助卡。
- 点击房间后才结算未用帮助卡金币。
- 重放棋盘 hash 一致。

### 12.2 PlayMode 必测

- 首节点完整灰盒可玩。
- 8 卡 + 玩家正确显示。
- 点击、移动、补牌动画期间输入锁生效。
- 帮助奖励覆盖层与房间按钮顺序正确。
- 商店/宝箱/属性/金币房至少各一条垂直流程。
- 音频和动画缺资源时不阻断规则。

### 12.3 配置校验

`ConfigValidator` 必须覆盖：

- ID 唯一。
- 引用存在。
- 帮助卡 `restoreAfterNode` 有默认值。
- 旧字段 `isPermanentRemoveOnUse` 不再被新配置使用。
- 同名上限。
- 品质概率总和。
- 宝箱候选不足时降级策略。
- 遗物池排除初始遗物。
- 每个有 `effectGraphId` 的卡都能找到效果图。
- 每个技能触发器有合法 trigger。

---

## 13. 后续接入顺序

表现层不要抢在规则修正前大规模接入。推荐顺序：

1. P0：属性/护甲/伤害/帮助卡语义/通关流程修正。
2. P1：战斗管线 + DamageContext + 关键遗物/技能触发。
3. P2：EffectSystem MVP，迁移帮助卡。
4. P3：事件补全，UI/Anim 可稳定订阅。
5. P4：EasySave、Replay、Debug 面板。
6. P5：CardView、DOTween、AudioKit、ResKit、Text Animator。
7. P6：全 playtest 内容验收。

本架构以 `Plan.md` 的阶段任务为执行入口。若本文件和旧过程性报告冲突，以本文件和最新版 `Assets/Docs` 为准；若 `Assets/Docs/02-卡牌/卡组管理.md` 仍保留旧帮助卡复原语义，以 `帮助卡系统.md` 和 `层级系统.md` 的新版语义为准，并在策划侧修文档。