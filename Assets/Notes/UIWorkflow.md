# UI 接入与面板管理规范

> 最后更新：2026-06-11  
> **唯一入口文档**：新增/接入 UI 时只读此文件。设计稿索引见 `Assets/Docs/06-UI/界面布局.md`。

## 1. 原则

- **Gameplay 只表达 UI 需求**（事件 + `TableNineUIRequestPanelData`），不引用 prefab、按钮、`UIPanel`。
- **稳定键 `UIKey`**（`domain.intent`）是 Gameplay、注册表、文档的唯一对接名。
- **正式 Prefab 优先**；缺失时走 Registry 配置的 `FallbackStrategy`，由 `UIFallbackPanel` 统一拼装。
- **UI 按钮只发 Command**，不直接改 Model；需要阻断操作时配合 `InputLockReason.OverlayVisible`（房间选择等非阻塞 UI 除外）。

## 2. 运行时链路

```text
Command / System
  → 领域事件（或 Bootstrap 直接开 HUD）
  → TableNineUIRouter 组装 TableNineUIRequestPanelData
  → TableNineUIRuntime.Open(registry, data)
  → Registry 查 UIKey
      ├─ 有 Prefab → UIKit 按 PanelName 打开正式面板
      └─ 无 Prefab → UIFallbackPanel 按 FallbackStrategy 拼装
  → 选项/按钮 → Command → 关闭面板（TableNineUIRuntime.Close）
```

配置资产：`Assets/ScriptableObjects/TableNineUIPanelRegistry.asset`  
Bootstrap 在 `GameplayBootstrap` 注入 Registry 并启动 Router；HUD 由 `UIKit.OpenPanel<UIGameplayPanel>` 直接打开，不经 Router。

## 3. 数据分工（填表架构）

| 数据源 | 路径 | 职责 |
|--------|------|------|
| UIKey 常量 | `Assets/Scripts/UI/TableNineUIKeys.cs` | 稳定键 + `TableNineUIType` / `TableNineUIFallbackStrategy` 枚举 |
| 编辑器元数据 | `Assets/Scripts/Editor/UIPanelMetadataCatalog.cs` | 中文名、描述、分类、触发事件、期望元素、设计备注 |
| 运行时注册表 | `TableNineUIPanelRegistry.cs` + `.asset` | Prefab、PanelName、层级、Fallback、是否挡操作 |
| 事件路由 | `Assets/Scripts/UI/TableNineUIRouter.cs` | 监听事件 → 组装 `TableNineUIRequestPanelData` |
| 面板请求 | `Assets/Scripts/UI/TableNineUIRequestPanelData.cs` | Title / Message / Choices / SecondaryChoices |

**规则**：编辑器展示信息只来自 `UIPanelMetadataCatalog` + Registry，不在编辑器代码里硬编码面板文案。  
新增 `UIType` / `FallbackStrategy` 时，同步更新 Catalog 里的 `GetUITypeLabel` / `GetFallbackStrategyLabel`。

## 4. 已注册面板（11）

| UIKey | 中文 | 触发 | Router | 说明 |
|-------|------|------|:------:|------|
| `gameplay.hud` | 主游戏界面 | Bootstrap | — | 必须正式 Prefab；`FallbackStrategy=None` |
| `popup.message` | 通用提示 | `PopupRequestedEvent` | ✓ | |
| `choice.attribute` | 属性提升 | `AttributeChoiceRequestedEvent` | ✓ | |
| `choice.room` | 房间选择 | `RoomChoiceRequestedEvent` | ✓ | `BlocksGameplayInput=false` |
| `choice.tutor_skill` | 导师技能 | `TutorSkillChoiceRequestedEvent` | ✗ | 事件已定义，Router 待接 |
| `reward.help` | 帮助卡奖励 | `HelpRewardGeneratedEvent` | ✓ | |
| `reward.chest` | 宝箱遗物 | `ChestRewardGeneratedEvent` | ✓ | |
| `shop.main` | 商店 | `ShopOpenedEvent` | ✓ | 删卡目前在 Shop fallback 的 SecondaryChoices |
| `prompt.next_node` | 下一节点提示 | 奖励链结束事件 | ✓ | |
| `confirm.delete_help_card` | 删卡确认 | 商店（规划） | ✗ | Registry 已登记；正式 UI 待做 |
| `card.detail` | 卡牌详情 | 卡牌交互（规划） | ✗ | Registry 已登记；Router 待接 |

状态色含义（编辑器侧栏）：绿 = 已绑 Prefab；橙 = 仅 Fallback；红 = 未配置。

## 5. 新增面板（标准 4 步）

### ① 定义 UIKey

`TableNineUIKeys.cs`：

```csharp
public const string BossReward = "reward.boss";  // domain.intent，全小写
```

### ② 填写元数据（编辑器自动展示）

`UIPanelMetadataCatalog.Entries` 追加 `PanelMeta`（`ChineseName`、`ChineseDescription`、`Category` 必填；`TriggerEvent` / `ExpectedElements` / `DesignNote` 建议填）。

分类枚举：`Hud` `Popup` `Choice` `Reward` `Shop` `Prompt` `Confirm` `Detail`。

### ③ 配置 Registry

**推荐**：在 `TableNineUIPanelRegistry.CreateRecommendedDefaults()` 追加默认条目 → Unity 打开 **TableNine > UI 面板管理**（`Ctrl+Alt+U`）→ **同步推荐默认值** → 在窗口内编辑 Prefab / Fallback。

Registry 单条字段：

| 字段 | 说明 |
|------|------|
| `UIKey` | 与 Keys 常量一致 |
| `UIType` | 语义类型（HUD / Popup / RewardChoice 等） |
| `PanelName` | UIKit 面板名，通常与脚本类名一致 |
| `Prefab` | 正式 UI；空则走 Fallback |
| `Level` | HUD 用 `Common`，覆盖层用 `PopUI` |
| `OpenType` | 一般 `Single`；可叠加提示用 `Multiple` |
| `FallbackStrategy` | 见下表 |
| `BlocksGameplayInput` | Fallback 是否全屏挡操作 |
| `Notes` | 策划/程序备注 |

### ④ 接入 Router

`TableNineUIRouter` 监听领域事件，组装 `TableNineUIRequestPanelData`，调用 `TableNineUIRuntime.Open`；关闭时用 `TableNineUIRuntime.Close(registry, uiKey)`。

```csharp
// 选项只绑 Command，示例：
data.Choices.Add(TableNineUIChoiceData.Command(
    id, label, desc, meta,
    c => c.SendCommand(new SomeCommand(...))));
```

需要新事件时：在 `RuntimeUIEvents.cs` 定义 → System/Command 发送 → Router 注册。

**测试**：补 EditMode 测试验证事件 → PanelData → Command 链路（参考 `TableNineUIEventsEditModeTests`）。

## 6. Fallback

仅允许 `UIFallbackPanel` 创建；业务代码禁止临时 `new GameObject` 拼 UI。  
Fallback 组件 Prefab 在 Registry 资产底部配置，由 `TableNineUIRuntime` 注入 `PanelData`。

| 策略 | 用途 |
|------|------|
| `None` | 无 Fallback；缺 Prefab 则不打开（HUD 适用） |
| `AtomicPopup` | 标题 + 正文 + 确认 |
| `AtomicChoiceList` | 标题 + 纵向选项 + 可选关闭/跳过 |
| `AtomicRoomChoice` | 底部房间按钮组，非全屏挡操作 |
| `AtomicShopList` | 商品列表 + 删卡 + 离开 |
| `AtomicStatusPrompt` | 顶部轻量提示条 |

正式 Prefab 完成后**只改 Registry 的 Prefab 字段**，不改 Gameplay。

## 7. 面板管理编辑器

| 项 | 说明 |
|----|------|
| 打开 | 菜单 **TableNine > UI 面板管理** / `Ctrl+Alt+U`；或选中 Registry 资产点「打开 UI 面板管理窗口」 |
| 布局 | 顶栏 + Toolbar；左侧分类导航（250px）；右侧详情（说明 + 接入配置 + Fallback + 备注）；底部「全局 Fallback 预制件」 |
| 工具 | **同步推荐默认值**（合并代码默认，不覆盖已有 Prefab）、**刷新**、**定位资产** |
| 样式 | UI Toolkit **inline C# 样式**（暖棕控制台风）；**不依赖 USS** |
| DescriptionPanel 文案 | 侧栏底部 **DescriptionPanel 描述文案**：列出全部 HUD 描述键，实时字数校验（**40 字上限**，含空格；禁止换行），支持 **应用精简默认文案** |
| 代码 | `Assets/Scripts/Editor/TableNineUIRegistryEditor.cs`、`DescriptionPanelConfigEditorSection.cs` |

### DescriptionPanel 文案约束

- **展示区域**：`UIGameplayPanel` 内 `DescriptionPanel / DescriptionText`（约 108×85 px，12px 像素字体）。
- **字数上限**：单行 **40 字**（`DescriptionPanelTextRules.MaxLength`）；空格计入字数；**不要使用换行**（历史上 `[Phase] 牌堆 N\n消息` 会造成严重溢出，已移除）。
- **配置资产**：`TableNineDescriptionPanelConfig`（推荐路径 `Assets/ScriptableObjects/TableNineDescriptionPanelConfig.asset`），由 `TableNineUIPanelRegistry.DescriptionPanelConfig` 引用；Bootstrap 启动时注入 `DescriptionPanelTexts`。
- **运行时入口**：`DescriptionPanelTexts.Get(key)` / `Format(key, args)`；Gameplay 侧通过 `GameplayMessageEvent` 或 HUD 状态键写入，面板侧 `Sanitize` 兜底截断。
- **键名常量**：`DescriptionPanelTextKeys`；内置精简默认见 `DescriptionPanelTextDefaults`。
- **编辑器校验**：输入框下方显示 `当前字数 / 40`；超限或含换行时显示 Warning/Error。

## 8. 架构约束

- Command / System：**不**打开 UIKit、**不**引用 UI 控件或 prefab。
- 阻塞型覆盖层打开阶段加 `InputLockReason.OverlayVisible`；**房间选择不加**（清场后仍可拾取/用卡）。
- 禁止 `GameObject.Find`、硬编码层级路径作为正式绑定；旧 prefab 的 `FindDeep` 仅作过渡。
- `UIChoiceOverlayPanel` 仍兼容旧 `ChoiseButton` 命名；多 key 复用同一正式面板（属性/奖励/导师/删卡确认）。
- `GameplayWorldPresenter` 仍有 Bootstrap 场景自动查找，重做表现时应改为序列化引用。

## 9. 关键文件

| 文件 | 用途 |
|------|------|
| `TableNineUIKeys.cs` | UIKey + 枚举 |
| `TableNineUIPanelRegistry.cs` / `.asset` | 运行时映射 + 默认条目 |
| `UIPanelMetadataCatalog.cs` | 编辑器中文元数据 |
| `TableNineUIRegistryEditor.cs` | 面板管理窗口 |
| `DescriptionPanelConfigEditorSection.cs` | DescriptionPanel 文案专属编辑区 |
| `TableNineDescriptionPanelConfig.cs` | DescriptionPanel 文案 ScriptableObject |
| `DescriptionPanelTextKeys.cs` / `DescriptionPanelTexts.cs` | 文案键与运行时访问 |
| `DescriptionPanelTextRules.cs` | 40 字校验规则 |
| `TableNineUIRouter.cs` / `TableNineUIRuntime` | 事件路由与开关面板 |
| `TableNineUIRequestPanelData.cs` | 请求数据结构 |
| `UIFallbackPanel.cs` | Fallback 运行时 |
| `TableNineUIKitConfig.cs` | Registry 驱动的 PanelLoader |
| `RuntimeUIEvents.cs` | UI 相关领域事件 |
| `GameplayBootstrap.cs` | Registry + Router 启动 |

## 10. AI 检查清单

新增 UI 面板时确认：

- [ ] `TableNineUIKeys.cs` — UIKey 常量
- [ ] `UIPanelMetadataCatalog.cs` — PanelMeta
- [ ] `TableNineUIPanelRegistry.cs` — `CreateRecommendedDefaults()` 默认条目
- [ ] `RuntimeUIEvents.cs` — 新事件（若需要）
- [ ] `TableNineUIRouter.cs` — 监听 + Open/Close + Choices→Command
- [ ] `.asset` — 同步默认值并绑 Prefab / Fallback
- [ ] EditMode 测试
- [ ] 本文档 §4 面板表 — 更新 Router 状态一行
- [ ] DescriptionPanel 新文案 — 在 **UI 面板管理 → DescriptionPanel 描述文案** 登记键值，并确认 ≤40 字
