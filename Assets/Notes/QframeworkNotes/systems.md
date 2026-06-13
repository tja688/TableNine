# systems.md — System 总表

> 最后维护：2026-06-13  
> 导航：[architecture.md](architecture.md) | [commands.md](commands.md) | [models.md](models.md) | [events.md](events.md) | [controllers.md](controllers.md)

---

本文档记录 TableNine 项目中所有 QFramework System 的接口、实现、API、事件收发。共 15 个 System，分布在 6 个文件中。

---

## 1. IRunSystem / RunSystem

**文件**：`System/RuntimeSystems.cs`  
**职责**：解析随机种子，为新一局提供确定性或随机的起始值。

### 核心 API

| 方法 | 说明 |
|------|------|
| `int ResolveSeed(int? seedOverride)` | 返回指定种子或 `Environment.TickCount` |

**事件发送**：无  
**事件监听**：无  
**状态**：完整实现

---

## 2. ILevelFlowSystem / LevelFlowSystem

**文件**：`System/RuntimeSystems.cs`  
**职责**：控制游戏流程阶段切换，将指定 FlowPhase 写入 FlowModel。

### 核心 API

| 方法 | 说明 |
|------|------|
| `void EnterPhase(FlowPhase phase)` | 设置当前流程阶段 |

**事件发送**：无（直接写 Model，由 FlowModel.SetPhase 发事件）  
**事件监听**：无  
**状态**：完整实现

---

## 3. IBoardSystem / BoardSystem

**文件**：`System/RuntimeSystems.cs`  
**职责**：管理 3×3 棋盘布局——卡牌放置、移除、顺/逆时针旋转外环。

### 核心 API

| 方法 | 说明 |
|------|------|
| `void ResetBoardWithPlayer(CardUid playerUid)` | 重置棋盘，玩家置于中央 5 号位 |
| `bool IsOrthogonalAdjacentToPlayer(BoardSlotNo slot)` | 判断是否与玩家正交相邻 |
| `IReadOnlyList<BoardSlotNo> GetEmptySlots()` | 获取所有空位 |
| `void PlaceCard(CardUid uid, BoardSlotNo slot, CardPlacementSource source)` | 放置卡牌 |
| `CardUid? RemoveCardAt(BoardSlotNo slot, RemoveReason reason)` | 移除指定格卡牌 |
| `void RotateClockwise(BoardMoveReason reason)` | 顺时针旋转外环 |
| `void RotateCounterclockwise(BoardMoveReason reason)` | 逆时针旋转外环 |

### 事件

| 方向 | 事件 |
|------|------|
| 发送 | `CardPlacedEvent`、`BoardSlotChangedEvent`、`CardMovedEvent`、`CardRemovedEvent`、`BoardRotatedEvent` |
| 监听 | 无 |

**状态**：完整实现

---

## 4. IDeckSystem / DeckSystem

**文件**：`System/RuntimeSystems.cs`  
**职责**：管理恶魔牌组生成、帮助卡注入、节点快照、战斗预览更新。

### 核心 API

| 方法 | 说明 |
|------|------|
| `void GenerateDemonDeck(int layer, int nodeInLayer)` | 按层级规则生成怪物牌组 |
| `void InjectHelpCardsToBattleDeck(IReadOnlyList<string> cardIds)` | 注入帮助卡到战斗抽牌堆并洗牌 |
| `void SnapshotHelpDeck()` | 快照当前帮助卡组（节点恢复用） |
| `void UpdateNextBattlePreview()` | 更新下一场战斗预览 |
| `bool HasMonsterRemaining()` | 检查棋盘和牌组是否还有怪物 |

### 事件

| 方向 | 事件 |
|------|------|
| 发送 | `BattleDeckCardsInjectedEvent`、`BattleDeckChangedEvent` |
| 监听 | 无 |

**状态**：完整实现

---

## 5. ICombatSystem / CombatSystem

**文件**：`System/RuntimeSystems.cs`  
**职责**：处理战斗核心逻辑——可行性判定、上下文构建、伤害计算、并行伤害组。

### 核心 API

| 方法 | 说明 |
|------|------|
| `bool CanStartCombat(CardUid monsterUid, out string reason)` | 检查是否可发起战斗 |
| `CombatContext BuildCombatContext(CardUid monsterUid)` | 构建战斗上下文 |
| `void PopulateCombatStats(CombatContext context)` | 填充双方有效属性 |
| `bool PlayerActsFirst(EffectiveStats player, EffectiveStats monster)` | 判断先手 |
| `int CalculateDamage(EffectiveStats attacker, EffectiveStats defender)` | 计算伤害值 |
| `DamageContext BuildCombatDamage(...)` | 构建伤害上下文 |
| `IReadOnlyList<DamageContext> BuildParallelDamageGroup(...)` | 构建并行伤害组（含技能触发） |

### 事件

| 方向 | 事件 |
|------|------|
| 发送 | 无 |
| 监听 | 无 |

**依赖**：调用 `IStatSystem`、`ISkillSystem`  
**状态**：完整实现

---

## 6. IStatSystem / StatSystem

**文件**：`System/RuntimeSystems.cs`  
**职责**：计算玩家和怪物的有效属性（含遗物加成、套装效果、位置技能加成），执行护甲初始化；并作为 Stage1 怪物被动技能的运行时入口（监听移动/放置/击杀/战斗事件，派发伤害、治疗与永久属性变更 Command）。

### 核心 API

| 方法 | 说明 |
|------|------|
| `EffectiveStats GetEffectivePlayerStats()` | 获取玩家有效属性（含遗物/套装加成） |
| `EffectiveStats GetEffectiveMonsterStats(CardUid monsterUid)` | 获取怪物有效属性（含位置加成、光环、动态先攻） |
| `void FillArmorFromDefenseAtNodeStart()` | 节点开始时用防御填充护甲 |

### 怪物有效属性加成（位置/光环）

| 技能 | 触发条件 | 效果 |
|------|----------|------|
| 黑桃幼崽 | 自身处于格6 | 攻击+2；获得先攻 |
| 梅花幼崽 | 场上有梅花幼崽处于格1/2/3 | 其他怪物攻击+2（不可叠加） |
| 防护光环 | 场上有防护光环处于格1/4/7 | 其他怪物防御+2 |

### 怪物被动事件链（`OnInit` 注册）

| 事件 | 处理技能 | 行为 |
|------|----------|------|
| `CardPlacedEvent` | 预先伏击 | 怪物被打出到格2/4/6/8 时对玩家造成 3 点伤害 |
| `CardMovedEvent`（`IsBoardMovement`） | 红桃幼崽 / 方块幼崽 / 破防专家 / 医疗兵 | 旋转移动到格8 → 永久+2 血；格4 → 永久+2 防；格1/2/3 → 玩家护甲-2；格7/8/9 → 全体场上怪物回血 4 |
| `MonsterKilledEvent` | 复仇 | 场上持有复仇技能的怪物攻击+2 |
| `CombatAfterResolvedEvent` | 尖盾 / 爱之躯 | 格1/4/7 战斗后按损失护甲反弹伤害；格7/8/9 战斗后自愈 1 血 |

派发 Command：`ApplyDamageCommand`、`ApplyStatChangeCommand`、`ChangeArmorCommand`、`ApplyEffectHealCommand`。

### 事件

| 方向 | 事件 |
|------|------|
| 发送 | `ArmorChangedEvent`、`StatsDirtyEvent`（经 Command 间接发送） |
| 监听 | `CardPlacedEvent`、`CardMovedEvent`、`MonsterKilledEvent`、`CombatAfterResolvedEvent` |

**依赖**：`ICollectionModel`、`IConfigModel`、`IBoardModel`、`IPlayerModel`  
**状态**：完整实现（Stage1 怪物技能已接线；等级4+ 移动触发技能仍待后续阶段）

---

## 7. IInputLockSystem / InputLockSystem

**文件**：`System/RuntimeSystems.cs`  
**职责**：管理游戏输入的锁定/解锁状态，支持多锁因叠加。

### 核心 API

| 方法 | 说明 |
|------|------|
| `void Lock(InputLockReason reason)` | 加锁 |
| `void Unlock(InputLockReason reason)` | 解锁 |
| `bool IsLocked()` | 查询是否被锁 |

**事件发送**：无  
**事件监听**：无  
**状态**：完整实现

---

## 8. IRewardSystem / RewardSystem

**文件**：`System/RuntimeSystems.cs`  
**职责**：管理所有奖励生成逻辑——帮助卡奖励、房间候选、导师技能、未用帮助卡结算、快照恢复。

### 核心 API

| 方法 | 说明 |
|------|------|
| `void GenerateHelpRewardCandidates()` | 生成帮助卡三选一（动态卡池+品质权重） |
| `void GenerateRoomCandidates()` | 生成房间候选（金币/宝箱/属性/商店） |
| `void GenerateTutorSkillCandidates()` | 生成导师技能候选（排除已拥有） |
| `void SettleUnusedHelpCards()` | 结算未用帮助卡（+10 金币/张） |
| `void RestoreHelpDeckSnapshotByRestoreAfterNode()` | 按快照恢复帮助卡组 |
| `bool CanAddHelpCard(string cardId)` | 检查是否可添加帮助卡（容量+同名上限） |

### 事件

| 方向 | 事件 |
|------|------|
| 发送 | `GoldChangedEvent`、`HelpCardsSettledEvent`、`HelpDeckRestoredEvent` |
| 监听 | 无 |

**状态**：完整实现

---

## 9. IRelicSystem / RelicSystem

**文件**：`System/RuntimeSystems.cs`  
**职责**：管理遗物的获取、丢弃、宝箱遗物候选生成及遗物属性应用。

### 核心 API

| 方法 | 说明 |
|------|------|
| `bool AddRelic(string relicId)` | 添加遗物（容量/重复检查） |
| `bool DiscardRelic(string relicId)` | 丢弃遗物（获金币补偿） |
| `void GenerateChestRewardCandidates(ChestTier chestTier)` | 按宝箱品质生成遗物候选 |
| `bool HasRelic(string relicId)` | 检查是否拥有指定遗物 |
| `void ApplyRelicStats()` | 触发遗物属性变更通知 |

### 事件

| 方向 | 事件 |
|------|------|
| 发送 | `PopupRequestedEvent`、`RelicAddedEvent`、`RelicDiscardedEvent`、`GoldChangedEvent`、`RelicStatsChangedEvent` |
| 监听 | 无 |

**状态**：完整实现

---

## 10. IShopSystem / ShopSystem

**文件**：`System/RuntimeSystems.cs`  
**职责**：管理商店功能——商品生成、购买帮助卡、花费金币删除帮助卡。

### 核心 API

| 方法 | 说明 |
|------|------|
| `void GenerateShopCards()` | 生成商店待售卡牌 |
| `bool BuyHelpCard(string cardId)` | 购买帮助卡（扣金币、容量检查） |
| `bool DeleteHelpCardForGold(string cardId)` | 花费金币删除帮助卡 |

### 事件

| 方向 | 事件 |
|------|------|
| 发送 | `PopupRequestedEvent`、`HelpCardPurchasedEvent`、`GoldChangedEvent`、`HelpCardDeletedForGoldEvent`、`ItemSlotChangedEvent` |
| 监听 | 无 |

**状态**：完整实现

---

## 11. IEffectSystem / EffectSystem

**文件**：`System/EffectSystem.cs`  
**职责**：效果图解析引擎——根据效果图 ID 和原子定义分发各种效果。

### 核心 API

| 方法 | 说明 |
|------|------|
| `void ResolveEffectGraph(string effectGraphId, EffectContext context, ICanSendCommand commandSender)` | 解析完整效果图 |
| `void ResolveAtom(EffectAtomDefinition atom, EffectContext context, ICanSendCommand commandSender)` | 解析单个效果原子 |

### 支持的 Atom 类型

| AtomType | 对应命令/行为 |
|----------|-------------|
| Heal | `ApplyEffectHealCommand` |
| Damage | `ApplyEffectDamageAtomCommand` |
| AddGold | `ApplyEffectGoldCommand` |
| ModifyStat | `ApplyStatChangeCommand` |
| ConsumeHelpCard | `ConsumeHelpCardCommand` |
| OpenChoiceOverlay | `OpenAttributeChoiceOverlayCommand` / `OpenChestRewardOverlayCommand` |
| OpenTargeting | `OpenHelpTargetingCommand` |
| AddCardToHelpDeck | 直接添加到 DeckModel |
| InjectCardToBattleDeck | `InjectHelpCardsToBattleDeckCommand` |
| MoveBoard | `RotateBoardRingCommand` |
| SwapCards | `SwapBoardCardsCommand` |
| RemoveCard | `RemoveBoardCardsCommand` |
| ApplyStatus | `ApplyBlessingShieldCommand` |

### 事件

| 方向 | 事件 |
|------|------|
| 发送 | `GameplayMessageEvent`（暴力卡/瞭望塔等状态消息） |
| 监听 | 无 |

**状态**：完整实现（13 种 Atom 全覆盖）

---

## 12. ISkillSystem / SkillSystem

**文件**：`System/SkillSystem.cs`  
**职责**：技能触发引擎——基于触发器类型、绑定条件和递归保护执行技能效果图。

### 核心 API

| 方法 | 说明 |
|------|------|
| `void Trigger(SkillTrigger trigger, TriggerContext context, ICanSendCommand commandSender)` | 触发指定类型的所有技能 |
| `void CollectParallelDamage(TriggerContext context, List<DamageContext> output)` | 收集并行伤害（战斗叠加用） |
| `IReadOnlyList<SkillTriggerLog> RecentLogs` | 最近 64 条触发日志 |
| `void ResetTriggerDepth()` | 重置递归深度 |
| `void BeginRootCommand()` | 标记根命令开始 |

### 触发器阶段

OnNodeStart, OnBeforePlayerAction, OnAfterPlayerAction, OnBeforeCombat, OnModifyDamage, OnAfterDamage, OnCardMoved, OnMonsterKilled, OnNodeClear, OnNodeEnd

### 内部机制

- `EffectTriggerQueue`：最大递归深度 8，防死循环
- 条件评估支持：`always`、`first_hit_player_attacks_monster`、`player_defender_monster_attacks`、`moved_to_adjacent_player`、`moved_to_slot_3_killable`、`in_item_slot`
- 所有者类型：遗物、玩家技能、怪物技能、帮助卡被动

### 事件

| 方向 | 事件 |
|------|------|
| 发送 | `SkillTriggeredEvent` |
| 监听 | `CardMovedEvent`（自动触发 OnCardMoved 技能） |

**状态**：完整实现

---

## 13. ISaveSystem / SaveSystem

**文件**：`System/SaveSystem.cs`  
**职责**：完整的存档系统——捕获当前局全部状态并序列化到持久存储，支持加载恢复和版本兼容。

### 核心 API

| 方法 | 说明 |
|------|------|
| `bool HasSave(string slot)` | 检查指定槽位是否有存档 |
| `RunSaveData CaptureCurrentRun(SaveRunReason reason)` | 捕获当前局面为存档数据 |
| `void SaveCurrentRun(string slot, SaveRunReason reason)` | 保存到指定槽位 |
| `bool TryLoadRun(string slot)` | 加载存档 |
| `void ApplySaveData(RunSaveData save)` | 应用存档数据到所有 Model |
| `void DeleteSave(string slot)` | 删除存档 |

### 事件

| 方向 | 事件 |
|------|------|
| 发送 | `RunSavedEvent`、`RunLoadedEvent`、`RunSaveDeletedEvent`、`HelpRewardGeneratedEvent`、`RoomChoiceRequestedEvent`、`ChestRewardGeneratedEvent`、`ShopOpenedEvent`、`TutorSkillChoiceRequestedEvent`、`AttributeChoiceRequestedEvent`（加载后恢复 UI 覆盖层） |
| 监听 | 无 |

**状态**：完整实现（极为详尽，覆盖所有 Model 状态的序列化/反序列化）

---

## 14. IDebugEventSystem / DebugEventSystem

**文件**：`System/DebugEventSystem.cs`  
**职责**：调试用事件日志系统，监听 21 种游戏事件并记录摘要到调试日志。

### 核心 API

无公开 API（接口为空，纯内部监听）。

### 事件

| 方向 | 事件 |
|------|------|
| 发送 | 无 |
| 监听 | `MonsterKilledEvent`、`LevelClearReadyEvent`、`RunSavedEvent`、`RunLoadedEvent`、`GameOverEvent`、`VictoryEvent`、`FlowPhaseChangedEvent`、`DamageAppliedEvent`、`ArmorChangedEvent`、`HealAppliedEvent`、`CardPlacedEvent`、`CardRemovedEvent`、`BoardRotatedEvent`、`DialogueRequestedEvent`、`DialogueCompletedEvent`、`PresentationSequenceRequestedEvent`、`PresentationSequenceCompletedEvent`、`EffectResolvedEvent`、`OverlayOpenedEvent`、`OverlayClosedEvent`、`RunReplayCompletedEvent` |

**状态**：完整实现

---

## 15. INarrativeSystem / NarrativeSystem

**文件**：`System/NarrativeSystem.cs`  
**职责**：叙事消息队列——将 `GameplayMessageEvent` 排入队列，在无对话锁时逐条发送对话命令。

### 核心 API

| 方法 | 说明 |
|------|------|
| `void EnqueueMessage(string message)` | 将消息排入队列 |

### 事件

| 方向 | 事件 |
|------|------|
| 发送 | 无（发送 `StartDialogueCommand`） |
| 监听 | `GameplayMessageEvent`、`DialogueCompletedEvent` |

**状态**：完整实现

---

## 附：辅助类

### MonsterDeckComposer（静态工具类）

**文件**：`System/RuntimeSystems.cs`  
**职责**：按层级规则（强制卡、等级配额）组合怪物牌组。  
**方法**：`static List<string> Compose(MonsterDeckRuleDefinition rule, IRandomUtility randomUtility)`

### PlayerGoldExtensions（静态扩展）

**文件**：`System/RuntimeSystems.cs`  
**职责**：确保每次金币变更时都发送 `GoldChangedEvent`。  
**方法**：`static void ChangeGold(this ICanSendEvent sender, IPlayerModel playerModel, int delta)`
