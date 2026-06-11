# UI 编辑器架构与开发规范

> 日期：2026-06-11  
> 本文档定义 UI 面板管理编辑器的填表式架构，以及后续 AI 协作开发的标准化流程。

## 1. 架构总览

UI 面板管理采用**填表式架构**：编辑器界面的所有中文标注、分类、描述均从两个数据源读取，新增面板只需在这两处填数据，编辑器自动展示。

```text
┌─────────────────────────────────────┐
│  UIPanelMetadataCatalog.cs          │ ← 元数据（中文名、描述、分类、期望元素）
│  (Editor 目录，静态字典)              │
└──────────────┬──────────────────────┘
               │ 读取
┌──────────────▼──────────────────────┐
│  TableNineUIRegistryEditorWindow    │ ← 编辑器窗口（自动展示）
│  (UI Toolkit EditorWindow)          │
└──────────────▲──────────────────────┘
               │ 读取
┌──────────────┴──────────────────────┐
│  TableNineUIPanelRegistry.asset     │ ← 运行时配置（UIKey、Prefab、Fallback）
│  (ScriptableObject)                 │
└─────────────────────────────────────┘
```

核心原则：**数据驱动展示**。编辑器不包含任何面板专属逻辑或硬编码的 UI 知识，一切信息从元数据目录和注册表资产中读取。

## 2. 新增面板的标准流程

当 Gameplay 需要一个新的 UI 面板时，按以下 4 步操作：

### 第一步：定义 UIKey

在 `TableNineUIKeys.cs` 中添加常量：

```csharp
public const string BossReward = "reward.boss";
```

命名规则：`domain.intent`，全小写，用点号分隔。

### 第二步：填写元数据

在 `Assets/Scripts/Editor/UIPanelMetadataCatalog.cs` 的 `Entries` 字典中追加一条：

```csharp
["reward.boss"] = new PanelMeta
{
    ChineseName = "Boss 奖励",
    ChineseDescription = "击败 Boss 后的三选一遗物奖励，品质高于普通宝箱。",
    Category = Category.Reward,
    TriggerEvent = "BossRewardGeneratedEvent",
    ExpectedElements = "标题文本、3个遗物卡(Name/Quality/Effect/Select)、跳过按钮(+30金币)",
    DesignNote = "与普通宝箱类似但品质更高，建议新增或复用 ChestRelicChoiceWindow。"
}
```

字段说明：

| 字段 | 用途 | 必填 |
|------|------|------|
| `ChineseName` | 编辑器中显示的面板中文名 | 是 |
| `ChineseDescription` | 编辑器中显示的功能描述 | 是 |
| `Category` | 编辑器中的分类归属 | 是 |
| `TriggerEvent` | 触发该面板的领域事件名 | 建议填 |
| `ExpectedElements` | 面板期望包含的 UI 元素说明 | 建议填 |
| `DesignNote` | 来自 UI 设计文档的备注 | 选填 |

可用分类（`Category` 枚举）：

| 枚举值 | 中文 | 适用场景 |
|--------|------|----------|
| `Hud` | HUD 面板 | 常驻显示的主界面信息 |
| `Popup` | 弹窗面板 | 模态提示和信息弹窗 |
| `Choice` | 选择覆盖层 | 各类多选一界面 |
| `Reward` | 奖励面板 | 卡牌/遗物奖励选择 |
| `Shop` | 商店面板 | 购买和删卡界面 |
| `Prompt` | 状态提示 | 轻量级状态引导 |
| `Confirm` | 确认面板 | 操作确认对话框 |
| `Detail` | 详情面板 | 物品/卡牌详情展示 |

### 第三步：配置注册表

在 `TableNineUIPanelRegistry.asset` 中新增面板条目。有两种方式：

**方式 A：通过编辑器窗口**  
打开 `TableNine > UI 面板管理`（或 `Ctrl+Alt+U`），点击「同步推荐默认值」自动添加缺失条目，然后手动编辑新条目。

**方式 B：通过代码**  
在 `TableNineUIPanelRegistry.cs` 的 `CreateRecommendedDefaults()` 方法中追加：

```csharp
new PanelEntry
{
    UIKey = TableNineUIKeys.BossReward,
    UIType = TableNineUIType.RewardChoice,
    PanelName = "UIBossRewardPanel",
    Level = UILevel.PopUI,
    OpenType = PanelOpenType.Single,
    FallbackStrategy = TableNineUIFallbackStrategy.AtomicChoiceList,
    BlocksGameplayInput = true,
    Notes = "Boss reward selection with skip."
}
```

然后在 Unity 中点击「同步推荐默认值」按钮。

### 第四步：接入事件路由

在 `TableNineUIRouter.cs` 中监听对应的领域事件，组装 `TableNineUIRequestPanelData`。

```csharp
private void OnBossRewardGenerated(BossRewardGeneratedEvent e)
{
    var data = new TableNineUIRequestPanelData
    {
        Key = TableNineUIKeys.BossReward,
        Title = "Boss 奖励 — 三选一遗物",
        Message = "选择一个遗物作为奖励",
        UIType = TableNineUIType.RewardChoice,
        FallbackStrategy = TableNineUIFallbackStrategy.AtomicChoiceList,
        BlocksGameplayInput = true
    };
    // ... 填充 Choices
    TableNineUIRuntime.Open(mRegistry, data);
}
```

## 3. 编辑器窗口使用指南

### 打开方式

1. 菜单：`TableNine > UI 面板管理`
2. 快捷键：`Ctrl + Alt + U`
3. 在 Inspector 中选中 `TableNineUIPanelRegistry` 资产，点击「打开 UI 面板管理窗口」按钮

### 界面说明

编辑器窗口从上到下包含：

**顶部横幅**：显示注册表名称和简要说明。

**统计栏**：实时显示面板总数、已接入正式 Prefab 的数量、使用 Fallback 的数量、未配置的数量。

**工具栏**：「同步推荐默认值」按钮将代码中定义的推荐配置合并到注册表资产中；「刷新」按钮重新加载数据；「定位资产」按钮在 Project 面板中高亮显示注册表资产。

**分类面板列表**：按功能分类展示所有面板条目。每个面板显示为一张卡片，卡片左侧有颜色状态条：
- 绿色：已接入正式 Prefab
- 橙色：使用 Fallback 原子组件
- 红色：未配置

点击卡片头部可展开/折叠详情，详情包含三个区域：
- **面板信息**：来自元数据目录的中文描述、触发事件、期望 UI 元素、设计备注
- **接入配置**：UI Key、UI 类型、面板名、Prefab 引用、层级、打开方式（均可编辑）
- **Fallback 配置**：回退策略、是否阻止交互（均可编辑）

**Fallback 组件预制件区**：管理 5 个原子组件的预制件引用。

## 4. 完整 UI 接入链路

```text
Command / System
  → 发送领域事件（如 BossRewardGeneratedEvent）
  → TableNineUIRouter 监听事件，组装 TableNineUIRequestPanelData
  → TableNineUIRuntime.Open() 查找注册表
  → 有正式 Prefab：按 PanelName 交给 UIKit 打开
  → 无正式 Prefab：按 FallbackStrategy 用 UIFallbackPanel 运行时拼 UI
  → 按钮只发送 Command，不直接修改 Model
```

## 5. 枚举值速查

### TableNineUIType（UI 类型）

| 枚举值 | 中文 | 说明 |
|--------|------|------|
| `Hud` | HUD（常驻信息） | 战斗中始终显示的主界面 |
| `Popup` | 弹窗（模态提示） | 需要用户注意或操作的消息 |
| `ChoiceOverlay` | 选择覆盖层（多选一） | 属性提升、技能选择等 |
| `RoomChoice` | 房间选择 | 节点通关后的房间二选一 |
| `RewardChoice` | 奖励选择 | 帮助卡、遗物等奖励三选一 |
| `Shop` | 商店 | 购买和删卡界面 |
| `Confirmation` | 确认对话框 | 删除确认等 |
| `Detail` | 详情展示 | 卡牌/遗物详情 |
| `StatusPrompt` | 状态提示 | 轻量级引导 |

### TableNineUIFallbackStrategy（回退策略）

| 枚举值 | 中文 | 说明 |
|--------|------|------|
| `None` | 无 | 不提供 fallback，缺失时面板不会打开 |
| `AtomicPopup` | 原子弹窗 | 居中标题、正文、确认按钮 |
| `AtomicChoiceList` | 原子选择列表 | 标题 + 说明 + 纵向选项列表 + 可选关闭 |
| `AtomicRoomChoice` | 原子房间选择 | 底部水平排列的房间按钮组 |
| `AtomicShopList` | 原子商店列表 | 商店购买列表 + 删卡列表 + 离开按钮 |
| `AtomicStatusPrompt` | 原子状态提示 | 顶部小型状态提示条 |

## 6. 文件清单

| 文件 | 用途 |
|------|------|
| `Assets/Scripts/UI/TableNineUIKeys.cs` | UIKey 常量 + UIType/FallbackStrategy 枚举 |
| `Assets/Scripts/UI/TableNineUIPanelRegistry.cs` | ScriptableObject 注册表（运行时 + 序列化） |
| `Assets/Scripts/UI/TableNineUIRouter.cs` | 事件驱动路由 + TableNineUIRuntime 静态工具类 |
| `Assets/Scripts/UI/TableNineUIRequestPanelData.cs` | 面板请求数据 + ChoiceData |
| `Assets/Scripts/UI/UIFallbackPanel.cs` | Fallback 面板运行时生成器 |
| `Assets/Scripts/UI/TableNineUIKitConfig.cs` | UIKit 配置（注册表驱动的 PanelLoader） |
| `Assets/Scripts/Editor/UIPanelMetadataCatalog.cs` | 元数据目录（填表式架构核心） |
| `Assets/Scripts/Editor/TableNineUIRegistryEditor.cs` | UI Toolkit 编辑器窗口 |
| `Assets/Scripts/Editor/Styles/TableNineUIRegistryEditorStyles.uss` | 编辑器样式表 |
| `Assets/ScriptableObjects/TableNineUIPanelRegistry.asset` | 注册表资产（配置数据） |

## 7. 维护公约

- **新增面板**必须填写元数据（UIPanelMetadataCatalog）和注册表（Registry）。
- **元数据目录**是编辑器的唯一数据源，不得在编辑器代码中硬编码任何面板信息。
- **UIKey 命名**遵循 `domain.intent` 格式，保持稳定性，一旦定义不可随意改名。
- **枚举扩展**：若新增 UIType 或 FallbackStrategy，同步在 UIPanelMetadataCatalog 的 `GetUITypeLabel()` 和 `GetFallbackStrategyLabel()` 中补充中文映射。
- **样式修改**：USS 文件支持热重载，修改后点击「刷新」按钮即可预览。
- **Odin Inspector**：SO 上的 Odin 属性保持现状，编辑器窗口通过按钮从 Inspector 打开，两者互不冲突。

## 8. AI Agent 开发检查清单

当 AI Agent 需要新增一个 UI 面板时，检查以下文件是否均已更新：

- [ ] `TableNineUIKeys.cs`：添加 UIKey 常量
- [ ] `UIPanelMetadataCatalog.cs`：添加 PanelMeta 条目（中文名、描述、分类）
- [ ] `TableNineUIPanelRegistry.cs`：在 `CreateRecommendedDefaults()` 添加默认配置（可选）
- [ ] `RuntimeUIEvents.cs`：定义领域事件（如果需要新事件）
- [ ] `TableNineUIRouter.cs`：监听事件并组装 PanelData
- [ ] 对应 Panel 脚本或 Fallback 策略已就绪
- [ ] `TableNineUIPanelRegistry.asset`：同步推荐默认值或手动添加条目
- [ ] 本文档的枚举速查表（如有新增枚举值）
