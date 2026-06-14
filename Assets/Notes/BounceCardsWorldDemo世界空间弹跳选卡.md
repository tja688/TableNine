# BounceCardsWorldDemo 世界空间弹跳选卡笔记

> 2026-06-14

## 背景

临时 Demo：在 Unity 世界空间（无 UI）复刻 `Frontend_Animation_Re-creation` 项目中的 **BounceCards_V3** 交互，使用标准 `Card.prefab` + 真实卡牌 SO 烘焙卡面。

源实现为 UI `RectTransform` + DOTween `DOAnchorPos`（`ReplicaV3BounceCardsEffect.cs`）；本 Demo 改为世界空间 `Transform` + `Collider2D` 命中，并追加**选卡后轰飞落屏 + 选中卡移至槽位**动作。

## 做了什么

### BounceCards 核心（对照 V3 / React Bits）

| 源效果 | Unity 实现 |
|--------|------------|
| 扇形排布 + 初始 Z 旋转 | `mBaseOffsetsX` / `mBaseRotations`（默认 5 张） |
| 入场 `scale:0 → 1` 弹性错峰 | `DOScale` + `OutElastic`，`EntryDelay/Stagger/Duration` |
| 悬停抬升 | `HoverLiftY`，`DOAnchorPos` → `DOLocalMove` |
| 兄弟横向推开 | `HoverPushOffset`，按与悬停索引距离 `HoverSiblingDelayStep` 错峰 |
| 悬停归零旋转 | `DOLocalRotate(Vector3.zero)` |
| 过渡曲线 | `OutBack` + `HoverOvershoot` |

- 默认 **5 张**卡，从 `IConfigModel` 读 Monster / Help / Tutor SO，经 `BakedCardFaceComposer` 烘焙。
- 运行时创建 `BounceCardsContainer`（父于 Demo 根，默认偏移 `(6.5, 0.5, 0)`）。

### 描述面板

- 悬停卡牌 → `CardPreviewDescriptionComposer` 写入右下角 `DescriptionText`。
- 侧栏悬停时让位（`UIGameplayPanel.IsSidePanelHovered`）。
- 无悬停时显示默认提示文案。

### 选卡动作

1. **点击悬停中的卡牌** → 锁定交互（`mSelectionLocked`）。
2. **未选中卡**：抛物线物理轨迹轰出屏幕下方后 `SetActive(false)`。
3. **选中卡**：延迟后 `DOMove` 至 `Target Slot`（默认 `NineGrid Main CardSlots/CardSlot1`），旋转收平。

落屏运动（单段连续曲线，非分段直线）：

```
x(t) = x₀ + vₓ·t
y(t) = y₀ + vᵧ₀·t - ½g·t²
```

- `vᵧ₀` = `FallLaunchUpward`
- `g` = `FallGravity`
- 横向射程 = `FallHorizontalReach` + 距选中索引 × `FallHorizontalReachStep`
- 落点 Y 由相机 `ViewportToWorldPoint(0.5, -0.2)` + `FallBelowScreenPadding` + 半卡高计算
- 时间轴 `Ease.Linear` 采样，保证弧线速度连续

### 与 CardStackWorldDemo 场景分工

| Demo | 状态 | 说明 |
|------|------|------|
| `BounceCardsWorldDemo` | **启用** | 扇形弹跳 + 选卡轰飞 |
| `CardStackWorldDemo` | **禁用** | 避免两套右侧演示叠在一起 |

## 场景接线

| 对象 | 说明 |
|------|------|
| `TableNineBootstrap` / `BounceCardsWorldDemo` | 已挂载 Demo 组件，Play 即可 |
| `Container Local Offset` | 卡群默认 `(6.5, 0.5, 0)` |
| `Target Slot` | 已绑定 `NineGrid Main CardSlots/CardSlot1` |

不需要时可整对象删除，不影响正式流程。

## 相关文件

| 文件 | 说明 |
|------|------|
| `Assets/Scripts/Game/CardPreview/BounceCardsWorldDemo.cs` | 弹跳、选卡、落屏、描述 |
| `Assets/Prefabs/Cards/Card.prefab` | 标准世界空间卡 prefab |
| `Assets/Scenes/TableNineBootstrap.unity` | 场景内 `BounceCardsWorldDemo` 对象 |
| `Frontend_Animation_Re-creation/.../ReplicaV3BounceCardsEffect.cs` | UI 版源参考 |
| `Frontend_Animation_Re-creation/.../BounceCards.jsx` | React Bits 原始参考 |

## 架构说明

- 走 QFramework `IController`，读 `IConfigModel`，与 `RandomCardPreviewDemo` / `CardStackWorldDemo` 同级。
- 脚本位于 `Assets/Scripts/Game/CardPreview/`（Runtime 程序集，勿放 `Tests/PlayMode`）。
- 表现层自包含，不修改 `GameplayWorldPresenter`；选中卡仅视觉对齐槽位，不发 `PlaceCard` Command。

## 迭代记录（手感调优）

1. **初版落屏** — 垂直 `InQuad` 下落，易显平淡。
2. **轰飞版** — 两段 `DOMove`（弹峰值 → 落点）+ `DOPunchScale`，出现「快速跳几个点再慢慢掉」的不自然感。
3. **中途消失** — 曾用 `DOScale(0)`，卡牌掉到一半就缩没；已移除，改为完全出屏后再隐藏。
4. **当前版** — 单段抛物线物理采样，横向匀速 + 纵向重力，旋转随进度 `LerpAngle`，弧线连贯。

## 坑点

1. **槽位 scale 继承**  
   `CardSlot1` 节点约有 **1.82×** scale。选中卡只同步槽位 **world position**，不 parent 到槽位，避免卡牌变大。

2. **落点必须用相机视口算**  
   固定 `-Y` 偏移在不同分辨率/相机下可能未出屏；`GetFallOffScreenTarget` 用 `ViewportToWorldPoint` 保底。

3. **命中顺序**  
   悬停/点击从 `mCards` 尾部向前扫（高 sorting 优先），与 UI 版从顶层子节点检测一致。

## 测试步骤

1. Play `TableNineBootstrap`
2. 右侧 5 张卡弹性入场 → 悬停一张，确认兄弟推开 + 右下描述更新
3. 点击悬停卡 → 其余沿弧线轰出屏幕下方（不中途缩没），选中卡飞到 `CardSlot1`
4. Console 应有 `[BounceCardsWorldDemo] Selected '...' -> CardSlot1.` 日志

## 可调参数速查

| 分组 | 字段 | 作用 |
|------|------|------|
| Hover | `HoverPushOffset` / `HoverLiftY` | 推挤距离、抬升高度 |
| Entry | `EntryDelay` / `EntryStagger` | 入场错峰 |
| Fall | `FallLaunchUpward` / `FallGravity` | 弧线高度与下落加速 |
| Fall | `FallHorizontalReach` | 横向轰飞力度 |
| Fall | `FallStaggerStep` | 相邻卡出发延迟 |
| Selection | `MoveToSlotDuration` | 选中卡飞向槽位时长 |

## 后续可接

- 选中后写领域层（`CreateCard` + `PlaceCard`）与 `CardStackWorldDemo` debug 路径对齐
- 轰飞音效 / 选中确认音效（`IAudioUtility`）
- 与正式选牌 UI 流程合并或互斥策略
- 重置 / 再发一轮（当前 `mSelectionLocked` 后不可重开）
