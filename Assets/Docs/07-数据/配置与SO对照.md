---
tags:
  - 数据
  - 配置
  - 实现
created: 2026-06-13
---

# 配置与 SO 对照

> 本页面向策划/设计阅读：说明 **设计文档中的数据表** 与 **Unity 工程内 ScriptableObject 配置** 的对应关系。  
> 完整技术细节（类名、加载链路、Editor 菜单、待办）见开发笔记：  
> `Assets/Notes/SO配置与设计文档对照.md`

## 设计文档 → SO 域

| 本目录 / 设计文档 | 工程 SO | 说明 |
|------------------|---------|------|
| [[游戏开发项目/九宫牌局/05-职业与层级/职业]] | `TableNineCharacterConfig` | 角色初始属性、初始技能/帮助卡 ID |
| [[游戏开发项目/九宫牌局/07-数据/怪物卡数据]] | `TableNineCardConfig`（怪物条目） | `CardType = Monster` |
| [[游戏开发项目/九宫牌局/07-数据/帮助卡数据]] | `TableNineCardConfig`（帮助条目）+ `TableNineEffectConfig` | 卡牌数值在 Card SO；效果逻辑在 EffectGraph |
| [[游戏开发项目/九宫牌局/01-机制规则/发牌机制]]、[[游戏开发项目/九宫牌局/05-职业与层级/层级系统]] | `TableNineCardConfig`（MonsterDeckRules） | 各层各节点恶魔卡组生成规则 |
| [[游戏开发项目/九宫牌局/04-技能/玩家技能]]、[[游戏开发项目/九宫牌局/04-技能/怪物技能]] | `TableNineSkillConfig` | 技能定义 + 效果绑定 + 怪物行为规则 |
| [[游戏开发项目/九宫牌局/07-数据/遗物数据]] | `TableNineRelicConfig`（Relics） | 遗物数值与描述 |
| [[游戏开发项目/九宫牌局/05-职业与层级/层级系统]]、[[游戏开发项目/九宫牌局/05-职业与层级/商店]] | `TableNineRelicConfig`（Rooms） | 金币房/宝箱房/属性房/商店等 |

Master 资产 `TableNineGameConfig` 引用以上全部子 SO，运行时由 `GameplayBootstrap` 注入。

## 同步约定

- **设计案 Markdown 不会自动写入 SO**。改表后需开发在 Unity 中编辑 SO，或运行 `TableNine/Game Config/Sync From Code Defaults` 从代码种子导出。
- 改 SO 后运行 `TableNine/Game Config/Validate` 检查引用完整性。
- 帮助卡「体系」列 ↔ SO 字段 `SystemTag`；品质 ↔ `Quality`。

## UI 与表现

- **数值 SO 不含图标/预制体字段**。卡面图按 `cardId` 约定路径加载（`Resources/TableNine/Cards/{id}`）。
- UI 布局见 [[游戏开发项目/九宫牌局/06-UI/界面布局]]；当前工程保留四个 UI Prefab（Gameplay / Description / ChoiceOverlay / Popup），无 UI Registry SO。

## 相关

- [[游戏开发项目/九宫牌局/07-数据/帮助卡数据]]
- [[游戏开发项目/九宫牌局/07-数据/怪物卡数据]]
- [[游戏开发项目/九宫牌局/07-数据/遗物数据]]
- [[游戏开发项目/九宫牌局/06-UI/界面布局]]
