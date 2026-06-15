# NineGridCardMoveDemo 九宫格发牌与外圈跳格整合

> 状态：整合演示已挂在 `Assets/Scenes/TableNineBootstrap.unity` 的 `NineGridCardMoveDemoRoot`。
> 用途：把原 `NineGridCardMoveDemo` 的外圈跳格手感与 `AgileCardDealerWorldDemo` 的牌堆发牌手感合并到同一个世界空间演示流。

## 1. 使用方式

进入 `TableNineBootstrap` 场景并 Play：

| 输入 | 行为 |
| --- | --- |
| `1` | 从牌堆连续发牌到九宫格外圈全部空位，按正式外圈顺序 `1 -> 2 -> 3 -> 6 -> 9 -> 8 -> 7 -> 4`，跳过格 5 玩家格 |
| 鼠标左键点击任意已落位卡牌 | 外圈所有已落位卡牌顺时针跳动一格 |
| 鼠标右键点击任意已落位卡牌 | 先移除该卡；若牌堆未空，立刻发一张牌补该格；补位动画完成后再顺时针跳动一格 |
| 牌堆为空后，鼠标左键点击任意外圈空格 | 外圈已有卡牌仍可正常顺时针跳动一格 |

当前已清理旧测试快捷键：不再使用 `2` 触发移动，不再暴露清空测试键。旧 `AgileCardDealerWorldDemo` 检测到整合版 `NineGridCardMoveDemo` 存在时会自动停用，避免继续处理“鼠标点槽位发牌”的旧切片输入。

## 2. 正式规则对齐

本演示按以下设计文档收束：

| 设计点 | 对应实现 |
| --- | --- |
| 玩家卡固定在格 5 | 发牌只处理外圈 8 格，永不向 `CardSlot5ForPlayer` 发牌 |
| 外圈顺时针移动 | 使用 `NineGridOuterRingUtility` / `BoardSlotUtility.ClockwiseRing` 的顺序 |
| 出现空格时优先补牌 | 右键移除后，若牌堆仍有牌，补位动画完成后才进入旋转 |
| 战斗卡组耗尽后允许空格 | 牌堆为空时不再补位，空格点击仍可触发旋转表现 |
| 补牌防并发 | `mIsSequencing` 阻塞输入，发牌、补位、旋转串行完成 |

## 3. 相关文件

| 文件 | 作用 |
| --- | --- |
| `Assets/Scripts/Game/CardPreview/NineGridCardMoveDemo.cs` | 整合入口：牌堆生成、按 `1` 批量发外圈、鼠标左/右键交互、补位后旋转、DOTween 跳格 |
| `Assets/Scripts/Game/CardPreview/AgileCardDealerWorldDemo.cs` | 旧发牌切片保留；整合版存在时自动静默 |
| `Assets/Scripts/Game/NineGridOuterRingUtility.cs` | 外圈顺序工具：外圈判断、取顺时针下一格、找第一个空位 |
| `Assets/Scripts/Tests/EditMode/NineGridOuterRingUtilityEditModeTests.cs` | 外圈顺序与满格行为测试 |
| `Assets/Prefabs/Cards/Card.prefab` | 标准世界空间卡承载 |
| `Assets/Scenes/TableNineBootstrap.unity` | 演示挂载场景，不手动编辑 `.unity` 文本 |

## 4. 生成与发牌管线

整合版仍走正式卡牌烘焙链路：

```text
CardDefinition
  -> BakedCardRenderDataFactory.CreateFromCardDefinition()
  -> BakedCardFaceComposer.ComposeSet()
  -> Card.prefab / CardView
  -> CardView.ShowBaked()
```

启动后默认生成 20 张牌堆数据，只显示顶牌。按 `1` 后从顶牌开始连续发入外圈空位，飞行动画使用原灵动发牌器的二阶贝塞尔弧线、落位旋转归正、缩放弹性反馈，并更新 `DeckLeftText`。

## 5. 跳格与补位顺序

外圈跳格只移动当前已落位卡牌，空格会保留为空：

```text
1 -> 2 -> 3 -> 6 -> 9 -> 8 -> 7 -> 4 -> 1
```

右键移除的顺序固定为：

```text
右键命中卡牌
  -> 移除该卡
  -> 若牌堆未空，发顶牌补入该格
  -> 补位动画完成
  -> 外圈顺时针跳动一格
  -> 解锁输入
```

这样不会出现补位和跳格同时抢同一张卡 Transform 的情况。

## 6. 当前边界

1. 这仍是世界空间表现演示，卡牌占位由 `NineGridCardMoveDemo` 自己维护，尚未写入正式 `BoardModel`。
2. 发牌顺序已按正式外圈顺序走，不包含开局“玩家侧 3 张 / 恶魔 3 张 / 战斗卡组 2 张”的规则层抽牌拆分。
3. 右键补位使用当前演示牌堆顶牌，牌堆为空后保留永久空格。
4. 空格点击只在牌堆为空后触发，避免与“有牌必补”的正式补牌规则冲突。
5. `AgileCardDealerWorldDemo` 作为旧切片保留，但在整合演示启用时不再参与输入。

## 7. 手感参数

2026-06-15 调快版基准：

| 参数 | 当前值 | 调整方向 |
| --- | --- | --- |
| `mPreDealAimDuration` | `0.08` | 顶牌发射前朝目标空位快速对位；越小越干脆，`0` 为取消预瞄 |
| `mDeckRelayoutDuration` | `0.09` | 牌堆顶牌切换/整理速度；越小越利落 |
| `mDealDuration` | `0.32` | 发牌飞行时长；想更快可试 `0.26 ~ 0.30`，想更有重量可回到 `0.38` |
| `mDealArcHeight` | `0.55` | 发牌弧线高度；越低越像弹射直达，越高越飘 |
| `mLandingRotationOvershoot` | `1.55` | 落位回正的回弹幅度；越大越弹，但过大会显得晃 |
| `mDealScaleMultiplier` | `1.11` | 发牌瞬间放大反馈；越大越“啪” |
| `mMoveDuration` | `0.28` | 外圈跳格时长；越小越爽快，低于 `0.22` 容易像瞬移 |
| `mHopArcHeight` | `0.16` | 跳格小弧线；越低越贴地，越高越轻飘 |

旧 `AgileCardDealerWorldDemo` 的悬停指向也同步调快：`mAimSpringStiffness = 190`、`mAimSpringDamping = 21`、`mMaxAimAngularSpeed = 1680`。如果之后临时单独启用旧切片，想让对位更凌厉就继续加 `mAimSpringStiffness` 和 `mMaxAimAngularSpeed`；如果出现来回抖，再加一点 `mAimSpringDamping`。

## 8. 验证记录

2026-06-15 整合记录：

1. 合并发牌器 notes 与九宫格移动 notes 到本文档。
2. 清理旧测试快捷键说明：`2` 移动键与清空键不再是当前演示入口。
3. 代码侧由 `mIsSequencing` 串行阻塞右键“移除 -> 补位 -> 跳动”的顺序。

2026-06-15 手感调整：

1. 发牌时长从 `0.54` 调到 `0.32`，弧线从 `0.8` 调到 `0.55`。
2. 新增发牌前 `0.08` 秒快速对位旋转。
3. 外圈跳格从 `0.34` 调到 `0.28`。
4. 通过 Unity MCP 同步并保存 `TableNineBootstrap` 场景实例参数。
