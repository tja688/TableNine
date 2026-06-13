# events.md — Event 总表

> 最后维护：2026-06-13  
> 导航：[architecture.md](architecture.md) | [commands.md](commands.md) | [systems.md](systems.md) | [models.md](models.md) | [controllers.md](controllers.md)

---

本文档记录 TableNine 项目中所有 QFramework Event，共 **73 条**（含 2 条跨文件重复定义），分布在 3 个文件中。事件是 UI、Anim、Audio、Debug 的唯一稳定订阅点。事件命名使用过去式或事实描述，不承载下一步规则决策。

---

## 文件分布

| 文件 | 事件数 | 领域 |
|------|--------|------|
| `Event/RuntimeEvents.cs` | ~62 | 核心运行时事件 |
| `UI/RuntimeUIEvents.cs` | ~5 | UI 层事件 |
| `Skill/SkillRuntimeData.cs` | 2（+2 重复） | 技能相关事件 |

---

## 一、流程事件

| # | 事件名 | 关键字段 | 说明 |
|---|--------|---------|------|
| 1 | `RunStartedEvent` | — | 新局开始 |
| 2 | `NodeStartedEvent` | `Layer`, `NodeInLayer` | 节点开始 |
| 3 | `FlowPhaseChangedEvent` | `OldPhase`, `NewPhase` | 流程阶段切换 |
| 4 | `InputLockChangedEvent` | `IsLocked` | 输入锁状态变化 |
| 5 | `LevelClearReadyEvent` | — | 清场完成，进入奖励流程 |
| 6 | `PlayerActionCommittedEvent` | — | 玩家行动提交完成 |
| 7 | `NodeCompletedEvent` | `Layer`, `NodeInLayer` | 节点结束 |
| 8 | `NodeAdvancedEvent` | `Layer`, `NodeInLayer` | 推进到下一节点 |
| 9 | `LayerAdvancedEvent` | `Layer` | 推进到下一层 |
| 10 | `LayerCompletedEvent` | `Layer` | 层级完成 |
| 11 | `GameOverEvent` | — | 游戏结束 |
| 12 | `VictoryEvent` | — | 通关胜利 |

---

## 二、棋盘事件

| # | 事件名 | 关键字段 | 说明 |
|---|--------|---------|------|
| 13 | `CardPlacedEvent` | `CardUid`, `BoardSlotNo`, `CardPlacementSource` | 卡牌放置到棋盘格 |
| 14 | `CardRemovedEvent` | `CardUid`, `BoardSlotNo`, `RemoveReason` | 卡牌从棋盘移除 |
| 15 | `CardMovedEvent` | `CardUid`, `FromSlot`, `ToSlot`, `BoardMoveReason` | 卡牌在棋盘内移动 |
| 16 | `BoardSlotChangedEvent` | `BoardSlotNo` | 棋盘格子状态变化（通用） |
| 17 | `BoardRotatedEvent` | `BoardMoveReason` | 棋盘外环旋转 |
| 18 | `RefillStartedEvent` | — | 补牌开始 |
| 19 | `RefillCompletedEvent` | — | 补牌完成 |
| 20 | `BattleDeckChangedEvent` | — | 战斗抽牌堆变化 |
| 21 | `BattleDeckCardsInjectedEvent` | `InjectedCardUids` | 帮助卡注入战斗牌堆 |
| 22 | `HelpDeckRestoredEvent` | — | 帮助卡组从快照恢复 |
| 23 | `HelpCardsSettledEvent` | `SettledCount`, `TotalGold` | 未用帮助卡结算完成 |
| 24 | `ItemSlotChangedEvent` | `SlotIndex` | 道具格变化 |

---

## 三、战斗 / 伤害事件

| # | 事件名 | 关键字段 | 说明 |
|---|--------|---------|------|
| 25 | `CombatStartedEvent` | `PlayerUid`, `MonsterUid` | 战斗开始 |
| 26 | `CombatBeforeResolvedEvent` | `CombatContext` | 战斗结算前（技能触发点） |
| 27 | `CombatResolvedEvent` | `CombatContext` | 战斗核心结算完成 |
| 28 | `CombatAfterResolvedEvent` | `CombatContext` | 战斗结算后（技能触发点） |
| 29 | `DamageAppliedEvent` | `DamageContext` | 伤害施加完成 |
| 30 | `DamageCalculatedEvent` | `DamageContext` | 伤害计算完成 |
| 31 | `ArmorChangedEvent` | `CardUid`, `NewArmor` | 护甲值变化 |
| 32 | `HealAppliedEvent` | `CardUid`, `HealAmount` | 治疗施加 |
| 33 | `DamagePreventedEvent` | `CardUid`, `DamageAmount` | 伤害被阻止（庇佑） |
| 34 | `DeathPreventedEvent` | `CardUid` | 死亡被阻止（凤凰羽毛） |
| 35 | `MonsterKilledEvent` | `CardUid`, `DefinitionId`, `MonsterLevel` | 怪物被击杀 |
| 36 | `StatsDirtyEvent` | — | 属性需要重新计算/刷新 UI |

---

## 四、效果 / 技能事件

| # | 事件名 | 关键字段 | 说明 |
|---|--------|---------|------|
| 37 | `EffectResolvedEvent` | `EffectGraphId` | 效果图解析完成 |
| 38 | `SkillTriggeredEvent` | `SkillId`, `SkillTrigger` | 技能被触发 |
| 39 | `RelicTriggeredEvent` | `RelicId` | 遗物被触发 |
| 40 | `RelicStatsChangedEvent` | — | 遗物属性加成变化 |

---

## 五、奖励事件

| # | 事件名 | 关键字段 | 说明 |
|---|--------|---------|------|
| 41 | `HelpRewardGeneratedEvent` | `CardIds` | 帮助卡三选一候选生成 |
| 42 | `HelpRewardPickedEvent` | `CardId` | 选择了帮助卡奖励 |
| 43 | `HelpRewardSkippedEvent` | `GoldGained` | 跳过帮助卡奖励 |
| 44 | `RelicRewardPickedEvent` | `RelicId` | 选择了遗物奖励 |
| 45 | `ChestRewardGeneratedEvent` | `RelicIds` | 宝箱遗物候选生成 |
| 46 | `ChestRewardSkippedEvent` | `GoldGained` | 跳过宝箱奖励 |
| 47 | `TutorSkillChosenEvent` | `SkillId` | 选择了导师技能 |

---

## 六、房间事件

| # | 事件名 | 关键字段 | 说明 |
|---|--------|---------|------|
| 48 | `RoomChoiceRequestedEvent` | `RoomCandidateIds` | 房间选择请求 |
| 49 | `RoomChosenEvent` | `RoomId`, `RoomType` | 房间被选择 |
| 50 | `RoomResolvedEvent` | `RoomId` | 房间效果解析完成 |

---

## 七、商店 / 经济事件

| # | 事件名 | 关键字段 | 说明 |
|---|--------|---------|------|
| 51 | `ShopOpenedEvent` | `ShopCardIds` | 商店打开 |
| 52 | `HelpCardPurchasedEvent` | `CardId` | 帮助卡购买成功 |
| 53 | `HelpCardDeletedForGoldEvent` | `CardId` | 帮助卡被金币删除 |
| 54 | `GoldChangedEvent` | `NewGold` | 金币变化 |

---

## 八、遗物事件

| # | 事件名 | 关键字段 | 说明 |
|---|--------|---------|------|
| 55 | `RelicAddedEvent` | `RelicId` | 遗物添加 |
| 56 | `RelicDiscardedEvent` | `RelicId` | 遗物丢弃 |

---

## 九、演示序列事件

| # | 事件名 | 关键字段 | 说明 |
|---|--------|---------|------|
| 57 | `PresentationSequenceRequestedEvent` | `SequenceType` | 请求播放演示序列 |
| 58 | `PresentationSequenceCompletedEvent` | `CompletionAction` | 演示序列播放完成 |

---

## 十、对话事件

| # | 事件名 | 关键字段 | 说明 |
|---|--------|---------|------|
| 59 | `DialogueRequestedEvent` | `Message` | 请求显示对话 |
| 60 | `DialogueCompletedEvent` | — | 对话播放完成 |

---

## 十一、UI / 覆盖层事件

| # | 事件名 | 关键字段 | 来源文件 | 说明 |
|---|--------|---------|---------|------|
| 61 | `OverlayOpenedEvent` | `OverlayId` | RuntimeEvents.cs | 覆盖层打开 |
| 62 | `OverlayClosedEvent` | `OverlayId` | RuntimeEvents.cs | 覆盖层关闭 |
| 63 | `PopupRequestedEvent` | `Message` | RuntimeEvents.cs | 弹窗请求 |
| 64 | `GameplayMessageEvent` | `Message`, `MessageKey` | RuntimeEvents.cs | 游戏内消息 |
| 65 | `AttributeChoiceRequestedEvent` | — | RuntimeUIEvents.cs | 属性选择请求 |
| 66 | `AttributeChoiceResolvedEvent` | `Choice` | RuntimeUIEvents.cs | 属性选择完成 |
| 67 | `RoomChoiceButtonClickedEvent` | `RoomId` | RuntimeUIEvents.cs | 房间按钮点击 |
| 68 | `TutorSkillChoiceRequestedEvent` | `SkillIds` | RuntimeUIEvents.cs | 导师技能选择请求 |
| 69 | `ItemSlotDisplayEvent` | `SlotIndex`, `CardUid` | RuntimeUIEvents.cs | 道具格展示更新 |

---

## 十二、存档 / 回放事件

| # | 事件名 | 关键字段 | 说明 |
|---|--------|---------|------|
| 70 | `RunSavedEvent` | `Slot`, `Reason` | 存档保存完成 |
| 71 | `RunLoadedEvent` | `Slot` | 存档加载完成 |
| 72 | `RunSaveDeletedEvent` | `Slot` | 存档删除完成 |
| 73 | `RunReplayCompletedEvent` | `Success`, `HashMatch` | 回放完成 |

---

## 十三、技能移动事件

| # | 事件名 | 关键字段 | 来源文件 | 说明 |
|---|--------|---------|---------|------|
| 74 | `CardMovedEvent`（技能版） | `CardUid`, `FromSlot`, `ToSlot` | SkillRuntimeData.cs | 卡牌移动（技能系统监听） |
| 75 | `SkillTriggeredEvent`（技能版） | `SkillId`, `Trigger` | SkillRuntimeData.cs | 技能触发 |

> **注**：`CardMovedEvent` 和 `SkillTriggeredEvent` 在 `SkillRuntimeData.cs` 中定义，与 `RuntimeEvents.cs` 中的同名事件为同一类型（在同一个编译单元中），此处列出是为完整性。

---

## 事件命名规范

- 使用**过去式**或**事实描述**：`MonsterKilledEvent`（已击杀），不用 `KillMonsterEvent`。
- 事件只通知**事实**，不承载下一步规则决策。
- 新事件应归入对应领域文件，并在本文档补充条目。

---

## 事件订阅者速查

| 订阅者 | 监听事件数 | 详见 |
|--------|-----------|------|
| `TableNineUIRouter` | 18 | [controllers.md](controllers.md) |
| `GameplayWorldPresenter` | 13 | [controllers.md](controllers.md) |
| `DebugEventSystem` | 21 | [systems.md](systems.md#14-idebugeventsystem--debugeventsystem) |
| `SkillSystem` | 1 | [systems.md](systems.md#12-iskillsystem--skillsystem) |
| `NarrativeSystem` | 2 | [systems.md](systems.md#15-inarrativesystem--narrativesystem) |
| `UIGameplayPanel` | 6 | [controllers.md](controllers.md) |
