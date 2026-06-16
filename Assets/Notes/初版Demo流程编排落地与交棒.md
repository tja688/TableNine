# 初版 Demo 流程编排落地与交棒

> 落地日期：2026-06-15
> 分支：当前工作分支
> 状态：**主游戏循环地基已跑通并经 PlayMode 实测**；表现层精致化（Dock 手牌 / 删牌 / 战斗碎裂）列为后续波次
> 关联：`当前游戏实现深度拆解.md`（领域层运作）、`卡牌烘焙与显示系统.md`（卡面）、各组件 Demo 笔记

---

## 0. 一句话结论

领域层（Command/Event/Phase/Lock）本就成熟，**真正缺的是「表现接线」**：覆盖层事件没人接、外圈发牌被 skip、棋盘被独立 Demo 视觉接管。本次新增一个**总流程编排器 `GameFlowDirector`**，让领域层成为唯一权威，把"需要玩家做选择"的事件路由到表现组件并回传命令，从而把游戏**完整跑通**：

```
开新局(真实发牌) → 玩家点格(战斗/拾卡/旋转) → 清关 → 三选一帮助卡(BounceCards) → 选房间(按钮) → 下一节点 → … → 失败/胜利(重开按钮)
```

PlayMode 实测链路（execute_code 驱动验证）：
`runActive=True/boardCards=9` → `GenerateHelpReward` → `phase=HelpRewardChoosing, BounceCards 激活并烘焙3张, 跳过按钮显示` → `PickHelpCardReward` → `phase=RoomChoosing, 房间按钮显示, BounceCards 隐藏` → `ChooseRoom(Attribute)` → `phase=PlayerControl, node=2, boardCards=9`。

---

## 1. 核心设计原则（后续务必延续）

- **领域层永远是权威**。所有新表现/交互组件只做两件事：① 听事件演表现；② 玩家操作回传既有 `Command`。**绝不**在表现层私自改战斗/经济/流程，不重复发事件，不绕过 `InputLock`/`Phase`。
- 编排器只"翻译"，不"决策"：`*RequestedEvent / *GeneratedEvent` → 显示组件 → 玩家选 → `Pick*/Choose*/Skip*` 命令。
- 选择决策依据：纯动效=Presenter 监听事件；阻塞演出=`PlayPresentationSequenceCommand`；二段选目标=`PendingHelpCardAction`；模态选择=`OverlayVisible`+对应命令。（详见 `当前游戏实现深度拆解.md` §11）

---

## 2. 新增文件

| 路径 | 职责 |
|------|------|
| `Assets/Scripts/Game/Flow/GameFlowDirector.cs` | **总编排器**。进场开真实局；监听覆盖层事件路由到组件；回传选择命令；GameOver/Victory 重开。`[DefaultExecutionOrder(50)]` |
| `Assets/Scripts/Game/Flow/FlowChoiceButtonOverlay.cs` | 运行期生成的**兜底按钮覆盖层**（屏幕空间 Canvas + `标准Button组件.prefab`）。用于暂无专属卡面的模态选择：选房间 / 宝箱选遗物 / 商店 / 失败胜利重开 / 跳过。支持 `dimBackground`、`anchorBottom`（跳过按钮贴底、不遮挡世界卡）。 |

## 3. 修改文件

| 路径 | 改动 |
|------|------|
| `Assets/Scripts/Game/CardPreview/BounceCardsWorldDemo.cs` | 新增 **choice 模式**（外部驱动多选一）：`mChoiceMode` 开关、`EnsureInitialized()`（幂等初始化，模板走 `BakedCardPrefabRefs` 回退）、`BeginCardChoice(defs, onPicked)`、`HideChoice()`、`BuildCardsFrom(defs)`、`TeardownCards()`。选中后立即回调领域命令、其余卡保留落屏手感。**原 Demo 行为不变**（`mChoiceMode=false` 时仍走随机池演示）。 |

## 4. 场景接线（`TableNineBootstrap.unity`，已保存）

| 对象 | 改动 | 原因 |
|------|------|------|
| `GameplayBootstrap`（挂 `GameplaySceneController`） | **新增 `GameFlowDirector` 组件**；`mBootstrapPlayerCardOnly = false` | 由 Director 统一开真实局，停掉 presenter 的 `skipOpeningDeal` 单独开局 |
| `BounceCardsWorldDemo` | `mChoiceMode = true`，保持 inactive | 转为编排器按需驱动的多选一总成 |
| `NineGridCardMoveDemoRoot` | **停用** | 它是与领域层脱节的独立视觉棋盘（自有牌堆/自旋转/右键自删），与"真实发牌+领域权威棋盘"冲突、且会与 `BoardSlotClickProxy` 双重处理点击 |
| `AgileCardDealerWorldDemo` | **停用** | 旧发牌切片，同理冲突 |
| `DockCardsWorldDemo` | **停用** | 独立假手牌，待后续波次改为事件驱动道具栏（见 §6） |
| `Item CardSlots` | **激活** | 让 `GameplayWorldPresenter` 渲染道具卡 + `ItemSlotClickProxy` 接收点击 → `ClickItemSlotCommand`（拾卡/用卡灰盒链路可用） |

> 棋盘现由领域层 + `GameplayWorldPresenter`（事件驱动，已存在）权威渲染：`BoardCardView1~9` 显示真实发牌结果。

## 5. 覆盖层路由表（GameFlowDirector）

| 领域事件 | 表现 | 玩家操作 → 命令 |
|----------|------|----------------|
| `HelpRewardGeneratedEvent` | BounceCards 多选一 + 底部跳过按钮 | 选卡→`PickHelpCardRewardCommand`；跳过→`SkipHelpRewardCommand` |
| `TutorSkillChoiceRequestedEvent` | BounceCards（`tutor_{skillId}` 卡面） | 选卡→`ChooseTutorSkillCommand` |
| `RoomChoiceRequestedEvent` | 按钮覆盖层 | →`ChooseRoomCommand` |
| `ChestRewardGeneratedEvent` | 按钮覆盖层 + 跳过 | →`PickRelicRewardCommand` / `SkipChestRewardCommand` |
| `ShopOpenedEvent` | 按钮覆盖层（购买+离开） | →`BuyHelpCardCommand` / `CloseShopCommand` |
| `GameOverEvent` / `VictoryEvent` | 按钮覆盖层（重开） | →`StartNewRunCommand` |
| `FlowPhaseChangedEvent==PlayerControl` | 兜底收掉所有覆盖层 | — |

> 房间/宝箱/商店暂用兜底按钮：因为 room/relic **没有烘焙卡面**（`CardDefinition` 之外）。BounceCards 仅用于有卡面的 help/tutor。

## 6. 后续波次路线（本次有意延后，留给你迭代）

按优先级：

1. **棋盘手感回折叠（高）**：当前棋盘为 presenter 瞬切，丢了 `NineGridCardMoveDemo` 的发牌弧线/跳格/落位弹性。建议把这些手感改成**事件驱动**挂到 `GameplayWorldPresenter`：`CardPlacedEvent`→发牌弧线，`BoardRotatedEvent`/`CardMovedEvent`→跳格 tween。`NineGridCardMoveDemo` 里的 `DealNextCardToSlot`/`PlayMoveTweens`/二阶贝塞尔参数可直接迁移复用。
2. **战斗 / 玩家死亡碎裂（高）**：把 `CardFakeShatterEffect` 接到**真实**死亡事件而非右键预览。难点是时序——`KillMonsterCommand` 会先把卡移出棋盘再发 `MonsterKilledEvent`，需在移除前捕获该怪 `BoardCardView{slot}` 的世界位置/精灵快照（建议监听 `CombatStartedEvent` 记录 monsterUid 当前 slot，`MonsterKilledEvent` 时在该位置播碎裂）。玩家死亡：`GameOverEvent` → 碎裂 `BoardCardView5`。**碰撞只对怪物**已天然成立（领域层 `ClickBoardSlotCommand` 只有点怪才进 `StartCombatCommand`；碎裂只挂怪死/玩家死）。
3. **Dock 改事件驱动道具栏（中）**：`DockCardsWorldDemo` 重构为监听 `ItemSlotChangedEvent` 渲染 `DeckModel.ItemSlots`；点击/拖出→`ClickItemSlotCommand`（=用卡，等同 `UseHelpCardCommand`）。飞刀类（`PendingHelpCardAction` 进入 `ThrowingKnifeTarget`）只做"选中目标放大反馈"，真正选目标仍走 `ResolveTargetingCommand`（点棋盘格）。重构方式同 BounceCards 的 choice 模式：加外部驱动 API + 幂等初始化。重构期间灰盒 `Item CardSlots` 可继续兜底。
4. **CircularGallery 接删牌（中）**：商店"删牌"场景用 `CircularGalleryWorldDemo` 承载 `DeckModel.OwnedHelpCards` 列表，选中→`DeleteHelpCardForGoldCommand(uid)`。当前商店仅"购买+离开"按钮，删牌未接。
5. **覆盖层美化（低）**：room/chest/shop 目前是兜底按钮，后续可做专属面板或给 relic/room 配图标。

---

## 7. 已知遗留 / 坑点

- **场景中存在 1 个 "missing script"**（`The referenced script (Unknown) on this Behaviour is missing!`）——既有遗留，非本次引入；建议后续定位清理（可能挂在某停用 Demo 或旧对象上）。
- `BackgroundBlurHost.OnDestroy` 在编辑态调用 `Destroy` 报错（play/stop 切换时）——你新写的 `BackgroundBlur` 组件既有问题，建议 `OnDestroy` 里区分 `Application.isPlaying` 用 `DestroyImmediate`。非本次引入。
- BounceCards choice 模式对 3 张牌使用了 5 张扇形的前 3 个偏移，略偏左（`mBaseOffsetsX`）。纯视觉，后续可按实际张数居中。
- `DebugKillAllMonstersCommand` 只清场上怪、不清战斗牌堆里的怪，因此单独调它不会清关；验证清关请用真实战斗或直接 `GenerateHelpRewardCommand`。
- 重开/失败/胜利兜底按钮用的是 `标准Button组件.prefab`（你留的万用回退），符合"没它就没法玩"的兜底定位。

## 8. 验证记录（2026-06-15）

- `refresh_unity(force/scripts/compile)`：**0 error**。
- PlayMode 实测（execute_code 驱动）：开真实局→`boardCards=9`、HUD `LifeNumber=x14 / CoinNumber=x55 / CharacterName=小鬼` 与模型一致；三选一→选卡→选房间→下一节点全链路通过；运行期仅剩既有 "missing script" 1 条报错。
- 玩家场中心卡（`BoardCardView5`）由 presenter 按 `StatsDirty/DamageApplied/ArmorChanged` 事件刷新；左侧 HUD 血量/金币/技能/遗物本就接好，之前看不到只因未真正开局。

---

## 9. 可玩性深排与修复追加（2026-06-16）

> 触发原因：用户实机打开后反馈「连最基本可操控能力都没有」。本轮重新按真实 PlayMode 路径排查，确认上一轮“流程已通”的结论漏掉了两个会让玩家体感卡死的点。

### 9.1 根因判断

1. **卡面视觉 Collider 吃掉槽位点击**  
   `GameplayWorldPresenter` 运行期生成的 `BoardCardView1~9` / `ItemCardView1~5` 复用了标准卡 prefab，卡面自带 `BoxCollider2D`。这些 collider 与底下 `CardSlot*` 的 collider 完全重叠，但点击代理 `BoardSlotClickProxy` / `ItemSlotClickProxy` 挂在槽位上，不在视觉卡上。玩家自然点击卡面时，物理命中会先打到没有代理的 `BoardCardView`，于是表现为“点了没反应”。

2. **满棋盘无补牌分支会停在 `BoardMoving`**  
   战斗或行动完成后会走 `CommitPlayerActionCommand` → `BoardMoving` → `RequestRefillBoardCommand`。当棋盘没有空位可补时，旧代码直接 `CheckClearConditionCommand` 后返回，没有把 `FlowPhase` 收回 `PlayerControl`。如果该次行动没有杀死怪、没有产生空格，就会停在 `BoardMoving`。

3. **`UIPopupPanel.prefab` 源 Prefab 残留 missing script**  
   场景实例清理后仍会在 Play/Stop 时报 `StaggeredMenu` missing script。最终定位到 `Assets/Prefabs/UI/UIPopupPanel.prefab` 的 Prefab 源，已通过 Unity `PrefabUtility` 移除 1 个 missing MonoBehaviour。

### 9.2 本轮改动

| 文件 | 改动 |
|------|------|
| `Assets/Scripts/Game/GameplaySceneController.cs` | 新增 `DisableVisualCardColliders()`，仅禁用 Presenter 自己生成的棋盘/道具视觉卡 collider，让槽位 collider 成为唯一输入命中目标；不影响 Bounce/Dock/Circular 这些需要卡面 collider 的组件。 |
| `Assets/Scripts/Command/RuntimeCommands.cs` | `RequestRefillBoardCommand` 在 `GetEmptySlots().Count == 0` 分支先 `SetPhase(PlayerControl)`，再检查清关，避免满棋盘行动后卡 `BoardMoving`。 |
| `Assets/Scripts/Tests/PlayMode/TableNineBootstrapPlayModeTests.cs` | 更新 demo 期望：场景应自动开真实局、进入 `PlayerControl`、九格有牌、槽位点击不被 `BoardCardView` collider 阻挡；Console 测试改为只捕获 error/exception/assert。 |
| `Assets/Scripts/Tests/EditMode/TableNineR8PresentationEditModeTests.cs` | 新增满棋盘且怪未死亡时，战斗结算后必须回到 `PlayerControl` 的回归测试。 |
| `Assets/Prefabs/UI/UIPopupPanel.prefab` | 移除 `StaggeredMenu` 上的 missing script。 |
| `Assets/Scenes/TableNineBootstrap.unity` | Unity MCP 修复/保存场景，当前 `validate` clean。 |

### 9.3 验证记录

- Unity 刷新/编译：**0 error**。
- `TableNineR8PresentationEditModeTests`：**6/6 passed**。
- `TableNineBootstrapPlayModeTests`：**3/3 passed**。
- 手动 PlayMode 探针：
  - 开场：`run=True / phase=PlayerControl / locked=False / occupied=9`。
  - 槽位命中：`slot2Hits=1 / visualHit=False`，卡面视觉不再吞点击。
  - 点相邻怪物：点击后仍回到 `PlayerControl`，无锁，满棋盘不再卡 `BoardMoving`。
  - 点相邻帮助卡：道具槽 `0 -> 1`，仍 `PlayerControl` 且无锁。
- 最终 Console：**0 error / 0 warning**。
- `manage_scene(validate)`：`TableNineBootstrap` **clean**。

### 9.4 后续提醒

- 棋盘精致移动/碎裂/Dock 手牌仍按 §6 路线推进；本轮只修 demo 可玩性地基，不强接高风险动效。
- 如果后续把视觉卡改成可拖拽/可悬停对象，必须重新设计“视觉卡输入”和“槽位输入”的权责，避免再次出现两个 collider 抢同一点击的问题。
