using System;
using System.Collections.Generic;
using QFramework;
using UnityEngine;

public sealed class TableNineUIRouter : IController, IDisposable
{
    private readonly TableNineUIPanelRegistry mRegistry;
    private readonly List<IUnRegister> mEventRegisters = new List<IUnRegister>();

    public TableNineUIRouter(TableNineUIPanelRegistry registry)
    {
        mRegistry = registry;
    }

    public IArchitecture GetArchitecture()
    {
        return TableNine.Interface;
    }

    public void Start()
    {
        Register<PopupRequestedEvent>(OnPopupRequested);
        Register<AttributeChoiceRequestedEvent>(OnAttributeChoiceRequested);
        Register<AttributeChoiceResolvedEvent>(_ => TableNineUIRuntime.Close(mRegistry, TableNineUIKeys.AttributeChoice));
        Register<RoomChoiceRequestedEvent>(OnRoomChoiceRequested);
        Register<RoomChosenEvent>(_ => TableNineUIRuntime.Close(mRegistry, TableNineUIKeys.RoomChoice));
        Register<HelpRewardGeneratedEvent>(OnHelpRewardGenerated);
        Register<HelpRewardPickedEvent>(_ => CloseHelpRewardAndPromptNextNode());
        Register<HelpRewardSkippedEvent>(_ => CloseHelpRewardAndPromptNextNode());
        Register<ChestRewardGeneratedEvent>(OnChestRewardGenerated);
        Register<RelicRewardPickedEvent>(_ => TableNineUIRuntime.Close(mRegistry, TableNineUIKeys.ChestReward));
        Register<ChestRewardSkippedEvent>(_ => TableNineUIRuntime.Close(mRegistry, TableNineUIKeys.ChestReward));
        Register<ShopOpenedEvent>(_ => OpenShop());
        Register<HelpCardPurchasedEvent>(_ => RefreshShopIfOpen());
        Register<HelpCardDeletedForGoldEvent>(_ => RefreshShopIfOpen());
        Register<NodeCompletedEvent>(_ => TableNineUIRuntime.Close(mRegistry, TableNineUIKeys.NextNodePrompt));
        Register<NodeAdvancedEvent>(_ => TableNineUIRuntime.Close(mRegistry, TableNineUIKeys.NextNodePrompt));
    }

    public void Dispose()
    {
        for (var i = 0; i < mEventRegisters.Count; i++)
        {
            mEventRegisters[i].UnRegister();
        }

        mEventRegisters.Clear();
    }

    private void Register<TEvent>(Action<TEvent> handler)
    {
        mEventRegisters.Add(this.RegisterEvent(handler));
    }

    private void OnPopupRequested(PopupRequestedEvent evt)
    {
        TableNineUIRuntime.Open(mRegistry, new TableNineUIRequestPanelData
        {
            Key = TableNineUIKeys.PopupMessage,
            Title = "提示",
            Message = evt.Message,
            CloseLabel = "确定"
        });
    }

    private void OnAttributeChoiceRequested(AttributeChoiceRequestedEvent evt)
    {
        var data = new TableNineUIRequestPanelData
        {
            Key = TableNineUIKeys.AttributeChoice,
            Title = "属性提升",
            Message = "选择一项永久提升。",
            CloseOnChoice = false
        };

        data.Choices.Add(TableNineUIChoiceData.Command("attack", "攻击 +1", "提升后立即影响战斗。", string.Empty,
            controller => controller.SendCommand(new ResolveAttributeChoiceCommand(AttributeUpgradeChoice.Attack))));
        data.Choices.Add(TableNineUIChoiceData.Command("defense", "防御 +1", "降低后续受到的伤害。", string.Empty,
            controller => controller.SendCommand(new ResolveAttributeChoiceCommand(AttributeUpgradeChoice.Defense))));
        data.Choices.Add(TableNineUIChoiceData.Command("max_hp", "生命 +2", "提高生命上限并恢复 2 点。", string.Empty,
            controller => controller.SendCommand(new ResolveAttributeChoiceCommand(AttributeUpgradeChoice.MaxHp))));

        TableNineUIRuntime.Open(mRegistry, data);
    }

    private void OnRoomChoiceRequested(RoomChoiceRequestedEvent evt)
    {
        var configModel = this.GetModel<IConfigModel>();
        var data = new TableNineUIRequestPanelData
        {
            Key = TableNineUIKeys.RoomChoice,
            Title = "选择下一个房间",
            Message = "节点已清空，仍可在下方按钮外拾取或使用帮助卡。",
            CloseOnChoice = false
        };

        for (var i = 0; i < evt.RoomIds.Count; i++)
        {
            var roomId = evt.RoomIds[i];
            var capturedRoomId = roomId;
            var room = configModel.GetRoomDefinition(roomId);
            data.Choices.Add(TableNineUIChoiceData.Command(roomId, room.DisplayName, DescribeRoom(room), string.Empty,
                controller => controller.SendCommand(new ChooseRoomCommand(capturedRoomId))));
        }

        TableNineUIRuntime.Open(mRegistry, data);
    }

    private void OnHelpRewardGenerated(HelpRewardGeneratedEvent evt)
    {
        TableNineUIRuntime.Close(mRegistry, TableNineUIKeys.ShopMain);
        TableNineUIRuntime.Close(mRegistry, TableNineUIKeys.ChestReward);
        TableNineUIRuntime.Close(mRegistry, TableNineUIKeys.NextNodePrompt);

        var configModel = this.GetModel<IConfigModel>();
        var data = new TableNineUIRequestPanelData
        {
            Key = TableNineUIKeys.HelpReward,
            Title = "选择帮助卡",
            Message = "选择一张加入帮助卡组，或跳过换金币。",
            CloseOnChoice = false,
            CloseLabel = $"+{RewardConstants.SkipHelpRewardGold} 金币"
        };

        for (var i = 0; i < evt.CardIds.Count; i++)
        {
            var cardId = evt.CardIds[i];
            var capturedCardId = cardId;
            var card = configModel.GetCardDefinition(cardId);
            data.Choices.Add(TableNineUIChoiceData.Command(cardId, card.DisplayName, DescribeCard(card), $"{card.Quality}",
                controller => controller.SendCommand(new PickHelpCardRewardCommand(capturedCardId))));
        }

        data.CloseAction = controller => controller.SendCommand(new SkipHelpRewardCommand());
        TableNineUIRuntime.Open(mRegistry, data);
    }

    private void OnChestRewardGenerated(ChestRewardGeneratedEvent evt)
    {
        var configModel = this.GetModel<IConfigModel>();
        var data = new TableNineUIRequestPanelData
        {
            Key = TableNineUIKeys.ChestReward,
            Title = "选择遗物",
            Message = "选择一件遗物，或跳过换金币。",
            CloseOnChoice = false,
            CloseLabel = $"+{RewardConstants.SkipChestRewardGold} 金币"
        };

        for (var i = 0; i < evt.RelicIds.Count; i++)
        {
            var relicId = evt.RelicIds[i];
            var capturedRelicId = relicId;
            var relic = configModel.GetRelicDefinition(relicId);
            data.Choices.Add(TableNineUIChoiceData.Command(relicId, relic.DisplayName, DescribeRelic(relic), $"{relic.Quality}",
                controller => controller.SendCommand(new PickRelicRewardCommand(capturedRelicId))));
        }

        data.CloseAction = controller => controller.SendCommand(new SkipChestRewardCommand());
        TableNineUIRuntime.Open(mRegistry, data);
    }

    private void OpenShop()
    {
        var rewardModel = this.GetModel<IRewardModel>();
        var deckModel = this.GetModel<IDeckModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var configModel = this.GetModel<IConfigModel>();
        var data = new TableNineUIRequestPanelData
        {
            Key = TableNineUIKeys.ShopMain,
            Title = "商店",
            Message = "购买帮助卡，或删除已有帮助卡换金币。",
            CloseOnChoice = false,
            CloseLabel = "离开"
        };

        for (var i = 0; i < rewardModel.ShopCardIds.Count; i++)
        {
            var cardId = rewardModel.ShopCardIds[i];
            var capturedCardId = cardId;
            var card = configModel.GetCardDefinition(cardId);
            data.Choices.Add(TableNineUIChoiceData.Command(cardId, card.DisplayName, DescribeCard(card), $"{card.Price}G",
                controller => controller.SendCommand(new BuyHelpCardCommand(capturedCardId))));
        }

        for (var i = 0; i < deckModel.OwnedHelpCards.Count; i++)
        {
            var uid = deckModel.OwnedHelpCards[i];
            if (!deckModel.HelpCardStates.TryGetValue(uid.Value, out var state) || state.IsPermanentlyRemoved)
            {
                continue;
            }

            if (!collectionModel.TryGetCard(uid, out var runtime))
            {
                continue;
            }

            var capturedUid = uid;
            data.SecondaryChoices.Add(TableNineUIChoiceData.Command(uid.Value.ToString(), runtime.DisplayName, "删除这张帮助卡。", $"+{RewardConstants.DeleteHelpCardGold}G",
                controller => controller.SendCommand(new DeleteHelpCardForGoldCommand(capturedUid))));
        }

        data.CloseAction = controller => controller.SendCommand(new CloseShopCommand());
        TableNineUIRuntime.Open(mRegistry, data);
    }

    private void RefreshShopIfOpen()
    {
        if (this.GetModel<IFlowModel>().Phase.Value == FlowPhase.Shop)
        {
            OpenShop();
        }
    }

    private void CloseHelpRewardAndPromptNextNode()
    {
        TableNineUIRuntime.Close(mRegistry, TableNineUIKeys.HelpReward);
        OpenNextNodePrompt();
    }

    private void OpenNextNodePrompt()
    {
        var runModel = this.GetModel<IRunModel>();
        TableNineUIRuntime.Open(mRegistry, new TableNineUIRequestPanelData
        {
            Key = TableNineUIKeys.NextNodePrompt,
            Title = "节点奖励已结算",
            Message = $"第 {runModel.Layer.Value} 层第 {runModel.NodeInLayer.Value} 节点完成。",
            CloseLabel = "下一节点",
            CloseAction = controller => controller.SendCommand(new ProceedToNextNodeCommand())
        });
    }

    private static string DescribeRoom(RoomDefinition room)
    {
        switch (room.RoomType)
        {
            case RoomType.Gold:
                return $"+{room.RewardGold} 金币";
            case RoomType.Chest:
                return "打开遗物选择";
            case RoomType.Attribute:
                return "获得属性提升卡";
            case RoomType.Shop:
                return "购买或删除帮助卡";
            default:
                return room.RoomType.ToString();
        }
    }

    private static string DescribeCard(CardDefinition card)
    {
        if (card.CardType == CardType.Help)
        {
            return $"帮助卡 / {card.Quality}";
        }

        if (card.CardType == CardType.Monster)
        {
            return $"HP {card.BaseHp} ATK {card.BaseAttack} DEF {card.BaseDefense}";
        }

        return card.CardType.ToString();
    }

    private static string DescribeRelic(RelicDefinition relic)
    {
        var parts = new List<string>();
        if (relic.StatAttackBonus != 0) parts.Add($"攻 {Signed(relic.StatAttackBonus)}");
        if (relic.StatDefenseBonus != 0) parts.Add($"防 {Signed(relic.StatDefenseBonus)}");
        if (relic.StatMaxHpBonus != 0) parts.Add($"血 {Signed(relic.StatMaxHpBonus)}");
        if (!string.IsNullOrWhiteSpace(relic.TriggerDescription)) parts.Add(relic.TriggerDescription);
        return parts.Count > 0 ? string.Join(" / ", parts) : relic.Quality.ToString();
    }

    private static string Signed(int value)
    {
        return value > 0 ? $"+{value}" : value.ToString();
    }
}

public static class TableNineUIRuntime
{
    public static UIPanel Open(TableNineUIPanelRegistry registry, TableNineUIRequestPanelData data)
    {
        var entry = registry != null ? registry.GetEntryOrDefault(data.Key) : null;
        if (entry == null)
        {
            Debug.LogWarning($"UI entry missing and no registry is available: {data.Key}");
            return null;
        }

        data.UIType = entry.UIType;
        data.FallbackStrategy = entry.FallbackStrategy;
        data.BlocksGameplayInput = entry.BlocksGameplayInput;

        // 注入 Fallback 组件化预制件引用
        if (registry != null)
        {
            data.FallbackButtonPrefab = registry.FallbackButtonPrefab;
            data.FallbackTextPrefab = registry.FallbackTextPrefab;
            data.FallbackIconPrefab = registry.FallbackIconPrefab;
            data.FallbackPanelPrefab = registry.FallbackPanelPrefab;
            data.FallbackScrollViewPrefab = registry.FallbackScrollViewPrefab;
        }

        if (entry.HasFormalPrefab)
        {
            return OpenPanel(entry.PanelName, null, entry.Level, entry.OpenType, data);
        }

        if (entry.FallbackStrategy == TableNineUIFallbackStrategy.None)
        {
            Debug.LogWarning($"UI entry has no formal prefab and no fallback: {data.Key}");
            return null;
        }

        return OpenPanel(GetFallbackPanelName(data.Key), typeof(UIFallbackPanel), entry.Level, entry.OpenType, data);
    }

    public static void Close(TableNineUIPanelRegistry registry, string uiKey)
    {
        var entry = registry != null ? registry.GetEntry(uiKey) : null;
        if (entry != null && !string.IsNullOrWhiteSpace(entry.PanelName))
        {
            ClosePanel(entry.PanelName);
        }

        ClosePanel(GetFallbackPanelName(uiKey));
    }

    private static UIPanel OpenPanel(string panelName, Type panelType, UILevel level, PanelOpenType openType, IUIData data)
    {
        var keys = PanelSearchKeys.Allocate();
        keys.GameObjName = panelName;
        keys.PanelType = panelType;
        keys.Level = level;
        keys.OpenType = openType;
        keys.UIData = data;

        var panel = UIManager.Instance.OpenUI(keys) as UIPanel;
        keys.Recycle2Cache();
        return panel;
    }

    private static void ClosePanel(string panelName)
    {
        if (string.IsNullOrWhiteSpace(panelName))
        {
            return;
        }

        var keys = PanelSearchKeys.Allocate();
        keys.GameObjName = panelName;
        UIManager.Instance.CloseUI(keys);
        keys.Recycle2Cache();
    }

    private static string GetFallbackPanelName(string uiKey)
    {
        return TableNineUIKeys.GeneratedFallbackPanelPrefix + Sanitize(uiKey);
    }

    private static string Sanitize(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "missing";
        }

        var chars = value.ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '_' && chars[i] != '-')
            {
                chars[i] = '_';
            }
        }

        return new string(chars);
    }
}
