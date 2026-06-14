# CircularGalleryWorldDemo 贝塞尔弧线 + Scene 手柄落地笔记

> 2026-06-14

## 背景

临时 Demo：弧线牌列交互。从最初的 React Bits CircularGallery（圆弧 + 滚动浏览 + 点选）迭代为**二次贝塞尔弧线** + **Scene 视图可视化手柄** + **判定区** + **拖拽入槽**。使用标准 `Card.prefab` + 真实卡牌 SO 烘焙链路。

## 做了什么

### 贝塞尔弧线（替代原 ComputeArc 圆弧）

```
B(t) = (1-t)²·P0 + 2(1-t)t·P1 + t²·P2
B'(t) = 2(1-t)(P1-P0) + 2t(P2-P1)
```

- 三个控制点 `mControlPointStart/Mid/End` 存为**本地坐标**（跟随父物体移动）。
- `BezierArc` 静态工具类（Eval / Tangent / Normal），`public` 以便 Editor 程序集访问。
- 卡牌用 t 均匀分布在 [0,1]（8 张牌 t = 0, 1/7, 2/7...1），位置和旋转从曲线采样。
- 切线方向决定卡牌 Z 旋转，法线方向用于悬停抬升偏移。

### Scene 视图手柄（CircularGalleryEditor）

| 手柄 | 功能 |
|------|------|
| P0 绿球 + PositionHandle | 拖动调整弧线起点 |
| P1 黄球 + PositionHandle | 拖动调整弧线中点（弯曲程度） |
| P2 红球 + PositionHandle | 拖动调整弧线终点 |
| 判定区中心 PositionHandle | 移动判定矩形 |
| 判定区角上 PositionHandle | 调整判定矩形大小 |

绘制内容：
- 贝塞尔主曲线（蓝色 3px，3 段三次近似 `Handles.DrawBezier`）
- 控制多边形辅助线（P0→P1、P1→P2，半透明白）
- 卡牌位置标记（沿曲线短法线）
- 判定区矩形（半透明黄色填充 + 边框）

Inspector 折叠分组：Bezier Arc Control Points / Judgment Area / Card Count。

### 判定区 + 收起

- `mJudgmentCenter`（Vector2 本地坐标）+ `mJudgmentSize`（Vector2）定义矩形。
- Browsing 阶段 `GetMouseButtonDown(0)` 时先检测是否在判定区内：
  - 区外 → `BeginCollapse()`：全部激活卡牌 DOScale(0) + 上移 0.4 + stagger → 禁用 → Collapsed 阶段
  - 区内 + 悬停在牌上 → 开始拖拽
- 拖拽中释放到区外 → 先弹回 → 再收起
- 收起后不可恢复（临时 demo，无需展开）

### 拖拽入槽 + 弹簧挤占

- `BeginCardDrag` → `UpdateCardDrag`（跟随鼠标 + 倾斜）→ `EndCardDrag`
- 松手在 CardSlot2 范围内 → DOMove + OutBack 入槽，剩余卡牌重新分配 t 参数 + `SpringMath.Step` 弹簧挤占
- 松手不在槽位 → 弹簧回弹到弧线原位（沿切线方向偏移量 AnimatedX → 0）
- 入槽后 `RebuildActiveTValues()` 重算活跃卡牌的 t 值（跳过已入槽的牌）

### 双侧进场

- 卡牌分左右两半，从屏幕两侧外飞入曲线目标位置
- 外侧牌先动、内侧后动（stagger），配合 OutCubic
- 进场完成后 `SyncSpringState()` 同步弹簧状态，避免切换到 Browsing 时跳变

### 阶段状态机

```
Entering → Browsing → Collapsed（点击区外）
                    ↘ Complete（全部入槽）
```

## 场景接线

| 对象 | 说明 |
|------|------|
| `TableNineBootstrap` / `CircularGalleryWorldDemo` | 已挂载，Play 即可 |
| `mControlPointStart/Mid/End` | Scene 手柄调，默认 `(-6,-0.5,0)` / `(0,1.5,0)` / `(6,-0.5,0)` |
| `mJudgmentCenter/Size` | Scene 手柄调，默认 `(0,0.5)` / `(16,8)` |
| `mLinkedSlot` | 空时自动回退 `NineGrid Main CardSlots/CardSlot2` |

不需要时可整对象删除，不影响正式流程。

## 相关文件

| 文件 | 说明 |
|------|------|
| `Assets/Scripts/Game/CardPreview/CircularGalleryWorldDemo.cs` | 弧线控制器、拖拽/入槽/弹回/收起/描述 + `BezierArc` 工具类 |
| `Assets/Scripts/Game/CardPreview/Editor/CircularGalleryEditor.cs` | Scene 手柄、曲线绘制、判定区绘制 |
| `Assets/Prefabs/Cards/Card.prefab` | 标准世界空间卡 prefab |
| `Assets/Scenes/TableNineBootstrap.unity` | 场景内 `CircularGalleryWorldDemo` 对象 |

## 架构说明

- 走 QFramework `IController`，读 `IConfigModel` / 发 Command。
- `BezierArc` 为 `public static` 类，运行时程序集 (Assembly-CSharp) 与 Editor 程序集 (Assembly-CSharp-Editor) 均可访问（项目无 asmdef）。
- 表现层自包含，不修改 `GameplayWorldPresenter` 正式棋盘卡逻辑。

## 坑点

1. **DrawBezier 切线参数**
   `Handles.DrawBezier` 的 startTangent / endTangent 是三次贝塞尔**控制点位置**（非方向向量）。直接用二次贝塞尔导数 `B'(t)` 会使曲线严重偏离。
   修复：将切线缩放 `dt/3` 后加减到段端点，得到三次近似控制点。

2. **弹簧方向**
   原圆弧用标量 X 偏移做弹簧。改为贝塞尔后，弹簧偏移沿**切线方向**（`tangent.normalized * AnimatedX`），目标值始终为 0（静止在曲线上）。

3. **入槽后 t 重分配**
   入槽一张牌后，剩余牌需在曲线上重新均匀分布（`RebuildActiveTValues`），否则挤占动画的目标位置不对。

4. **DOTween Sequence 生命周期**
   进场完成的 `DOTween.Sequence()` 必须加入 `mTweens` 列表，否则 `OnDestroy` 时不会 Kill，可能在销毁后回调。

5. **BeginCardDrag 先验证再设状态**
   必须在 `TryGetPointerWorld` 成功后才设 `IsDragging = true`，否则相机为 null 时卡牌卡在拖拽态无法恢复。

## 测试步骤

1. Play `TableNineBootstrap`
2. 卡牌从左右两侧飞入贝塞尔弧线，静止展示
3. 悬停卡牌 → 沿法线抬升 + DescriptionText 更新
4. 拖拽卡牌 → 跟随鼠标 + 倾斜效果
5. 拖到 CardSlot2 附近松手 → DOMove 入槽 + Console 日志 + 其余牌弹簧挤占
6. 拖到空处松手 → 弹簧回弹到弧线原位
7. 点击判定区外 → 全部卡牌缩小上移消失，效果收起
8. 全部入槽 → Complete

## 后续可接

- 多槽位关联（当前 deliberately 单槽 CardSlot2）
- 入槽后与 `GameplayWorldPresenter` 正式 `BoardSlotView` 合并策略
- 收起/入槽音效（`IAudioUtility`）
- 弧线控制点运行时动画（如展开/收缩）
