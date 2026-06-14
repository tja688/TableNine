# Gameplay HUD 侧栏交互笔记

> 2026-06-14

## 做了什么

- `UIGameplayPanel` 接入真实玩家技能 / 遗物：从 `IPlayerModel` 读数据，在 `SkillPanel/Grids`、`RelicPanel/Grids` 槽位显示 SO 图标。
- 鼠标悬停图标时，向 `DescriptionText` 写入名称 + 描述（`SidePanelDescriptionComposer`）。
- 临时测试：`GameplaySidePanelDebugDemo` 挂于场景 `UIGameplayPanel`，**空格**随机加遗物或技能；`mAutoStartRun` 默认自动开局。
- 卡牌预览 Demo 切卡键改为 **Tab**，避免与空格测试冲突；侧栏悬停时卡牌描述让位（`IsSidePanelHovered`）。

## 坑点

- `DescriptionText` 不在 `UIGameplayPanel` 子层级，而是场景独立预制体 `DescriptionPanel`。面板内 `FindDeep` 找不到，`mDescriptionText` 为 null，图标能显示但悬停无反应。
- 修复：子层级找不到时全场景按名称查找 `DescriptionText`（与 `RandomCardPreviewDemo` 一致）。
- `UIGameplayPanel` 预制体根节点 scale 曾为 `(0,0,0)`，已改为 `(1,1,1)`。

## 相关文件

| 文件 | 说明 |
|------|------|
| `Assets/Scripts/UI/UIGameplayPanel.cs` | 侧栏刷新 + 悬停 |
| `Assets/Scripts/UI/SidePanelDescriptionComposer.cs` | 技能/遗物描述拼装 |
| `Assets/Scripts/Tests/PlayMode/GameplaySidePanelDebugDemo.cs` | 空格临时测试 |
| `Assets/Prefabs/UI/UIGameplayPanel.prefab` | 侧栏槽位布局 |
| `Assets/Prefabs/UI/DescriptionPanel.prefab` | 描述区（独立） |

## 测试

1. Play `TableNineBootstrap`
2. 空格加遗物/技能
3. 鼠标指向侧栏图标，看 `DescriptionPanel` 描述是否变化
