# architecture.md — TableNine 架构总览与导航

> 最后维护：2026-06-13  
> 项目：TableNine / Unity 2022.3 LTS / Built-in RP / 2D 卡牌 Roguelike / QFramework  
> 分支：`dev`  
> 本文档是 AI Agent 进入项目后的**第一站**，提供全局视图并导航到各分层权威文档。

---

## 0. 文档地图

| 文档 | 职责 | 路径 |
|------|------|------|
| **architecture.md**（本文） | 架构总览、分层规则、注册表、数据模型、设计不变量 | `Assets/Notes/QframeworkNotes/architecture.md` |
| **commands.md** | Command 全表（69 条），含职责、依赖、事件、状态 | `Assets/Notes/QframeworkNotes/commands.md` |
| **systems.md** | System 全表（15 个），含 API、事件收发 | `Assets/Notes/QframeworkNotes/systems.md` |
| **models.md** | Model 全表（8 个），含字段、方法 | `Assets/Notes/QframeworkNotes/models.md` |
| **events.md** | Event 全表（73 条），按领域分类 | `Assets/Notes/QframeworkNotes/events.md` |
| **controllers.md** | Controller / 表现层入口、Query、Utility 说明 | `Assets/Notes/QframeworkNotes/controllers.md` |
| ArchitectureDesign.md | 重构设计蓝图（新版规则 vs 旧语义迁移指引） | `Assets/Notes/ArchitectureDesign.md` |
| rules.md | 全局约束（QFramework 层规则、渲染配置、文件保护） | `rules.md`（项目根目录） |
| agents.md | AI 工作规则与协作规范 | `agents.md`（项目根目录） |

**权威优先级**：`rules.md` > `architecture.md` > 各分层文档 > `ArchitectureDesign.md`（迁移蓝图，已落地部分以分层文档为准）。

---

## 1. 一句话架构

TableNine 是一款九宫格棋盘驱动的 2D 卡牌 Roguelike。核心循环：发牌 → 棋盘交互 → 战斗 → 清场奖励 → 房间选择 → 下一节点。所有状态变更通过 Command 提交，规则逻辑集中在 System，表现层只订阅 Event。

---

## 2. QFramework 注册表（TableNine.cs）

### 2.1 Utility（10 个）

| 接口 | 实现 | 职责 |
|------|------|------|
| `IRandomUtility` | `UnityRandomUtility` | 随机数 |
| `ISaveUtility` | `EasySaveUtility` / `MemorySaveUtility` | 持久化存储（运行态用 EasySave，测试用内存） |
| `IConfigUtility` | `ScriptableConfigUtility` | ScriptableObject 配置加载 |
| `IResourceUtility` | `RuntimeResourceUtility` | ResKit 资源加载 |
| `IAudioUtility` | `ResourceAudioUtility` | AudioKit 音频适配 |
| `ITextAnimatorUtility` | `NullTextAnimatorUtility` | 文本动效（当前空实现，运行态替换） |
| `ISequenceUtility` | `ImmediateSequenceUtility` | 演示序列（测试立即完成，运行态替换 ActionKit） |
| `ICommandTraceUtility` | `CommandTraceUtility` | Command 执行追踪日志 |
| `ICommandReplayUtility` | `CommandReplayUtility` | Command 录制与回放 |
| `IDebugEventLogUtility` | `DebugEventLogUtility` | 调试事件日志 |

### 2.2 Model（8 个）

| 接口 | 实现 | 职责 | 详见 |
|------|------|------|------|
| `IRunModel` | `RunModel` | 单局进度：层号、节点、种子、角色 | [models.md](models.md#1-irunmodel--runmodel) |
| `IPlayerModel` | `PlayerModel` | 玩家属性、金币、技能、遗物 | [models.md](models.md#2-iplayermodel--playermodel) |
| `IBoardModel` | `BoardModel` | 9 格棋盘占用状态 | [models.md](models.md#3-iboardmodel--boardmodel) |
| `IDeckModel` | `DeckModel` | 帮助卡组、恶魔牌堆、战斗抽牌堆、道具栏 | [models.md](models.md#4-ideckmodel--deckmodel) |
| `ICollectionModel` | `CollectionModel` | 全局 CardRuntime 注册表 | [models.md](models.md#5-icollectionmodel--collectionmodel) |
| `IConfigModel` | `ConfigModel` | 静态配置加载与查询 | [models.md](models.md#6-iconfigmodel--configmodel) |
| `IFlowModel` | `FlowModel` | 流程阶段 + 输入锁 | [models.md](models.md#7-iflowmodel--flowmodel) |
| `IRewardModel` | `RewardModel` | 奖励候选（帮助卡/遗物/导师/商店/房间） | [models.md](models.md#8-irewardmodel--rewardmodel) |

### 2.3 System（15 个）

| 接口 | 实现 | 职责 | 详见 |
|------|------|------|------|
| `IRunSystem` | `RunSystem` | 种子解析 | [systems.md](systems.md#1-irunsystem--runsystem) |
| `ILevelFlowSystem` | `LevelFlowSystem` | 流程阶段切换 | [systems.md](systems.md#2-ilevelflowsystem--levelflowsystem) |
| `IBoardSystem` | `BoardSystem` | 棋盘放置/移除/旋转 | [systems.md](systems.md#3-iboardsystem--boardsystem) |
| `IDeckSystem` | `DeckSystem` | 恶魔牌组生成/帮助卡注入/快照 | [systems.md](systems.md#4-idecksystem--decksystem) |
| `ICombatSystem` | `CombatSystem` | 战斗上下文/先手/伤害计算 | [systems.md](systems.md#5-icombatsystem--combatsystem) |
| `IStatSystem` | `StatSystem` | 有效属性计算、护甲填充、怪物被动技能事件链 | [systems.md](systems.md#6-istatsystem--statsystem) |
| `IEffectSystem` | `EffectSystem` | 效果图解析引擎（13 种 Atom） | [systems.md](systems.md#11-ieffectsystem--effectsystem) |
| `ISkillSystem` | `SkillSystem` | 技能触发/防递归/条件评估 | [systems.md](systems.md#12-iskillsystem--skillsystem) |
| `IInputLockSystem` | `InputLockSystem` | 输入锁管理 | [systems.md](systems.md#7-iinputlocksystem--inputlocksystem) |
| `IRewardSystem` | `RewardSystem` | 奖励生成/帮助卡结算/快照恢复 | [systems.md](systems.md#8-irewardsystem--rewardsystem) |
| `IRelicSystem` | `RelicSystem` | 遗物 CRUD/宝箱候选 | [systems.md](systems.md#9-irelicsystem--relicsystem) |
| `IShopSystem` | `ShopSystem` | 商店购买/删除 | [systems.md](systems.md#10-ishopsystem--shopsystem) |
| `ISaveSystem` | `SaveSystem` | 存档捕获/序列化/加载恢复 | [systems.md](systems.md#13-isavesystem--savesystem) |
| `IDebugEventSystem` | `DebugEventSystem` | 调试事件日志（监听 21 种事件） | [systems.md](systems.md#14-idebugeventsystem--debugeventsystem) |
| `INarrativeSystem` | `NarrativeSystem` | 叙事消息队列 | [systems.md](systems.md#15-inarrativesystem--narrativesystem) |

---

## 3. 目录结构

```text
Assets/Scripts/
├── TableNine.cs              # QFramework Architecture 唯一入口
├── Data/                     # 枚举、DTO、运行态值对象、SaveData
│   ├── GameRuntimeData.cs    # 核心枚举 + CardRuntime + EffectiveStats + DamageContext 等
│   └── RunSaveData.cs        # 存档数据结构
├── Model/                    # 运行态状态（8 个 Model）
│   └── RuntimeModels.cs      # 全部 Model 接口与实现
├── System/                   # 规则能力和编排（15 个 System）
│   ├── RuntimeSystems.cs     # 10 个核心 System
│   ├── EffectSystem.cs       # 效果图引擎
│   ├── SkillSystem.cs        # 技能触发引擎
│   ├── SaveSystem.cs         # 存档系统
│   ├── DebugEventSystem.cs   # 调试事件
│   └── NarrativeSystem.cs    # 叙事队列
├── Command/                  # 所有状态变更入口（69 个 Command）
│   ├── RuntimeCommands.cs    # 核心运行时命令（48 个）
│   ├── EffectAtomCommands.cs # 效果原子命令（11 个）
│   ├── SaveDebugCommands.cs  # 存档/调试命令（8 个）
│   ├── SkillTriggerCommands.cs # 技能触发命令
│   └── DebugPingCommand.cs   # 调试心跳
├── Event/                    # 事实事件定义（73 条）
│   └── RuntimeEvents.cs      # 全部事件 struct
├── Query/                    # 只读复杂查询（6 个）
│   └── RuntimeQueries.cs
├── Skill/                    # 技能数据定义
│   └── SkillRuntimeData.cs   # SkillTrigger 枚举 + 技能相关事件
├── Effect/                   # 效果数据定义
│   └── EffectRuntimeData.cs  # EffectAtomType + EffectAtomDefinition + EffectGraphDefinition
├── Config/                   # 配置工厂与校验
├── Utility/                  # 基础设施（存档、随机、哈希、Bug 报告等）
├── Game/                     # 场景 Controller、CardView、BoardSlotView
├── UI/                       # UIKit Panel / ViewController
├── Anim/                     # DOTween/ActionKit 表现编排
└── Tests/                    # EditMode、PlayMode、Replay
```

---

## 4. QFramework 层规则

| 层 | 允许做 | 禁止做 |
|---|---|---|
| **Model** | 保存状态、提供受控写入方法、发送 Event | 调用 System、发动画、做复杂规则编排 |
| **System** | 规则计算、流程服务、候选生成、事件监听/发送 | 直接响应 UI 点击绕过 Command |
| **Command** | 提交状态变化、串联 System、发事实事件、派发子 Command | 持有 MonoBehaviour、直接播放音效/动画 |
| **Query** | 复杂只读计算 | 修改 Model |
| **Utility** | 外部库适配、存取资源、随机数、保存 | 写业务规则 |
| **UI/Game/Anim** | 发 Command、读 Model/Query、订阅 Event | 直接改 Model/System 内部状态 |

**Command 执行管线**（TableNine.ExecuteCommand 重写）：每条 Command 执行前自动经过 `ICommandTraceUtility.Before` → `ICommandReplayUtility.Record` → 实际执行 → `Trace.After`。根命令还会调用 `ISkillSystem.BeginRootCommand()` 重置递归深度。

---

## 5. 核心数据模型速查

### 5.1 枚举

| 枚举 | 值 | 定义位置 |
|------|-----|---------|
| `CardType` | Player, Monster, Help, Room, Tutor | GameRuntimeData.cs |
| `CardQuality` | Initial, White, Blue, Gold, Red | GameRuntimeData.cs |
| `MonsterLevel` | None, Level1~4, Elite, Boss | GameRuntimeData.cs |
| `Suit` | None, Spade, Heart, Diamond, Club | GameRuntimeData.cs |
| `RoomType` | None, Shop, Gold, Chest, Attribute | GameRuntimeData.cs |
| `FlowPhase` | None, Boot, RunStarting, NodeStarting, OpeningDeal, PlayerControl, CombatResolving, BoardMoving, BoardRefilling, ClearReady, RoomChoosing, RoomResolving, HelpRewardChoosing, ChestRewardChoosing, TutorSkillChoosing, Shop, GameOver, NodeEnding, LayerComplete, Victory | GameRuntimeData.cs |
| `InputLockReason` | BoardRefillRunning, CombatResolving, BoardMoving, SequenceRunning, DialogueRunning, OverlayVisible, RewardOverlay, ShopOverlay | GameRuntimeData.cs |
| `DamageType` | Combat, HelpCard, Reflect, Relic, Skill, Room, Debug | GameRuntimeData.cs |
| `StatType` | Attack, Defense, MaxHp, CurrentHp, Armor | GameRuntimeData.cs |

### 5.2 核心运行态类型

| 类型 | 职责 |
|------|------|
| `CardUid` (struct) | 卡牌实例唯一标识（自增 int） |
| `BoardSlotNo` (struct) | 棋盘格位编号 1~9 |
| `CardRuntime` | 卡牌运行态：UID、定义 ID、属性、护甲、格位、技能列表 |
| `HelpCardState` | 帮助卡运行态：临时移除/永久移除/在棋盘/在道具格 |
| `HelpDeckSnapshot` | 帮助卡组节点开始快照 |
| `PendingHelpCardAction` | 待处理的帮助卡动作（飞刀目标/属性选择/庇佑/交换） |
| `EffectiveStats` (struct) | 有效属性：HP、护甲、攻击、防御、伤害减免、先攻 |
| `DamageContext` | 伤害上下文：来源、目标、减免、护甲吸收、HP 伤害、ignoreArmor |
| `CombatContext` | 战斗上下文：双方 UID、属性、先手判断、先手/反击伤害组 |
| `CardDefinition` | 卡牌静态配置：ID、类型、品质、属性、效果图 ID、技能等 |
| `SkillDefinition` | 技能配置：触发器、条件、效果图、先攻、MaxHp 加成 |
| `RelicDefinition` / `RelicInstance` | 遗物配置与运行态实例 |
| `RoomDefinition` | 房间配置：类型、奖励金币、属性加成、注入卡牌 |

---

## 6. 核心流程

### 6.1 单局循环

```text
StartNewRunCommand
  → StartNodeCommand
    → DeckSystem.GenerateDemonDeck
    → DeckSystem.SnapshotHelpDeck
    → BoardSystem.ResetBoardWithPlayer
    → DealOpeningCardsCommand (3帮助+3恶魔+混洗+2战斗)
    → StatSystem.FillArmorFromDefenseAtNodeStart
    → PlayerControl

  ── 玩家控制 ──
  ClickBoardSlotCommand → 拾取帮助卡 / 战斗 / 移动
  ClickItemSlotCommand → 使用道具帮助卡
  UseHelpCardCommand → EffectSystem.ResolveEffectGraph
  CommitPlayerActionCommand → 旋转 → 补牌 → 清场检测

  ── 战斗 ──
  StartCombatCommand → CombatSystem.BuildContext
  ResolveCombatCommand → 先手/反击伤害组 → 击杀/死亡检查

  ── 清场 ──
  CheckClearConditionCommand → ClearReady → GenerateHelpRewardCommand

  ── 奖励 ──
  PickHelpCardRewardCommand / SkipHelpRewardCommand
    → EnterRoomChoosingCommand → 2 个房间候选

  ── 房间 ──
  ChooseRoomCommand → 房间效果 → SettleNodeEndCommand
    → ProceedToNextNodeCommand → StartNodeCommand (循环)
    或 VictoryCommand / GameOverCommand
```

### 6.2 战斗管线

```text
ValidateCombat → BuildCombatContext → PopulateCombatStats
  → PlayerActsFirst → BuildCombatDamage (FirstHit)
  → SkillSystem.CollectParallelDamage (荆棘甲/刺皮)
  → ApplyDamageGroupCommand (护甲快照统一结算)
  → DeathCheck → KillMonster / ApplyDeathPrevent
  → CounterHit (目标存活)
  → SkillSystem.Trigger(OnAfterCombat)
  → CommitPlayerAction
```

### 6.3 帮助卡生命周期

```text
节点开始：DeckSystem.SnapshotHelpDeck (uid 快照)
使用帮助卡：UseHelpCardCommand → EffectSystem → ConsumeHelpCardCommand
  → 读取 CardDefinition.RestoreAfterNode
  → true: IsTemporarilyRemoved (节点结束恢复)
  → false: IsPermanentlyRemoved (永久移除)
节点结束：RewardSystem.SettleUnusedHelpCards (未用 +10 金币)
  → RewardSystem.RestoreHelpDeckSnapshotByRestoreAfterNode
```

---

## 7. 设计不变量

以下不变量是代码必须满足的硬性约束，详见 `ArchitectureDesign.md`：

1. **防御 → 护甲**：防御不直接参与战斗减伤；节点开始和获得防御时，同步填充等量护甲。
2. **伤害公式**：`damageBeforeArmor = max(0, rawAttack - damageReduction)`，然后护甲吸收，最后扣 HP。
3. **帮助卡语义**：默认使用后永久移除；`restoreAfterNode=true` 的卡仅临时移除。
4. **通关流程**：ClearReady → 帮助奖励三选一 → 房间选择（2 候选）→ 节点结束结算。
5. **先攻规则**：双方无先攻→玩家先；双方有先攻→玩家先；仅一方有→该方先。先手击杀不反击。
6. **并行伤害**：荆棘甲与玩家先手伤害并行；刺皮与怪物反击同步。
7. **Command 是唯一状态提交入口**：System/Model 不直接响应 UI 点击。
8. **Event 是唯一表现订阅点**：UI/Anim/Audio 只订阅事件，不写 Model。

---

## 8. 配置与数据

- 配置源：`GameConfigDatabase` (ScriptableObject) 或 `DefaultGameConfigFactory` 回退
- 校验：`ConfigValidator` 在 `ConfigModel.OnInit()` 执行
- 卡牌、角色、技能、遗物、房间、怪物牌组规则均通过 `IConfigModel` 查询
- 常量：`RewardConstants` 集中管理金币奖励、候选数量、品质权重等

---

## 9. 维护约定

每次对项目进行代码改动后，**必须同步更新**对应的分层文档：

| 改动类型 | 需更新的文档 |
|---------|------------|
| 新增/删除/修改 Command | `commands.md` |
| 新增/删除/修改 System | `systems.md` |
| 新增/删除/修改 Model | `models.md` |
| 新增/删除/修改 Event | `events.md` |
| 新增/删除/修改 Controller/UI/Query/Utility | `controllers.md` |
| 架构层面变更（注册表、分层规则、流程） | `architecture.md`（本文） |
| 任何文档更新后 | 检查本文档的导航链接是否仍然有效 |
