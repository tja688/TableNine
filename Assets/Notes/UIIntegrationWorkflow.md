# UI 接入工作流

> 日期：2026-06-11  
> 目的：在正式 UI 逐步制作前，先建立 Gameplay 与 UI 表现的稳定接入面，避免继续扩大硬编码面板引用、临时查找和强绑定。

## 1. 当前 UI 触发点

当前 Gameplay 中会触发 UI 或 UI 状态变化的地方主要有：

| 触发来源 | 事件 / Command | UI Key | 当前表现 |
|---|---|---|---|
| HUD 刷新 | `BattleDeckChangedEvent`、`DamageAppliedEvent`、`ItemSlotChangedEvent`、`GoldChangedEvent` 等 | `gameplay.hud` | 正式 `UIGameplayPanel` |
| 普通提示 | `PopupRequestedEvent` | `popup.message` | 正式 `UIPopupPanel`，缺失时 fallback |
| 属性提升卡 | `AttributeChoiceRequestedEvent` | `choice.attribute` | 正式 `UIChoiceOverlayPanel`，缺失时 fallback |
| 节点清空后选择房间 | `RoomChoiceRequestedEvent` | `choice.room` | fallback，后续可替换正式面板 |
| 帮助卡奖励 | `HelpRewardGeneratedEvent` | `reward.help` | fallback，带跳过按钮 |
| 宝箱遗物奖励 | `ChestRewardGeneratedEvent` | `reward.chest` | fallback，带跳过按钮 |
| 商店 | `ShopOpenedEvent` | `shop.main` | fallback，支持购买和删卡换金币 |
| 奖励链完成后进入下一节点 | `HelpRewardPickedEvent` / `HelpRewardSkippedEvent` | `prompt.next_node` | fallback 状态提示 |

## 2. 接入结构

UI 接入链路固定为：

```text
Command / System
  -> 发送领域事件或 UI 请求事件
  -> TableNineUIRouter 组装 TableNineUIRequestPanelData
  -> TableNineUIPanelRegistry 根据 UI Key 查正式 prefab
  -> 有正式 prefab：按 PanelName 交给 UIKit 打开
  -> 无正式 prefab：按 FallbackStrategy 打开 UIFallbackPanel
  -> 按钮只发送 Command，不直接修改 Model
```

Gameplay 代码只表达“需要选择房间 / 需要奖励选择 / 需要弹提示”，不引用具体 prefab 或按钮。

## 3. 配置资产

集中配置资产：

```text
Assets/ScriptableObjects/TableNineUIPanelRegistry.asset
```

每条配置包含：

| 字段 | 用途 |
|---|---|
| `UIKey` | 稳定接入键，例如 `reward.help`。Gameplay 和文档只认这个 key。 |
| `UIType` | UI 类型，用于识别 HUD、Popup、Reward、Shop、Confirm 等语义。 |
| `PanelName` | UIKit 面板名。正式 prefab 接入时通常与面板脚本名一致。 |
| `Prefab` | 正式 UI prefab。为空时走 fallback。 |
| `Level` | UIKit 层级。HUD 用 `Common`，覆盖层一般用 `PopUI`。 |
| `OpenType` | `Single` 或 `Multiple`。奖励、商店、房间选择一般 `Single`，普通 popup 可 `Multiple`。 |
| `FallbackStrategy` | 正式 prefab 缺失时使用的原子 UI 拼装策略。 |
| `BlocksGameplayInput` | fallback 是否生成全屏遮罩阻止底层点击。房间选择为 false，因为清场后仍允许拾取/使用帮助卡。 |
| `Notes` | 给策划/UI/开发说明用途和特殊规则。 |

注册表运行时会合并推荐默认 key；旧资产只填 PanelName 也不会断。但正式协作时仍建议在资产里明确填写 UIKey。

## 4. Fallback 策略

fallback 只允许由 `UIFallbackPanel` 统一创建，不允许业务代码到处临时 `new GameObject` 拼 UI。

| 策略 | 用途 |
|---|---|
| `AtomicPopup` | 居中标题、正文、确认按钮。 |
| `AtomicChoiceList` | 标题、说明、纵向选项列表、可选关闭按钮。 |
| `AtomicRoomChoice` | 底部非阻塞房间按钮组，只拦截按钮区域点击。 |
| `AtomicShopList` | 商店购买列表 + 删除帮助卡列表 + 离开按钮。 |
| `AtomicStatusPrompt` | 小型状态提示，例如“下一节点”。 |

fallback 原子组件当前包含：标题文本、正文文本、按钮、简单列表、滚动列表、遮罩、状态条。它们只保证流程可跑通，不作为最终视觉标准。

## 5. 新增 UI 需求流程

新增 UI 需求时按以下顺序接入：

1. 定义稳定 UI Key，放到 `TableNineUIKeys`，命名使用 `domain.intent`，例如 `reward.boss`、`confirm.delete_help_card`。
2. Gameplay 层新增领域事件，或复用已有事件。事件里传候选 id / 上下文，不传 prefab、按钮、Transform。
3. 在 `TableNineUIRouter` 中监听事件，组装 `TableNineUIRequestPanelData`。
4. 按钮行为只发送 Command，例如 `PickHelpCardRewardCommand`、`CloseShopCommand`。
5. 在 `TableNineUIPanelRegistry.asset` 中新增 UIKey 配置，先设 fallback，正式 prefab 做好后只补 `Prefab`。
6. 正式 UI 面板可以读取同一个 `TableNineUIRequestPanelData`，也可以定义自己的 PanelData，但不得让 Gameplay 直接引用它。
7. 补一条 EditMode 测试验证事件、锁、候选数据或 Command 流转。

## 6. 维护公约

- `Command` / `System` 不打开 UIKit 面板，不引用 `UIPanel`、`Button`、`TMP_Text`、prefab。
- UI 按钮不直接改 Model；状态变更只发 Command。
- UI 显示从 Model / Query / 事件数据读取。
- 新覆盖层如果会阻止底层点击，Command 进入该阶段时必须加 `InputLockReason.OverlayVisible`，完成/跳过时解锁。
- 清场房间选择是例外：它是非阻塞 UI，不加 Overlay 锁，因为玩家仍可拾取或使用帮助卡。
- 不在新 UI 代码中使用 `GameObject.Find`、硬编码层级路径、深层自动查找作为正式绑定方案。临时兼容旧 prefab 的 `FindDeep` 应逐步迁出。
- fallback 只能用于“正式 UI 尚未制作”的阶段；当正式 prefab 完成时，优先在 Registry 里绑定，不改 Gameplay。
- 新增 UI Key 后同步更新本文档或 `Assets/Docs/06-UI/界面布局.md` 的索引。

## 7. 现有遗留点

- `GameplayWorldPresenter` 仍有旧场景对象自动查找逻辑，用于兼容当前 Bootstrap 场景。后续重做棋盘/卡牌表现时，应改为序列化引用或专用配置资产。
- `UIChoiceOverlayPanel` 仍保留旧 `ChoiseButton` 命名兼容。正式 prefab 可按新命名重做，并继续接收 `TableNineUIRequestPanelData`。
- 商店删卡当前 fallback 直接列出可删除帮助卡；正式 UI 后续建议加确认面板，对应 `confirm.delete_help_card`。
