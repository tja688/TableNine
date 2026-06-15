# DockCardsWorldDemo 世界空间 Dock 卡牌交互笔记

> 2026-06-15

## 背景

临时 Demo：在 Unity 世界空间（无 UI）复刻 `Frontend_Animation_Re-creation` 项目中的 Dock 放大交互，使用标准 `Card.prefab` + 真实卡牌 SO 烘焙卡面，用于快速验证底部手牌栏的 hover、选中、拖拽与描述联动手感。

当前这份 Demo 已经历两轮交互修复，现阶段重点包含：

1. 鼠标从卡牌上方掠过时，Dock 仍会误触放大。
2. 拖拽需要“先选中再长按”，起手不直观，松手回位也偏硬。
3. 卡牌悬停描述走的是 Demo 内临时 tooltip，没有复用场景现有 `DescriptionText`。
4. 需要一个明确的“可回收区”来区分回手与打出。

## 做了什么

### Dock 核心（参考 DockMagnifyToolbar 思路）

| 交互点 | 当前实现 |
|--------|----------|
| 底部均匀排布 | 按 `mSlotWidthWorld + mItemGapWorld` 计算世界空间槽位 |
| 近距放大 | `mMagnificationPixels / mBaseItemSizePixels` 换算倍率 |
| 悬停抬升 | `mHoverLiftPixels` 转世界单位后走弹簧 |
| 回弹参数 | `SpringMath.Step`，stiffness=150 / damping=12 |
| 拖拽起手 | 长按或轻微位移超阈值都可起拖 |
| 回收区判定 | `OnGUI` 屏幕区块 + 松手时按鼠标屏幕坐标检测 |
| 区内回手 | 进入回收区松手 → 原卡缓动回当前手牌槽位 |
| 区外打出 | 离开回收区松手 → 直接销毁该卡并移出 Dock |
| 打出补位 | 重新计算剩余卡槽中心并均匀补齐 |
| 松手回位 | 起点缓存 + `EaseOutBack` 缓出 + 结束时强制贴回目标位 |

- 默认 **5 张**卡，从 `IConfigModel` 读 Monster / Help / Tutor SO，经 `BakedCardFaceComposer` 烘焙。
- 运行时创建 `DockCardsRoot`，并按视口底部 `mViewportY` 锚点排布。

### Hover 命中修复

之前 Dock 放大只看鼠标与每张卡槽中心的 **X 距离**，因此鼠标即使不在卡牌内部，只要横向靠近也会触发放大。

当前改为两段判定：

1. 先用 `Collider2D.OverlapPoint` 算出 `mHoveredIndex`，只有鼠标真实命中卡牌时才算激活 Hover。
2. 只有 `mHoveredIndex >= 0` 时，才继续用横向距离做相邻卡的放大衰减。

结果是：

- 鼠标在卡牌上方空域不再误触。
- 命中某张卡时，仍保留 Dock 式的邻近联动放大。

### 选中态异常变大修复

之前代码里，选中卡会额外执行一段：

- `targetScale = Mathf.Max(targetScale, 1f + 0.06f)`
- 以及单独的选中抬升逻辑

这会导致卡牌即使不处于 hover，也可能长期保持“比别的卡大一圈”的视觉状态。

本轮修复后：

- 选中态只保留层级标记（sorting boost），方便视觉上知道当前卡是谁。
- 不再给选中卡附加额外 scale。
- 也移除了单独的选中抬升，让形变只来自 hover 与拖拽过程本身。

### 拖拽起手修复

之前逻辑要求：

1. 先点击把卡牌设为 `mSelectedIndex`
2. 再次长按达到 `mLongPressDuration`
3. 才真正 `BeginDrag`

这会让拖拽经常看起来像“按住了但没反应”。

当前改为：

- 鼠标按下时记录 `mPressStartedScreen`
- 在按住阶段满足任一条件即可起拖：
  - 长按达到 `mLongPressDuration`
  - 鼠标位移超过 `mDragStartThresholdPixels`

这样既保留了长按起拖，也支持更符合直觉的“按住轻拖直接起手”。

### 松手回位缓动修复

之前回位是每帧拿“当前位置 → 当前目标位置”做 `Lerp`，视觉上容易偏硬，而且若目标在回程中变化，容易显得不够稳定。

当前改为：

1. 松手时缓存：
   - `ReturnStartPosition`
   - `ReturnStartScale`
2. 回位期间始终使用：
   - 位置：`Vector3.LerpUnclamped(start, target, EaseOutBack(t))`
   - 缩放：`Vector3.LerpUnclamped(startScale, targetScale, easeOutCubic)`
3. 结束帧强制：
   - `Wrapper.position = targetPos`
   - `Wrapper.localScale = targetScale`

结果是：

- 回位有明显的缓出和轻微回弹感。
- 最终落点准确贴回当前槽位，不会差半格。

### 可回收区 / 打出销毁 / 自动补位

这轮补了一个屏幕 GUI 回收区，用来把“回手”和“打出”动作分开：

1. `OnGUI` 按屏幕归一化 `Rect` 绘制 `可回收区 / 拖入回手`
2. 拖拽松手时读取当前鼠标屏幕坐标
3. 若坐标落在回收区：
   - 调 `BeginReturnDrag()`
   - 当前卡沿回位动画返回手牌
4. 若坐标不在回收区：
   - 视为打出
   - 直接销毁该卡 wrapper
   - 从 `mCards` 列表移除
   - 重新计算剩余卡牌排序与槽位

当前“打出”仍是临时 Demo 语义，先用 `Destroy` 处理，没有接正式命令链。

为保证移除后牌列不会留下空洞，打出后会执行：

- `ReindexCards()`：重排 sortingOrder
- `RepositionAllCards()`：按剩余数量重算总宽与居中排布

效果上就是：

- 回收区内松手：回手
- 回收区外松手：打出
- 打出后余牌自动均匀补位

### DescriptionText 描述联动

这次把 Demo 内的世界空间 tooltip 去掉，直接接入场景现有的 `DescriptionText` 逻辑，跟 `RandomCardPreviewDemo`、`CardStackWorldDemo`、`CircularGalleryWorldDemo` 保持同一套体验。

当前行为：

- Hover 卡牌：右侧 `DescriptionText` 显示该卡描述
- 拖拽中：持续显示被拖卡描述
- 回位中：持续显示回位卡描述
- 无目标卡时：回退到 `mDefaultHint`
- 若侧栏悬停（`UIGameplayPanel.IsSidePanelHovered`）：让位给正式侧栏

## 场景接线

| 对象 | 说明 |
|------|------|
| `TableNineBootstrap` / `DockCardsWorldDemo` | 已挂载 Demo 组件 |
| `DescriptionText` | 运行时自动查找并复用 |
| `DockCardsRoot` | 运行时动态创建，不污染常驻层级 |
| `Recycle Zone` | `OnGUI` 运行时绘制，位置由 `mRecycleZoneViewportRect` 控制 |

不需要时可整对象删除，不影响正式流程。

## 相关文件

| 文件 | 说明 |
|------|------|
| `Assets/Scripts/Game/CardPreview/DockCardsWorldDemo.cs` | Dock 排布、hover、拖拽、描述联动 |
| `Assets/Prefabs/Cards/Card.prefab` | 标准世界空间卡 prefab |
| `Assets/Scenes/TableNineBootstrap.unity` | 场景内 `DockCardsWorldDemo` 对象 |
| `Frontend_Animation_Re-creation/Assets/ReplicaV3/Runtime/Effects/DockMagnifyToolbar/ReplicaV3DockMagnifyToolbarEffect.cs` | 参考的 Dock 命中与邻近放大思路 |

## 架构说明

- 走 QFramework `IController`，读 `IConfigModel`，不改领域层。
- 脚本位于 `Assets/Scripts/Game/CardPreview/`，属于 Runtime 程序集。
- 当前仍是表现层临时 Demo，不发正式出牌 / 放置 Command。

## 坑点

1. **Dock 放大不能只看 X 距离**  
   若缺少卡牌实体命中判定，鼠标在上方空域也会触发整排联动。

2. **选中态不要叠加额外 scale**  
   Dock 本身已有 hover magnify，选中再叠一层很容易看起来像“卡牌尺寸错乱”。

3. **回位动画要固定起点**  
   从当前位置反复 Lerp 到动态目标会显得僵，还可能造成终点贴合不稳；最好缓存回位起点并在结束帧强制落位。

4. **描述面板应复用场景现有链路**  
   Demo 自己额外拉一个 tooltip 容易跟 HUD 侧栏描述分裂，后续也不利于统一体验。

5. **打出后必须立即重排手牌索引**  
   只删对象不重算 `mCards` 索引与 `SortingOrder`，会留下视觉空洞，且后续 hover / 选中索引会错位。

## 验证结果

- 已触发 Unity 刷新。
- Console 无新的编译错误。
- 当前仅剩项目内原有的两个未使用字段 warning：
  - `RandomCardPreviewDemo.mFallbackWorldCardHeight`
  - `GameplayWorldPresenter.mFallbackWorldCardHeight`

## 测试步骤

1. Play `TableNineBootstrap`
2. 鼠标从卡牌上方掠过，确认不会误触 Dock 放大
3. 鼠标真正进入卡牌，确认当前卡与邻近卡按 Dock 方式联动放大
4. 按住一张卡轻拖，确认无需“先选中再长按”即可起拖
5. 拖入右上 `可回收区` 松手，确认卡牌带缓动回位，并准确贴回槽位
6. 在回收区外任意位置松手，确认该卡被打出销毁，剩余卡牌自动均匀补位
7. Hover / 拖拽 / 回位时确认右侧 `DescriptionText` 会刷新对应卡描述

## 后续可接

- 选中 / 拖拽状态增加更明确的视觉反馈（边框、高亮、阴影），但不再动卡牌 scale
- 拖拽后接正式出牌或棋盘槽位吸附
- 为 Dock 增加入场动画与音效，和其他卡牌预览 Demo 统一风格
