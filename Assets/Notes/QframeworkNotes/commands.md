# commands.md — Command 总表

> 最后维护：2026-06-13  
> 导航：[architecture.md](architecture.md) | [systems.md](systems.md) | [models.md](models.md) | [events.md](events.md) | [controllers.md](controllers.md)

---

本文档记录 TableNine 项目中所有 QFramework Command，共 **69 个**，分布在 5 个文件中。Command 是状态变更的唯一提交入口，禁止直接修改 Model。

---

## 总览统计

| 文件 | Command 数 | 主题 |
|------|-----------|------|
| `Command/RuntimeCommands.cs` | 48 | 核心运行时命令 |
| `Command/EffectAtomCommands.cs` | 11 | 效果原子命令 |
| `Command/SaveDebugCommands.cs` | 8 | 存档/调试命令 |
| `Command/SkillTriggerCommands.cs` | 1 | 技能触发 |
| `Command/DebugPingCommand.cs` | 1 | 调试心跳 |

---

## 一、流程 Command

### 1. StartNewRunCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：初始化一局全新 Run——清空所有模型，设置种子，创建玩家卡和初始帮助卡，放置棋盘，开始首节点并保存。  
**依赖**：`IConfigModel`, `IRunModel`, `IPlayerModel`, `IBoardModel`, `IDeckModel`, `ICollectionModel`, `IFlowModel`, `IRandomUtility`, `IRunSystem`, `IBoardSystem`  
**派发子命令**：`StartNodeCommand`, `SaveRunCommand`  
**发送事件**：无

### 2. StartNodeCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：开始指定层级和节点——重置节点状态，生成恶魔牌组，快照帮助牌组，重置棋盘并执行开局发牌。  
**依赖**：`IRunModel`, `IPlayerModel`, `IDeckModel`, `IBoardSystem`, `IDeckSystem`, `IFlowModel`  
**派发子命令**：`DealOpeningCardsCommand`  
**发送事件**：无

### 3. DealOpeningCardsCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：执行开局发牌——3 帮助 + 3 恶魔 + 混洗 + 2 战斗卡分配到棋盘和抽牌堆，触发节点开始技能。  
**依赖**：`IDeckModel`, `IBoardSystem`, `IRandomUtility`, `IDeckSystem`, `IFlowModel`, `IStatSystem`, `ISkillSystem`  
**派发子命令**：无  
**发送事件**：无

### 4. CheckClearConditionCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：检查清场——无怪物时切换到 ClearReady，触发节点清除技能，生成帮助卡奖励。  
**依赖**：`IDeckSystem`, `IFlowModel`, `IRunModel`, `ISkillSystem`  
**派发子命令**：`GenerateHelpRewardCommand`  
**发送事件**：`LevelClearReadyEvent`

### 5. EnterRoomChoosingCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：进入房间选择阶段——解锁浮层锁定，生成房间候选并广播。  
**依赖**：`IFlowModel`, `IRewardSystem`, `IRewardModel`, `IInputLockSystem`  
**派发子命令**：无  
**发送事件**：`RoomChoiceRequestedEvent`

### 6. ChooseRoomCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：选择房间——根据房间类型（金币/宝箱/属性/商店）执行不同奖励逻辑。  
**依赖**：`IConfigModel`, `IFlowModel`, `IInputLockSystem`, `IPlayerModel`, `ICollectionModel`, `IRewardModel`, `IRelicSystem`, `IStatSystem`, `IShopSystem`  
**派发子命令**：`SettleNodeEndCommand`, `ApplyStatChangeCommand`, `ProceedToNextNodeCommand`  
**发送事件**：`RoomChosenEvent`, `GameplayMessageEvent`, `ChestRewardGeneratedEvent`, `StatsDirtyEvent`, `ShopOpenedEvent`

### 7. SettleNodeEndCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：结算节点结束——处理未用帮助卡，按 RestoreAfterNode 规则恢复快照，裁减超限卡组，清除节点状态。  
**依赖**：`IDeckModel`, `IRewardSystem`  
**派发子命令**：无  
**发送事件**：无

### 8. ProceedToNextNodeCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：推进到下一节点——处理节点递增、层级完成、通关胜利等分支，每步保存存档。  
**依赖**：`IRunModel`, `IFlowModel`  
**派发子命令**：`StartNodeCommand`, `SaveRunCommand`  
**发送事件**：`VictoryEvent`, `GameplayMessageEvent`, `LayerAdvancedEvent`, `NodeCompletedEvent`, `NodeAdvancedEvent`, `LayerCompletedEvent`

### 9. GameOverCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：触发游戏结束——切换到 GameOver 阶段并保存。  
**依赖**：`IFlowModel`  
**派发子命令**：`SaveRunCommand`  
**发送事件**：`GameOverEvent`

---

## 二、棋盘 Command

### 10. ClickBoardSlotCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：处理玩家点击棋盘格子——路由到目标选择/拾取帮助卡/战斗/移动。  
**依赖**：`IDeckModel`, `CanInteractBoardSlotQuery`  
**派发子命令**：`ResolveTargetingCommand`, `ResolveSwapTargetCommand`, `CommitPlayerActionCommand`, `PickHelpCardToItemSlotCommand`, `StartCombatCommand`  
**发送事件**：无

### 11. ClickItemSlotCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：处理玩家点击道具格子——使用其中的帮助卡。  
**依赖**：`IFlowModel`, `IDeckModel`  
**派发子命令**：`UseHelpCardCommand`  
**发送事件**：无

### 12. PickHelpCardToItemSlotCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：将棋盘上的帮助卡拾取到道具格——若已满则弹窗提示。  
**依赖**：`IDeckModel`, `ICollectionModel`, `IBoardSystem`  
**派发子命令**：无  
**发送事件**：`ItemSlotChangedEvent`, `PopupRequestedEvent`（格满时）

### 13. CommitPlayerActionCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：提交一次玩家行动——若在 PlayerControl 阶段则旋转棋盘并播放序列，否则直接检查清场。  
**依赖**：`IFlowModel`, `IInputLockSystem`, `IBoardSystem`  
**派发子命令**：`PlayPresentationSequenceCommand`, `CheckClearConditionCommand`  
**发送事件**：`PlayerActionCommittedEvent`

### 14. RequestRefillBoardCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：请求补牌——若无空格则检查清场，若已有补牌运行中则标记待处理，否则启动补牌。  
**依赖**：`IDeckModel`, `IBoardModel`, `IInputLockSystem`, `IFlowModel`  
**派发子命令**：`RefillBoardCommand`, `CheckClearConditionCommand`  
**发送事件**：无

### 15. RefillBoardCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：执行棋盘补牌——从战斗抽牌堆取牌填入空格。  
**依赖**：`IBoardModel`, `IDeckModel`, `IBoardSystem`, `IDeckSystem`  
**派发子命令**：`PlayPresentationSequenceCommand`, `CompleteBoardRefillCommand`  
**发送事件**：无

---

## 三、战斗 Command

### 16. StartCombatCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：开始战斗——校验可行性，锁定输入，切换阶段，构建上下文并派发解析。  
**依赖**：`ICombatSystem`, `IFlowModel`, `IInputLockSystem`  
**派发子命令**：`ResolveCombatCommand`, `PlayPresentationSequenceCommand`  
**发送事件**：`CombatStartedEvent`

### 17. ResolveCombatCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：解析战斗全流程——先后手伤害组、死亡检查（含凤凰羽毛复活）、战斗后技能。  
**依赖**：`ICombatSystem`, `ICollectionModel`, `ISkillSystem`, `GetEffectivePlayerStatsQuery`, `GetEffectiveMonsterStatsQuery`  
**派发子命令**：`ApplyDamageGroupCommand`, `KillMonsterCommand`, `ApplyDeathPreventCommand`  
**发送事件**：`CombatBeforeResolvedEvent`, `CombatResolvedEvent`, `CombatAfterResolvedEvent`

### 18. ApplyDamageGroupCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：批量施加伤害组——快照护甲统一结算，支持庇佑护盾拦截。  
**依赖**：`ICollectionModel`, `IDeckModel`  
**派发子命令**：无  
**发送事件**：`DamageAppliedEvent`, `ArmorChangedEvent`, `StatsDirtyEvent`, `DamagePreventedEvent`

### 19. ApplyDamageCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：对单张卡牌施加伤害——支持庇佑拦截，护甲吸收后扣 HP。  
**依赖**：`ICollectionModel`, `IDeckModel`  
**派发子命令**：无  
**发送事件**：`DamagePreventedEvent`, `DamageAppliedEvent`, `ArmorChangedEvent`, `StatsDirtyEvent`

### 20. ApplyDeathPreventCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：检查并执行死亡防止——凤凰羽毛消耗并恢复 50% HP。  
**依赖**：`IRelicSystem`, `ICollectionModel`, `IStatSystem`, `IPlayerModel`  
**派发子命令**：无  
**发送事件**：`DamagePreventedEvent`, `StatsDirtyEvent`

### 21. SetArmorCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：将指定卡牌护甲设定为给定值。  
**依赖**：`ICollectionModel`  
**派发子命令**：无  
**发送事件**：`ArmorChangedEvent`, `StatsDirtyEvent`

### 22. ChangeArmorCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：按增量改变护甲值（不低于 0）。  
**依赖**：`ICollectionModel`  
**派发子命令**：无  
**发送事件**：`ArmorChangedEvent`, `StatsDirtyEvent`

### 23. ApplyStatChangeCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：通用属性变更——支持攻击/防御/最大HP/当前HP/护甲，防御增加时同步护甲。  
**依赖**：`ICollectionModel`  
**派发子命令**：`ChangeArmorCommand`（当 StatType 为 Armor 时）  
**发送事件**：`ArmorChangedEvent`, `StatsDirtyEvent`

### 24. KillMonsterCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：击杀怪物——从棋盘移除，发放金币，按等级注入奖励卡，精英触发导师选择。  
**依赖**：`ICollectionModel`, `IPlayerModel`, `IDeckModel`, `IDeckSystem`, `IBoardSystem`  
**派发子命令**：无  
**发送事件**：`MonsterKilledEvent`

---

## 四、帮助卡 / 效果 Command

### 24. AddHelpCardCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：向帮助卡组加入一张卡；`HelpCardAddPolicy.BypassDeckCapacity` 供遗物/技能效果突破容量。  
**依赖**：`IRewardSystem`

### 25. UseHelpCardCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：使用帮助卡——读取效果图 ID，通过 EffectSystem 解析执行。  
**依赖**：`ICollectionModel`, `IConfigModel`, `ISkillSystem`  
**派发子命令**：`ResolveEffectGraphCommand`  
**发送事件**：`GameplayMessageEvent`（效果缺失时）

### 26. ResolveEffectGraphCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：解析效果图——调用 EffectSystem 执行。  
**依赖**：`IEffectSystem`  
**派发子命令**：无  
**发送事件**：`EffectResolvedEvent`

### 27. ConsumeHelpCardCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：消耗帮助卡——从棋盘/道具格移除，按 `RestoreAfterNode` 决定临时或永久移除。  
**依赖**：`IDeckModel`, `ICollectionModel`, `IConfigModel`, `IBoardSystem`  
**派发子命令**：无  
**发送事件**：`ItemSlotChangedEvent`（若在道具格中）

### 28. ResolveTargetingCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：解析投掷小刀类目标选择——对目标施加伤害或削减护甲。  
**依赖**：`IDeckModel`, `IBoardModel`, `ICollectionModel`, `IPlayerModel`  
**派发子命令**：`ChangeArmorCommand`, `ApplyDamageCommand`, `KillMonsterCommand`, `ConsumeHelpCardCommand`, `RequestRefillBoardCommand`, `CheckClearConditionCommand`  
**发送事件**：`GameplayMessageEvent`

### 29. ResolveSwapTargetCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：解析交换类目标选择——选择两张卡后交换位置。  
**依赖**：`IDeckModel`, `IBoardModel`, `ICollectionModel`, `IPlayerModel`  
**派发子命令**：`SwapBoardCardsCommand`, `ConsumeHelpCardCommand`, `CheckClearConditionCommand`  
**发送事件**：`GameplayMessageEvent`

### 30. ResolveAttributeChoiceCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：解析属性升级选择——攻击+1/防御+1/最大HP+2。  
**依赖**：`IDeckModel`, `IPlayerModel`, `ICollectionModel`, `IInputLockSystem`  
**派发子命令**：`ApplyStatChangeCommand`, `ConsumeHelpCardCommand`  
**发送事件**：`GameplayMessageEvent`, `AttributeChoiceResolvedEvent`

### 31. ApplyEffectHealCommand

**文件**：`Command/EffectAtomCommands.cs`  
**职责**：对指定卡牌施加治疗效果——支持固定数值和满血回复。  
**依赖**：`ICollectionModel`  
**派发子命令**：无  
**发送事件**：`HealAppliedEvent`, `StatsDirtyEvent`, `GameplayMessageEvent`

### 32. ApplyEffectGoldCommand

**文件**：`Command/EffectAtomCommands.cs`  
**职责**：增加或减少玩家金币。  
**依赖**：`IPlayerModel`  
**派发子命令**：无  
**发送事件**：`GameplayMessageEvent`

### 33. OpenAttributeChoiceOverlayCommand

**文件**：`Command/EffectAtomCommands.cs`  
**职责**：打开属性选择浮层（攻击/防御/生命），锁定输入。  
**依赖**：`IDeckModel`, `IInputLockSystem`  
**派发子命令**：无  
**发送事件**：`GameplayMessageEvent`, `AttributeChoiceRequestedEvent`

### 34. OpenChestRewardOverlayCommand

**文件**：`Command/EffectAtomCommands.cs`  
**职责**：打开宝箱奖励浮层——生成候选遗物并锁定输入。  
**依赖**：`IFlowModel`, `IRewardModel`, `IInputLockSystem`, `IRelicSystem`  
**派发子命令**：无  
**发送事件**：`ChestRewardGeneratedEvent`

### 35. OpenHelpTargetingCommand

**文件**：`Command/EffectAtomCommands.cs`  
**职责**：打开帮助卡目标选择模式（投掷小刀/交换位置）。  
**依赖**：`IDeckModel`, `IFlowModel`  
**派发子命令**：无  
**发送事件**：`GameplayMessageEvent`

### 36. RotateBoardRingCommand

**文件**：`Command/EffectAtomCommands.cs`  
**职责**：帮助卡效果触发的棋盘旋转（顺/逆时针）。  
**依赖**：`IFlowModel`, `IInputLockSystem`, `IBoardSystem`  
**派发子命令**：`PlayPresentationSequenceCommand`  
**发送事件**：无

### 37. SwapBoardCardsCommand

**文件**：`Command/EffectAtomCommands.cs`  
**职责**：交换棋盘上两张卡牌位置。  
**依赖**：`IBoardSystem`, `IBoardModel`, `ICollectionModel`  
**派发子命令**：无  
**发送事件**：`BoardSlotChangedEvent` (×2)

### 38. RemoveBoardCardsCommand

**文件**：`Command/EffectAtomCommands.cs`  
**职责**：从棋盘移除多张卡牌并从 CollectionModel 永久删除。  
**依赖**：`IBoardSystem`, `ICollectionModel`  
**派发子命令**：无  
**发送事件**：无

### 39. ApplyBlessingShieldCommand

**文件**：`Command/EffectAtomCommands.cs`  
**职责**：激活庇佑护盾——使下次受到的伤害归零。  
**依赖**：`IDeckModel`  
**派发子命令**：无  
**发送事件**：`GameplayMessageEvent`

### 40. InjectHelpCardsToBattleDeckCommand

**文件**：`Command/EffectAtomCommands.cs`  
**职责**：将帮助卡注入战斗抽牌堆。  
**依赖**：`IDeckSystem`  
**派发子命令**：无  
**发送事件**：无

### 41. ApplyEffectDamageAtomCommand

**文件**：`Command/EffectAtomCommands.cs`  
**职责**：效果图原子伤害——支持单体/全体/前排，击杀后触发补牌或清场检查。  
**依赖**：`ICollectionModel`, `IBoardModel`, `IPlayerModel`  
**派发子命令**：`ApplyDamageCommand`, `KillMonsterCommand`, `RequestRefillBoardCommand`, `CheckClearConditionCommand`  
**发送事件**：无

---

## 五、奖励 / 房间 Command

### 42. GenerateHelpRewardCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：生成帮助卡三选一——切换到奖励阶段，锁定输入并广播候选。  
**依赖**：`IFlowModel`, `IRewardSystem`, `IRewardModel`, `IInputLockSystem`  
**派发子命令**：无  
**发送事件**：`HelpRewardGeneratedEvent`

### 43. PickHelpCardRewardCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：选择帮助卡奖励——创建实例加入牌组，进入房间选择。  
**依赖**：`IConfigModel`, `ICollectionModel`, `IDeckModel`, `IRewardSystem`, `IInputLockSystem`, `IFlowModel`  
**派发子命令**：`EnterRoomChoosingCommand`  
**发送事件**：`HelpRewardPickedEvent`, `GameplayMessageEvent`, `PopupRequestedEvent`

### 44. SkipHelpRewardCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：跳过帮助卡奖励——发 +10 金币补偿，进入房间选择。  
**依赖**：`IPlayerModel`, `IInputLockSystem`, `IFlowModel`  
**派发子命令**：`EnterRoomChoosingCommand`  
**发送事件**：`HelpRewardSkippedEvent`, `GameplayMessageEvent`

### 45. PickRelicRewardCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：选择遗物奖励——添加遗物，根据来源执行后续流程。  
**依赖**：`IRelicSystem`, `IInputLockSystem`, `IFlowModel`, `IRewardModel`  
**派发子命令**：`ProceedToNextNodeCommand`  
**发送事件**：`RelicRewardPickedEvent`

### 46. SkipChestRewardCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：跳过宝箱奖励——发 +20 金币补偿。  
**依赖**：`IPlayerModel`, `IInputLockSystem`, `IFlowModel`, `IRewardModel`  
**派发子命令**：`ProceedToNextNodeCommand`  
**发送事件**：`ChestRewardSkippedEvent`, `GameplayMessageEvent`

### 47. ChooseTutorSkillCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：选择导师技能——校验不可重复，添加技能，同步 HP 加成。  
**依赖**：`IPlayerModel`, `IConfigModel`, `IInputLockSystem`, `IFlowModel`  
**派发子命令**：`ApplyStatChangeCommand`, `CheckClearConditionCommand`  
**发送事件**：`TutorSkillChosenEvent`, `GameplayMessageEvent`, `PopupRequestedEvent`

### 48. DiscardRelicCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：丢弃指定遗物。  
**依赖**：`IRelicSystem`  
**派发子命令**：无  
**发送事件**：无

---

## 六、商店 Command

### 49. OpenShopCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：打开商店浮层——切换到商店阶段，生成候选并广播。  
**依赖**：`IFlowModel`, `IInputLockSystem`, `IShopSystem`, `IRewardModel`  
**派发子命令**：无  
**发送事件**：`ShopOpenedEvent`

### 50. BuyHelpCardCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：购买商店中的帮助卡。  
**依赖**：`IShopSystem`  
**派发子命令**：无  
**发送事件**：无

### 51. DeleteHelpCardForGoldCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：花费金币删除帮助卡。  
**依赖**：`IShopSystem`  
**派发子命令**：无  
**发送事件**：无

### 52. CloseShopCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：关闭商店——根据来源进入下一节点或返回奖励阶段。  
**依赖**：`IInputLockSystem`, `IFlowModel`, `IRewardSystem`, `IRewardModel`  
**派发子命令**：`ProceedToNextNodeCommand`  
**发送事件**：`HelpRewardGeneratedEvent`（非房间来源时）

---

## 七、演示序列 Command

### 53. PlayPresentationSequenceCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：播放演示序列（战斗/旋转/补牌动画），锁定输入。  
**依赖**：`IInputLockSystem`, `ISequenceUtility`  
**派发子命令**：`FinishSequenceCommand`  
**发送事件**：`PresentationSequenceRequestedEvent`

### 54. FinishSequenceCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：完成演示序列——解锁输入，根据完成动作路由到对应收尾命令。  
**依赖**：`IInputLockSystem`  
**派发子命令**：`CompleteCombatPresentationCommand`, `CompleteBoardRotationCommand`, `CompleteBoardRefillCommand`  
**发送事件**：`PresentationSequenceCompletedEvent`

### 55. CompleteCombatPresentationCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：战斗演示完成——解锁战斗锁定，判断玩家死亡或继续。  
**依赖**：`IInputLockSystem`  
**派发子命令**：`GameOverCommand`, `CommitPlayerActionCommand`  
**发送事件**：无

### 56. CompleteBoardRotationCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：棋盘旋转完成——解锁移动锁定，请求补牌。  
**依赖**：`IInputLockSystem`  
**派发子命令**：`RequestRefillBoardCommand`  
**发送事件**：无

### 57. CompleteBoardRefillCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：补牌完成——解锁补牌锁定，处理导师技能选择或回到玩家控制。  
**依赖**：`IDeckModel`, `IRewardSystem`, `IRewardModel`, `IInputLockSystem`, `IFlowModel`  
**派发子命令**：`CheckClearConditionCommand`  
**发送事件**：`TutorSkillChoiceRequestedEvent`

---

## 八、对话 Command

### 58. StartDialogueCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：开始播放对话——锁定输入，请求 UI 显示文本。  
**依赖**：`IInputLockSystem`, `ITextAnimatorUtility`  
**派发子命令**：`FinishDialogueCommand`  
**发送事件**：`DialogueRequestedEvent`

### 59. FinishDialogueCommand

**文件**：`Command/RuntimeCommands.cs`  
**职责**：完成对话——解锁对话锁定。  
**依赖**：`IInputLockSystem`  
**派发子命令**：无  
**发送事件**：`DialogueCompletedEvent`

---

## 九、存档 / 调试 Command

### 60. SaveRunCommand

**文件**：`Command/SaveDebugCommands.cs`  
**职责**：调用存档系统保存当前 Run。  
**依赖**：`ISaveSystem`  
**发送事件**：无

### 61. LoadRunCommand

**文件**：`Command/SaveDebugCommands.cs`  
**职责**：从指定槽位加载 Run，失败时弹窗提示。  
**依赖**：`ISaveSystem`  
**发送事件**：`PopupRequestedEvent`

### 62. ClearSaveCommand

**文件**：`Command/SaveDebugCommands.cs`  
**职责**：删除指定槽位存档。  
**依赖**：`ISaveSystem`  
**发送事件**：无

### 63. ReplayRunCommand

**文件**：`Command/SaveDebugCommands.cs`  
**职责**：根据录像数据重放命令序列，结束后校验哈希一致性。  
**依赖**：`ICommandReplayUtility`, `CommandReplayFactory`, `GetRunSnapshotHashQuery`  
**发送事件**：`RunReplayCompletedEvent`

### 64. CopyBugReportCommand

**文件**：`Command/SaveDebugCommands.cs`  
**职责**：生成 Bug 报告字符串。  
**依赖**：`BugReportBuilder`  
**发送事件**：无

### 65. DebugKillAllMonstersCommand

**文件**：`Command/SaveDebugCommands.cs`  
**职责**：调试——遍历棋盘击杀所有怪物。  
**依赖**：`IBoardModel`, `ICollectionModel`  
**派发子命令**：`KillMonsterCommand`, `CheckClearConditionCommand`  
**发送事件**：无

### 66. DebugSpawnHelpCardCommand

**文件**：`Command/SaveDebugCommands.cs`  
**职责**：调试——按 ID 生成帮助卡加入牌组。  
**依赖**：`IConfigModel`, `IDeckModel`, `ICollectionModel`, `IRewardSystem`  
**发送事件**：`PopupRequestedEvent`, `HelpCardPurchasedEvent`

### 67. DebugAddRelicCommand

**文件**：`Command/SaveDebugCommands.cs`  
**职责**：调试——添加指定遗物。  
**依赖**：`IRelicSystem`  
**发送事件**：无

---

## 十、其他 Command

### 68. TriggerSkillSystemCommand

**文件**：`Command/SkillTriggerCommands.cs`  
**职责**：将技能触发器和上下文转发给 SkillSystem 处理。  
**依赖**：`ISkillSystem`  
**发送事件**：无

### 69. DebugPingCommand

**文件**：`Command/DebugPingCommand.cs`  
**职责**：调试心跳——输出日志验证管线畅通。  
**依赖**：无  
**发送事件**：无

---

## 附：辅助类（非 Command）

| 类名 | 类型 | 说明 |
|------|------|------|
| `DamageApplication` | `internal static class` | 伤害结算工具——`ResolveAgainstSnapshot` 计算护甲吸收与 HP 伤害 |
| `ChestRewardFlow` | `internal static class` | 宝箱奖励流程工具——`ResumeAfterChestReward` 统一宝箱后流程恢复 |
