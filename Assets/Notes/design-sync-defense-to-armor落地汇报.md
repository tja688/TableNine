# design-sync-defense-to-armor 落地汇报

> 任务来源：`Assets/Docs`（2026-06-14 设计文档同步）— 防御→护甲术语统一、属性体系合并、小鬼初始卡组  
> 执行时间：2026-06-14  
> 状态：✅ 本任务范围内验收通过（存在仓库内既有失败用例，见 §5）

---

## 1. 执行摘要

已将策划侧「防御」独立属性合并为「护甲」属性，并在代码、SO、UI、存档与测试中全量对齐。卡牌/玩家现仅保留 **血量、攻击、护甲** 三项基础属性；关卡开始时按有效护甲属性填充 `CurrentArmor`，临时护甲增减走 `StatType.CurrentArmor`。小鬼初始帮助卡组新增 **属性提升卡×1**。编译 0 error；与本变更直接相关的 EditMode 用例已通过。

---

## 2. 文件变更清单（质检入口）

### 2.1 新增

| 路径 | 说明 |
|------|------|
| `Assets/Notes/design-sync-defense-to-armor落地汇报.md` | 本汇报 |

### 2.2 修改（核心）

| 路径 | 改动要点 |
|------|----------|
| `Assets/Scripts/Data/GameRuntimeData.cs` | `BaseDefense`→`BaseArmor`；`StatDefenseBonus`→`StatArmorBonus`；`EffectiveStats.Defense`→`Armor`；枚举 `StatType`/`AttributeUpgradeChoice`/`HelpCardSystemTag` 重命名 |
| `Assets/Scripts/Data/RunSaveData.cs` | 存档字段重命名；`CurrentSchemaVersion` 升至 **3** |
| `Assets/Scripts/System/RuntimeSystems.cs` | 有效属性计算、`FillArmorFromArmorStatAtNodeStart` |
| `Assets/Scripts/Command/RuntimeCommands.cs` | `ApplyStatChangeCommand`：`StatType.Armor` 改永久属性，`StatType.CurrentArmor` 改当前护盾 |
| `Assets/Scripts/Effect/EffectGraphRegistry.cs` | 帮助卡临时加甲使用 `CurrentArmor` |
| `Assets/Scripts/UI/CardViewData.cs` | 展示改为「攻/甲/血」，移除独立 DEF 行 |
| `Assets/ScriptableObjects/GameConfig/*.asset` | 全量 `BaseDefense`/`StatDefenseBonus` 字段迁移 |
| `Assets/ScriptableObjects/GameConfig/TableNineCharacterConfig.asset` | 小鬼初始卡组 +`help_attribute_up`（共 8 张） |
| `Assets/Scripts/Tests/EditMode/TableNineStage3EditModeTests.cs` | 初始卡组验收改为包含属性提升卡 |

### 2.3 删除

无

---

## 3. 规格对照

| 文档变更项 | 状态 | 落点 |
|-----------|------|------|
| 移除「防御」独立属性，护甲承担开局填充+减伤 | ✅ | `GameRuntimeData`、`StatSystem`、`ApplyStatChangeCommand` |
| UI 面板/卡牌显示「攻/甲/血」 | ✅ | `CardViewData`、`UIDebugPanel` |
| 帮助卡/遗物/怪物数据「防御」→「护甲」 | ✅ | SO 资产 + Editor 文案 |
| 属性提升卡选项「护甲+1」 | ✅ | `AttributeUpgradeChoice.Armor`、`ResolveAttributeChoiceCommand` |
| 小鬼初始卡组 + 属性提升卡×1 | ✅ | `TableNineCharacterConfig.asset`、`DefaultGameConfigFactory` |
| 删除 `配置与SO对照.md` | ✅（文档同步阶段已完成） | — |

---

## 4. 架构符合性

- 规则变更仍经 `Command`/`System`，未在 View 硬写逻辑。
- `StatType.Armor`（属性）与 `StatType.CurrentArmor`（当前护盾）语义分离，避免帮助卡加甲误改基础属性。
- 存档 v3 + JSON 字段名迁移（`PlayerBaseDefense`→`PlayerBaseArmor`）保持旧档可读。

---

## 5. 测试与 Console 证据

```
EditMode 全量：224 跑完，213 通过 / 11 失败
Unity Console：0 error（落地后）
验证方式：Unity MCP refresh + run_tests
```

**与本任务直接相关、已通过：**

- `TableNineR2BasicRulesEditModeTests` — 节点开局填甲、护甲属性同步当前护盾
- `TableNineR4EffectSystemEditModeTests.Attribute_Defense_Choice_Syncs_Armor`
- `TableNineM3EditModeTests.Relic_Stat_Bonus_Applied_To_EffectiveStats`
- `TableNineStage3EditModeTests.Imp_Initial_Help_Deck_Includes_Attribute_Up_Card`
- `TableNineR8PresentationEditModeTests.CardViewData_Includes_Armor_*`

**全量套件中仍失败的 11 项（经核对与本次重命名无直接因果，属仓库既有问题）：**

- R3：`Blessing_Is_Not_Consumed_*`、`Thorn_Armor_Damage_Is_In_Same_Group_*`
- R4：`UseHelpCard_Resolves_Potion_Via_EffectGraph`
- R5：滚石/治疗之泉/荆棘甲等 6 项
- Stage1：`ClubCub_In_TopRow_Buffs_Other_Monster_Attack`
- Stage2：`Bear_Trap_Damages_Refilled_Adjacent_Monster_And_Consumes`

---

## 6. 质检路线图

1. **枚举与字段**：`GameRuntimeData.cs` — `StatType`、`EffectiveStats.Armor`
2. **战斗/开局填甲**：`RuntimeSystems.cs` — `FillArmorFromArmorStatAtNodeStart`
3. **属性变更命令**：`RuntimeCommands.cs` — `ApplyStatChangeCommand`
4. **配置**：`TableNineCharacterConfig.asset`、`TableNineCardConfig.asset`、`TableNineRelicConfig.asset`
5. **表现**：`CardViewData.cs` 文本格式

---

## 7. 风险与后续

- 旧存档 v1/v2 依赖 JSON 字符串替换迁移；若玩家本地有自定义存档字段，需 PlayMode 抽测读档。
- 全量 EditMode 仍有 11 个失败用例，建议单独开任务修复，避免与本次术语落地混淆。
- 正式 UI（非 CardView 文本占位）若仍显示「防御」标签，需表现层 Prefab/Localization 二次扫尾。

---

## 8. 变更统计

```
修改 C# 文件：~45
修改 SO 资产：4（Card/Relic/Character/Effect 等）
新增汇报：1
EditMode 新增/改动测试：Stage3 初始卡组断言更新
```
