# NineGridCardMoveDemo 九宫格外圈移动效果

> 状态：临时测试脚本已挂在 `Assets/Scenes/TableNineBootstrap.unity` 的 `NineGridCardMoveDemoRoot`。
> 用途：为后续九宫格卡牌移动、入场、跳格、位移触发、连锁表现搭建一个可调试的世界空间效果底座。

## 1. 使用方式

进入 `TableNineBootstrap` 场景并 Play：

| 输入 | 行为 |
| --- | --- |
| `1` | 在九宫格外圈第一个空位生成一张正式烘焙卡牌 |
| `2` | 所有由 Demo 生成的卡牌沿外圈顺时针移动一格 |

生成顺序使用设计案里的九宫格外圈顺序：

```text
1 -> 2 -> 3 -> 6 -> 9 -> 8 -> 7 -> 4 -> 1
```

当前生成策略默认是怪物卡 / 帮助卡交替生成。外圈 8 格满后不再生成，避免无意覆盖已有卡牌。

## 2. 相关文件

| 文件 | 作用 |
| --- | --- |
| `Assets/Scripts/Game/CardPreview/NineGridCardMoveDemo.cs` | 测试入口：按键、生成卡牌、外圈移动、DOTween 跳格动画 |
| `Assets/Scripts/Game/NineGridOuterRingUtility.cs` | 外圈顺序工具：取第 N 个外圈格、取顺时针下一格、找第一个空位 |
| `Assets/Scripts/Tests/EditMode/NineGridOuterRingUtilityEditModeTests.cs` | 外圈顺序与满格行为测试 |
| `Assets/Scenes/TableNineBootstrap.unity` | 已通过 Unity MCP 挂载 `NineGridCardMoveDemoRoot` |

## 3. 生成管线

这个 Demo 没有用临时文字卡，也没有直接拼 SpriteRenderer，而是走项目当前的正式卡牌显示链路：

```text
CardDefinition
  -> BakedCardRenderDataFactory.CreateFromCardDefinition()
  -> BakedCardFaceComposer.ComposeSet()
  -> Card.prefab / CardView
  -> CardView.ShowBaked()
```

实现细节：

1. `Start()` 时初始化 `BakedCardFaceComposer`，并从 `IConfigModel` 读取怪物卡池与帮助卡池。
2. `SpawnNextCard()` 先通过 `NineGridOuterRingUtility.TryGetFirstEmptySlot()` 找外圈第一个空位。
3. `ResolveNextDefinition()` 根据 `mAlternateMonsterAndHelp`、`mFirstSpawnType`、`mMonsterCardId`、`mHelpCardId` 决定生成哪张卡。
4. `CreateCardEntry()` 创建一个外层 wrapper：`MoveDemo_{slot}_{type}_{cardId}`。
5. wrapper 下实例化 `Assets/Prefabs/Cards/Card.prefab`，挂载/初始化 `CardView`。
6. 使用烘焙结果 `BakedCardSpriteSet` 调用 `cardView.ShowBaked(sprites)`。
7. 所有子 SpriteRenderer 会被设置到 `Cards_Front` sorting layer，避免被棋盘槽位遮挡。

注意：这里的卡牌只属于 Demo 自己的表现层占位表 `mCardsBySlot`，没有写入 `BoardModel`、`CollectionModel` 或正式战斗牌堆。因此它适合做表现测试和未来效果打样，不会污染当前规则流。

## 4. 槽位定位

Demo 默认查找场景对象：

```text
NineGrid Main CardSlots
```

外圈槽位按子节点名查找：

```text
CardSlot1
CardSlot2
CardSlot3
CardSlot6
CardSlot9
CardSlot8
CardSlot7
CardSlot4
```

格 5 是玩家格，当前不参与外圈移动。代码里保留了 `CardSlot5ForPlayer` 的查找兼容，但外圈工具不会返回 5。

`mCardWorldOffset` 默认为 `(0, 0, -0.02)`，用于让 Demo 卡牌在世界空间深度上略微贴近镜头。主要可见性依赖 sorting layer：`Cards_Front`。

## 5. 移动动画

按 `2` 时执行 `MoveClockwiseOneStep()`：

1. 遍历外圈顺序，收集当前已生成卡牌。
2. 每张卡计算目标格：`NineGridOuterRingUtility.GetNextClockwiseSlot(fromSlot)`。
3. 先更新 `mCardsBySlot` 到新槽位，随后播放表现动画。
4. 每张卡的 wrapper 通过 DOTween 同时执行位置曲线和可选缩放。

位置不是直接 `DOMove` 直线，而是 `DOTween.To(0 -> 1)` 驱动二阶贝塞尔：

```text
start -> control -> target
```

当 `mEnableHopArc = true` 时，控制点会加 `Vector3.up * mHopArcHeight`，形成轻微抛物线，让卡牌像“跳格子”。

缩放由 `mEnableHopScale` 控制：

```text
BaseScale -> BaseScale * mHopScaleMultiplier -> BaseScale
```

实现上使用 `DOScale(...).SetLoops(2, LoopType.Yoyo)`，和位移动画 `Join` 到同一个 sequence。

## 6. 可调参数

`NineGridCardMoveDemoRoot` Inspector 暴露了以下重点参数：

| 参数 | 默认值 | 说明 |
| --- | --- | --- |
| `mSpawnKey` | `Alpha1` | 生成卡牌按键 |
| `mMoveKey` | `Alpha2` | 外圈移动按键 |
| `mClearKey` | `None` | 可手动指定清空按键 |
| `mBoardRootName` | `NineGrid Main CardSlots` | 自动查找的棋盘槽位根节点 |
| `mCardWorldOffset` | `(0, 0, -0.02)` | 生成卡牌的世界偏移 |
| `mWorldCardHeight` | `BakedCardRenderDataFactory.CanonicalWorldCardHeight` | 卡牌世界高度 |
| `mSortingLayerName` | `Cards_Front` | 生成卡牌的 sorting layer |
| `mBaseSortingOrder` | `560` | 生成卡牌基础 order |
| `mAlternateMonsterAndHelp` | `true` | 是否怪物 / 帮助交替生成 |
| `mFirstSpawnType` | `Monster` | 第一张生成类型 |
| `mMonsterCardId` | 空 | 指定怪物卡 ID；为空则从怪物池轮询 |
| `mHelpCardId` | 空 | 指定帮助卡 ID；为空则从帮助卡池轮询 |
| `mMoveDuration` | `0.34` | 单格移动时间 |
| `mMoveEase` | `InOutSine` | 位移缓动 |
| `mEnableHopArc` | `true` | 是否启用弧线跳格 |
| `mHopArcHeight` | `0.18` | 弧线高度 |
| `mEnableHopScale` | `true` | 是否启用放大缩小 |
| `mHopScaleMultiplier` | `1.08` | 跳格放大倍率 |
| `mHopScaleEase` | `OutQuad` | 缩放缓动 |
| `mDestroySpawnedCardsOnDestroy` | `true` | Demo 销毁时是否清理生成卡 |

## 7. 后续扩展建议

### 7.1 从测试表现过渡到正式规则表现

当前 Demo 自己维护 `mCardsBySlot`，适合快速调手感。后续正式接入时建议让规则层负责真实位置变化：

```text
BoardSystem.RotateClockwise()
  -> CardMovedEvent / BoardRotatedEvent
  -> 表现层根据事件播放移动动画
  -> 动画结束后刷新/解锁输入
```

这样可以避免表现层和 `BoardModel` 的位置状态分叉。

### 7.2 抽出可复用移动 Animator

如果后续要做很多移动类效果，可以把 `PlayMoveTweens()` 抽成类似：

```text
NineGridCardMoveAnimator
```

可输入：

| 输入 | 说明 |
| --- | --- |
| 卡牌 Transform | 要移动的视觉节点 |
| 起点 / 终点 | 世界坐标 |
| MovementProfile | 位移时长、Ease、弧线、缩放、旋转、音效 |
| onComplete | 单张卡结束回调 |

这样普通外圈移动、拉杆换位、击退、吸附、入场落位都能共用同一套手感参数。

### 7.3 增加移动阶段事件

后续如果要叠很多表现，可以考虑每张卡移动时拆出阶段：

```text
BeforeMove
MoveStart
MoveApex
MoveLand
MoveComplete
```

适合挂：

| 阶段 | 可搭效果 |
| --- | --- |
| `MoveStart` | 起跳音效、卡牌轻微压缩、拖影开启 |
| `MoveApex` | 高点闪光、技能预警、路径提示 |
| `MoveLand` | 落地音效、尘点、槽位震动、数字飘字 |
| `MoveComplete` | 触发位置技能、刷新选中状态、检查连锁 |

### 7.4 移动阻塞与输入锁

Demo 里用 `mIsMoving` 防止移动过程中重复按键。正式流里建议接 `IInputLockSystem` 或表现序列：

```text
PlayPresentationSequenceCommand(PresentationSequenceType.BoardRotation)
```

当前正式架构已有 `ISequenceUtility` 与 `PresentationSequenceRequestedEvent`，后面做规则驱动动画时可以复用这条线。

### 7.5 Sorting Layer 约定

目前生成卡统一设置：

```text
sortingLayerName = Cards_Front
sortingOrder = mBaseSortingOrder + mSpawnCount
```

后续如果做拖拽、选中、飞行卡牌，建议临时切到：

```text
Cards_Drag
```

落位后再回到：

```text
Cards_Front
```

这比单纯拉高 `sortingOrder` 更稳定，尤其是卡牌跨 UI / 世界物体 / 特效层的时候。

## 8. 当前边界

1. Demo 生成的卡不进入正式 `BoardModel`，所以不会触发正式 `CardMovedEvent`、怪物技能、帮助卡被动。
2. Demo 不检查场景中已有正式棋盘卡，只检查自己生成过的卡。
3. 清空逻辑只清自己维护的 `mCardsBySlot`。
4. `mMonsterCardId` / `mHelpCardId` 如果填错或类型不匹配，会回退到对应卡池轮询。
5. `BakedCardFaceComposer` 会创建隐藏相机进行运行时烘焙；频繁大量生成时应考虑缓存烘焙结果。

## 9. 推荐下一步

如果继续在这个基础上搭效果，建议优先做三件事：

1. 把 `PlayMoveTweens()` 抽成可复用 Animator，参数做成独立 Profile。
2. 增加移动落地阶段回调，用于挂落地音效、槽位闪烁、技能触发预告。
3. 做一个“正式 BoardRotatedEvent -> 表现移动 -> 完成回调”的桥接版本，让当前手感进入正式战斗流程。
