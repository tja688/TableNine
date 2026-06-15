# CardBattleEffectPreview 落地汇报

> **维护提示（2026-06-15）：** 碎裂实现已从 Shader 假裂纹升级为粒子 mesh 方案，且有未解 Lifetime 问题。请以 `Assets/Notes/卡牌粒子碎裂效果_维护交棒.md` 为准。

> 任务来源：用户即时表现需求；约束参考 `Assets/Docs/01-机制规则/战斗机制.md`、`Assets/Docs/01-机制规则/发牌机制.md`  
> 执行时间：2026-06-15  
> 分支：`快速轻量开发初版demo`  
> 状态：验收通过

---

## 1. 执行摘要

已为当前棋盘卡牌接入右键触发的战斗表现预览：玩家卡冲撞目标卡，目标卡出现假裂纹和碎片爆散，效果结束后再移除目标并请求补牌。实现包含 5 套可切换参数/风格，运行时按数字键 `2`-`6` 切换。效果播放期间使用 `InputLockReason.SequenceRunning` 阻塞后续输入，避免移除、补牌、重复点击抢时序。

---

## 2. 文件变更清单

### 2.1 新增

| 路径 | 说明 |
|------|------|
| `Assets/Scripts/Game/GameplayBattleEffectPreviewController.cs` | 右键战斗效果控制器：五套风格、互碰位移、假碎裂、Cinemachine Impulse 反射接入、相机兜底震动、输入锁收尾 |

### 2.2 修改

| 路径 | 改动要点 |
|------|----------|
| `Assets/Scripts/Game/GameplaySceneController.cs` | 主 Presenter 启动时自动挂载效果控制器；棋盘点击代理右键改为先触发表现流程，左键原逻辑保持 |
| `Assets/Scripts/Game/CardPreview/NineGridCardMoveDemo.cs` | 当前场景试玩入口的右键直接移除改为互碰/假碎裂/震动表现后再移除并补牌 |

### 2.3 删除

无。

---

## 3. 任务规格对照

| 任务项 | 状态 | 落点 |
|--------|------|------|
| 5 种不同参数/风格表现，按 `2`-`6` 切换 | 完成 | `GameplayBattleEffectPreviewController.Update`、`BattleEffectStyles` |
| 卡牌互碰后目标卡假碎裂，右键触发并覆盖直接消除 | 完成 | `TryPlayRightClickBattleEffect`、`NineGridCardMoveDemo.PlayRightClickBattleFxThenRemove` |
| 可调震动幅度的 Cinemachine 震动 | 完成 | `mBattleEffectShakeAmplitudeMultiplier`、`TriggerBattleCameraShake` |
| 阻塞后续动作，注意时序和状态 | 完成 | 主战斗路径用 `InputLockReason.SequenceRunning`；当前 demo 路径用 `mIsSequencing` 包裹表现流程，表现结束后再移除/补牌/旋转 |

---

## 4. 测试与 Console 证据

```
EditMode：8/8 通过
PlayMode：N/A
Unity Console：0 error
Warning：2 条既有未使用字段 warning；2 条 MCP 连接/批处理提示 warning
验证方式：Unity MCP refresh + Console + EditMode 测试
```

已跑测试：

- `TableNineArchitectureEditModeTests`
- `TableNineR8PresentationEditModeTests`

---

## 5. 风险、假设与后续

- 右键效果按“演示/预览消除”处理：目标从棋盘移除并触发补牌，但不走正式战斗伤害结算。
- Cinemachine 使用反射接入，避免当前项目脚本程序集直接引用 Cinemachine 包导致编译风险；若场景没有 Cinemachine listener，会自动使用轻量相机位移兜底震动。
- 震动幅度可在 `GameplayBattleEffectPreviewController` Inspector 中调 `Battle Effect Shake Amplitude Multiplier`；频率可调 `Battle Effect Shake Frequency Multiplier`。
