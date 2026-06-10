# Plan.md

> 目标：把《ArchitectureDesign.md》拆成可执行的落地阶段。  
> 原则：先跑通规则闭环，再补全内容；先保证 Command/Model/System 可测，再接 UI/Anim；所有阶段都要能编译、能回放、能定位问题。

---

## 总体里程碑

| 里程碑 | 结果 | 验收标准 |
|---|---|---|
| M0：框架落地 | QFramework 架构入口、目录、模块接口、调试日志可用 | 空场景启动无报错；`TableNine` 初始化；Command 拦截能打印日志。 |
| M1：规则沙盒 | 无完整 UI，也能在测试里跑九宫格/卡组/战斗 | EditMode 测试覆盖开局发牌、点击战斗、旋转、补牌、通关判断。 |
| M2：首个可玩节点 | 第一节点可视化打通 | 场上 8 卡 + 玩家；可击杀怪物；可用基础帮助卡；可通关。 |
| M3：奖励闭环 | 通关、房间、选卡、商店、宝箱、导师接入 | 通关后可继续使用帮助卡；房间选择后奖励流程正确；帮助卡快照恢复正确。 |
| M4：一层可玩 | 第一层 9 节点、精英、层主可玩 | 第 5 节点精英、第 9 节点层主；击败后奖励注入正确。 |
| M5：全 playtest | 3 层 27 节点，全部核心卡/怪/技能/遗物上线 | 可完整通关或失败；存档/读档/重放/Debug 面板可用。 |
| M6：打磨与稳定 | 动画、音效、Yarn、错误兜底、数据校验 | 高频 Bug 可通过 seed + Command log 复现；内容表校验无 error。 |

---

## 阶段 0：工程与 QFramework 接入

### 目标

建立 Unity 2022.3 LTS + Built-in + QFramework 的基础工程，形成不会返工的目录、命名、编译边界。

### 任务

1. 导入 QFramework Toolkits 到 `Assets/QFramework`。
2. 建立 `Assets/Scripts` 平铺目录：`Model/System/Command/Event/Query/UI/Game/Utility/Anim/Data/Config/Tests`。
3. 创建 `TableNine.cs`，继承 `Architecture<TableNine>`。
4. 创建 Assembly Definition：
   - `TableNine.Runtime`
   - `TableNine.Tests.EditMode`
   - `TableNine.Tests.PlayMode`
   - 可选：`TableNine.Editor`
5. 创建基础接口空壳：
   - Models：`IRunModel`、`IPlayerModel`、`IBoardModel`、`IDeckModel`、`ICollectionModel`、`IConfigModel`、`IFlowModel`。
   - Systems：`IRunSystem`、`ILevelFlowSystem`、`IBoardSystem`、`IDeckSystem`、`ICombatSystem`、`IStatSystem`、`IEffectSystem`。
   - Utilities：`IRandomUtility`、`ISaveUtility`、`IConfigUtility`、`ISequenceUtility`。
6. 覆盖 `TableNine.ExecuteCommand`，接入 `CommandTraceUtility`。
7. 创建 `GameplayBootstrap`，在场景启动时初始化 ResKit、UIKit、TableNine。

### 产出

- `TableNine.cs`
- 基础目录与 asmdef
- 空模块接口与实现
- `CommandTraceUtility`
- 一个空启动场景

### 验收

- Unity 编译通过。
- 运行空场景，Console 输出 Architecture 初始化完成。
- 发送一个 `DebugPingCommand`，Command 拦截日志能显示 before/after。

---

## 阶段 1：数据定义与配置加载

### 目标

先把策划内容变成稳定 ID 和可校验配置，避免后续逻辑里硬编码中文名。

### 任务

1. 创建基础枚举和值对象：`CardType`、`CardQuality`、`MonsterLevel`、`Suit`、`RoomType`、`FlowPhase`、`CardUid`、`BoardSlotNo`。
2. 创建配置 DTO/ScriptableObject：
   - `CardDefinition`
   - `SkillDefinition`
   - `RelicDefinition`
   - `EffectGraphDefinition`
   - `MonsterDeckRuleDefinition`
   - `CharacterDefinition`
   - `RoomDefinition`
3. 建立第一批配置：
   - 职业：小鬼。
   - 帮助卡：恢复药水、飞刀、普通宝箱卡、属性提升卡。
   - 怪物：无色卡、黑桃2、红桃2、方块2、梅花2。
   - 技能：轻车熟路、先攻、四个幼崽技能。
   - 遗物：木盾、木剑、木甲、活着的肉、荆棘甲、凤凰羽毛。
4. `ConfigModel` 加载并建立索引。
5. `IConfigUtility` 先用 Resources/ScriptableObject 直读；后续切 ResKit。
6. 写配置校验器第一版：ID 唯一、引用存在、概率总和、帮助卡上限、遗物重复。

### 产出

- `Data/Definition` 相关类。
- `Config/` 第一批资产。
- `ConfigModel` 与 `ScriptableConfigUtility`。
- `ConfigValidator`。

### 验收

- 运行时能按 ID 取到小鬼、恢复药水、飞刀、无色卡。
- 校验器能发现不存在的 skillId/effectGraphId。
- 所有配置不通过中文名进行逻辑引用。

---

## 阶段 2：运行态 Model 与 UID 体系

### 目标

建立玩家、卡实例、帮助卡 uid、牌堆、棋盘、流程状态。

### 任务

1. 实现 `CollectionModel`：创建/查询/移除 `CardRuntime`。
2. 实现 `PlayerModel`：金币、技能、遗物、玩家卡 uid、基础属性。
3. 实现 `DeckModel`：
   - 帮助卡组 owned list。
   - 每张帮助卡唯一 uid。
   - 节点开始快照。
   - 战斗牌堆 Queue。
   - 道具牌格 5 格。
4. 实现 `BoardModel`：
   - EasyGrid 3×3。
   - 格 1~9 映射。
   - 玩家位置。
5. 实现 `RunModel` 与 `FlowModel`。
6. 创建 `StartNewRunCommand`：从小鬼配置创建玩家与初始帮助卡组。
7. 创建 EditMode 测试：
   - uid 自增且唯一。
   - 小鬼初始卡组数量与内容正确。
   - 玩家出生格为 5。

### 产出

- 全部核心 Model 初版。
- `CardRuntime`、`HelpCardState`、`DeckRuntime`。
- `StartNewRunCommand`。

### 验收

- 测试中执行新局后，玩家 HP/攻击/防御正确。
- 初始帮助卡组生成 8 张独立 uid。
- 棋盘只有玩家卡在格 5。

---

## 阶段 3：九宫格与开局发牌

### 目标

跑通节点开始、恶魔卡组生成、3+3+2 发牌、下一张预览。

### 任务

1. 实现 `MonsterDeckRuleDefinition` 第一层第 1~2 节点。
2. 实现 `DeckSystem.GenerateDemonDeck`：总数精确等于 `9 + X`。
3. 实现 `StartNodeCommand`：
   - 清理上个节点状态。
   - 生成恶魔卡组。
   - 保存帮助卡组快照。
   - 重置棋盘。
4. 实现 `DealOpeningCardsCommand`：帮助 3、恶魔 3、混洗、再抽 2。
5. 实现 `BattleDeckChangedEvent` 与下一张预览。
6. 实现棋盘事件：`CardPlacedEvent`、`BoardSlotChangedEvent`。
7. 测试：
   - 开局场上 8 张卡 + 玩家。
   - 没有牌发到格 5。
   - 战斗牌堆数量正确。
   - 相同 seed 发牌结果一致。

### 产出

- `DeckSystem` 初版。
- `StartNodeCommand`、`DealOpeningCardsCommand`。
- 发牌测试。

### 验收

- 新局后自动进入第 1 节点并完成开局发牌。
- 测试可复现固定 seed 棋盘布局。

---

## 阶段 4：棋盘点击、移动、补牌、防抖

### 目标

实现九宫格基础交互：邻接判断、点击空格旋转、帮助卡拾取、空格补牌。

### 任务

1. 实现 `CanInteractBoardSlotQuery`。
2. 实现 `ClickBoardSlotCommand` 路由：
   - 空格且正交相邻：行动 + 顺时针旋转。
   - 帮助卡且正交相邻：进入道具牌格。
   - 怪物且正交相邻：暂时只输出战斗入口事件，战斗阶段再接。
3. 实现 `BoardSystem.RotateClockwise/CounterClockwise`。
4. 实现 `PickHelpCardToItemSlotCommand`，道具牌格上限 5。
5. 实现 `RequestRefillBoardCommand` 与 `RefillBoardCommand`。
6. 接入 `ISequenceUtility`，测试模式立即执行，运行模式 ActionKit 延迟。
7. 实现输入锁原因：`BoardRefillRunning`、`BoardMoving`。
8. 测试：
   - 正交相邻与对角相邻判定。
   - 点击空格后环形移动正确。
   - 多空格补牌不吞卡。
   - refillRunning 时多次请求只执行一轮批量补牌。

### 产出

- `BoardSystem`
- `InputLockSystem` 初版
- `ClickBoardSlotCommand`
- `RequestRefillBoardCommand` / `RefillBoardCommand`

### 验收

- 在测试里连续移除多个格子，补牌数量正确。
- 快速连续请求补牌不会让牌堆数量异常。

---

## 阶段 5：战斗与有效属性

### 目标

实现玩家与怪物基础战斗、先攻、死亡、金币、通关判断。

### 任务

1. 实现 `EffectiveStats` 与 `StatSystem`。
2. 实现 `GetEffectivePlayerStatsQuery`、`GetEffectiveMonsterStatsQuery`。
3. 实现 `StartCombatCommand`。
4. 实现 `ApplyDamageCommand`：
   - 普通战斗伤害 = 攻击 - 防御，最低 0。
   - 先不接复杂减伤，只保留扩展点。
5. 实现先攻判断。
6. 实现 `KillMonsterCommand`：
   - 怪物移除。
   - +5 金币。
   - 发送 `MonsterKilledEvent`。
7. 实现 `CheckClearConditionCommand`：棋盘 + 战斗牌堆无怪物时进入 `ClearReady`。
8. 测试：
   - 双方无先攻时玩家先。
   - 怪物被首击击杀不反击。
   - 怪物存活会反击。
   - 伤害最低 0。
   - 怪物死亡 +5 金币。

### 产出

- `CombatSystem`
- `StatSystem`
- 战斗相关 Command/Event/Query

### 验收

- 第 1 节点可通过测试完整击杀所有怪物并进入 `ClearReady`。

---

## 阶段 6：效果系统 MVP 与基础帮助卡

### 目标

建立可扩展效果流水线，并上线第一批帮助卡。

### 任务

1. 定义 `IEffectAtom`、`EffectContext`、`EffectGraphDefinition`。
2. 实现 `EffectSystem.ResolveEffectGraph`。
3. 实现 Atom：
   - `DamageAtom`
   - `HealAtom`
   - `AddGoldAtom`
   - `ModifyStatAtom`
   - `OpenChoiceOverlayAtom`
   - `ConsumeHelpCardAtom`
4. 实现 `UseHelpCardCommand`。
5. 接入帮助卡：
   - 恢复药水。
   - 飞刀。
   - 属性提升卡。
   - 普通宝箱卡先只打开候选，不接完整遗物池。
6. 实现目标选择：
   - 单体目标。
   - 任意怪物。
   - 自身玩家。
7. 测试：
   - 恢复药水永久移除。
   - 飞刀造成 6 伤害。
   - 属性提升正确修改永久属性。
   - 帮助卡道具格使用不受邻接限制。

### 产出

- `EffectSystem` MVP
- 基础 Atom
- 基础帮助卡可用

### 验收

- UI 未完成也能通过 Command 使用帮助卡并改变状态。

---

## 阶段 7：奖励、房间、帮助卡快照恢复

### 目标

完成通关后奖励闭环，确保帮助卡组恢复/永久移除/新增追加不出错。

### 任务

1. 实现 `LevelClearReadyEvent` 后的房间候选。
2. 实现 `ChooseRoomCommand`：
   - 结算未使用帮助卡金币。
   - 恢复节点开始快照。
   - 保留永久移除。
   - 追加新获得卡。
3. 实现帮助卡三选一：
   - 品质概率 65/30/5/0。
   - 跳过 +10 金币。
   - 同名最多 3。
   - 容量按所有卡数量计数，层容量 12/18/24。
4. 实现房间：
   - 金币房：金币卡进入战斗牌组。
   - 宝箱房：宝箱卡进入战斗牌组。
   - 属性房：属性卡进入战斗牌组。
   - 商店：进入商店流程。
5. 实现 `RewardModel` 保存当前候选。
6. 测试：
   - 节点内临时使用的帮助卡恢复。
   - 永久移除不恢复。
   - 未使用帮助卡每张 +10。
   - 跳过选卡 +10。
   - 达到容量/同名上限弹窗且不加入。

### 产出

- `RewardSystem`
- `ChooseRoomCommand`
- 帮助卡奖励流程
- 房间基础流程

### 验收

- 可从第 1 节点通关进入第 2 节点，帮助卡状态正确。

---

## 阶段 8：遗物、宝箱、商店、导师技能

### 目标

完成 playtest 的局外成长与节点间成长内容。

### 任务

1. 实现 `RelicSystem`：
   - 遗物列表。
   - 12 格上限。
   - 去重。
   - 丢弃 +20。
2. 实现宝箱三选一：
   - 普通/蓝色/金色宝箱概率配置。
   - 已拥有遗物排除。
   - 跳过 +20。
   - 满格弹窗。
3. 实现第一批遗物：
   - 活着的肉。
   - 木盾、木剑、木甲套装。
   - 荆棘甲。
   - 凤凰羽毛。
4. 实现 `ShopSystem`：
   - 展示 6 张帮助卡。
   - 购买。
   - 删除帮助卡换金币。
   - 金币不足/容量满提示。
5. 实现导师技能三选一：
   - 精英死亡触发。
   - 玩家技能不可叠加，同类取强/覆盖。
6. 实现第一批玩家技能：轻车熟路、刺皮、硬皮。
7. 测试：
   - 宝箱不会出现已拥有遗物。
   - 遗物满格时不能获得新遗物。
   - 凤凰羽毛阻止一次死亡并移除。
   - 商店删除帮助卡加金币。
   - 轻车熟路触发白色帮助卡三选一。

### 产出

- `RelicSystem`
- `ShopSystem`
- 宝箱/导师奖励流程
- 遗物与技能 MVP

### 验收

- 精英战斗后能拿导师技能；宝箱卡能给遗物；商店可购买/删卡。

---

## 阶段 9：怪物技能与复杂触发系统

### 目标

把所有位置词条、移动触发、光环、召唤、禁止移动纳入统一触发队列。

### 任务

1. 实现 `EffectTriggerQueue`：优先级排序、防递归、触发日志。
2. 实现触发阶段：
   - `OnMoveToSlot`
   - `OnAfterBoardMove`
   - `OnBeforeCombat`
   - `OnModifyDamage`
   - `OnCardRemoved`
   - `OnMonsterKilled`
   - `OnAfterPlayerAction`
3. 实现怪物技能：
   - 黑桃幼崽、红桃幼崽、方块幼崽、梅花幼崽。
   - 尖盾、爱之躯、破防专家、叫人！。
   - 复仇、医疗兵、防护光环、不休追击。
   - 侧方打击、红桃之母、牢不可破、竖向拉杆。
   - 决斗、战舞、暴力、黑桃皇室。
4. 为每个技能写最小测试。
5. 添加 Debug 触发链输出。

### 产出

- `SkillSystem` 完整版
- 怪物技能全覆盖
- 触发队列与防递归

### 验收

- 每个技能至少有一个自动化测试或调试场景验证。
- 红桃之母不会递归召唤红桃之母。
- 决斗锁移动在源卡移除后解除。

---

## 阶段 10：UIKit 可玩 UI

### 目标

把规则沙盒接到完整可玩界面。

### 任务

1. 建立 `UIGameplayPanel`：玩家信息、装备栏、九宫格、道具牌格、技能栏、牌组预览、介绍区。
2. 用 CodeGenKit/ViewController 绑定 UI 控件。
3. 实现 `CardView`、`BoardSlotView`、`ItemSlotView`。
4. 接入事件：
   - 发牌。
   - 移动。
   - 受击。
   - 移除。
   - 属性刷新。
   - 牌组预览。
5. 建立 Overlay：
   - 房间选择。
   - 帮助卡奖励。
   - 商店。
   - 宝箱。
   - 导师。
   - 属性提升。
   - 弹窗。
6. UI 点击全部发 Command。
7. 覆盖层打开/关闭接输入锁。

### 产出

- 可玩 UI 主界面。
- 所有核心 Overlay。
- Popup 系统。

### 验收

- 不打开 Debug 面板也能完整玩完第一层。
- 覆盖层显示时底层棋盘不能点击。
- UI 显示的是有效属性，不是裸值。

---

## 阶段 11：动画、音频、资源加载

### 目标

用 QFramework Toolkits 完成表现层，同时不污染规则主干。

### 任务

1. ResKit：
   - 标记 UI Prefab、Card Prefab、Sprite、Audio。
   - `QFResKitUtility` 接入模拟模式。
2. PoolKit：
   - CardView 池。
   - DamageNumber 池。
   - Popup/Effect 池。
3. ActionKit：
   - 发牌间隔。
   - 补牌延迟 0.45/0.5 秒。
   - 战斗 hit sequence。
   - Overlay 动效。
4. AudioKit：
   - BGM。
   - 点击、战斗、击杀、奖励、错误提示音。
   - 设置界面音量开关。
5. Anim：
   - `CardDealAnimator`
   - `BoardMoveAnimator`
   - `CombatHitAnimator`
   - `DamageNumberAnimator`
   - `OverlayAnimator`
6. 测试模式下 `ISequenceUtility` 可切换为立即执行。

### 产出

- 表现层完整接入。
- 资源加载路径统一。
- 音效/BGM 设置可保存。

### 验收

- 动画期间输入锁正确。
- 关闭动画/快速模式时规则测试仍能跑。
- CardView 被移除后能回收到对象池。

---

## 阶段 12：Easy Save 与 Yarn

### 目标

接入既有团队习惯工具，并保持架构边界。

### 任务

1. `EasySaveUtility` 封装 ES3。
2. `SaveSystem` 生成 `RunSaveData`。
3. 安全点保存：新局、节点开始、节点结束、手动暂停。
4. 读档恢复：
   - 恢复玩家、帮助卡组、技能、遗物、层/节点。
   - 不恢复节点中动画中间态。
5. Save schema version。
6. `YarnDialogueUtility` 封装 DialogueRunner。
7. Yarn 运行时加 `DialogueRunning` 输入锁。
8. Yarn 命令转发 QFramework Command。
9. 做首批教程节点：首次帮助卡、首次宝箱、首次商店、首次精英。

### 产出

- 存档/读档。
- Yarn 基础对话。
- Yarn 命令桥。

### 验收

- 退出再进入可从安全点恢复。
- Yarn 对话期间底层输入被锁。
- Yarn 命令不会直接改 Model。

---

## 阶段 13：完整内容接入

### 目标

把所有 playtest 设计案内容配置化并通过校验。

### 任务

1. 帮助卡 20 张全部接入效果图。
2. 第一层怪物、精英、层主全部配置。
3. 技能全部配置。
4. 遗物全部配置。
5. 每关流程 1~9 节点规则全部配置；扩展到 3 层时复制并调参。
6. 房间奖励与商店池配置。
7. 内容校验器跑通。
8. 给每类复杂效果至少一个测试场景。

### 产出

- 完整 playtest 内容配置。
- 内容校验报告。
- 复杂卡/技能测试场景。

### 验收

- 内容校验无 error；warning 只保留已确认的策划待定项。
- 3 层 27 节点可完整进入，不因缺配置中断。

---

## 阶段 14：Debug、回放、QA 稳定

### 目标

让 playtest 反馈可复现、可定位、可修复。

### 任务

1. `UIDebugPanel`：
   - seed。
   - Command 序号。
   - 当前 phase。
   - 输入锁原因。
   - 棋盘 9 格。
   - 战斗牌堆与下一张。
   - 帮助卡组快照。
   - 最近事件。
2. Command 日志落盘。
3. 种子 + Command list 重放。
4. 异常快照：
   - 当前棋盘。
   - 玩家属性。
   - 牌堆。
   - 输入锁。
   - 最近触发链。
5. 常见断言：
   - 不允许两个卡占同一格。
   - 不允许 uid 丢失。
   - 不允许牌同时在棋盘和牌堆。
   - 不允许输入锁永久残留。
   - 不允许奖励候选为空但 UI 打开。
6. Playtest 快捷键：
   - 一键杀死所有怪。
   - 一键开宝箱。
   - 一键获得指定遗物/技能/帮助卡。
   - 切换快速动画。

### 产出

- Debug 面板。
- 回放系统。
- 异常快照。
- QA 快捷工具。

### 验收

- 任意 playtest Bug 可要求玩家提交 seed + log。
- 本地能重放到相同棋盘状态。

---

## 阶段 15：打包前整理

### 目标

清理 playtest 阻塞项，保证构建稳定。

### 任务

1. WebGL/PC 目标平台确认。
2. 禁用浏览器默认右键菜单，保证遗物右键丢弃可用。
3. ResKit 资源模式确认：Editor 模拟模式、构建模式。
4. Easy Save 路径和清档按钮确认。
5. 错误弹窗兜底。
6. 所有 Debug 工具仅开发构建打开。
7. UI 分辨率适配。
8. 性能检查：
   - CardView 池。
   - DamageNumber 池。
   - 无频繁 LINQ 热路径，尤其是每帧 UI。
   - StatSystem 可接受实时查询；若卡顿再做 AuraCache。
9. 最终内容校验。

### 产出

- Playtest 构建。
- 已知问题清单。
- 复现说明。

### 验收

- 新局、失败、胜利、读档、重开无崩溃。
- 一局内不会出现吞卡、重复补牌、输入锁卡死。

---

## 并行工作建议

### 程序主线

1. 阶段 0~5：规则主干。
2. 阶段 6~9：效果/奖励/技能/遗物。
3. 阶段 10~12：UI/表现/存档/Yarn。
4. 阶段 13~15：全内容与稳定。

### 策划/技术策划主线

1. 把所有卡、怪、技能、遗物转成稳定 ID。
2. 修正概率与规则歧义。
3. 用 Odin 配效果图。
4. 跑配置校验器。
5. 每次内容变更提交校验报告。

### 美术/UI 主线

1. 先出灰盒 UI Prefab。
2. 绑定 ViewController。
3. 替换 CardView 素材。
4. 逐步加动画和音效。

---

## 风险清单与处理策略

| 风险 | 影响 | 处理 |
|---|---|---|
| 补牌并发吞卡 | 核心玩法崩坏 | `refillRunning/refillPending` + Command 测试 + 牌归属断言。 |
| 效果触发递归 | 卡死或无限召唤 | `EffectTriggerQueue`、单 Command 触发上限、触发链日志。 |
| 帮助卡临时/永久移除混乱 | 卡组构筑错误 | uid + 节点快照 + 移除模式枚举 + 测试。 |
| 规则与表现耦合 | 难测、难修 | 规则立即改 Model，Anim 只监听 Event；时间由 `ISequenceUtility` 控制。 |
| 有效属性来源太多 | UI 显示与战斗不一致 | 所有显示和战斗都走 `StatSystem`。 |
| 概率/配置错误 | 奖励异常 | 编辑器校验器强制检查。 |
| UI 覆盖层穿透点击 | 重复战斗/状态错乱 | `OverlayVisible` 输入锁 + UI raycast 阻挡。 |
| 存档保存中间态 | 读档状态不可恢复 | 只在安全点保存；不保存动画/补牌中间态。 |
| 中文名硬编码 | 改名导致逻辑坏 | 稳定 ID，中文只显示。 |

---

## 第一批测试用例清单

### Board

- `SlotNo_To_XY_And_Back_IsCorrect`
- `Orthogonal_Neighbor_Of_Center_Is_2_4_6_8`
- `Diagonal_Is_Not_Interactable`
- `RotateClockwise_Moves_Ring_Correctly`
- `Rotate_Does_Not_Duplicate_Cards`

### Deck

- `NewRun_Creates_Unique_HelpCard_Uids`
- `OpeningDeal_Places_Three_Help_Three_Demon_Two_Battle`
- `Refill_Multiple_Empty_Slots_Draws_Until_Full_Or_DeckEmpty`
- `Refill_Debounce_Does_Not_Double_Draw`
- `HelpDeck_Snapshot_Restores_Temporary_Removed_Cards`
- `HelpDeck_Permanent_Removed_Cards_Do_Not_Restore`

### Combat

- `Combat_Damage_Min_Zero`
- `Player_Attacks_First_When_No_FirstStrike`
- `Player_Attacks_First_When_Both_FirstStrike`
- `FirstStrike_Side_Attacks_First_When_Only_One_Has_FirstStrike`
- `Dead_Monster_Does_Not_CounterAttack`
- `MonsterKill_Adds_5_Gold`

### Rewards

- `HelpReward_Skip_Adds_10_Gold`
- `ChestReward_Skip_Adds_20_Gold`
- `RelicReward_Excludes_Owned_Relics`
- `Relic_Full_Shows_Popup_And_Does_Not_Add`
- `Shop_Delete_HelpCard_Adds_10_Gold`

### Effects

- `Potion_Heals_10_And_Permanently_Removes`
- `ThrowingKnife_Deals_6_Damage`
- `AttributeCard_Attack_Option_Adds_1_Attack`
- `Blessing_Prevents_One_Damage_Only`
- `PhoenixFeather_Prevents_Lethal_And_Removes_Relic`

---

## 内容落地顺序

为了尽快 playtest，不建议按文档顺序全部做完再接 UI。建议顺序：

1. 小鬼 + 初始帮助卡组。
2. 无色卡 + 四个 2 级花色怪。
3. 恢复药水 + 飞刀 + 属性提升卡 + 普通宝箱卡。
4. 基础战斗 + 补牌 + 通关。
5. 帮助卡奖励 + 商店 + 宝箱。
6. 幼崽技能 + 先攻。
7. 第一层完整怪物。
8. 精英 + 导师。
9. 层主 + 胜利。
10. 全帮助卡、全遗物、全技能。

---

## 阶段完成定义

每个阶段完成时必须满足：

- Unity 编译无错误。
- 相关 EditMode 测试通过。
- 没有新增未解释的配置校验 error。
- 关键 Command 有日志。
- 新增 UI 的点击入口只发 Command，不直接改 Model。
- 新增外部库调用只出现在 Utility/UI/Anim，不进入 Model。
- 新增规则至少有一个自动化测试或 Debug 场景。

