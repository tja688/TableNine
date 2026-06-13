# 6-13-design-change-stage2-stage3 落地汇报

> 任务来源：[6-13 docs 设计变更.md](6-13%20docs%20设计变更.md) — stage 2 + stage 3  
> 执行时间：2026-06-13  
> 分支：工作区未提交  
> 状态：✅ 验收通过（EditMode）

---

## 1. 执行摘要

补全了 stage 2 中断前已落地的帮助卡/怪物数据与效果，并完成 stage 3：帮助卡体系标签、卡组上限突破与节点末裁减、小鬼初始卡组调整、提示文案与「旋转」术语对齐。相关 QFramework 分层文档已同步。

---

## 2. 文件变更清单（质检入口）

### 2.1 新增

| 路径 | 说明 |
|------|------|
| `Assets/Scripts/Config/HelpCardSystemTagConfig.cs` | 帮助卡体系标签配置表 |
| `Assets/Scripts/Tests/EditMode/TableNineStage2HelpCardDataEditModeTests.cs` | stage 2 数据与效果回归 |
| `Assets/Scripts/Tests/EditMode/TableNineStage3EditModeTests.cs` | stage 3 卡组规则与标签回归 |

### 2.2 修改

| 路径 | 改动要点 |
|------|----------|
| `Assets/Scripts/Config/DefaultGameConfigFactory.cs` | 属性提升卡改金色、金币卡改蓝色；小鬼初始卡组去掉属性提升卡 |
| `Assets/Scripts/Config/DefaultGameConfigPlaytestContent.cs` | stage 2 新卡/怪物重平衡/庇佑魔法卡改名（未追踪 diff 前已改） |
| `Assets/Scripts/Effect/EffectGraphRegistry.cs` | 耐用盾牌、盾击教程、传送、绑票等效果图 |
| `Assets/Scripts/Data/GameRuntimeData.cs` | `HelpCardSystemTag`、`HelpCardAddPolicy`、`CardDefinition.SystemTag`、`HelpDeckMessages` |
| `Assets/Scripts/System/RuntimeSystems.cs` | `TryAddHelpCard`/`TrimHelpDeckOverflow`；金色宝箱遗物注入；弹窗改「卡组上限」 |
| `Assets/Scripts/Command/RuntimeCommands.cs` | `AddHelpCardCommand`；`SettleNodeEndCommand` 调用裁减 |
| `Assets/Scripts/System/EffectSystem.cs` | `AddCardToHelpDeck` 走 bypass 策略 |
| `Assets/Scripts/UI/TableNineUIRouter.cs` | 帮助卡 UI 描述展示体系标签 |
| `Assets/Scripts/UI/CardViewData.cs` | `SystemTagText` 字段 |
| `Assets/Notes/QframeworkNotes/architecture.md` | 帮助卡生命周期、CardDefinition 字段 |
| `Assets/Notes/QframeworkNotes/systems.md` | Reward/Relic/Board 旋转术语与 API |
| `Assets/Notes/QframeworkNotes/commands.md` | `AddHelpCardCommand`、`SettleNodeEndCommand` |
| `Assets/Notes/QframeworkNotes/models.md` | `CardDefinition.SystemTag` |

### 2.3 删除

无

---

## 3. 任务规格对照

来源：[6-13 docs 设计变更.md](6-13%20docs%20设计变更.md)

### Stage 2

| 任务项 | 状态 | 落点 |
|--------|------|------|
| 等级1–4 怪物血量/攻击/防御重平衡 | ✅ | `DefaultGameConfigMonsters.cs` / `DefaultGameConfigPlaytestContent.cs` |
| 无色卡并入等级1池 | ✅ | `Layer1Level1MonsterIds` 含 `MonsterColorlessId` |
| 红桃5 改为 30/0/0 | ✅ | `MonsterHeart5Id` |
| 新增白色卡：耐用盾牌、捕熊陷阱、传送卡、血液转换 | ✅ | `AddPlaytestHelpCards` + `EffectGraphRegistry` |
| 新增蓝色卡：盾击教程、绑票 | ✅ | 同上 |
| 属性提升卡降为金色 | ✅ | `DefaultGameConfigFactory` |
| 破击锤破甲 10；旋转轮逆时针 | ✅ | `EffectGraphRegistry`（R4 已对齐，stage2 测试覆盖） |

### Stage 3

| 任务项 | 状态 | 落点 |
|--------|------|------|
| 各卡新增体系标签 | ✅ | `HelpCardSystemTagConfig` + `CardDefinition.SystemTag` |
| 遗物/技能获得卡可突破卡组上限 | ✅ | `HelpCardAddPolicy.BypassDeckCapacity` |
| 关卡结束超限强制移除最后加入的卡 | ✅ | `TrimHelpDeckOverflow` |
| 提示文案「卡组上限」 | ✅ | `HelpDeckMessages.CapacityOrSameNameBlocked` |
| 小鬼去掉初始属性提升卡 | ✅ | `CharacterImp` `InitialHelpCardIds` |
| 顺时针移动命名为「旋转」 | ✅ | `systems.md` / 设计文档 `卡牌移动.md` 已一致 |

### 验收项

| 验收项 | 状态 |
|--------|------|
| 编译 0 error | ✅（Unity MCP） |
| Stage2/Stage3 EditMode 测试 | ✅ |
| 分层权威文档同步 | ✅ |

---

## 4. 测试证据

| 测试类 | 结果 |
|--------|------|
| `TableNineStage2HelpCardDataEditModeTests` | 8/8 通过 |
| `TableNineStage3EditModeTests` | 6/6 通过 |

Console：0 error（验证时）

---

## 5. 质检入口

优先打开：

- `Assets/Scripts/Config/HelpCardSystemTagConfig.cs`
- `Assets/Scripts/System/RuntimeSystems.cs`（`RewardSystem.TryAddHelpCard` / `TrimHelpDeckOverflow`）
- `Assets/Scripts/Tests/EditMode/TableNineStage3EditModeTests.cs`
- `Assets/Notes/QframeworkNotes/systems.md` §8 IRewardSystem

---

## 6. 风险与待确认

1. **军械库技能**「关卡结束加入帮助卡」尚未有运行时绑定，bypass 策略已预留于 `AddHelpCardCommand`，待技能管线接入时复用。
2. **金色宝箱**仅在 `AddRelic` 时注入 2 张卡；若设计还要求其他触发点，需后续补效果图绑定。
3. stage 1（怪物技能大幅调整）不在本次范围，需另开落地任务。
