# CardStackWorldDemo 世界空间堆叠交互笔记

> 2026-06-14

## 背景

临时 Demo：在 Unity 世界空间（无 UI）复刻 [React Bits Stack](https://reactbits.dev/) 卡牌堆叠交互，使用标准 `Card.prefab` + 真实卡牌 SO 烘焙卡面，用于快速验证拖拽手感、槽位放置与描述联动。

## 做了什么

### 堆叠核心（React Stack 对照）

| React 属性 | Unity 实现 |
|------------|------------|
| `randomRotation` | 每张卡 ±5° 随机 Z 偏移 |
| `sensitivity` | 按卡高换算 `mDragThreshold`，超阈值可送到底 |
| `sendToBackOnClick` | 未命中槽位松手时顶卡送到底层 |
| `animationConfig` | 弹簧 stiffness=260 / damping=20 |
| 拖拽 3D 倾斜 | DragLayer 上 X/Y 倾斜（±60°） |
| 扇形堆叠 | VisualPivot 绕右下 pivot 做 Z 旋转 + 缩放 |

- 默认 **6 张**卡，从 `IConfigModel` 读 Monster / Help / Tutor SO，经 `BakedCardFaceComposer` 烘焙。
- 运行时创建 `CardStackRoot`（堆叠区）与 `PlacedCardsRoot`（已放置卡，避免继承槽位 scale）。

### 单槽位关联放置 + Debug

- Inspector **`Linked Slot`**：手动拖入任意 `NineGrid Main CardSlots` 下槽位 Transform。
- **`Linked Board Slot No`**：对应棋盘格编号（debug 写领域层 / 发 Command 用）。
- **`Linked Slot Snap Distance`**：吸附判定半径（默认 1.2）。
- 松手在槽位上 → 对齐槽位 world position 放置，并可选：
  - `StartNewRunCommand`（`Auto Start Run For Debug`）
  - `CreateCard` + `PlaceCard`（槽空时）
  - `ClickBoardSlotCommand`（`Trigger Board Slot Command On Place`）
- 未指定 `Linked Slot` 时回退 `NineGrid Main CardSlots/CardSlot3`。

### 离槽倾斜

- **不在关联槽位上**拖动时：DragLayer 弹簧倾斜（堆叠顶卡 + 槽位已放卡均适用）。
- **进入关联槽位范围**时：倾斜收平，便于对齐。
- 堆叠内拖动：扇形旋转 + 倾斜同时生效；从槽位拖出：卡面保持平整，仅 DragLayer 倾斜。

### 拖出槽位回堆叠

- 已放置卡可再拖；松手**不在**关联槽位 → 缓动回堆叠**最顶**（`Return To Stack Duration`，默认 0.22s）。
- 回位用 Lerp，不走倾斜弹簧；扇形角度/缩放线性插值。

### 描述面板

- 悬停顶卡 / 槽位上的卡 / 拖拽中 → `CardPreviewDescriptionComposer` 写 `DescriptionText`。
- 侧栏悬停时让位（`UIGameplayPanel.IsSidePanelHovered`）。

## 场景接线

| 对象 | 说明 |
|------|------|
| `TableNineBootstrap` / `CardStackWorldDemo` | 已挂载 Demo 组件，Play 即可 |
| `Stack Local Offset` | 堆叠区默认 `(6.5, 0.5, 0)` |
| `Linked Slot` | 在 Inspector 指定目标槽位 |

不需要时可整对象删除，不影响正式流程。

## 相关文件

| 文件 | 说明 |
|------|------|
| `Assets/Scripts/Game/CardPreview/CardStackWorldDemo.cs` | 堆叠控制器、槽位/debug/描述 |
| `Assets/Scripts/Game/CardPreview/CardStackWorldCard.cs` | 单卡拖拽、倾斜、放置、回堆叠动画 |
| `Assets/Prefabs/Cards/Card.prefab` | 标准世界空间卡 prefab |
| `Assets/Scenes/TableNineBootstrap.unity` | 场景内 `CardStackWorldDemo` 对象 |

## 架构说明

- 走 QFramework `IController`，读 `IConfigModel` / 发 Command，与 `RandomCardPreviewDemo` 同级，属 **Runtime 程序集**（勿放 `Tests/PlayMode` asmdef，否则 Play 模式挂不上组件）。
- 表现层自包含，不修改 `GameplayWorldPresenter` 正式棋盘卡逻辑；debug 放置仅通过 Command 触发现有流程。

## 坑点

1. **放置后卡牌变大**  
   曾 parent 到 `NineGrid Main CardSlots` 槽位节点，继承了约 **1.82×** scale。  
   修复：parent 到 `PlacedCardsRoot`，只同步槽位 **world position**，`localScale = 1`。

2. **拖拽无倾斜**  
   `Tick` 的 `mDragging` 分支只更新了位置，未每帧 `ApplyVisualTransform` / `ApplyDragTilt`。  
   修复：拖拽分支内显式应用倾斜；`SetDragTarget` 对所有拖拽态更新 tilt 弹簧。

3. **PlayMode 程序集**  
   脚本若放在 `Assets/Scripts/Tests/PlayMode/`，`autoReferenced: false` 导致场景无法挂载。已迁到 `Assets/Scripts/Game/CardPreview/`。

4. **与 RandomCardPreviewDemo 分工**  
   - `RandomCardPreviewDemo`：单卡 Tab 切卡 + 悬停描述（`CardSlot1`）  
   - `CardStackWorldDemo`：多卡堆叠 + 槽位/debug 实验（独立对象，可删）

## 测试步骤

1. Play `TableNineBootstrap`
2. 在棋盘右侧堆叠区拖顶卡 → 确认扇形 + **离槽倾斜**
3. 拖到 Inspector 指定的 **Linked Slot** 松手 → 卡面对齐、尺寸正常、Console 有 debug 日志
4. 从槽位拖出到空白处松手 → 缓动回堆叠顶
5. 未命中槽位在堆叠区松手 → 顶卡送到底层（`Send To Back On Click`）
6. 悬停 / 拖拽时 `DescriptionPanel` 描述更新

## 后续可接

- 多槽位 / 棋盘格映射表（当前 deliberately 单槽 + 手动关联）
- 放置后与 `GameplayWorldPresenter` 正式 `BoardCardView` 合并或互斥策略
- 回堆叠 / 放置音效（`IAudioUtility`）
