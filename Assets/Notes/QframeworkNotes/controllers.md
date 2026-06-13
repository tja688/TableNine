# controllers.md — Controller / 表现层入口说明

> 最后维护：2026-06-13  
> 导航：[architecture.md](architecture.md) | [commands.md](commands.md) | [systems.md](systems.md) | [models.md](models.md) | [events.md](events.md)

---

本文档记录 TableNine 项目中所有表现层入口，包括 Game 层 Controller、UI 层 Panel、Query 和 Utility。表现层只做三件事：读 Model/Query 展示状态、监听 Event 刷新视图、将输入转为 Command。

---

## 一、Game 层（场景 Controller）

### 1. GameplayBootstrap

**文件**：`Game/GameplayBootstrap.cs`  
**类型**：MonoBehaviour（场景入口）  
**职责**：场景启动时初始化 QFramework Architecture（`TableNine.Init()`）和 UIKit，是整个游戏的运行时入口点。

### 2. GameplaySceneController / GameplayWorldPresenter

**文件**：`Game/GameplaySceneController.cs`  
**类型**：MonoBehaviour + IController  
**职责**：主棋盘世界视图控制器，管理 9 个 BoardSlotView 的创建和绑定，订阅棋盘/战斗/奖励事件驱动视觉更新。

**订阅事件（13 个）**：
`CardPlacedEvent`, `CardRemovedEvent`, `BoardRotatedEvent`, `BoardSlotChangedEvent`, `DamageAppliedEvent`, `HealAppliedEvent`, `ArmorChangedEvent`, `MonsterKilledEvent`, `StatsDirtyEvent`, `HelpRewardGeneratedEvent`, `RoomChoiceRequestedEvent`, `LevelClearReadyEvent`, `FlowPhaseChangedEvent`

**发送 Command**：无（通过子组件代理）

### 3. BoardSlotClickProxy

**文件**：`Game/GameplaySceneController.cs`  
**类型**：MonoBehaviour  
**职责**：棋盘格子点击代理——将点击事件转为 `ClickBoardSlotCommand`。

### 4. ItemSlotClickProxy

**文件**：`Game/GameplaySceneController.cs`  
**类型**：MonoBehaviour  
**职责**：道具格子点击代理——将点击事件转为 `ClickItemSlotCommand`。

### 5. CardView

**文件**：`Game/CardView.cs`  
**类型**：MonoBehaviour  
**职责**：单张卡牌的视觉表现——正面属性、护甲、状态图标的展示。

### 6. BoardSlotView

**文件**：`Game/CardView.cs`  
**类型**：MonoBehaviour  
**职责**：单个棋盘格位的视觉容器——管理其中 CardView 的显示/隐藏。

### 7. CardViewPresenter

**文件**：`Game/CardView.cs`  
**类型**：MonoBehaviour + IController  
**职责**：卡牌视图的 Presenter——监听事件，将 CardRuntime 状态映射到 CardView 视觉元素。

---

## 二、UI 层

### 1. TableNineUIRouter

**文件**：`UI/TableNineUIRouter.cs`  
**类型**：MonoBehaviour + IController  
**职责**：中央 UI 路由器——订阅全局事件并路由到对应 UI Panel 的打开/关闭/刷新。

**订阅事件（18 个）**：
`FlowPhaseChangedEvent`, `HelpRewardGeneratedEvent`, `RoomChoiceRequestedEvent`, `ChestRewardGeneratedEvent`, `ShopOpenedEvent`, `TutorSkillChoiceRequestedEvent`, `AttributeChoiceRequestedEvent`, `GameOverEvent`, `VictoryEvent`, `PopupRequestedEvent`, `GameplayMessageEvent`, `GoldChangedEvent`, `StatsDirtyEvent`, `OverlayOpenedEvent`, `OverlayClosedEvent`, `DialogueRequestedEvent`, `PresentationSequenceRequestedEvent`, `InputLockChangedEvent`

**发送 Command（10 个）**：
`PickHelpCardRewardCommand`, `SkipHelpRewardCommand`, `ChooseRoomCommand`, `PickRelicRewardCommand`, `SkipChestRewardCommand`, `ChooseTutorSkillCommand`, `ResolveAttributeChoiceCommand`, `BuyHelpCardCommand`, `DeleteHelpCardForGoldCommand`, `CloseShopCommand`

### 2. UIGameplayPanel

**文件**：`UI/UIGameplayPanel.cs`  
**类型**：UIPanel (UIKit)  
**职责**：主 HUD 面板——显示玩家属性、金币、牌堆信息、流程状态。

**订阅事件（6 个）**：
`StatsDirtyEvent`, `GoldChangedEvent`, `BattleDeckChangedEvent`, `FlowPhaseChangedEvent`, `HelpDeckRestoredEvent`, `HelpCardsSettledEvent`

### 3. UIChoiceOverlayPanel

**文件**：`UI/UIChoiceOverlayPanel.cs`  
**类型**：UIPanel (UIKit)  
**职责**：通用选择浮层——属性三选一、帮助卡三选一等。

**发送 Command**：`ResolveAttributeChoiceCommand`

### 4. UIDebugPanel

**文件**：`UI/UIDebugPanel.cs`  
**类型**：UIPanel (UIKit)  
**职责**：开发调试面板——提供 8 种调试操作入口。

**发送 Command（8 个）**：
`SaveRunCommand`, `LoadRunCommand`, `ClearSaveCommand`, `DebugKillAllMonstersCommand`, `DebugSpawnHelpCardCommand`, `DebugAddRelicCommand`, `CopyBugReportCommand`, `ReplayRunCommand`

### 5. UIFallbackPanel

**文件**：`UI/UIFallbackPanel.cs`  
**类型**：UIPanel (UIKit)  
**职责**：通用回退面板构建器——为没有专用 Panel 的浮层提供基础 UI。

### 6. TableNineUIRuntime

**文件**：`UI/TableNineUIRouter.cs`（同文件）  
**类型**：静态工具类  
**职责**：提供静态方法快速获取 UI Router 实例和发送 Command。

---

## 三、UI 辅助文件

| 文件 | 职责 |
|------|------|
| `UI/UIPanelKeys.cs` | UIKit 面板注册 Key 常量 |
| `UI/UIConfig.cs` | UI 配置数据 |
| `UI/UITextKeys.cs` | 文本 Key 常量 |
| `UI/UITextProvider.cs` | 文本获取工具 |
| `UI/UIRegistry.cs` | UI 面板注册表 |
| `UI/RuntimeUIEvents.cs` | UI 层事件定义（6 个） |
| `UI/TestRecorder.cs` | 测试录制工具 |

---

## 四、Query（只读查询）

**文件**：`Query/RuntimeQueries.cs`  
共 6 个 Query，均为 `AbstractQuery<T>` 实现。

| # | Query | 返回类型 | 说明 |
|---|-------|---------|------|
| 1 | `GetEffectivePlayerStatsQuery` | `EffectiveStats` | 获取玩家有效属性（委托 StatSystem） |
| 2 | `GetEffectiveMonsterStatsQuery` | `EffectiveStats` | 获取指定怪物有效属性 |
| 3 | `CanInteractBoardSlotQuery` | `InteractionResult` | 棋盘格子点击合法性判断 |
| 4 | `GetBoardSnapshotHashQuery` | `string` | 计算棋盘快照哈希（重放校验用） |
| 5 | `GetRunSnapshotHashQuery` | `string` | 计算完整 Run 哈希 |
| 6 | `GetMonsterRemainingDetailQuery` | `string` | 获取剩余怪物详情（Debug 用） |

---

## 五、Utility（基础设施）

### 5.1 核心 Utility

**文件**：`Utility/InfrastructureUtilities.cs`

| 接口 | 实现 | 说明 |
|------|------|------|
| `IRandomUtility` | `UnityRandomUtility` | Unity 随机数封装 |
| `ISaveUtility` | `MemorySaveUtility` / `EasySaveUtility` | 存储抽象（测试用内存，运行态用 EasySave） |
| `IConfigUtility` | `ScriptableConfigUtility` | ScriptableObject 配置加载 |
| `IResourceUtility` | `RuntimeResourceUtility` | ResKit 资源加载适配 |
| `IAudioUtility` | `ResourceAudioUtility` | AudioKit 音频适配 |
| `ITextAnimatorUtility` | `NullTextAnimatorUtility` | 文本动效（当前空实现） |
| `ISequenceUtility` | `ImmediateSequenceUtility` | 演示序列（测试立即完成） |
| `ICommandTraceUtility` | `CommandTraceUtility` | Command 执行追踪 |
| `ICommandReplayUtility` | `CommandReplayUtility` | Command 录制回放 |
| `IDebugEventLogUtility` | `DebugEventLogUtility` | 调试事件日志 |

### 5.2 回放 Utility

**文件**：`Utility/ReplayUtilities.cs`

| 类 | 说明 |
|----|------|
| `CommandReplayData` | 回放数据结构（命令序列序列化） |
| `CommandReplayFactory` | 从录制数据构建回放实例 |
| `CommandReplayRecorder` | 命令录制器 |

### 5.3 持久化 Utility

**文件**：`Utility/EasySaveUtility.cs`  
**职责**：Easy Save 3 封装，提供文件级存档读写，支持异常回退到内存存档。

### 5.4 哈希 Utility

**文件**：`Utility/RunSnapshotHashUtility.cs`  
**职责**：FNV-1a 状态哈希计算——支持棋盘哈希和完整 Run 哈希，用于重放校验和 Debug。

### 5.5 Bug 报告 Utility

**文件**：`Utility/BugReportBuilder.cs`  
**职责**：生成结构化 Bug 报告文本——包含 seed、command 历史、board hash、save data 摘要。

---

## 六、Skill / Effect 数据层

### 6.1 Skill 数据

**文件**：`Skill/SkillRuntimeData.cs`

| 类型 | 说明 |
|------|------|
| `SkillTrigger` (enum) | 技能触发阶段枚举 |
| `TriggerContext` | 触发上下文数据 |
| `SkillTriggerLog` | 触发日志记录 |
| `SkillOwnerKind` (enum) | 技能所有者类型（Relic/Player/Monster/HelpCard） |

### 6.2 Effect 数据

**文件**：`Effect/EffectRuntimeData.cs`

| 类型 | 说明 |
|------|------|
| `EffectAtomType` (enum) | 效果原子类型（Heal/Damage/AddGold/ModifyStat 等 13 种） |
| `EffectAtomDefinition` | 原子定义——类型 + 参数 |
| `EffectGraphDefinition` | 效果图定义——原子列表 |
| `EffectContext` | 效果执行上下文 |
| `EffectGraphRegistry` | 效果图注册表 |

---

## 七、Anim 层

**目录**：`Anim/`  
当前为动画编排预留目录。按架构规则，Anim 层只监听 Event 播放动画，不改 Model。运行时通过 `ISequenceUtility` 抽象动画等待。

---

## 八、Tests 层

**目录**：`Tests/`

| 类型 | 说明 |
|------|------|
| EditMode Tests | 规则逻辑单元测试（护甲、伤害、帮助卡、通关流程等） |
| PlayMode Tests | 运行时集成测试（首节点完整流程、UI 交互等） |
| Replay Tests | 重放一致性测试（同一 seed + 命令序列 → 相同状态） |
