# Markdown
## 1. 你当前已有，但需要补字段/状态的常驻 UI

| 你现在的面板             | Web 端实际需求                                                                                             | 你需要核对补充                                                                                                                                                                   |
| ------------------ | ----------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `PlayerInfoPanel`  | 玩家信息显示：姓名、血量、攻击、防御、金币，并有血条和节点信息。Web 初始玩家数据包含 `hp/maxHp/attack/defense/gold`。 HUD 里还会显示当前层、节点、精英/层主标记。 | 你现在有姓名、头像、血量、金币。建议补：`AttackIcon/AttackNumber`、`DefenseIcon/DefenseNumber`、`HpBar`、`NodeInfoText`。                                                                         |
| `RelicPanel`       | Web 是 12 格遗物栏，4 列 × 3 行；遗物格可悬停显示名称，右键丢弃遗物并 +20 金币。                                                    | 你有 `Grids`，方向对。核对是否是 12 格；另补“遗物详情展示/悬停描述”和“丢弃交互”。PC 可右键，移动端建议长按或单独丢弃按钮。                                                                                                   |
| `DeckInfoPanel`    | Web 的牌组区不是简单敌方/己方两个堆，而是显示“下一张抽牌预览”、战斗卡组数量、帮助卡组种类/数量/容量。 更新时会显示下一张是怪物卡还是帮助卡及其名称/属性/效果。                 | 你现在有 `EnemyDecksBG/Ani`、`PlayerDecksBG/Ani`。建议补：`NextCardPreviewRoot`、`NextCardTypeText`、`NextCardNameText`、`NextCardStatText`、`BattleDeckCountText`、`HelpDeckCountText`。 |
| `SkillPanel`       | Web 技能栏显示已习得技能，初始技能是“轻车熟路”，击败精英后会增加技能。 技能栏本身是一个文本区域。                                                  | 你有 `SkillText` 和 `Grids`，可以保留。核对是否支持多行技能、技能描述或点击查看详情。                                                                                                                     |
| `DescriptionPanel` | Web 有一个怪物技能详情面板，鼠标悬停怪物时显示怪物名、词条说明、动态 buff，例如战舞、皇室、庇佑、决斗。                                              | 你图里有两个 `DescriptionPanel`，建议其中一个明确做 `MonsterSkillTooltipPanel`，另一个做通用卡牌/遗物描述。至少需要：标题、词条列表、效果描述、动态状态行。**当前 HUD 内 DescriptionText 单行上限 40 字（含空格，勿换行），文案在 UI 面板管理 → DescriptionPanel 描述文案 维护。** |
| 下方道具牌格，已放世界空间      | Web 是 5 个道具牌格，显示“道具牌格（上限5）”，空格显示序号，有卡时显示名称、效果、“点击使用”。                                                 | 你说已放世界空间，核对：5 格、空格序号、卡名、效果、点击使用、选中高亮。                                                                                                                                     |

## 2. 你现在明显缺的常驻 UI / 按钮

**底部战斗日志 `LogText`**
Web 底部有一行状态日志，默认文案是“点击怪物战斗 / 点击帮助卡拾取”，各种操作都会更新它。
建议你在 `UIGameplayPanel` 下补一个：`BattleLogText`。很多反馈都靠它，例如金币不足、道具牌格已满、只能攻击相邻格、瞄准模式提示等。

**通关按钮 `PassButton / CompleteNodeButton`**
Web 在当前节点怪物清空且战斗牌组空后，在九宫格下方显示“🏆 通关”按钮，点了才进入奖励环节。
你现在的 `PassRoot` 在 Overlay 里，但 Web 的通关按钮是战斗场景里的按钮，不是选择弹窗里的按钮。建议补一个独立的 `NodePassButton`，可以挂在世界空间九宫格下方，或 UI 层对齐九宫格下方。

**瞄准模式视觉反馈**
飞刀、火球术、撞击教程、破击锤、交换卡等会进入瞄准模式：高亮所有怪物、选中的道具格高亮、鼠标变准星、Esc/点击其他取消。 
Unity 这边建议补：`TargetingHintText` 或复用 `BattleLogText`，以及道具格/怪物卡的选中边框状态。移动端最好额外有一个 `CancelTargetButton`，因为不能依赖 Esc。

## 3. 你 `UIChoiceOverlayPanel` 里缺的弹窗类型

你现在有：

```text
3or2for1ChoiseWindow
ShopChoiseWindow
DeleteCardChoiseWindow
```

这三个只覆盖了一部分。Web 端 OverlayManager 实际有这些模态界面：

### A. 帮助卡三选一窗口：你基本已有，但要补 Skip +10

Web 的帮助卡选择是 3 张卡横排，标题“选择一张帮助卡加入卡组”，每张卡显示名称、品质、效果、点击选择，并有“跳过 → +10 金币”。
你的 `3or2for1ChoiseWindow` 可以复用，但要确认：

```text
TitleText
ChoiceButtonRoot / 3 个卡牌按钮
每个按钮：NameText / QualityText / DescText / ActionText
PassRoot：跳过 → +10金币
```

### B. 房间二选一窗口：目前看截图里没有独立模式

每个节点通关后，Web 会先显示“选择下一个房间类型”，随机给 2 个房间：商店、金币房、宝箱房、属性房。 弹窗显示房间 icon、名称、描述、选择按钮。

建议你补一个模式或独立窗口：

```text
RoomChoiceWindow
- TitleText：选择下一个房间类型
- RoomButtonRoot：2 个房间卡
  - IconText
  - NameText
  - DescText
  - SelectText
```

这个可以复用 `3or2for1ChoiseWindow`，但逻辑上不是“选卡”，而是“选房间”，没有跳过按钮。

### C. 商店窗口：你有，但要确认 6 个商品 + 删除 + 下一节点

Web 商店一次展示 6 个商品，商品可能显示“已售出”；底部有“删除帮助卡(+10金币)”和“前往下一节点”。  

你的 `ShopChoiseWindow` 需要核对：

```text
ShopChoiceWindow
- TitleText：商店 — 金币: X
- GoodsRoot：6 个 ShopCardButton
  - NameText
  - QualityText
  - EffectText
  - PriceText：💰价格 购买
  - SoldOutState：已售出
- DeleteHelpCardButton：删除帮助卡(+10金币)
- NextNodeButton：前往下一节点
```

你图里的 `ShopChoiseArrowRoot` 在 Web 里没有对应需求；Web 不是翻页商店，而是 6 个直接展示。如果 Unity 想做滚动/翻页可以保留，但不是策划版本必需。

### D. 删除帮助卡窗口：你有，但要补“返回商店”和空状态

Web 删除界面显示当前帮助卡组，4 列排布，每张卡有“删除 +10金币”；卡组为空时显示“帮助卡组为空”；底部有“返回商店”。

你的 `DeleteCardChoiseWindow` 建议核对：

```text
DeleteCardChoiceWindow
- TitleText：删除帮助卡 — 金币: X
- CardGridRoot：动态若干帮助卡，建议 4 列
  - NameText
  - QualityText
  - EffectText
  - DeleteButton：删除 +10金币
- EmptyText：帮助卡组为空
- BackToShopButton：返回商店
```

### E. 宝箱三选一遗物窗口：截图里没看到，需要新增

使用普通/蓝色/金色宝箱卡时，Web 会弹“宝箱 — 三选一遗物”，显示 3 个遗物卡，另有“跳过 → +20金币”。 宝箱卡本身有普通、蓝色、金色三种概率。 

建议新增：

```text
ChestRelicChoiceWindow
- TitleText：宝箱 — 三选一遗物
- RelicChoiceRoot：3 个遗物卡
  - NameText
  - QualityText
  - EffectText
  - SelectText
- SkipButton：跳过 → +20金币
```

### F. 导师卡技能三选一窗口：截图里没看到，需要新增或复用三选一

击败精英后会出现导师卡，随机 3 个技能让玩家永久习得，没有跳过按钮。 

建议新增模式：

```text
MentorSkillChoiceWindow
- TitleText：导师卡 — 选择一项技能永久习得
- SkillChoiceRoot：3 个技能卡
  - SkillNameText
  - SkillDescText
  - SelectText
```

也可以复用 `3or2for1ChoiseWindow`，但要能隐藏 `PassRoot`。

### G. 属性提升三选一按钮窗口：截图里没看到，需要新增

使用“属性提升卡”时，Web 弹“属性提升 — 选择一项”，三个竖向按钮：攻击 +1、防御 +1、血量 +2。 

建议新增：

```text
AttributeBoostWindow
- TitleText：属性提升 — 选择一项
- AttackButton：攻击 +1（当前: X）
- DefenseButton：防御 +1（当前: X）
- MaxHpButton：血量 +2（当前: X）
```

### H. 第一层通关提示窗口：截图里没看到，需要新增或用 Popup

第一层 9 个节点完成后，Web 显示“第一层通关”覆盖层和通关说明。
建议新增：

```text
LayerCompleteWindow
- TitleText：第一层通关
- DescText：恭喜！第一层9个节点已全部通过...
```

## 4. `UIPopupPanel` 需要支持的行为

Web 的 Popup 是一个全屏半透明遮罩 + 中间提示框，点击遮罩/框/文字都会关闭。
你现在 `UIPopupPanel > InfoWindow > CloseButton` 是有基础的。建议核对：

```text
UIPopupPanel
- Mask
- InfoWindow
  - MessageText
  - CloseButton
```

它要能展示这些错误提示：帮助卡组已满、同名卡达到上限、金币不足、遗物格满、没有可瞄准怪物、道具牌格已满等。

## 5. 推荐你最终整理成这套 Unity 层级

你可以在现有结构上这样补，改动最小：

```text
UIGameplayPanel
  PlayerInfoPanel
    CharacterName
    PortraitImage
    LifeIcon
    LifeNumber
    HpBar
    AttackIcon
    AttackNumber
    DefenseIcon
    DefenseNumber
    CoinIcon
    CoinNumber
    NodeInfoText

  RelicPanel
    RelicText
    Grids  // 12格
    RelicTooltipRoot / 或复用 DescriptionPanel

  DeckInfoPanel
    NextCardPreviewRoot
      CardBG
      TypeText
      NameText
      StatOrEffectText
    BattleDeckCountText
    HelpDeckCountText
    EnemyDecksBG / EnemyDecksAni  // 可保留做表现
    PlayerDecksBG / PlayerDecksAni

  SkillPanel
    SkillText
    Grids

  MonsterSkillTooltipPanel
    TitleText
    DescriptionText

  GeneralDescriptionPanel
    DescriptionText

  BattleLogText
  NodePassButton

  UIChoiceOverlayPanel
    Mask
    HelpCardChoiceWindow
    RoomChoiceWindow
    ShopChoiceWindow
    DeleteCardChoiceWindow
    ChestRelicChoiceWindow
    MentorSkillChoiceWindow
    AttributeBoostWindow
    LayerCompleteWindow

  UIPopupPanel
    Mask
    InfoWindow
      MessageText
      CloseButton
```