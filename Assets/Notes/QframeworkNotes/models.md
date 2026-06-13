# models.md — Model 总表

> 最后维护：2026-06-13  
> 导航：[architecture.md](architecture.md) | [commands.md](commands.md) | [systems.md](systems.md) | [events.md](events.md) | [controllers.md](controllers.md)

---

本文档记录 TableNine 项目中所有 QFramework Model 的接口、实现、字段和方法。全部 Model 定义在 `Model/RuntimeModels.cs` 中，遵循统一模式：接口继承 `IModel`，实现继承 `AbstractModel`，使用 `BindableProperty<T>` 提供响应式状态。

---

## 1. IRunModel / RunModel

**文件**：`Model/RuntimeModels.cs`  
**职责**：存储当前 Roguelike 单局运行的进度信息。

### 状态字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `Layer` | `BindableProperty<int>` | 当前层号 |
| `NodeInLayer` | `BindableProperty<int>` | 当前层内节点号 |
| `Seed` | `BindableProperty<int>` | 随机种子 |
| `IsRunActive` | `BindableProperty<bool>` | 是否正在进行一局 |
| `CharacterId` | `string` | 选择的角色 ID |

### 方法

| 方法 | 说明 |
|------|------|
| `StartRun(string characterId, int seed)` | 初始化新局 |
| `SetNode(int layer, int nodeInLayer)` | 移动到指定节点 |

---

## 2. IPlayerModel / PlayerModel

**文件**：`Model/RuntimeModels.cs`  
**职责**：存储玩家角色的基础属性、金币、技能列表和遗物背包。

### 状态字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `PlayerCardUid` | `CardUid` | 玩家在棋盘上的卡牌 UID |
| `Gold` | `BindableProperty<int>` | 当前金币 |
| `BaseHp` | `int` | 基础血量 |
| `BaseAttack` | `int` | 基础攻击 |
| `BaseDefense` | `int` | 基础防御 |
| `SkillIds` | `IReadOnlyList<string>` | 已获取技能 ID 列表 |
| `Relics` | `IReadOnlyList<RelicInstance>` | 遗物背包（上限 12） |
| `MaxRelicCount` | `int` | 遗物上限 = 12 |

### 方法

| 方法 | 说明 |
|------|------|
| `ResetFromCharacter(CharacterDefinition, CardUid)` | 从角色定义重置玩家状态 |
| `AddRelic(RelicInstance relic)` | 添加遗物（检查上限和重复） |
| `RemoveRelic(string relicId)` | 移除遗物 |
| `HasRelic(string relicId)` | 检查是否拥有指定未消耗遗物 |
| `AddSkill(string skillId)` | 添加技能（不可重复） |

---

## 3. IBoardModel / BoardModel

**文件**：`Model/RuntimeModels.cs`  
**职责**：管理 3×3 棋盘（格 1~9）上各槽位的卡牌占用情况及玩家所在格位。

### 状态字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `mSlots` | `CardUid?[]` (size 10, 1~9 used) | 每格卡牌 UID |
| `PlayerSlot` | `BoardSlotNo` | 玩家所在格位（默认 5） |

### 方法

| 方法 | 说明 |
|------|------|
| `GetCardAt(BoardSlotNo slot)` | 获取指定格的卡牌 UID（可空） |
| `SetCardAt(BoardSlotNo slot, CardUid? uid)` | 设置或清空指定格 |
| `Clear()` | 清空 1~9 所有格 |
| `GetEmptySlots()` | 返回所有空位列表 |

---

## 4. IDeckModel / DeckModel

**文件**：`Model/RuntimeModels.cs`  
**职责**：管理所有牌组相关状态——帮助卡组、恶魔牌堆、战斗抽牌堆、道具栏、快照和补给管线。

### 状态字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `OwnedHelpCards` | `List<CardUid>` | 拥有的所有帮助卡 UID |
| `HelpCardStates` | `Dictionary<int, HelpCardState>` | 每张帮助卡的运行态 |
| `DemonDeckQueue` | `Queue<CardUid>` | 恶魔牌堆抽取队列 |
| `BattleDrawPile` | `Queue<CardUid>` | 战斗抽牌堆 |
| `ItemSlots` | `CardUid?[]` (size 5) | 道具装备栏 |
| `NodeStartSnapshot` | `HelpDeckSnapshot` | 节点开始帮助卡快照 |
| `PendingHelpCardAction` | `PendingHelpCardAction` | 待处理帮助卡动作（飞刀/属性/庇佑/交换） |
| `NextBattleCardPreview` | `BindableProperty<CardPreview>` | 下一张战斗卡预览 |
| `RefillRunning` | `bool` | 补牌是否进行中 |
| `RefillPending` | `bool` | 是否有待处理补牌 |
| `PendingTutorSkillChoice` | `bool` | 是否有待选择导师技能 |

### 方法

| 方法 | 说明 |
|------|------|
| `ResetForNewRun()` | 新局重置 |
| `ClearNodeState()` | 清除节点临时状态 |
| `FindFirstEmptyItemSlot()` | 返回首个空道具格索引（-1 表示已满） |
| `GetCapacity(int layer)` | 获取当前层帮助卡容量 |
| `CountActiveHelpCards()` | 统计未永久移除的帮助卡数 |
| `CountHelpCardsById(string definitionId)` | 按 ID 统计同名卡数量 |
| `GetHelpDeckCapacity(int layer)` | 帮助卡上限公式：`6 × (layer + 1)` |

---

## 5. ICollectionModel / CollectionModel

**文件**：`Model/RuntimeModels.cs`  
**职责**：全局 CardRuntime 注册表，管理卡牌实例的创建、查询和生命周期。

### 状态字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `mCards` | `Dictionary<int, CardRuntime>` | 所有卡牌实例（UID → Runtime） |
| `mNextUid` | `int` | 自增 UID 计数器（从 1 开始） |

### 方法

| 方法 | 说明 |
|------|------|
| `CreatePlayerCard(CharacterDefinition)` | 从角色定义创建玩家卡 |
| `CreateCard(CardDefinition)` | 从卡牌定义创建通用卡 |
| `RestoreCard(CardRuntime runtime)` | 恢复卡牌注册（存档加载） |
| `GetCard(CardUid uid)` | 按 UID 获取（不存在则抛异常） |
| `TryGetCard(CardUid uid, out CardRuntime)` | 安全获取 |
| `RemoveCard(CardUid uid)` | 移除卡牌 |
| `Clear()` | 清空所有卡牌并重置 UID |
| `GetNextUid()` / `SetNextUid(int)` | UID 序列化支持 |

---

## 6. IConfigModel / ConfigModel

**文件**：`Model/RuntimeModels.cs`  
**职责**：加载并缓存所有游戏静态配置（卡牌、角色、技能、怪物牌组规则、遗物、房间），提供按 ID/类型查询接口。

### 状态字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `mCardsById` | `Dictionary<string, CardDefinition>` | 卡牌配置 |
| `mCharactersById` | `Dictionary<string, CharacterDefinition>` | 角色配置 |
| `mSkillsById` | `Dictionary<string, SkillDefinition>` | 技能配置 |
| `mMonsterRulesByKey` | `Dictionary<string, MonsterDeckRuleDefinition>` | 怪物牌组规则（key: `"layer:nodeInLayer"`） |
| `mRelicsById` | `Dictionary<string, RelicDefinition>` | 遗物配置 |
| `mRoomsById` | `Dictionary<string, RoomDefinition>` | 房间配置 |
| `mValidationErrors` | `List<string>` | 校验错误列表 |
| `IsLoaded` | `bool` | 是否已加载 |

### 方法

| 方法 | 说明 |
|------|------|
| `OnInit()` | 从 `GameConfigDatabase` 或回退工厂加载，执行 `ConfigValidator` |
| `GetCardDefinition(string cardId)` | 查询卡牌定义 |
| `TryGetCardDefinition(string cardId, out CardDefinition)` | 安全查询卡牌 |
| `GetCharacterDefinition(string characterId)` | 查询角色定义 |
| `GetSkillDefinition(string skillId)` | 查询技能定义 |
| `GetAllSkillDefinitions()` | 获取所有技能 |
| `GetMonsterDeckRule(int layer, int nodeInLayer)` | 按层/节点查询怪物牌组规则 |
| `GetAllHelpCardDefinitions()` | 获取所有帮助卡定义 |
| `GetHelpCardsByQuality(CardQuality quality)` | 按品质筛选帮助卡 |
| `GetRelicDefinition(string relicId)` | 查询遗物定义 |
| `GetAllRelicDefinitions()` | 获取所有遗物 |
| `GetRoomDefinition(string roomId)` | 查询房间定义 |
| `GetCardsByType(CardType cardType)` | 按类型筛选卡牌 |

---

## 7. IFlowModel / FlowModel

**文件**：`Model/RuntimeModels.cs`  
**职责**：管理回合流程阶段（FlowPhase）和输入锁定机制。

### 状态字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `Phase` | `BindableProperty<FlowPhase>` | 当前流程阶段 |
| `mActiveLocks` | `HashSet<InputLockReason>` | 当前活跃的输入锁集合 |
| `IsInputLocked` | `bool`（计算属性） | 是否有任何锁 |

### 方法

| 方法 | 说明 |
|------|------|
| `SetPhase(FlowPhase phase)` | 切换阶段，发送 `FlowPhaseChangedEvent` |
| `AddLock(InputLockReason reason)` | 加锁，发送 `InputLockChangedEvent` |
| `RemoveLock(InputLockReason reason)` | 解锁，发送 `InputLockChangedEvent` |
| `HasLock(InputLockReason reason)` | 检查是否有指定锁 |
| `Reset()` | 重置阶段和所有锁 |
| `RestoreActiveLocks(IEnumerable<InputLockReason>)` | 从序列化数据恢复锁 |

### 发送事件

`FlowPhaseChangedEvent`、`InputLockChangedEvent`

---

## 8. IRewardModel / RewardModel

**文件**：`Model/RuntimeModels.cs`  
**职责**：存储各种奖励候选项和当前奖励来源状态。

### 状态字段

| 字段 | 类型 | 说明 |
|------|------|------|
| `HelpRewardCardIds` | `List<string>` | 帮助卡奖励候选 ID |
| `ChestRewardRelicIds` | `List<string>` | 宝箱遗物候选 ID |
| `TutorSkillIds` | `List<string>` | 导师技能候选 ID |
| `ShopCardIds` | `List<string>` | 商店卡牌 ID |
| `RoomCandidateIds` | `List<string>` | 房间候选 ID |
| `CurrentRewardSource` | `RewardSource` | 当前奖励来源 |
| `RewardResumePhase` | `FlowPhase?` | 奖励选择完成后的恢复阶段 |

### 方法

| 方法 | 说明 |
|------|------|
| `Clear()` | 清空所有列表和状态 |
| `ClearHelpRewardCardIds()` | 清空帮助卡候选 |
| `ClearChestRewardRelicIds()` | 清空遗物候选 |
| `ClearTutorSkillIds()` | 清空导师候选 |
| `ClearShopCardIds()` | 清空商店卡牌 |
| `ClearRoomCandidateIds()` | 清空房间候选 |
| `AddHelpRewardCardId(string)` / `Add*` 系列 | 添加候选 |
| `RemoveShopCardId(string)` | 移除商店卡牌（购买后） |

---

## 附注

- 所有 Model 均不使用 `System` 层调用，遵循 QFramework 层规则。
- `FlowModel` 是唯一在 Model 层直接发送事件的 Model（`FlowPhaseChangedEvent`、`InputLockChangedEvent`），这是合理的因为阶段和锁变化属于底层状态通知。
