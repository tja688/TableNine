# ArchitectureDesign.md

> 项目：Unity 2022.3 LTS / Built-in Render Pipeline / 2D 卡牌 Roguelike / QFramework 全盘接受方案  
> 目标：把现有 playtest 体量设计案落地为可编码、可扩展、可调试、可回放的 QFramework 架构。  
> 组织偏好：保持 `Assets/Scripts` 下平铺式一级目录；模块边界靠命名、接口、QFramework 层级和注册关系约束，而不是深层目录。

**设计案来源**：`Assets/Docs`（由 Obsidian「九宫牌局」同步，报告见 `docs/design-sync/latest-sync-report.md`）  
**上次架构对齐设计同步**：2026-06-11（新增 `属性.md`，26 篇机制/卡牌/技能文档更新）

---

## 0. 设计结论

本项目的核心不是传统卡牌“手牌/费用/回合”框架，而是一个**3×3 九宫格状态机 + 双卡组补牌 + 四维属性（攻/防/血/护甲）+ 位置触发词条 + 帮助卡/遗物/技能效果流水线**。因此代码主干应以“规则状态”和“规则动作”为中心：

- `Model/` 只保存状态和少量状态查询入口，不直接驱动动画。
- `System/` 承担跨模块规则编排：关卡流程、九宫格、卡组、战斗、效果、奖励、商店、存档。
- `Command/` 表示玩家或系统的一次动作，是所有状态变更的主入口。
- `Event/` 表示一次性通知，用于 UI、Anim、Audio、Debug 订阅。
- `Query/` 只用于少量确实需要复合计算但不改状态的查询，例如有效属性、可交互性、奖励候选过滤。
- `Utility/` 只封装外部依赖与基础设施：Easy Save、Yarn、Odin 效果资产读取、ResKit、AudioKit、随机数、配置加载。
- `UI/` 与 `Game/` 作为 `IController` 表现层，发送 Command、读取 Model/System、监听 Event/BindableProperty。
- `Anim/` 不进入架构主干，只监听事件并播放表现；表现完成是否反馈给规则，由 `System` 的输入锁和 ActionKit 编排决定。

一句话落地原则：**规则先于表现；状态变更只走 Command；表现层只发起动作和响应事件；所有外部库放在 Utility 或 UI/Anim 适配层。**

### 0.1 顶层领域模型（2026-06-11 设计对齐）

以下四条是后续所有 Model/System/Command 划分的**不变量**，实现不得偏离：

| 支柱 | 设计定义 | 架构落点 |
|---|---|---|
| **九宫格状态机** | 玩家卡格 5 出生；除格 5 外每互动顺时针旋转；正交相邻决定棋盘拾取/战斗 | `BoardModel` + `BoardSystem` + `FlowModel.Phase` |
| **四维属性** | 攻/防/血/护甲；**防御在关卡开始与获得防御时转化为等量护甲**；受伤先扣护甲再扣血；属性下限 0；加血上限即时补等量当前血 | `CardRuntime` + `PlayerModel` + `StatSystem` + `ApplyDamageCommand` |
| **伤害两层结算** | ① 实际伤害 = 攻击 − **伤害减免**（技能/遗物，非对方防御）② 扣血时护甲优先吸收；标注「无视护甲」直扣血 | `CombatSystem` + `DamageContext` + `IEffectAtom.OnModifyDamage` |
| **双卡组 + 节点生命周期** | 帮助卡组跨节点；恶魔卡组每节点独立；战斗卡组 = 节点内混合洗堆；节点结束恶魔/战斗牌堆清空 | `DeckModel` + `DeckSystem` + `RunModel` |

**帮助卡生命周期（新版默认，与旧实现相反）**：

- 默认：**使用后永久移除**（无需卡面重复标注）。
- 例外：卡面标注「**使用后复原**」→ 仅当前节点临时移除，节点结束时从快照恢复。
- 节点开始：对帮助卡组做快照。
- 节点结束（点击房间卡后）：未使用且仍在场上/道具格的帮助卡每张 +10 金币；永久移除的不恢复；「使用后复原」的从快照恢复；节点内新获得的卡追加。

**节点通关 UI 顺序（新版）**：

```text
无怪物剩余
 -> 立即 HelpRewardChoosing（三选一，可跳过 +10）
 -> RoomChoosing（两个房间按钮；此阶段仍可拾取/使用场上帮助卡）
 -> 点击房间 -> 未用帮助卡金币结算 + 房间事件 + ProceedToNextNode
```

> **策划文档冲突待消**：`Assets/Docs/02-卡牌/卡组管理.md#复原规则` 仍写「默认临时移除、卡面标永久移除」；与 `帮助卡系统.md`、`层级系统.md` 相反。代码实现以 **帮助卡系统 + 层级系统** 为准，待 Obsidian 侧修订卡组管理。

### 0.2 设计文档导航（实现前必读）

| 主题 | 路径 |
|---|---|
| 游戏一句话与系统导航 | `Assets/Docs/00-核心概念/游戏概览.md` |
| 术语枢纽 | `Assets/Docs/00-核心概念/核心术语索引.md` |
| 属性（攻/防/血/护甲） | `Assets/Docs/01-机制规则/属性.md` |
| 伤害公式 | `Assets/Docs/01-机制规则/伤害公式.md` |
| 战斗流程 | `Assets/Docs/01-机制规则/战斗机制.md` |
| 发牌/补牌 | `Assets/Docs/01-机制规则/发牌机制.md` |
| 帮助卡规则 | `Assets/Docs/02-卡牌/帮助卡系统.md` |
| 卡组/快照 | `Assets/Docs/02-卡牌/卡组管理.md` |
| 节点循环 | `Assets/Docs/05-职业与层级/层级系统.md` |
| 数值表 | `Assets/Docs/07-数据/` |

---

## 1. QFramework 使用边界

### 1.1 QFramework 架构层映射

| QFramework 概念 | 本项目落点 | 使用原则 |
|---|---|---|
| `Architecture<TableNine>` | `Assets/Scripts/TableNine.cs` | 全局唯一注册入口；注册全部 Model/System/Utility；覆盖 `ExecuteCommand` 做日志、重放、测试钩子。 |
| `IModel` / `AbstractModel` | `Model/` | 保存运行态状态、BindableProperty、表索引；只提供安全改写方法给 Command/System 调用。 |
| `ISystem` / `AbstractSystem` | `System/` | 复杂规则服务与编排。System 可监听 Event、发送 Event、拿 Model/Utility/System。 |
| `ICommand` / `AbstractCommand` | `Command/` | 所有状态变更入口。玩家点击、系统补牌、战斗结算、奖励选择、存档都写成 Command。 |
| `IQuery<TResult>` | `Query/` | 少量复杂查询；不要把所有 getter 都 Query 化。 |
| `TypeEventSystem` / Architecture Event | `Event/` | 一次性事实通知，例如 `DamageAppliedEvent`、`CardMovedEvent`、`OverlayOpenedEvent`。 |
| `BindableProperty<T>` | Model 中高频 UI 数据 | HP、金币、阶段、输入锁、牌堆数量、当前预览牌等适合用 BindableProperty。棋盘格变化用事件更清晰。 |
| `IOCContainer` | QFramework 内部注册 | 不再额外引入 Zenject/全局单例；第三方 SDK 通过 `RegisterUtility<T>()` 进入容器。 |

### 1.2 QFramework Toolkits 选择

| 工具 | 是否采用 | 具体用途 |
|---|---:|---|
| UIKit | 是 | 主菜单、战斗 HUD、帮助卡奖励、商店、宝箱、导师卡、弹窗、暂停菜单。每个 Panel 独立测试场景。 |
| CodeGenKit / ViewController 绑定 | 是 | UI Prefab 组件自动绑定；减少手拖引用错误。 |
| ActionKit | 是 | 补牌延迟、批量补牌间隔、结算序列、覆盖层进出、动画等待、测试环境下可替换为立即执行。 |
| ResKit | 是 | 卡牌 Prefab、Sprite、音频、UI Prefab、Yarn/配置资源加载；playtest 可先跑模拟模式。 |
| AudioKit | 是 | BGM、点击音效、战斗音效、奖励音效、音量开关与设置持久化。 |
| FSMKit | 是 | `LevelFlowSystem` 的节点流程状态；`OverlaySystem` 可选。 |
| PoolKit | 是 | CardView、伤害数字、弹出提示、粒子/简单特效对象池。 |
| GridKit / EasyGrid | 是 | 3×3 棋盘底层结构；外部仍暴露格 1~9 编号。 |
| TableKit | 建议采用 | 配置表索引：按 cardId、quality、type、monsterLevel、suit 查找卡牌/怪物/遗物。 |
| TypeEventSystem.Global | 谨慎 | 只给少量脱离 Architecture 的编辑器/调试工具；游戏主流程使用 Architecture Event。 |
| SingletonKit | 一般不用 | Bootstrap Mono 可用普通组件；核心模块全部进 Architecture。 |
| LiveCodingKit | 可选 | 不作为 playtest 主依赖；可后续用于表现或参数快速试验。 |

---

## 2. 项目目录

建议保持你偏好的平铺结构，新增少量二级目录只用于文件量控制。

```text
Assets/
├─ QFramework/
├─ Scripts/
│  ├─ TableNine.cs
│  ├─ Model/
│  ├─ System/
│  ├─ Command/
│  ├─ Event/
│  ├─ Query/
│  ├─ UI/
│  ├─ Game/
│  ├─ Utility/
│  ├─ Anim/
│  ├─ Data/
│  ├─ Config/
│  └─ Tests/
├─ Art/
│  ├─ UIPrefab/
│  ├─ CardPrefab/
│  ├─ Sprite/
│  └─ Audio/
└─ Dialogue/
   └─ Yarn/
```

### 2.1 目录职责

| 目录 | 内容 |
|---|---|
| `Model/` | `RunModel`、`PlayerModel`、`BoardModel`、`DeckModel`、`CollectionModel`、`ConfigModel`、`FlowModel`、`RewardModel`。 |
| `System/` | `RunSystem`、`LevelFlowSystem`、`BoardSystem`、`DeckSystem`、`CombatSystem`、`EffectSystem`、`SkillSystem`、`RelicSystem`、`RewardSystem`、`ShopSystem`、`SaveSystem`、`OverlaySystem`、`InputLockSystem`。 |
| `Command/` | 所有状态变更动作，按领域命名。 |
| `Event/` | 只放事件 struct；事件名使用过去式或事实名。 |
| `Query/` | 有效属性、交互合法性、奖励候选、怪物剩余判断等。 |
| `UI/` | UIKit Panel、ViewController、UIElement、绑定脚本。 |
| `Game/` | 场景启动、BoardSlotView、CardView、CardInputRaycaster、GameplayBootstrap。 |
| `Utility/` | Easy Save、Yarn、Odin 效果资产、随机数、配置、资源、音频、时间序列。 |
| `Anim/` | 卡牌移动、翻牌、受击、数字跳字、屏幕震动、覆盖层动效。只依赖事件和 View，不直接改 Model。 |
| `Data/` | 纯 C# DTO、运行时实体、枚举、值对象、配置数据结构。 |
| `Config/` | ScriptableObject 配置、Odin 可视化效果资产、编辑器校验器。 |
| `Tests/` | EditMode 单元测试、PlayMode 垂直切片测试、种子回放测试。 |

---

## 3. TableNine 注册入口

`TableNine.cs` 是唯一 Architecture 入口。所有模块必须在这里注册，禁止在业务代码里手写全局单例。

```csharp
using QFramework;
using UnityEngine;

public sealed class TableNine : Architecture<TableNine>
{
    protected override void Init()
    {
        // Utilities: 外部库与基础设施
        RegisterUtility<IRandomUtility>(new UnityRandomUtility());
        RegisterUtility<IConfigUtility>(new ScriptableConfigUtility());
        RegisterUtility<ISaveUtility>(new EasySaveUtility());
        RegisterUtility<IYarnUtility>(new YarnDialogueUtility());
        RegisterUtility<IEffectAssetUtility>(new OdinEffectAssetUtility());
        RegisterUtility<IResourceUtility>(new QFResKitUtility());
        RegisterUtility<IAudioUtility>(new QFAudioKitUtility());
        RegisterUtility<ISequenceUtility>(new ActionKitSequenceUtility());
        RegisterUtility<ICommandTraceUtility>(new CommandTraceUtility());

        // Models: 运行态状态
        RegisterModel<IConfigModel>(new ConfigModel());
        RegisterModel<IRunModel>(new RunModel());
        RegisterModel<IPlayerModel>(new PlayerModel());
        RegisterModel<IBoardModel>(new BoardModel());
        RegisterModel<IDeckModel>(new DeckModel());
        RegisterModel<ICollectionModel>(new CollectionModel());
        RegisterModel<IFlowModel>(new FlowModel());
        RegisterModel<IRewardModel>(new RewardModel());
        RegisterModel<IOverlayModel>(new OverlayModel());

        // Systems: 规则服务与编排
        RegisterSystem<IRunSystem>(new RunSystem());
        RegisterSystem<ILevelFlowSystem>(new LevelFlowSystem());
        RegisterSystem<IBoardSystem>(new BoardSystem());
        RegisterSystem<IDeckSystem>(new DeckSystem());
        RegisterSystem<ICombatSystem>(new CombatSystem());
        RegisterSystem<IStatSystem>(new StatSystem());
        RegisterSystem<IEffectSystem>(new EffectSystem());
        RegisterSystem<ISkillSystem>(new SkillSystem());
        RegisterSystem<IRelicSystem>(new RelicSystem());
        RegisterSystem<IRewardSystem>(new RewardSystem());
        RegisterSystem<IShopSystem>(new ShopSystem());
        RegisterSystem<IInputLockSystem>(new InputLockSystem());
        RegisterSystem<IOverlaySystem>(new OverlaySystem());
        RegisterSystem<ISaveSystem>(new SaveSystem());
        RegisterSystem<INarrativeSystem>(new NarrativeSystem());
    }

    protected override void ExecuteCommand(ICommand command)
    {
        var trace = GetUtility<ICommandTraceUtility>();
        trace.Before(command);
        base.ExecuteCommand(command);
        trace.After(command);
    }

    protected override TResult ExecuteCommand<TResult>(ICommand<TResult> command)
    {
        var trace = GetUtility<ICommandTraceUtility>();
        trace.Before(command);
        var result = base.ExecuteCommand(command);
        trace.After(command, result);
        return result;
    }
}
```

### 3.1 Command 拦截用途

`ExecuteCommand` 拦截会成为 playtest 期最重要的调试基础：

- 记录玩家输入与系统自动 Command。
- 显示战斗结算流水。
- 保存可重放 seed + command list。
- 单元测试中断言某次点击产生了哪些事件。
- 捕获异常时输出当前棋盘、牌堆、输入锁、阶段。

---

## 4. 数据模型设计

### 4.1 核心枚举和值对象

```csharp
public enum CardType { Player, Monster, Help, Room, Tutor }
public enum CardQuality { White, Blue, Gold, Red, Initial }
public enum MonsterLevel { Level1, Level2, Level3, Level4, Elite, Boss }
public enum Suit { None, Spade, Heart, Diamond, Club }
public enum RoomType { Shop, GoldRoom, ChestRoom, AttributeRoom }
public enum FlowPhase
{
    None,
    Boot,
    MainMenu,
    RunStarting,
    NodeStarting,
    OpeningDeal,
    PlayerControl,
    CombatResolving,
    BoardMoving,
    BoardRefilling,
    ClearReady,
    RoomChoosing,
    HelpRewardChoosing,
    Shop,
    ChestRewardChoosing,
    TutorSkillChoosing,
    GameOver,
    Victory
}

public readonly struct CardUid
{
    public readonly int Value;
    public CardUid(int value) => Value = value;
}

public readonly struct BoardSlotNo
{
    public readonly int Value; // 1..9
    public BoardSlotNo(int value) => Value = value;
}
```

### 4.2 静态配置

静态配置以 ScriptableObject 为主，Odin 负责外层编辑体验；运行态只读配置转成 DTO 或索引表，不直接把 MonoBehaviour/Prefab 混入规则层。

#### `CardDefinition`

| 字段 | 说明 |
|---|---|
| `cardId` | 稳定字符串 ID，例如 `help_throwing_knife`。 |
| `displayName` | 显示名，例如“飞刀”。 |
| `cardType` | Player / Monster / Help / Room / Tutor。 |
| `quality` | 白/蓝/金/红/初始。 |
| `price` | 商店价格。 |
| `suit` / `rank` / `monsterLevel` | 怪物花色、牌面、等级。 |
| `baseHp/baseAttack/baseDefense` | 怪物/玩家静态基础值。 |
| `skillIds` | 怪物固定词条或玩家初始技能。 |
| `effectGraphId` | 帮助卡/房间卡/导师卡对应效果图。 |
| `tags` | `Chest`、`Consumable`、`Tower`、`Projectile` 等。 |
| `restoreAfterNode` | 帮助卡是否在节点结束后复原；**默认 `false`（永久移除）**；仅「使用后复原」卡为 `true`。 |
| `sameNameLimit` | 默认 3，可配置。 |

> 旧字段 `isPermanentRemoveOnUse` 语义已反转，实现迁移为 `restoreAfterNode = !isPermanentRemoveOnUse` 或重命名配置列。

#### `SkillDefinition`

| 字段 | 说明 |
|---|---|
| `skillId` | 稳定 ID。 |
| `ownerType` | Player / Monster / Both。 |
| `stackPolicy` | 怪物可叠加、玩家取强、覆盖、不可叠。 |
| `triggerSpecs` | 触发器列表，例如 `OnMoveToSlot`、`BeforeCombat`、`OnMonsterRemoved`。 |
| `effectGraphId` | 触发后执行的效果图。 |
| `priority` | 同阶段效果顺序。 |

#### `RelicDefinition`

| 字段 | 说明 |
|---|---|
| `relicId` | 稳定 ID。 |
| `quality` | 白/蓝/金/初始。 |
| `statModifiers` | 常驻攻击/防御/生命上限等。 |
| `triggerSpecs` | 击败精英/层主、战斗时、受致命伤、关卡结束等。 |
| `isOneShot` | 凤凰羽毛、金色宝箱等一次性效果。 |
| `excludeFromPool` | 初始遗物不进入宝箱池。 |

#### `MonsterDeckRuleDefinition`

每层每节点一条配置：

```csharp
public sealed class MonsterDeckRuleDefinition
{
    public int layer;          // 1..3
    public int nodeInLayer;    // 1..9
    public int totalFormulaBase = 9; // total = 9 + globalNodeIndex or nodeIndex
    public List<MonsterLevelRange> ranges;
    public bool forceElite;
    public bool forceBoss;
}
```

> 注意：这里建议把“总数 = 9 + X”中的 X 明确保存为 `globalNodeIndex` 或 `nodeInLayer`，并在策划表里写死说明。当前文档更像“当前地图节点数”，实现时先按**当前层内节点序号**做 playtest，保留配置开关。

### 4.3 运行态实体

#### `CardRuntime`

```csharp
public sealed class CardRuntime
{
    public CardUid uid;
    public string definitionId;
    public CardType cardType;
    public BoardSlotNo? boardSlot;
    public int? itemSlotIndex;

    public int currentHp;
    public int maxHp;
    public int currentArmor;   // 运行时护甲，受伤优先扣除；节点开始由有效防御填充
    public int baseAttack;
    public int baseDefense;

    public int moveCount;
    public int actionCounter;
    public bool isRemovedThisNode;
    public bool isPermanentlyRemoved;
    public bool isFaceUp = true;

    public RuntimeModifierList modifiers = new();
    public RuntimeStatusList statuses = new();
    public Dictionary<string, int> counters = new();
}
```

玩家卡也建议作为 `CardRuntime` 存在，原因是：

- 设计中玩家固定出生格 5，但可能被效果移动。
- 正交相邻判定取决于玩家当前格。
- UI 上玩家卡与怪物卡共享部分表现管线。

#### `HelpDeckRuntime`

```csharp
public sealed class HelpDeckRuntime
{
    public List<CardUid> ownedHelpCards = new();
    public Dictionary<CardUid, HelpCardState> stateByUid = new();
    public HelpDeckSnapshot nodeStartSnapshot;
    public int nextHelpUid;
}
```

关键规则：帮助卡每张有唯一 uid；节点开始保存快照；**默认使用后永久移除**；仅 `restoreAfterNode=true` 的卡节点结束从快照恢复；节点内新获得卡追加；未使用且仍在场上/道具格的卡节点结束结算 +10 金币。

#### `BattleDeckRuntime`

```csharp
public sealed class BattleDeckRuntime
{
    public Queue<CardUid> drawPile = new();
    public List<CardUid> demonDeckGenerated = new();
    public List<CardUid> openingHelpDrawn = new();
    public List<CardUid> openingDemonDrawn = new();
    public bool refillPending;
    public bool refillRunning;
}
```

---

## 5. Model 划分

### 5.1 `RunModel`

职责：当前一局的元数据。

- `BindableProperty<int> Layer`
- `BindableProperty<int> NodeInLayer`
- `BindableProperty<int> GlobalNodeIndex`
- `BindableProperty<int> Seed`
- `BindableProperty<bool> IsRunActive`
- `RunSaveData CurrentSaveDraft`
- 当前房间、是否击败精英/层主、是否通关。

不负责：生成怪物、发牌、奖励逻辑。

### 5.2 `PlayerModel`

职责：玩家长期与当前节点状态。

- 玩家 card uid。
- 基础属性、永久属性、节点临时属性。
- **当前护甲**（与玩家 `CardRuntime.currentArmor` 同步或由 PlayerModel 代理）。
- 金币 `BindableProperty<int>`。
- 技能列表。
- 遗物列表。
- 状态：庇佑、历战目标、暴力卡临时翻倍、**伤害减免**修正（技能/遗物，参与伤害公式而非直接减对方防御）。

不直接计算有效属性；有效属性走 `StatSystem` 或 `GetEffectivePlayerStatsQuery`。

### 5.3 `BoardModel`

职责：九宫格占位与棋盘事件数据。

- 内部：`EasyGrid<CardUid?> grid = new EasyGrid<CardUid?>(3, 3)`。
- 外部：只暴露格 1~9 API。
- `CardUid? GetCardAt(BoardSlotNo slot)`。
- `void SetCardAt(BoardSlotNo slot, CardUid? uid)` 只给 BoardSystem/Command 调用。
- `BoardSlotNo PlayerSlot`。
- 常量：
  - 顺时针环：`1 -> 2 -> 3 -> 6 -> 9 -> 8 -> 7 -> 4 -> 1`
  - 逆时针环：反向。

### 5.4 `DeckModel`

职责：帮助卡组、恶魔卡组、战斗卡组、道具牌格。

- 帮助卡 owned list 与 uid 状态。
- 节点开始快照。
- 战斗牌堆 Queue。
- 道具牌格 5 格。
- 当前下一张预览 `BindableProperty<CardPreview>`。
- 帮助卡组容量规则。

### 5.5 `CollectionModel`

职责：所有运行态卡实例、技能实例、遗物实例的索引。

- `Dictionary<CardUid, CardRuntime>`。
- 根据 uid 查询卡实例。
- 运行时创建/销毁卡实例。
- 不做规则判断，只做实体仓库。

### 5.6 `ConfigModel`

职责：配置索引。

- `CardDefinitionTable`。
- `SkillDefinitionTable`。
- `RelicDefinitionTable`。
- `MonsterDeckRuleTable`。
- `RoomDefinitionTable`。
- 按品质、类型、等级、花色等建立 TableKit 索引。

### 5.7 `FlowModel`

职责：当前流程阶段与输入锁原因。

- `BindableProperty<FlowPhase> Phase`
- `BindableProperty<bool> IsInputLocked`
- `HashSet<InputLockReason>`
- 当前 Action 序列编号，用于防止异步回调落到旧节点。

### 5.8 `RewardModel`

职责：当前打开的奖励/商店/宝箱/导师候选。

- 帮助卡三选一候选。
- 商店 6 张卡候选。
- 宝箱遗物候选。
- 导师技能候选。
- 当前奖励上下文来源：关卡通关、宝箱卡、精英、房间。

### 5.9 `OverlayModel`

职责：覆盖层/弹窗堆栈。

- 打开层级列表。
- 是否阻止底层点击。
- 当前 popup 文案。

---

## 6. System 划分

### 6.1 `RunSystem`

职责：整局生命周期。

主要 API：

```csharp
void NewRun(string characterId, int? seedOverride = null);
void ContinueRun(RunSaveData save);
void EndRunAsDefeat();
void EndRunAsVictory();
```

发起的 Command：

- `StartNewRunCommand`
- `LoadRunCommand`
- `StartNodeCommand`
- `EndRunCommand`

### 6.2 `LevelFlowSystem`

职责：关卡/节点状态机。

建议用 FSMKit 管理：

```text
Boot
 -> NodeStarting          // 节点开始：恶魔卡组生成、帮助卡组快照、玩家护甲 = 有效防御
 -> OpeningDeal
 -> PlayerControl
 -> CombatResolving / BoardMoving / BoardRefilling
 -> PlayerControl
 -> HelpRewardChoosing    // 无怪物剩余后立即弹出（可跳过 +10）
 -> ClearReady + RoomChoosing   // 选卡结束后出现房间按钮；仍可拾取/用卡
 -> Shop / ChestReward / Tutor / ...  // 房间事件子流程
 -> ProceedToNextNode     // 点击房间后：未用卡金币、快照恢复、清恶魔/战斗牌堆
 -> NodeStarting
 -> Victory / GameOver
```

关键规则：

- **帮助卡三选一先于房间按钮**（`HelpRewardChoosing` → `RoomChoosing`），与旧版「先房间后选卡」相反。
- `ClearReady`/`RoomChoosing` 期间玩家仍可拾取九宫格帮助卡、使用道具牌格；只是无法战斗。
- 点击房间后才结算未使用帮助卡金币、执行房间事件、恢复帮助卡组快照。
- 覆盖层打开时给 `InputLockSystem` 加锁。

### 6.3 `BoardSystem`

职责：棋盘格规则。

主要 API：

```csharp
bool IsOrthogonalAdjacentToPlayer(BoardSlotNo slot);
IReadOnlyList<BoardSlotNo> GetEmptySlots();
void PlaceCard(CardUid uid, BoardSlotNo slot);
void RemoveCard(BoardSlotNo slot, RemoveReason reason);
void RotateClockwise(BoardMoveReason reason);
void RotateCounterClockwise(BoardMoveReason reason);
void Swap(BoardSlotNo a, BoardSlotNo b);
```

触发事件：

- `CardPlacedEvent`
- `CardRemovedEvent`
- `CardMovedEvent`
- `BoardRotatedEvent`
- `BoardSlotChangedEvent`

移动后统一通知 `SkillSystem` / `EffectSystem` 检查 `OnMoveToSlot`、`OnAfterBoardMove`。

### 6.4 `DeckSystem`

职责：发牌、抽牌、补牌、防抖、帮助卡快照。

主要流程：

1. 节点开始：生成恶魔卡组，保存帮助卡组快照，玩家护甲 = 有效防御。
2. 开局发牌：帮助卡 3 张 + 恶魔卡 3 张随机空格；剩余帮助卡与恶魔卡洗成战斗卡组；再抽 2 张补满 8 张。
3. 空格补牌：接收多个补牌请求，合并成一次批量补牌。
4. 牌堆空：停止补牌，更新“牌组已空”。
5. 节点结束：恢复帮助卡快照，追加新增，清空恶魔卡组。

补牌防抖建议：

```text
RequestRefillBoardCommand
  -> DeckModel.refillPending = true
  -> 如果 refillRunning 则返回
  -> ActionKit 延迟 0.45/0.5 秒
  -> RefillBoardCommand 连续处理所有空格
  -> refillPending = false
  -> CheckClearConditionCommand
```

### 6.5 `CombatSystem`

职责：战斗结算。

结算管线：

```text
ValidateCombat
 -> BuildCombatContext
 -> BeforeCombat triggers
 -> CalculateEffectiveStats (攻/防/血；防不直接参与对攻减免)
 -> DetermineInitiative
 -> FirstHit: rawDamage = attack - targetDamageReduction
 -> ApplyDamage (护甲吸收 -> 扣血；ignoreArmor 跳过护甲层)
 -> DeathCheck
 -> CounterHit + 刺皮反伤（同步结算，非串行两段）
 -> 荆棘甲额外伤害（与先手伤害并行，设计步骤 4）
 -> Damage prevention (庇佑) / death prevention (凤凰羽毛)
 -> AfterCombat triggers
 -> KillSettlement
 -> CommitPlayerAction
 -> BoardMove if allowed
 -> RequestRefill
```

伤害结构：

```csharp
public sealed class DamageContext
{
    public CardUid source;
    public CardUid target;
    public int rawAttack;
    public int damageReduction;  // 技能/遗物减免，非对方 Defense
    public int damageBeforeArmor; // max(0, rawAttack - damageReduction)
    public int armorAbsorbed;
    public int hpDamage;
    public DamageType type; // Combat, HelpCard, Reflect, SkillExtra
    public bool ignoreArmor;     // 「无视护甲」直扣血
    public bool preventable;     // 庇佑等
    public List<string> tags;
}
```

关键规则实现点：

- **战斗伤害**：实际伤害 = 攻击 − **伤害减免**（技能/遗物）；**不是**攻击 − 对方防御。防御只负责在节点开始/获得防御时提供护甲。
- **护甲层**：`currentArmor` 优先吸收；为 0 后伤害才扣 `currentHp`；`ignoreArmor` 跳过护甲直扣血。
- 先攻：只有一方有先攻时先攻方先；双方都有或双方都没有时玩家先；先攻方击杀则对方不反击。
- **刺皮**：怪物存活反击时与反击伤害**同步**结算（设计已移除「反击后再刺皮」的串行描述）。
- 庇佑只免疫一次伤害；多段只免第一段。
- 凤凰羽毛：致命伤时恢复 50% 血量并消耗遗物。
- 怪物移除奖励 5 金币；精英/层主死亡注入对应奖励卡。
- 伤害公式文档已移除「特殊固定伤害」独立小节（尖盾/侧方打击等）；若卡牌仍需要固定伤害，走 `DamageAtom` + `ignoreArmor`/`damageReduction` 标签，而非写死在 `CombatSystem`。

### 6.6 `StatSystem`

职责：有效属性计算。

计算来源：

```text
【面板显示用有效攻/防/血】
基础属性 + 遗物 + 套装 + 技能位置加成 + 节点临时修正 + 历战/暴力卡/鼓舞/复仇等

【战斗伤害用】
rawAttack = 有效攻击
damageReduction = 技能/遗物提供的减免总和（与对方 Defense 无关）

【护甲来源】
节点开始：currentArmor = 有效防御
获得防御效果：currentArmor += 增量（等同防御增量）
```

设计要求：

- UI 显示有效攻/防/血，不能显示裸值；**护甲单独显示**（见 `界面布局`）。
- 防御与护甲分离：防御是「生成护甲的数值」，护甲是「当前可吸收伤害的缓冲」。
- 有效属性尽量实时计算；只缓存 UI 展示用 BindableProperty。
- 每次影响来源变化时发送 `StatsDirtyEvent`，UI 再查询有效属性。

### 6.7 `EffectSystem`

职责：执行帮助卡、技能、遗物、房间卡、宝箱、导师卡等“效果”。

核心接口：

```csharp
public interface IEffectAtom
{
    void Resolve(EffectContext context);
}

public sealed class EffectContext
{
    public EffectSource source;
    public CardUid? caster;
    public IReadOnlyList<CardUid> targets;
    public BoardSlotNo? sourceSlot;
    public RuntimeTagSet tags;
    public Dictionary<string, object> blackboard;
}
```

效果资产建议：

- Odin 外层编辑 `EffectGraphDefinition`。
- 每个 `EffectGraphDefinition` 由多个 `EffectAtomDefinition` 组成。
- 运行时 `IEffectAssetUtility` 把定义转成 `IEffectAtom`。
- `EffectSystem` 只认接口，不关心 Odin。

第一批原子效果：

| Atom | 用途 |
|---|---|
| `DamageAtom` | 飞刀、火球术、爆弹、撞击教程、瞭望塔。 |
| `HealAtom` | 恢复药水、食品卡、治疗泉、活力护符。 |
| `AddGoldAtom` | 金币卡、跳过奖励、未用帮助卡结算。 |
| `AddCardToHelpDeckAtom` | 选牌、商店购买、金色宝箱、军械库。 |
| `InjectCardToBattleDeckAtom` | 精英/层主死亡、红桃之母、金币房、宝箱房、属性房。 |
| `MoveBoardAtom` | 顺时针/逆时针旋转。 |
| `SwapCardsAtom` | 交换卡、竖向拉杆。 |
| `RemoveCardAtom` | 滚石、帮助卡消耗、怪物死亡。 |
| `ModifyStatAtom` | 属性提升卡、破击锤、临时攻击翻倍。 |
| `ApplyStatusAtom` | 庇佑、决斗禁止移动。 |
| `OpenChoiceOverlayAtom` | 宝箱三选一、属性提升三选一、导师三选一。 |
| `ConsumeHelpCardAtom` | 默认永久移除；`restoreAfterNode=true` 时仅节点内临时移除。 |

### 6.8 `SkillSystem`

职责：技能触发和叠加策略。

触发阶段建议：

```text
OnNodeStart
OnBeforePlayerAction
OnAfterPlayerAction
OnBeforeBoardMove
OnAfterBoardMove
OnMoveToSlot
OnBeforeCombat
OnModifyDamage
OnAfterDamage
OnCardRemoved
OnMonsterKilled
OnNodeClear
OnNodeEnd
```

叠加规则：

- 怪物技能默认可叠加，除非配置 `stackPolicy = UniqueOnBoard`。
- 玩家技能不可叠加，获得相同技能时取强或覆盖。
- “处于某格生效”的技能不写入永久属性，只在 `StatSystem` 条件计算。
- “移动到某格触发”的技能由 `BoardSystem` 发送 `CardMovedEvent` 后触发。

### 6.9 `RelicSystem`

职责：遗物属性、触发、丢弃、去重。

规则：

- 遗物最多 12 个。
- 宝箱候选排除已拥有遗物。
- 右键丢弃获得 20 金币。
- 初始遗物不进遗物池。
- 一次性遗物如凤凰羽毛、金色宝箱需要消费后移除或标记已触发。

### 6.10 `RewardSystem`

职责：帮助卡奖励、宝箱遗物、导师技能、房间奖励。

规则：

- 通关帮助卡三选一；可跳过 +10 金币。
- 宝箱遗物三选一；可跳过 +20 金币。
- 帮助卡品质概率：白 65%、蓝 30%、金 5%、红 0%。
- 已拥有遗物不重复出现。
- 装备栏满时弹窗提示，不直接吞掉奖励。

### 6.11 `ShopSystem`

职责：商店候选、购买、删除帮助卡换金币。

规则：

- 展示 6 张帮助卡。
- 购买加入帮助卡组，受容量与同名上限限制。
- 从当前帮助卡组删除任意数量帮助卡，每张 +10 金币。

### 6.12 `InputLockSystem`

职责：输入锁。

锁原因：

```csharp
public enum InputLockReason
{
    CombatResolving,
    BoardRefillRunning,
    BoardMoving,
    OverlayVisible,
    PopupVisible,
    SequenceRunning,
    DialogueRunning,
    GameEnded
}
```

规则：

- UI 点击前先发 `CanInteractQuery` 或由 Command 内部校验。
- 锁不只为了 UI，也防止测试脚本/键盘快捷键绕过。
- 所有覆盖层打开自动加 `OverlayVisible`；关闭后移除。

### 6.13 `SaveSystem`

职责：存档时机、版本、Easy Save 适配。

建议 playtest 阶段只在安全点存档：

- 新局开始。
- 节点开始前。
- 节点结束、房间/奖励/商店处理完。
- 玩家手动暂停保存。

存档内容只保存 ID 与运行态值，不保存 ScriptableObject/Prefab 引用：

```csharp
public sealed class RunSaveData
{
    public int schemaVersion;
    public int seed;
    public int randomState;
    public int layer;
    public int nodeInLayer;
    public PlayerSaveData player;
    public List<HelpCardSaveData> helpDeck;
    public List<string> relicIds;
    public List<string> skillIds;
    public Dictionary<string, int> permanentCounters;
}
```

### 6.14 `NarrativeSystem`

职责：Yarn 触发与输入锁。

- Yarn 运行时加 `DialogueRunning` 锁。
- Yarn 命令不直接改 Model，统一转发 QFramework Command。
- 可监听事件触发教程：第一次开宝箱、第一次进入商店、第一次精英、第一次遗物满格等。

---

## 7. Command 清单

### 7.1 Run / Flow

| Command | 说明 |
|---|---|
| `StartNewRunCommand(characterId, seed?)` | 创建新局、初始化玩家和初始帮助卡组。 |
| `LoadRunCommand(saveData)` | 从 Easy Save 数据恢复。 |
| `StartNodeCommand(layer, node)` | 进入节点，生成恶魔卡组并准备发牌。 |
| `DealOpeningCardsCommand` | 执行开局 3+3+2 发牌。 |
| `CheckClearConditionCommand` | 判断是否进入 ClearReady。 |
| `ChooseRoomCommand(roomType)` | 选择下一房间类型。 |
| `ProceedToNextNodeCommand` | 进入下一节点或胜利。 |
| `GameOverCommand` | 玩家死亡。 |
| `VictoryCommand` | 第 3 层层主击败后胜利。 |

### 7.2 Input / Board

| Command | 说明 |
|---|---|
| `ClickBoardSlotCommand(slot)` | UI 点击棋盘入口，内部路由到战斗/拾取/空格旋转。 |
| `ClickItemSlotCommand(index)` | 点击道具牌格使用帮助卡。 |
| `PickHelpCardToItemSlotCommand(cardUid)` | 九宫格帮助卡进入道具牌格。 |
| `RotateBoardClockwiseCommand(reason)` | 常规行动后顺时针移动。 |
| `RotateBoardCounterClockwiseCommand(reason)` | 旋转轮等逆时针效果。 |
| `SwapCardsCommand(slotA, slotB)` | 交换卡或竖向拉杆。 |
| `RemoveCardFromBoardCommand(cardUid, reason)` | 移除棋盘卡。 |
| `RequestRefillBoardCommand(reason)` | 请求补牌，防抖合并。 |
| `RefillBoardCommand` | 实际批量补牌。 |

### 7.3 Combat

| Command | 说明 |
|---|---|
| `StartCombatCommand(monsterUid)` | 战斗主入口。 |
| `ApplyDamageCommand(context)` | 统一伤害入口。 |
| `ApplyHealCommand(targetUid, amount, source)` | 治疗。 |
| `KillMonsterCommand(monsterUid, reason)` | 怪物死亡结算。 |
| `ApplyDeathPreventCommand(playerUid, source)` | 凤凰羽毛等免死。 |
| `CommitPlayerActionCommand(actionType)` | 统计行动、触发行动后移动/技能。 |

### 7.4 Help Card / Effect

| Command | 说明 |
|---|---|
| `UseHelpCardCommand(itemSlotIndex, targets)` | 道具牌格帮助卡使用入口。 |
| `ResolveEffectGraphCommand(effectGraphId, context)` | 执行效果图。 |
| `ConsumeHelpCardCommand(cardUid, restoreAfterNode)` | 默认永久移除；`restoreAfterNode=true` 时仅节点内临时移除。 |
| `ChooseAttributeUpgradeCommand(option)` | 属性提升卡。 |
| `OpenChestCommand(cardUid, chestType)` | 宝箱卡打开。 |
| `PickRelicRewardCommand(relicId)` | 获得遗物。 |
| `SkipChestRewardCommand` | 宝箱跳过 +20 金币。 |

### 7.5 Rewards / Shop / Skill / Relic

| Command | 说明 |
|---|---|
| `GenerateHelpRewardCommand(source)` | 生成帮助卡三选一。 |
| `PickHelpCardRewardCommand(cardId)` | 加入帮助卡组。 |
| `SkipHelpRewardCommand` | 跳过 +10 金币。 |
| `OpenShopCommand` | 打开商店。 |
| `BuyHelpCardCommand(cardId)` | 购买帮助卡。 |
| `DeleteHelpCardForGoldCommand(cardUid)` | 删除帮助卡 +10 金币。 |
| `ChooseTutorSkillCommand(skillId)` | 获得导师技能。 |
| `DiscardRelicCommand(relicId)` | 右键丢弃遗物 +20 金币。 |

### 7.6 Save / Debug

| Command | 说明 |
|---|---|
| `SaveRunCommand(reason)` | 保存当前局。 |
| `ClearSaveCommand(slot)` | 清除存档。 |
| `ReplayCommandLogCommand(seed, commands)` | Debug 重放。 |
| `DebugSpawnCardCommand(cardId, slot)` | 调试生成卡牌。 |

---

## 8. Event 清单

事件用 struct，尽量小而明确。

### 8.1 状态与流程

```csharp
public struct RunStartedEvent { public int seed; public string characterId; }
public struct NodeStartedEvent { public int layer; public int node; }
public struct FlowPhaseChangedEvent { public FlowPhase from; public FlowPhase to; }
public struct InputLockChangedEvent { public bool isLocked; public IReadOnlyCollection<InputLockReason> reasons; }
public struct LevelClearReadyEvent { public int layer; public int node; }
public struct NodeCompletedEvent { public int layer; public int node; }
public struct GameOverEvent { public string reason; }
public struct VictoryEvent { }
```

### 8.2 棋盘与牌堆

```csharp
public struct CardPlacedEvent { public CardUid uid; public BoardSlotNo slot; }
public struct CardMovedEvent { public CardUid uid; public BoardSlotNo from; public BoardSlotNo to; }
public struct CardRemovedEvent { public CardUid uid; public BoardSlotNo from; public RemoveReason reason; }
public struct BoardRotatedEvent { public BoardMoveDirection direction; public BoardMoveReason reason; }
public struct RefillStartedEvent { public int emptyCount; }
public struct RefillCompletedEvent { public int cardsDrawn; public bool deckEmpty; }
public struct BattleDeckChangedEvent { public int count; public CardPreview nextPreview; }
public struct HelpDeckChangedEvent { public int count; int capacity; }
```

### 8.3 战斗与效果

```csharp
public struct CombatStartedEvent { public CardUid player; public CardUid monster; }
public struct DamageCalculatedEvent { public DamageContext context; }
public struct DamageAppliedEvent { public CardUid target; public int amount; public DamageType type; }
public struct DamagePreventedEvent { public CardUid target; public string sourceId; }
public struct HealAppliedEvent { public CardUid target; public int amount; }
public struct MonsterKilledEvent { public CardUid monster; public string monsterId; }
public struct PlayerDiedEvent { public string reason; }
public struct EffectResolvedEvent { public string effectGraphId; public EffectSource source; }
```

### 8.4 奖励、商店、UI

```csharp
public struct GoldChangedEvent { public int oldValue; public int newValue; public int delta; }
public struct RelicAddedEvent { public string relicId; }
public struct RelicRemovedEvent { public string relicId; RemoveReason reason; }
public struct SkillAddedEvent { public string skillId; }
public struct HelpRewardGeneratedEvent { public IReadOnlyList<string> cardIds; }
public struct ShopOpenedEvent { public IReadOnlyList<string> cardIds; }
public struct ChestRewardGeneratedEvent { public IReadOnlyList<string> relicIds; }
public struct PopupRequestedEvent { public string message; }
public struct OverlayOpenedEvent { public string panelName; }
public struct OverlayClosedEvent { public string panelName; }
```

---

## 9. Query 清单

不要过度 Query 化。推荐只保留这些：

| Query | 返回 | 用途 |
|---|---|---|
| `GetEffectivePlayerStatsQuery` | `EffectiveStats` | UI 显示、战斗结算（含护甲/减免）。 |
| `GetEffectiveMonsterStatsQuery(monsterUid)` | `EffectiveStats` | 怪物卡显示、战斗结算。 |
| `GetPlayerArmorQuery` | int | 当前护甲（可与 EffectiveStats 合并）。 |
| `CanInteractBoardSlotQuery(slot)` | `InteractionResult` | UI hover/click、Command 防御性校验。 |
| `GetValidTargetsForHelpCardQuery(cardUid)` | `TargetSet` | 飞刀、火球、交换卡、宝箱等目标选择。 |
| `HasMonsterRemainingQuery` | bool | 通关判断：棋盘 + 战斗卡组。 |
| `GetHelpDeckCapacityQuery(layer)` | `DeckCapacityInfo` | 选牌/购买前提示。 |
| `GetRelicCandidatesQuery(chestType)` | `List<RelicDefinition>` | 宝箱去重与品质概率。 |
| `GetMonsterDeckRuleQuery(layer,node)` | `MonsterDeckRuleDefinition` | 恶魔卡组生成。 |

---

## 10. 关键流程设计

### 10.1 新局开始

```text
StartNewRunCommand
 -> ConfigModel 确认配置已加载
 -> RunModel 初始化 seed/layer/node
 -> PlayerModel 创建玩家属性
 -> CollectionModel 创建玩家 CardRuntime
 -> DeckModel 创建初始帮助卡 uid
 -> Apply 初始技能
 -> Send RunStartedEvent
 -> StartNodeCommand(1, 1)
```

职业“小鬼”当前初始：血量 10、攻击 3、防御 1、初始技能“轻车熟路”、初始帮助卡组为恢复药水×3、普通宝箱卡×1、属性提升卡×1、飞刀×3。

> 数据注意：`遗物.md` 内有“初始遗物：村好剑”，但职业“小鬼”写的是“初始遗物：无”。建议实现为 CharacterDefinition 决定是否带初始遗物；`村好剑` 作为可配置初始遗物但默认不挂到小鬼，等待策划确认。

### 10.2 节点开始与开局发牌

```text
StartNodeCommand
 -> FlowPhase = NodeStarting
 -> DeckSystem.GenerateDemonDeck(layer,node)
 -> DeckSystem.SnapshotHelpDeck()
 -> BoardSystem.ResetBoardWithPlayer()
 -> StatSystem: player.currentArmor = effectiveDefense
 -> DealOpeningCardsCommand
```

`DealOpeningCardsCommand`：

1. 从帮助卡组抽 3 张，随机放入空格。
2. 从恶魔卡组抽 3 张，随机放入空格。
3. 剩余帮助卡与恶魔卡合并洗牌成战斗卡组。
4. 从战斗卡组抽 2 张放入空格。
5. 场上达到 8 张卡 + 玩家卡。
6. 更新下一张预览。
7. 进入 `PlayerControl`。

### 10.3 玩家点击棋盘

```text
ClickBoardSlotCommand(slot)
 -> InputLockSystem.AssertUnlocked
 -> CanInteractBoardSlotQuery(slot)
 -> if empty orthogonal: CommitPlayerAction + RotateBoardClockwise
 -> if monster orthogonal: StartCombatCommand(monsterUid)
 -> if help orthogonal: PickHelpCardToItemSlotCommand(cardUid)
 -> else Popup/NoOp
```

帮助卡规则按新版落地：棋盘上的帮助卡点击后进入道具牌格；道具牌格中的帮助卡可对整个九宫格使用，不受玩家正交相邻限制。

### 10.4 玩家行动提交

```text
CommitPlayerActionCommand(actionType)
 -> 统计行动计数（设计已移除「好战词条」独立规则）
 -> SkillSystem.OnAfterPlayerAction
 -> if BoardMoveNotLocked: RotateBoardClockwiseCommand
 -> SkillSystem.OnAfterBoardMove
 -> RequestRefillBoardCommand
```

`决斗` 等效果会给 Board 或 Flow 加 `BoardMoveLocked` 状态，直到该牌移除。

### 10.5 补牌

```text
RequestRefillBoardCommand
 -> 若无空格：CheckClearConditionCommand
 -> 若 refillRunning：refillPending = true 后返回
 -> 加 InputLockReason.BoardRefillRunning
 -> ActionKit 延迟：击杀 0.5s，拾取帮助卡 0.45s，其他默认 0.5s
 -> RefillBoardCommand
 -> 多空格连续抽牌，直到填满或牌堆空
 -> 每张发出 CardPlacedEvent，表现层播放落牌
 -> 移除输入锁
 -> CheckClearConditionCommand
```

### 10.6 通关判定

```text
CheckClearConditionCommand
 -> HasMonsterRemainingQuery == false
 -> FlowPhase = HelpRewardChoosing
 -> GenerateHelpRewardCommand（三选一，可跳过 +10）
 -> 选卡/跳过后 FlowPhase = ClearReady
 -> GenerateRoomCandidates + RoomChoiceRequestedEvent
 -> UI 显示两个房间按钮
```

重要：

- **先帮助卡三选一，再房间按钮**；`ClearReady` 与 `RoomChoosing` 可并存或前后衔接，但房间 UI 不得在选卡覆盖层之前出现。
- `ClearReady`/`RoomChoosing` 期间仍允许拾取九宫格帮助卡、使用道具牌格；只是无法战斗。
- 点击房间按钮后才结算未使用帮助卡金币、执行房间事件、恢复快照、进入下一节点。

### 10.7 帮助卡恢复与金币结算

点击房间后（`ChooseRoomCommand` → 房间事件 → `ProceedToNextNodeCommand`）：

1. 统计本节点**未使用**且仍在场上/道具格的帮助卡，每张 +10 金币（按 uid 逐一匹配）。
2. 将帮助卡组从节点快照恢复：**仅 `restoreAfterNode=true` 的临时移除卡**；永久移除的不恢复。
3. 节点中新获得的帮助卡在恢复后追加。
4. 清空本节点恶魔卡组与战斗牌堆。
5. 下一节点开始时重新根据有效防御填充护甲。

### 10.8 战斗结算

```text
StartCombatCommand(monsterUid)
 -> Validate adjacency/input/monster alive
 -> FlowPhase = CombatResolving; lock CombatResolving
 -> CombatStartedEvent
 -> StatSystem 计算双方有效属性（攻/防/血/护甲）
 -> SkillSystem / RelicSystem BeforeCombat
 -> Determine initiative
 -> BuildDamage: attack - damageReduction
 -> ApplyDamage (护甲吸收 -> 扣血)
 -> DeathCheck(monster)
 -> if monster alive: ApplyDamage(monster -> player) + 刺皮同步反伤
 -> 荆棘甲/其他并行额外伤害
 -> DeathCheck(player); 庇佑/凤凰羽毛
 -> SkillSystem / RelicSystem AfterCombat
 -> if monster killed: KillMonsterCommand
 -> unlock CombatResolving
 -> CommitPlayerActionCommand(Combat)
```

节点开始（`StartNodeCommand`）额外步骤：`currentArmor = GetEffectiveDefense(player)`。

### 10.9 效果触发队列

为了避免复杂词条互相递归，建议所有触发器都进入 `EffectTriggerQueue`：

```text
Event 发生
 -> SkillSystem/RelicSystem 收集可触发项
 -> 按 priority + sourceUid 排序
 -> 逐个 ResolveEffectGraph
 -> 每个 EffectGraph 可发送新 Event
 -> 新 Event 进入下一轮队列
 -> 设置单帧/单 Command 最大触发数，防止无限循环
```

必须有防递归策略：

- `红桃之母` 召唤出的红桃不带红桃之母技能。
- 同一 `triggerId + sourceUid + commandSequenceId` 可配置只触发一次。
- Debug 模式输出触发链。

---

## 11. 帮助卡实现策略

| 帮助卡 | 实现方式 |
|---|---|
| 恢复药水 | `HealAtom(10)` + `ConsumeHelpCardAtom`（默认永久移除）。 |
| 庇佑魔法卡 | `ApplyStatusAtom(Blessing, stack=false)` + 永久移除。 |
| 飞刀 | 目标选择：任意卡牌；`DamageAtom(6, HelpCard)`。 |
| 火球术 | 公式：玩家有效攻击；对目标伤害。 |
| 旋转轮 | `RotateBoardCounterClockwiseCommand`，不动玩家所在牌/格 5规则按配置确认。 |
| 暴力卡 | 给玩家加一次性 `AttackMultiplier=2`，下一次与怪物互动后移除。 |
| 属性提升卡 | 打开属性三选一 Overlay；选择后永久加攻击/防御/生命。 |
| 金币卡 | `AddGoldAtom(50)`。 |
| 食品卡 | 玩家血量回满。 |
| 普通/蓝色/金色宝箱卡 | 打开遗物三选一；候选按宝箱品质概率；排除已拥有；可跳过。 |
| 治疗泉 | 同一张卡在棋盘/道具牌格有不同触发器。 |
| 滚石 | `OnMoveToSlot(3)` 检查格 6 怪物等级，合法则移除。 |
| 爆弹 | 对所有怪物 `DamageAtom(4)`。 |
| 撞击教程 | 公式：玩家当前血量。 |
| 交换卡 | 目标选择：除玩家外两张卡；`SwapCardsAtom`。 |
| 破击锤 | 对目标防御加 `-5` 修正。 |
| 瞭望塔 | 棋盘形态：移动到角落触发，计数 4 次后移除；道具牌格形态：每战斗一次触发。 |
| 倍增塔 | 棋盘形态：格 1 时对怪物帮助卡双触发；道具牌格形态：对玩家帮助卡双触发后永久移除。 |

---

## 12. 技能与遗物实现策略

### 12.1 怪物技能

| 技能类型 | 推荐实现 |
|---|---|
| 处于特定格加属性 | `StatSystem` 条件修正，不写入 Runtime 属性。 |
| 移动到特定格触发 | `CardMovedEvent` -> `SkillSystem`。 |
| 每移动 N 次 | `moveCount` 计数器 + 触发器。 |
| 光环 | `StatSystem` 扫描棋盘状态，或维护 AuraCache；playtest 先实时查询。 |
| 召唤/洗牌 | `InjectCardToBattleDeckAtom`。 |
| 禁止移动 | Board/Flow 状态锁，源卡移除时解除。 |
| 复仇累计 | 怪物 Runtime counter，`CardRemovedEvent` 触发 +4。 |

### 12.2 玩家技能

| 技能 | 推荐实现 |
|---|---|
| 轻车熟路 | `OnNodeClear` 或奖励阶段插入一次白色帮助卡三选一。 |
| 刺皮 | 怪物反击时与反击伤害**同步**对攻击者造成其攻击力伤害（`OnModifyDamage` 或并行 `ApplyDamage`）。 |
| 硬皮 | 获得时加生命上限；`OnNodeClear` 恢复 10。 |
| 历战 | CombatContext 记录上一敌人 uid；同敌叠加，换敌复原历战加成。 |
| 军械库 | 关卡结束生成飞刀/爆弹/破击锤三选一。 |
| 偶数仇恨 | `OnModifyDamage` 根据目标名称/牌面偶数翻倍。 |
| 塔之子 | `OnNodeStart` 将倍增塔放入道具牌格。 |

### 12.3 遗物

遗物全部进入 `RelicSystem`，属性部分由 `StatSystem` 汇总，触发部分走 EffectGraph。

重点：

- 套装：木盾/木剑/木甲由 `StatSystem` 判断同时拥有。
- 金剑：每进行一次战斗，遗物自身 counter -1 攻击。
- 凤凰羽毛：`OnBeforePlayerDeath` 消耗遗物并恢复 50% 血量。
- 金色宝箱：获得时一次性加入两张金色宝箱卡到帮助卡组。
- 村好剑：如启用，击败精英/层主永久 +1 攻击，但不跨局保留。

---

## 13. UI 架构

### 13.1 UIKit Panel

| Panel | 类型 | 说明 |
|---|---|---|
| `UIMainMenuPanel` | 普通页面 | 新局、继续、设置。 |
| `UIGameplayPanel` | 主 HUD | 玩家面板、装备栏、棋盘容器、道具牌格、技能栏、牌组区、介绍区。 |
| `UIRoomChoicePanel` | Overlay | 通关后的两个房间按钮。 |
| `UIHelpRewardPanel` | Overlay | 帮助卡三选一 + 跳过。 |
| `UIShopPanel` | Overlay | 商店 6 卡、删除帮助卡换金币。 |
| `UIChestRewardPanel` | Overlay | 遗物三选一 + 跳过。 |
| `UITutorSkillPanel` | Overlay | 技能三选一。 |
| `UIAttributeChoicePanel` | Overlay | 攻击/防御/生命三选一。 |
| `UIPopupPanel` | Popup | 容量满、遗物满、金币不足等提示。 |
| `UIPausePanel` | Popup | 保存、设置、返回主菜单。 |

### 13.2 ViewController 规则

UI 脚本实现 `IController`：

- 点击按钮只 `SendCommand`。
- 显示数据从 Model 的 BindableProperty 或 Query 获取。
- 状态变化通过 Event/BindableProperty 更新。
- 不直接调用 System 的改状态方法。
- 不保存规则状态，只保存控件引用和表现状态。

### 13.3 Board/Card View

`CardView` 不知道规则，只接受 `CardViewData`：

```csharp
public sealed class CardViewData
{
    public CardUid uid;
    public string displayName;
    public CardType type;
    public CardQuality quality;
    public int hp;
    public int armor;
    public int attack;
    public int defense;
    public string description;
    public Sprite portrait;
    public IReadOnlyList<string> skillIcons;
}
```

`CardViewPresenter` 监听：

- `CardPlacedEvent`
- `CardMovedEvent`
- `CardRemovedEvent`
- `StatsDirtyEvent`
- `DamageAppliedEvent`
- `HealAppliedEvent`

对象创建/回收走 PoolKit。

---

## 14. Anim 表现层

`Anim/` 不进入 Model/System/Command。推荐结构：

```text
Anim/
├─ BoardMoveAnimator.cs
├─ CardDealAnimator.cs
├─ CombatHitAnimator.cs
├─ DamageNumberAnimator.cs
├─ OverlayAnimator.cs
├─ CardHoverAnimator.cs
└─ AnimationEventBridge.cs
```

原则：

- 动画监听事件，不直接改规则状态。
- 需要阻塞输入的动画，由 `InputLockSystem` 加锁，ActionKit 到点解锁。
- Debug/自动测试模式下 `ISequenceUtility` 可切到立即完成，避免测试等待真实时间。
- 复杂卡牌移动表现只读 `CardMovedEvent` 的 from/to。
- 效果视觉可由 `EffectResolvedEvent`、`DamageAppliedEvent`、`RelicTriggeredEvent` 驱动。

---

## 15. Utility 适配层

### 15.1 Easy Save

```csharp
public interface ISaveUtility : IUtility
{
    bool HasSave(string slot);
    void Save<T>(string key, T data);
    T Load<T>(string key, T defaultValue = default);
    void Delete(string key);
}
```

`SaveSystem` 负责把 Model 打包成 `RunSaveData`，`EasySaveUtility` 只负责 ES3 API 调用。

### 15.2 Yarn

```csharp
public interface IYarnUtility : IUtility
{
    void StartDialogue(string nodeName);
    void StopDialogue();
    bool IsRunning { get; }
    void RegisterCommandHandler(string commandName, Action<string[]> handler);
}
```

Yarn 命令统一转发：

```text
<<give_gold 10>> -> SendCommand(new AddGoldCommand(10, GoldSource.Yarn))
<<open_help_tip first_shop>> -> SendCommand(new OpenPopupCommand(...))
```

### 15.3 Odin 效果资产

Odin 只负责外层可视化，不把 Odin 类型渗透到规则系统：

```csharp
public interface IEffectAssetUtility : IUtility
{
    EffectGraphDefinition LoadGraph(string graphId);
    IEffectAtom CreateAtom(EffectAtomDefinition definition);
}
```

### 15.4 随机数

必须封装，禁止各处直接 `UnityEngine.Random`。

```csharp
public interface IRandomUtility : IUtility
{
    void SetSeed(int seed);
    int Range(int minInclusive, int maxExclusive);
    float Value01();
    void Shuffle<T>(IList<T> list);
    T PickWeighted<T>(IReadOnlyList<WeightedItem<T>> items);
    int ExportState();
    void ImportState(int state);
}
```

理由：playtest 需要 seed 重放、Bug 复现、奖励概率验证。

### 15.5 ResKit / AudioKit / ActionKit 包装

虽然全盘接受 QFramework，但仍建议包一层接口，原因是测试环境和编辑器工具需要替换实现。

- `IResourceUtility`：封装 ResKit 初始化、同步/异步加载。
- `IAudioUtility`：封装 AudioKit 播放、开关、音量。
- `ISequenceUtility`：封装 ActionKit 延时/序列/并行，测试时可立即执行。

---

## 16. 配置校验器

Playtest 期内容多、规则细，必须做编辑器校验。

### 16.1 必做校验

- 所有 `cardId/skillId/relicId/effectGraphId` 唯一。
- 帮助卡同名上限默认 3。
- 帮助卡品质概率总和为 100%。
- 宝箱概率总和为 100%；当前蓝色宝箱写法 `50% + 50% + 10% = 110%`，需要配置层报 warning 并要求修正或归一化。
- 每个帮助卡都有价格、品质、消耗模式。
- 每个怪物的技能 ID 存在。
- 恶魔卡组规则最终数量等于 `9 + X`。
- 第 5 节点固定精英，第 9 节点固定层主。
- 遗物池排除初始遗物。
- 遗物上限 12，宝箱候选不足 3 时要有降级策略。
- 旋转轮文本存在“格5格5”笔误，配置中应明确是否“不移动玩家卡”还是“不移动格 5”。
- 第四版与帮助卡设计对帮助卡组上限口径不同：实现按“同名最多 3，容量按所有卡数量计数”的新版帮助卡设计；保留 `DeckCapacityCountMode` 以便切回“按种类计数”。

### 16.2 Debug 面板

建议做一个 `UIDebugPanel`，只在开发构建打开：

- 当前 seed、Command 序号。
- 当前 Phase、输入锁原因。
- 9 格 uid/cardId/hp/atk/def。
- 战斗牌堆剩余与下一张。
- 帮助卡组快照与当前状态。
- 怪物剩余判断详情。
- 最近 50 条 Event。
- 一键重放、一键胜利、一键生成指定卡。

---

## 17. 测试策略

### 17.1 EditMode 单元测试

优先覆盖纯规则：

- 格 1~9 与 EasyGrid 坐标映射。
- 正交相邻判断。
- 顺/逆时针旋转。
- 开局发牌 3+3+2。
- 多空格批量补牌与牌堆空处理。
- 帮助卡 uid 唯一与节点快照恢复。
- 帮助卡默认永久移除；`restoreAfterNode` 卡节点结束恢复。
- 护甲优先吸收伤害；防御转化为节点开始护甲。
- 伤害 = 攻击 − 伤害减免（非攻击 − 防御）。
- 恶魔卡组生成总数精确等于 `9 + X`。
- 先攻结算顺序。
- 致死不反击。
- 庇佑只免一次伤害。
- 遗物去重与 12 格上限。
- 商店删除帮助卡 +10 金币。

### 17.2 PlayMode 垂直切片测试

- 新局进入第 1 节点，场上 8 张卡 + 玩家。
- 点击邻接怪物，播放战斗、掉血、击杀、补牌。
- 通关后**先**帮助卡三选一，**再**房间按钮；期间仍可使用剩余帮助卡。
- 帮助卡三选一，跳过 +10。
- 商店购买与删除。
- 宝箱三选一与跳过 +20。
- 精英死亡出现导师卡。

### 17.3 种子回放测试

Command 日志格式：

```json
{
  "seed": 123456,
  "commands": [
    { "seq": 1, "type": "ClickBoardSlotCommand", "args": { "slot": 2 } },
    { "seq": 2, "type": "ClickItemSlotCommand", "args": { "index": 0 } }
  ]
}
```

用于复现“吞卡、重复点击、补牌并发、触发死循环”。

---

## 18. 需要暂存为配置项的规则分歧

不阻塞开发，但需要在配置里可切换，避免后期返工。

| 分歧 | 默认实现 | 备注 |
|---|---|---|
| 帮助卡使用后移除语义 | **默认永久移除**；例外「使用后复原」 | `卡组管理#复原规则` 段落仍为旧版，待策划修订。 |
| 帮助卡容量按数量还是种类 | 按所有卡数量计数，同名最多 3 | 帮助卡设计比第四版更细，应优先。 |
| X 是层内节点还是全局节点 | 先按层内节点 1..9 | 可配置公式。 |
| 玩家被效果移动后是否仍“不移动格 5” | 玩家作为 card runtime，移动规则按“玩家卡不参与环形移动” | 旋转轮/普通移动要配置明确。 |
| 初始遗物“村好剑”是否给小鬼 | 默认不给小鬼 | 职业文档写无；遗物文档保留初始遗物定义。 |
| 蓝色宝箱概率 110% | 校验报 warning；配置层先归一化或修正为 40/50/10 | 建议策划修表。 |

---

## 19. 文件命名约定

- Interface：`ICombatSystem`、`IBoardModel`。
- Model 实现：`CombatModel` 不推荐；状态按领域命名，如 `BoardModel`。
- Command：动词开头 + `Command`，例如 `StartCombatCommand`。
- Event：过去式或事实 + `Event`，例如 `DamageAppliedEvent`、`LevelClearReadyEvent`。
- Query：`Get/Can/Has` 开头 + `Query`。
- DTO：`Definition` 静态配置，`Runtime` 运行态，`SaveData` 存档。
- UI：`UIXxxPanel`、`XxxElement`、`XxxView`。
- Anim：`XxxAnimator`，不叫 `System`，避免和 QF System 混淆。

---

## 20. 最小可玩垂直切片范围

第一版能 playtest 的最小闭环：

1. 小鬼职业。
2. 第一层第 1~2 节点怪物池。
3. 九宫格发牌、点击、战斗、旋转、补牌。
4. 帮助卡：恢复药水、飞刀、普通宝箱卡、属性提升卡。
5. 遗物：木盾、木剑、木甲、活着的肉、荆棘甲、凤凰羽毛。
6. 技能：轻车熟路、先攻、黑桃幼崽、红桃幼崽、方块幼崽、梅花幼崽。
7. 通关后帮助卡三选一、跳过、商店、宝箱。
8. Easy Save 安全点保存。
9. Debug 面板和 Command 日志。

完成这个切片后，再扩展完整 27 节点、所有帮助卡、所有怪物技能、全遗物。

---

## 21. 参考来源

- QFramework 文档：https://qf.readthedocs.io/zh-cn/latest/
- QFramework 源码库：https://github.com/liangxiegame/QFramework
- **当前策划文档（主）**：`Assets/Docs/`（Obsidian 九宫牌局同步）
- 同步报告：`docs/design-sync/latest-sync-report.md`
- 历史归档（只作对照，不作实现准绳）：`第四版设计.md` 等旧版单文件

---

## 22. 实现差距核对（2026-06-11）

> 本节对照 `Assets/Docs` 最新设计与 `Assets/Scripts` 当前实现。优先级：**P0 阻断规则正确性，P1 流程/体验偏差，P2 内容未落地**。

### 22.1 P0 — 核心战斗与属性

| # | 设计（文档） | 当前实现 | 差距 |
|---|---|---|---|
| 1 | 四维属性；防御 → 护甲；受伤先扣护甲 | `CardRuntime` 无 `currentArmor`；`ApplyDamageCommand` 直扣 `CurrentHp` | 护甲层完全缺失 |
| 2 | 实际伤害 = 攻击 − **伤害减免** | `CombatSystem.CalculateDamage` = `attack - defender.Defense` | 仍用旧「攻减防」公式，防御被双重使用 |
| 3 | 节点开始护甲 = 有效防御 | `StartNodeCommand` 无护甲初始化 | 节点护甲未填充 |
| 4 | `DamageContext` 区分减免/护甲/无视护甲 | 仅 `ApplyDamageCommand(target, int damage)` | 伤害管线过于扁平，无法表达新公式 |

**建议改动路径**：扩展 `EffectiveStats`（`DamageReduction`、`CurrentArmor`）→ 重写 `CalculateCombatDamage` → 重写 `ApplyDamageCommand` 护甲吸收 → `StartNodeCommand` 填充护甲。

### 22.2 P0 — 节点通关流程顺序

| # | 设计 | 当前实现 | 差距 |
|---|---|---|---|
| 5 | 无怪物 → **立即**帮助卡三选一 → 再房间按钮 | `CheckClearConditionCommand` 直接 `RoomChoiceRequestedEvent` | 房间按钮先于选卡 |
| 6 | 帮助卡三选一在房间事件之前完成 | `ChooseRoomCommand` 内对金币房/属性房再 `GenerateHelpReward` | 选卡绑在房间后，顺序颠倒 |
| 7 | 点击房间后结算未用卡金币 + 房间事件 | 金币结算在 `ChooseRoomCommand` 开头（时机偏早但可接受）；选卡却在房间后 | 整体流程需重排 FSM |

**建议改动路径**：`CheckClearCondition` → `GenerateHelpRewardCommand`；`PickHelpReward`/`SkipHelpReward` 完成后再 `RoomChoiceRequestedEvent`；从 `ChooseRoomCommand` 移除帮助卡三选一逻辑。

### 22.3 P1 — 帮助卡消耗语义

| # | 设计 | 当前实现 | 差距 |
|---|---|---|---|
| 8 | 默认永久移除；例外「使用后复原」 | `CardDefinition.IsPermanentRemoveOnUse` 按卡配置，部分卡未设（默认 false） | 字段语义与默认值均反 |
| 9 | 节点结束仅恢复「使用后复原」卡 | `RestoreHelpDeckSnapshot` 恢复所有非永久移除（`IsTemporarilyRemoved`） | 与新版默认永久移除冲突 |
| 10 | `ConsumeHelpCardCommand` 默认 `permanentlyRemove=true` | 调用处传 `helpDefinition.IsPermanentRemoveOnUse` | 配置驱动旧语义 |

**建议改动路径**：配置改为 `RestoreAfterNode`（默认 false）；`ConsumeHelpCardCommand` 默认永久移除；快照恢复只处理 `restoreAfterNode` 卡。

### 22.4 P1 — 战斗细节

| # | 设计 | 当前实现 | 差距 |
|---|---|---|---|
| 11 | 刺皮与怪物反击同步结算 | `StartCombatCommand` 仅顺序玩家打/怪物打 | 刺皮未实现 |
| 12 | 荆棘甲战斗步骤 4 额外伤害 | 未在战斗管线中 | 未实现 |
| 13 | 怪物先攻时先攻方击杀不反击 | 反击判定有，先攻细节需回归测试 | 待测 |
| 14 | 战斗后旋转+补牌 | `StartCombatCommand` 末尾直接 `RotateClockwise` | 与 `CommitPlayerActionCommand` 路径需统一 |

### 22.5 P2 — 内容与文档

| # | 说明 |
|---|---|
| 15 | `卡组管理.md#复原规则` 与 `帮助卡系统.md` 矛盾，需策划统一后再改配置校验器文案 |
| 16 | 伤害公式移除的「特殊固定伤害」列表：若卡牌仍保留此类效果，应逐卡核对 `07-数据/帮助卡数据` 并走 Effect 管线 |
| 17 | `好战词条` 已从战斗机制删除；代码中无引用，无需改动 |
| 18 | UI：`CardViewData` 需增加护甲显示字段；玩家面板同步 |

### 22.6 建议落地顺序

```text
Phase A（规则地基）
  1. 属性/护甲数据模型 + 节点开始填护甲
  2. 伤害公式重写（减免 + 护甲层）
  3. ApplyDamageCommand 重构

Phase B（流程）
  4. CheckClear → HelpReward → RoomChoice 重排
  5. 帮助卡 restoreAfterNode 语义与快照

Phase C（战斗丰富度）
  6. 刺皮/荆棘甲/并行伤害
  7. 测试与 Debug 面板字段补齐
```

完成 Phase A+B 后，playtest 垂直切片（§20）才算与 2026-06-11 设计案对齐。
