using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using QFramework;

/// <summary>
/// M3 奖励闭环测试：通关 → 房间选择 → 帮助卡奖励 → 下一节点
/// 覆盖：帮助卡快照恢复、未使用帮助卡金币结算、跳过选卡奖励、
///       容量/同名上限、商店购买/删除、遗物系统、节点推进。
/// </summary>
[Category(TableNineTestCategories.LegacyRuleTests)]
public sealed class TableNineM3EditModeTests
{
    [SetUp]
    public void SetUp()
    {
        TableNine.ResetForTests();
    }

    [TearDown]
    public void TearDown()
    {
        TableNine.ResetForTests();
    }

    // ========================
    // 帮助卡快照恢复
    // ========================

    [Test]
    public void HelpDeck_Snapshot_Restores_Temporary_Removed_Cards()
    {
        StartRun(42);
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();

        var initialCount = deckModel.OwnedHelpCards.Count;

        // 找到一张帮助卡并临时移除（模拟使用但未消耗）
        var helpUid = FindFirstHelpCard();
        deckModel.HelpCardStates[helpUid.Value].IsTemporarilyRemoved = true;

        // 触发快照恢复
        var rewardSystem = TableNine.Interface.GetSystem<IRewardSystem>();
        rewardSystem.RestoreHelpDeckSnapshotByRestoreAfterNode();

        // 临时移除的帮助卡应被恢复
        var state = deckModel.HelpCardStates[helpUid.Value];
        Assert.That(state.IsTemporarilyRemoved, Is.False, "临时移除的帮助卡应在快照恢复后复原");
        Assert.That(state.IsOnBoard, Is.False);
        Assert.That(state.IsInItemSlot, Is.False);
        Assert.That(deckModel.OwnedHelpCards.Count, Is.EqualTo(initialCount), "帮助卡总数不变");
    }

    [Test]
    public void HelpDeck_Permanent_Removed_Cards_Cleaned_Up_On_Restore()
    {
        StartRun(42);
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();

        var initialCount = deckModel.OwnedHelpCards.Count;

        // 找到一张帮助卡并永久移除（模拟使用了药水/飞刀）
        var helpUid = FindFirstHelpCard();
        deckModel.HelpCardStates[helpUid.Value].IsPermanentlyRemoved = true;

        // 触发快照恢复
        var rewardSystem = TableNine.Interface.GetSystem<IRewardSystem>();
        rewardSystem.RestoreHelpDeckSnapshotByRestoreAfterNode();

        // 永久移除的帮助卡应从 OwnedHelpCards 和 HelpCardStates 中清除
        Assert.That(deckModel.HelpCardStates.ContainsKey(helpUid.Value), Is.False,
            "永久移除的帮助卡应从 HelpCardStates 清除");
        Assert.That(deckModel.OwnedHelpCards.Contains(helpUid), Is.False,
            "永久移除的帮助卡应从 OwnedHelpCards 移除");
        Assert.That(collectionModel.TryGetCard(helpUid, out _), Is.False,
            "永久移除的帮助卡应从 CollectionModel 清除");
        Assert.That(deckModel.OwnedHelpCards.Count, Is.EqualTo(initialCount - 1),
            "帮助卡总数应减 1");
    }

    [Test]
    public void HelpDeck_New_Cards_Acquired_During_Node_Are_Preserved()
    {
        StartRun(42);
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var configModel = TableNine.Interface.GetModel<IConfigModel>();

        // 快照已保存（StartNodeCommand 中调用），现在添加一张新卡
        var newDef = configModel.GetCardDefinition(DefaultGameConfigFactory.HelpBlessingId);
        var newRuntime = collectionModel.CreateCard(newDef);
        deckModel.OwnedHelpCards.Add(newRuntime.Uid);
        deckModel.HelpCardStates[newRuntime.Uid.Value] = new HelpCardState
        {
            Uid = newRuntime.Uid,
            DefinitionId = newRuntime.DefinitionId
        };

        var countBeforeRestore = deckModel.OwnedHelpCards.Count;

        // 触发快照恢复
        var rewardSystem = TableNine.Interface.GetSystem<IRewardSystem>();
        rewardSystem.RestoreHelpDeckSnapshotByRestoreAfterNode();

        // 新卡应保留
        Assert.That(deckModel.OwnedHelpCards.Contains(newRuntime.Uid), Is.True,
            "节点中新获得的帮助卡应在快照恢复后保留");
        Assert.That(deckModel.OwnedHelpCards.Count, Is.EqualTo(countBeforeRestore),
            "总帮助卡数应保持不变");

        // 新卡状态应为干净
        var newState = deckModel.HelpCardStates[newRuntime.Uid.Value];
        Assert.That(newState.IsOnBoard, Is.False);
        Assert.That(newState.IsInItemSlot, Is.False);
        Assert.That(newState.IsTemporarilyRemoved, Is.False);
    }

    [Test]
    public void HelpDeck_Restore_Emits_Event_With_Correct_Counts()
    {
        StartRun(42);
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var configModel = TableNine.Interface.GetModel<IConfigModel>();

        // 永久移除 1 张
        var permUid = FindFirstHelpCard();
        deckModel.HelpCardStates[permUid.Value].IsPermanentlyRemoved = true;

        // 临时移除 1 张
        var tempUid = FindFirstHelpCardExcluding(permUid);
        deckModel.HelpCardStates[tempUid.Value].IsTemporarilyRemoved = true;

        // 新增 1 张
        var newDef = configModel.GetCardDefinition(DefaultGameConfigFactory.HelpBlessingId);
        var newRuntime = collectionModel.CreateCard(newDef);
        deckModel.OwnedHelpCards.Add(newRuntime.Uid);
        deckModel.HelpCardStates[newRuntime.Uid.Value] = new HelpCardState
        {
            Uid = newRuntime.Uid,
            DefinitionId = newRuntime.DefinitionId
        };

        var events = new List<HelpDeckRestoredEvent>();
        var unReg = TableNine.Interface.RegisterEvent<HelpDeckRestoredEvent>(events.Add);

        var rewardSystem = TableNine.Interface.GetSystem<IRewardSystem>();
        rewardSystem.RestoreHelpDeckSnapshotByRestoreAfterNode();

        unReg.UnRegister();

        Assert.That(events.Count, Is.EqualTo(1));
        Assert.That(events[0].PermanentRemovedCount, Is.EqualTo(1), "永久移除数应为 1");
        Assert.That(events[0].NewCardCount, Is.EqualTo(1), "新卡数应为 1");
        Assert.That(events[0].RestoredCount, Is.GreaterThan(0), "恢复数应 > 0");
    }

    // ========================
    // 未使用帮助卡金币结算
    // ========================

    [Test]
    public void Unused_HelpCards_Give_10_Gold_Each()
    {
        StartRun(42);
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();

        // 计算在棋盘上或在道具栏中的帮助卡数量（未使用的）
        var unusedCount = 0;
        foreach (var pair in deckModel.HelpCardStates)
        {
            if (!pair.Value.IsPermanentlyRemoved && (pair.Value.IsOnBoard || pair.Value.IsInItemSlot))
            {
                unusedCount++;
            }
        }

        var goldBefore = playerModel.Gold.Value;

        var rewardSystem = TableNine.Interface.GetSystem<IRewardSystem>();
        rewardSystem.SettleUnusedHelpCards();

        Assert.That(playerModel.Gold.Value - goldBefore, Is.EqualTo(unusedCount * 10),
            $"应有 {unusedCount} 张未使用帮助卡各 +10 金币");
    }

    [Test]
    public void Unused_HelpCards_Settlement_Emits_Event()
    {
        StartRun(42);
        var events = new List<HelpCardsSettledEvent>();
        var unReg = TableNine.Interface.RegisterEvent<HelpCardsSettledEvent>(events.Add);

        var rewardSystem = TableNine.Interface.GetSystem<IRewardSystem>();
        rewardSystem.SettleUnusedHelpCards();

        unReg.UnRegister();

        Assert.That(events.Count, Is.EqualTo(1));
        Assert.That(events[0].GoldGained, Is.EqualTo(events[0].UnusedCount * 10));
    }

    // ========================
    // 帮助卡奖励生成
    // ========================

    [Test]
    public void GenerateHelpReward_Produces_Up_To_3_Candidates()
    {
        StartRun(42);
        var rewardModel = TableNine.Interface.GetModel<IRewardModel>();
        var events = new List<HelpRewardGeneratedEvent>();
        var unReg = TableNine.Interface.RegisterEvent<HelpRewardGeneratedEvent>(events.Add);

        TableNine.Interface.SendCommand(new GenerateHelpRewardCommand());

        Assert.That(rewardModel.HelpRewardCardIds.Count, Is.GreaterThan(0).And.LessThanOrEqualTo(3),
            "应生成 1~3 张帮助卡候选");
        Assert.That(events.Count, Is.EqualTo(1), "应触发 HelpRewardGeneratedEvent");
        Assert.That(events[0].CardIds.Count, Is.EqualTo(rewardModel.HelpRewardCardIds.Count));

        unReg.UnRegister();
    }

    [Test]
    public void GenerateHelpReward_Sets_Phase_And_Lock()
    {
        StartRun(42);
        var flowModel = TableNine.Interface.GetModel<IFlowModel>();

        TableNine.Interface.SendCommand(new GenerateHelpRewardCommand());

        Assert.That(flowModel.Phase.Value, Is.EqualTo(FlowPhase.HelpRewardChoosing));
        Assert.That(flowModel.HasLock(InputLockReason.OverlayVisible), Is.True, "应锁定 OverlayVisible");
    }

    // ========================
    // 选取帮助卡奖励
    // ========================

    [Test]
    public void PickHelpCardReward_Adds_Card_To_Deck()
    {
        StartRun(42);
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var rewardModel = TableNine.Interface.GetModel<IRewardModel>();

        var countBefore = deckModel.OwnedHelpCards.Count;

        TableNine.Interface.SendCommand(new GenerateHelpRewardCommand());
        Assert.That(rewardModel.HelpRewardCardIds.Count, Is.GreaterThan(0));

        var pickedCardId = rewardModel.HelpRewardCardIds[0];
        TableNine.Interface.SendCommand(new PickHelpCardRewardCommand(pickedCardId));

        Assert.That(deckModel.OwnedHelpCards.Count, Is.EqualTo(countBefore + 1), "选取后帮助卡组应 +1");
    }

    // ========================
    // 跳过帮助卡奖励 +10 金币
    // ========================

    [Test]
    public void HelpReward_Skip_Adds_10_Gold()
    {
        StartRun(42);
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();

        TableNine.Interface.SendCommand(new GenerateHelpRewardCommand());

        var goldBefore = playerModel.Gold.Value;
        TableNine.Interface.SendCommand(new SkipHelpRewardCommand());

        Assert.That(playerModel.Gold.Value - goldBefore, Is.EqualTo(10), "跳过帮助卡奖励应获得 10 金币");
    }

    [Test]
    public void HelpReward_Skip_Unlocks_And_Sets_Phase()
    {
        StartRun(42);
        var flowModel = TableNine.Interface.GetModel<IFlowModel>();

        TableNine.Interface.SendCommand(new GenerateHelpRewardCommand());
        TableNine.Interface.SendCommand(new SkipHelpRewardCommand());

        Assert.That(flowModel.Phase.Value, Is.EqualTo(FlowPhase.RoomChoosing));
        Assert.That(flowModel.HasLock(InputLockReason.OverlayVisible), Is.False, "跳过奖励应解锁 OverlayVisible");
    }

    // ========================
    // 容量 / 同名上限
    // ========================

    [Test]
    public void CanAddHelpCard_Full_Capacity_Returns_False()
    {
        StartRun(42);
        var rewardSystem = TableNine.Interface.GetSystem<IRewardSystem>();

        // Layer 1 上限 12 张
        FillHelpDeckToCapacity(12);

        Assert.That(rewardSystem.CanAddHelpCard(DefaultGameConfigFactory.HelpBlessingId), Is.False,
            "满容量时 CanAddHelpCard 应返回 false");
    }

    [Test]
    public void CanAddHelpCard_SameName_Limit_Returns_False()
    {
        StartRun(42);
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var configModel = TableNine.Interface.GetModel<IConfigModel>();
        var rewardSystem = TableNine.Interface.GetSystem<IRewardSystem>();

        // 清空并添加 3 张同名药水
        deckModel.OwnedHelpCards.Clear();
        deckModel.HelpCardStates.Clear();
        var potionDef = configModel.GetCardDefinition(DefaultGameConfigFactory.HelpPotionId);
        for (var i = 0; i < 3; i++)
        {
            var rt = collectionModel.CreateCard(potionDef);
            deckModel.OwnedHelpCards.Add(rt.Uid);
            deckModel.HelpCardStates[rt.Uid.Value] = new HelpCardState
            {
                Uid = rt.Uid,
                DefinitionId = DefaultGameConfigFactory.HelpPotionId
            };
        }

        Assert.That(rewardSystem.CanAddHelpCard(DefaultGameConfigFactory.HelpPotionId), Is.False,
            "同名卡达上限 3 时 CanAddHelpCard 应返回 false");
    }

    [Test]
    public void PickHelpCardReward_Full_Deck_Shows_Popup()
    {
        StartRun(42);
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var rewardModel = TableNine.Interface.GetModel<IRewardModel>();
        FillHelpDeckToCapacity(12);

        var popups = new List<PopupRequestedEvent>();
        var unReg = TableNine.Interface.RegisterEvent<PopupRequestedEvent>(popups.Add);

        // 手动设置候选（因为满容量时 Generate 不会产生候选）
        rewardModel.ClearHelpRewardCardIds();
        rewardModel.AddHelpRewardCardId(DefaultGameConfigFactory.HelpBlessingId);

        TableNine.Interface.SendCommand(new PickHelpCardRewardCommand(DefaultGameConfigFactory.HelpBlessingId));

        Assert.That(popups.Count, Is.GreaterThan(0), "满容量时应弹出提示");

        unReg.UnRegister();
    }

    // ========================
    // 房间选择
    // ========================

    [Test]
    public void ChooseRoom_Gold_Gives_Bonus_Gold()
    {
        StartRunAndClearNode();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();

        var goldBefore = playerModel.Gold.Value;
        TableNine.Interface.SendCommand(new ChooseRoomCommand(DefaultGameConfigFactory.RoomGoldId));

        // GoldRoom: 结算未使用卡 + RewardGold(30)
        Assert.That(playerModel.Gold.Value, Is.GreaterThan(goldBefore), "金币房应增加金币");
    }

    [Test]
    [Category(TableNineTestCategories.LegacyRuleTests)]
    public void ChooseRoom_Gold_No_Longer_Opens_HelpReward_Overlay()
    {
        StartRunAndClearNode();
        var flowModel = TableNine.Interface.GetModel<IFlowModel>();

        TableNine.Interface.SendCommand(new ChooseRoomCommand(DefaultGameConfigFactory.RoomGoldId));

        Assert.That(flowModel.Phase.Value, Is.Not.EqualTo(FlowPhase.HelpRewardChoosing));
    }

    [Test]
    public void ChooseRoom_Shop_Enters_Shop_Phase()
    {
        StartRunAndClearNode();
        var flowModel = TableNine.Interface.GetModel<IFlowModel>();
        var rewardModel = TableNine.Interface.GetModel<IRewardModel>();

        TableNine.Interface.SendCommand(new ChooseRoomCommand(DefaultGameConfigFactory.RoomShopId));

        Assert.That(flowModel.Phase.Value, Is.EqualTo(FlowPhase.Shop), "选择商店房应进入 Shop 阶段");
        Assert.That(rewardModel.ShopCardIds.Count, Is.GreaterThan(0), "应生成商店商品");
    }

    [Test]
    public void ChooseRoom_Chest_Enters_ChestRewardChoosing_Phase()
    {
        StartRunAndClearNode();
        var flowModel = TableNine.Interface.GetModel<IFlowModel>();
        var rewardModel = TableNine.Interface.GetModel<IRewardModel>();

        TableNine.Interface.SendCommand(new ChooseRoomCommand(DefaultGameConfigFactory.RoomChestId));

        Assert.That(flowModel.Phase.Value, Is.EqualTo(FlowPhase.ChestRewardChoosing),
            "选择宝箱房应进入 ChestRewardChoosing 阶段");
        Assert.That(rewardModel.ChestRewardRelicIds.Count, Is.GreaterThan(0),
            "应生成遗物候选");
    }

    [Test]
    [Category(TableNineTestCategories.LegacyRuleTests)]
    public void ChooseRoom_Attribute_Injects_Card_And_Proceeds()
    {
        StartRunAndClearNode();
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var runModel = TableNine.Interface.GetModel<IRunModel>();

        var countBefore = deckModel.OwnedHelpCards.Count;
        TableNine.Interface.SendCommand(new ChooseRoomCommand(DefaultGameConfigFactory.RoomAttributeId));

        Assert.That(deckModel.OwnedHelpCards.Count, Is.EqualTo(countBefore + 1),
            "属性房应注入一张帮助卡");
        Assert.That(runModel.NodeInLayer.Value, Is.EqualTo(2));
    }

    [Test]
    public void ChooseRoom_Restores_Snapshot_And_Settles_Gold()
    {
        StartRun(42);
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();

        // 临时移除一张帮助卡
        var helpUid = FindFirstHelpCard();
        deckModel.HelpCardStates[helpUid.Value].IsTemporarilyRemoved = true;

        var goldBefore = playerModel.Gold.Value;
        TableNine.Interface.SendCommand(new ChooseRoomCommand(DefaultGameConfigFactory.RoomGoldId));

        // 临时移除的卡应被恢复
        Assert.That(deckModel.HelpCardStates[helpUid.Value].IsTemporarilyRemoved, Is.False);
        // 金币应增加
        Assert.That(playerModel.Gold.Value, Is.GreaterThan(goldBefore));
    }

    // ========================
    // 商店系统
    // ========================

    [Test]
    public void Shop_Generates_Up_To_6_Cards()
    {
        StartRun(42);
        var rewardModel = TableNine.Interface.GetModel<IRewardModel>();
        var events = new List<ShopOpenedEvent>();
        var unReg = TableNine.Interface.RegisterEvent<ShopOpenedEvent>(events.Add);

        TableNine.Interface.SendCommand(new OpenShopCommand());

        Assert.That(rewardModel.ShopCardIds.Count, Is.GreaterThan(0).And.LessThanOrEqualTo(6),
            "商店应展示 1~6 张帮助卡");
        Assert.That(events.Count, Is.EqualTo(1), "应触发 ShopOpenedEvent");

        unReg.UnRegister();
    }

    [Test]
    public void Shop_Buy_Deducts_Gold_And_Adds_Card()
    {
        StartRun(42);
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var shopSystem = TableNine.Interface.GetSystem<IShopSystem>();
        var rewardModel = TableNine.Interface.GetModel<IRewardModel>();
        var configModel = TableNine.Interface.GetModel<IConfigModel>();

        playerModel.Gold.Value = 1000;

        // 生成商店商品
        shopSystem.GenerateShopCards();
        Assert.That(rewardModel.ShopCardIds.Count, Is.GreaterThan(0));

        // 找一个未达同名上限的商品
        string buyableCardId = null;
        for (var i = 0; i < rewardModel.ShopCardIds.Count; i++)
        {
            if (deckModel.CountHelpCardsById(rewardModel.ShopCardIds[i]) < 3)
            {
                buyableCardId = rewardModel.ShopCardIds[i];
                break;
            }
        }

        if (buyableCardId == null)
        {
            Assert.Inconclusive("商店所有商品均达同名上限，无法测试购买。");
            return;
        }

        var price = configModel.GetCardDefinition(buyableCardId).Price;
        var countBefore = deckModel.OwnedHelpCards.Count;
        var goldBefore = playerModel.Gold.Value;

        var bought = shopSystem.BuyHelpCard(buyableCardId);
        Assert.That(bought, Is.True, "购买应成功");
        Assert.That(deckModel.OwnedHelpCards.Count, Is.EqualTo(countBefore + 1), "购买后帮助卡组应 +1");
        Assert.That(playerModel.Gold.Value, Is.EqualTo(goldBefore - price), "金币应扣除商品价格");
    }

    [Test]
    public void Shop_Buy_Insufficient_Gold_Shows_Popup()
    {
        StartRun(42);
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        playerModel.Gold.Value = 0;

        var shopSystem = TableNine.Interface.GetSystem<IShopSystem>();
        var rewardModel = TableNine.Interface.GetModel<IRewardModel>();
        shopSystem.GenerateShopCards();

        // 找一个未达同名上限的商品来确保失败原因是金币不足
        string buyableCardId = null;
        for (var i = 0; i < rewardModel.ShopCardIds.Count; i++)
        {
            if (deckModel.CountHelpCardsById(rewardModel.ShopCardIds[i]) < 3)
            {
                buyableCardId = rewardModel.ShopCardIds[i];
                break;
            }
        }

        if (buyableCardId == null)
        {
            Assert.Inconclusive("商店所有商品均达同名上限。");
            return;
        }

        var popups = new List<PopupRequestedEvent>();
        var unReg = TableNine.Interface.RegisterEvent<PopupRequestedEvent>(popups.Add);

        TableNine.Interface.SendCommand(new BuyHelpCardCommand(buyableCardId));

        Assert.That(popups.Count, Is.GreaterThan(0), "金币不足时应弹出提示");

        unReg.UnRegister();
    }

    [Test]
    public void Shop_Delete_HelpCard_Adds_10_Gold()
    {
        StartRun(42);
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();

        var helpUid = FindFirstHelpCard();
        var goldBefore = playerModel.Gold.Value;
        var countBefore = deckModel.CountActiveHelpCards();

        TableNine.Interface.SendCommand(new DeleteHelpCardForGoldCommand(helpUid));

        Assert.That(playerModel.Gold.Value - goldBefore, Is.EqualTo(10), "删除帮助卡应获得 10 金币");
        Assert.That(deckModel.CountActiveHelpCards(), Is.EqualTo(countBefore - 1), "删除后活跃帮助卡应 -1");
    }

    [Test]
    public void CloseShop_Transitions_To_HelpReward()
    {
        StartRun(42);
        var flowModel = TableNine.Interface.GetModel<IFlowModel>();
        var rewardModel = TableNine.Interface.GetModel<IRewardModel>();

        TableNine.Interface.SendCommand(new OpenShopCommand());
        Assert.That(flowModel.Phase.Value, Is.EqualTo(FlowPhase.Shop));

        TableNine.Interface.SendCommand(new CloseShopCommand());
        Assert.That(flowModel.Phase.Value, Is.EqualTo(FlowPhase.HelpRewardChoosing),
            "关闭商店后应进入帮助卡选择");
        Assert.That(rewardModel.HelpRewardCardIds.Count, Is.GreaterThan(0),
            "关闭商店后应生成帮助卡候选");
    }

    // ========================
    // 遗物系统
    // ========================

    [Test]
    public void Relic_Add_Succeeds()
    {
        StartRun(42);
        var relicSystem = TableNine.Interface.GetSystem<IRelicSystem>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();

        var result = relicSystem.AddRelic(DefaultGameConfigFactory.RelicWoodShieldId);

        Assert.That(result, Is.True, "添加遗物应成功");
        Assert.That(relicSystem.HasRelic(DefaultGameConfigFactory.RelicWoodShieldId), Is.True,
            "应拥有该遗物");
        Assert.That(playerModel.Relics.Count, Is.EqualTo(1));
    }

    [Test]
    public void Relic_Add_Duplicate_Fails()
    {
        StartRun(42);
        var relicSystem = TableNine.Interface.GetSystem<IRelicSystem>();

        relicSystem.AddRelic(DefaultGameConfigFactory.RelicWoodShieldId);
        var result = relicSystem.AddRelic(DefaultGameConfigFactory.RelicWoodShieldId);

        Assert.That(result, Is.False, "重复添加遗物应失败");
    }

    [Test]
    public void Relic_Discard_Removes_And_Gives_20_Gold()
    {
        StartRun(42);
        var relicSystem = TableNine.Interface.GetSystem<IRelicSystem>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();

        relicSystem.AddRelic(DefaultGameConfigFactory.RelicWoodShieldId);
        var goldBefore = playerModel.Gold.Value;

        var result = relicSystem.DiscardRelic(DefaultGameConfigFactory.RelicWoodShieldId);

        Assert.That(result, Is.True, "丢弃遗物应成功");
        Assert.That(relicSystem.HasRelic(DefaultGameConfigFactory.RelicWoodShieldId), Is.False,
            "丢弃后不应拥有该遗物");
        Assert.That(playerModel.Gold.Value - goldBefore, Is.EqualTo(20), "丢弃遗物应 +20 金币");
    }

    [Test]
    public void Relic_Discard_NonExistent_Fails()
    {
        StartRun(42);
        var relicSystem = TableNine.Interface.GetSystem<IRelicSystem>();

        var result = relicSystem.DiscardRelic(DefaultGameConfigFactory.RelicWoodShieldId);

        Assert.That(result, Is.False, "丢弃未拥有的遗物应失败");
    }

    [Test]
    public void ChestReward_Excludes_Owned_Relics()
    {
        StartRun(42);
        var relicSystem = TableNine.Interface.GetSystem<IRelicSystem>();
        var rewardModel = TableNine.Interface.GetModel<IRewardModel>();

        // 先添加一个遗物
        relicSystem.AddRelic(DefaultGameConfigFactory.RelicWoodShieldId);

        // 生成宝箱候选
        relicSystem.GenerateChestRewardCandidates();

        Assert.That(rewardModel.ChestRewardRelicIds.Contains(DefaultGameConfigFactory.RelicWoodShieldId),
            Is.False, "已拥有的遗物不应出现在候选中");
        Assert.That(rewardModel.ChestRewardRelicIds.Count, Is.GreaterThan(0).And.LessThanOrEqualTo(3));
    }

    [Test]
    public void PickRelicReward_Adds_Relic_And_Transitions_To_HelpReward()
    {
        StartRunAndClearNode();
        var flowModel = TableNine.Interface.GetModel<IFlowModel>();
        var runModel = TableNine.Interface.GetModel<IRunModel>();
        var relicSystem = TableNine.Interface.GetSystem<IRelicSystem>();
        var rewardModel = TableNine.Interface.GetModel<IRewardModel>();

        // 选宝箱房
        TableNine.Interface.SendCommand(new SkipHelpRewardCommand());
        TableNine.Interface.SendCommand(new ChooseRoomCommand(DefaultGameConfigFactory.RoomChestId));
        Assert.That(flowModel.Phase.Value, Is.EqualTo(FlowPhase.ChestRewardChoosing));
        Assert.That(rewardModel.ChestRewardRelicIds.Count, Is.GreaterThan(0));

        var relicId = rewardModel.ChestRewardRelicIds[0];
        TableNine.Interface.SendCommand(new PickRelicRewardCommand(relicId));

        Assert.That(relicSystem.HasRelic(relicId), Is.True, "应获得选取的遗物");
        Assert.That(runModel.NodeInLayer.Value, Is.EqualTo(2), "房间宝箱选遗物后应推进到下一节点");
    }

    [Test]
    public void SkipChestReward_Gives_20_Gold_And_Transitions()
    {
        StartRunAndClearNode();
        var flowModel = TableNine.Interface.GetModel<IFlowModel>();
        var runModel = TableNine.Interface.GetModel<IRunModel>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();

        TableNine.Interface.SendCommand(new SkipHelpRewardCommand());
        TableNine.Interface.SendCommand(new ChooseRoomCommand(DefaultGameConfigFactory.RoomChestId));

        var goldBefore = playerModel.Gold.Value;
        TableNine.Interface.SendCommand(new SkipChestRewardCommand());

        Assert.That(playerModel.Gold.Value - goldBefore, Is.EqualTo(20), "跳过宝箱应获得 20 金币");
        Assert.That(runModel.NodeInLayer.Value, Is.EqualTo(2), "房间宝箱跳过后应推进到下一节点");
    }

    // ========================
    // 节点推进
    // ========================

    [Test]
    public void ProceedToNextNode_Advances_Node()
    {
        StartRunAndClearNode();
        var runModel = TableNine.Interface.GetModel<IRunModel>();

        var currentNode = runModel.NodeInLayer.Value;
        TableNine.Interface.SendCommand(new ProceedToNextNodeCommand());

        Assert.That(runModel.NodeInLayer.Value, Is.EqualTo(currentNode + 1), "节点应推进 +1");
    }

    [Test]
    public void ProceedToNextNode_At_Layer1_Node_9_Enters_LayerComplete()
    {
        StartRun(42);
        var runModel = TableNine.Interface.GetModel<IRunModel>();
        var flowModel = TableNine.Interface.GetModel<IFlowModel>();

        runModel.SetNode(1, 9);

        TableNine.Interface.SendCommand(new ProceedToNextNodeCommand());

        Assert.That(flowModel.Phase.Value, Is.EqualTo(FlowPhase.LayerComplete),
            "第一层第 9 节点后推进应进入 LayerComplete");
    }

    [Test]
    public void ProceedToNextNode_Emits_NodeCompleted_And_NodeAdvanced_Events()
    {
        StartRunAndClearNode();
        var runModel = TableNine.Interface.GetModel<IRunModel>();
        var completedEvents = new List<NodeCompletedEvent>();
        var advancedEvents = new List<NodeAdvancedEvent>();
        var unReg1 = TableNine.Interface.RegisterEvent<NodeCompletedEvent>(completedEvents.Add);
        var unReg2 = TableNine.Interface.RegisterEvent<NodeAdvancedEvent>(advancedEvents.Add);

        TableNine.Interface.SendCommand(new ProceedToNextNodeCommand());

        unReg1.UnRegister();
        unReg2.UnRegister();

        Assert.That(completedEvents.Count, Is.EqualTo(1));
        Assert.That(advancedEvents.Count, Is.EqualTo(1));
        Assert.That(advancedEvents[0].ToNode, Is.EqualTo(runModel.NodeInLayer.Value));
    }

    // ========================
    // DiscardRelicCommand
    // ========================

    [Test]
    public void DiscardRelicCommand_Removes_Relic_And_Gives_Gold()
    {
        StartRun(42);
        var relicSystem = TableNine.Interface.GetSystem<IRelicSystem>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();

        relicSystem.AddRelic(DefaultGameConfigFactory.RelicWoodSwordId);
        var goldBefore = playerModel.Gold.Value;

        TableNine.Interface.SendCommand(new DiscardRelicCommand(DefaultGameConfigFactory.RelicWoodSwordId));

        Assert.That(relicSystem.HasRelic(DefaultGameConfigFactory.RelicWoodSwordId), Is.False);
        Assert.That(playerModel.Gold.Value - goldBefore, Is.EqualTo(20));
    }

    // ========================
    // RoomCandidate 生成
    // ========================

    [Test]
    public void GenerateRoomCandidates_Produces_Two_Rooms()
    {
        StartRun(42);
        var rewardModel = TableNine.Interface.GetModel<IRewardModel>();
        var rewardSystem = TableNine.Interface.GetSystem<IRewardSystem>();

        rewardSystem.GenerateRoomCandidates();

        Assert.That(rewardModel.RoomCandidateIds.Count, Is.EqualTo(RewardConstants.RoomCandidateCount));
    }

    // ========================
    // 补充测试：QA 报告指出的测试盲区
    // ========================

    [Test]
    public void ChooseTutorSkill_Adds_Skill_Successfully()
    {
        StartRun(42);
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var flowModel = TableNine.Interface.GetModel<IFlowModel>();

        var skillCountBefore = playerModel.SkillIds.Count;
        TableNine.Interface.SendCommand(new ChooseTutorSkillCommand(DefaultGameConfigFactory.SkillFirstStrikeId));

        Assert.That(playerModel.SkillIds.Count, Is.EqualTo(skillCountBefore + 1), "应新增一个导师技能");
        Assert.That(flowModel.Phase.Value, Is.EqualTo(FlowPhase.PlayerControl), "选取后应回到 PlayerControl");
    }

    [Test]
    public void ChooseTutorSkill_Duplicate_Shows_Popup()
    {
        StartRun(42);
        var popups = new List<PopupRequestedEvent>();
        var unReg = TableNine.Interface.RegisterEvent<PopupRequestedEvent>(popups.Add);

        // 小鬼初始已有轻车熟路，再次选取应弹 Popup
        TableNine.Interface.SendCommand(new ChooseTutorSkillCommand(DefaultGameConfigFactory.SkillLightFootedId));

        unReg.UnRegister();
        Assert.That(popups.Count, Is.GreaterThan(0), "重复选取导师技能应弹出提示");
    }

    [Test]
    public void Relic_Full_Shows_Popup_And_Does_Not_Add()
    {
        StartRun(42);
        var relicSystem = TableNine.Interface.GetSystem<IRelicSystem>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var configModel = TableNine.Interface.GetModel<IConfigModel>();
        var popups = new List<PopupRequestedEvent>();
        var unReg = TableNine.Interface.RegisterEvent<PopupRequestedEvent>(popups.Add);

        // 填满 12 个遗物格（用反射绕过 HasRelic 重复检查）
        var relicIds = new[]
        {
            DefaultGameConfigFactory.RelicWoodShieldId,
            DefaultGameConfigFactory.RelicWoodSwordId,
            DefaultGameConfigFactory.RelicWoodArmorId,
            DefaultGameConfigFactory.RelicLivingFleshId,
            DefaultGameConfigFactory.RelicThornArmorId,
            DefaultGameConfigFactory.RelicPhoenixFeatherId
        };

        var relicsField = playerModel.GetType().GetField("mRelics",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var relicsList = (List<RelicInstance>)relicsField.GetValue(playerModel);

        for (var i = 0; i < 12; i++)
        {
            var def = configModel.GetRelicDefinition(relicIds[i % relicIds.Length]);
            relicsList.Add(RelicInstance.FromDefinition(def));
        }

        Assert.That(playerModel.Relics.Count, Is.EqualTo(12), "遗物栏应已满");

        // 尝试通过 RelicSystem 添加新遗物
        var result = relicSystem.AddRelic(relicIds[0]);

        unReg.UnRegister();
        Assert.That(result, Is.False, "遗物满时添加应失败");
        Assert.That(popups.Count, Is.GreaterThan(0), "遗物满时应弹出提示");
    }

    [Test]
    public void Relic_IsConsumed_HasRelic_Returns_False()
    {
        StartRun(42);
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var configModel = TableNine.Interface.GetModel<IConfigModel>();
        var relicSystem = TableNine.Interface.GetSystem<IRelicSystem>();

        var def = configModel.GetRelicDefinition(DefaultGameConfigFactory.RelicPhoenixFeatherId);
        var relic = RelicInstance.FromDefinition(def);
        playerModel.AddRelic(relic);

        Assert.That(relicSystem.HasRelic(DefaultGameConfigFactory.RelicPhoenixFeatherId), Is.True,
            "添加后应拥有遗物");

        // 标记为已消耗
        relic.IsConsumed = true;

        Assert.That(relicSystem.HasRelic(DefaultGameConfigFactory.RelicPhoenixFeatherId), Is.False,
            "消耗后 HasRelic 应返回 false");
        Assert.That(playerModel.Relics.Count, Is.EqualTo(1), "消耗后遗物仍在列表中");
    }

    [Test]
    public void Shop_Buy_Removes_Card_From_Display()
    {
        StartRun(42);
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var shopSystem = TableNine.Interface.GetSystem<IShopSystem>();
        var rewardModel = TableNine.Interface.GetModel<IRewardModel>();
        var configModel = TableNine.Interface.GetModel<IConfigModel>();

        playerModel.Gold.Value = 1000;
        shopSystem.GenerateShopCards();

        var countBefore = rewardModel.ShopCardIds.Count;

        // 找一个可购买的商品
        string buyableCardId = null;
        for (var i = 0; i < rewardModel.ShopCardIds.Count; i++)
        {
            if (deckModel.CountHelpCardsById(rewardModel.ShopCardIds[i]) < 3)
            {
                buyableCardId = rewardModel.ShopCardIds[i];
                break;
            }
        }

        if (buyableCardId == null)
        {
            Assert.Inconclusive("商店所有商品均达同名上限。");
            return;
        }

        shopSystem.BuyHelpCard(buyableCardId);

        Assert.That(rewardModel.ShopCardIds.Count, Is.EqualTo(countBefore - 1),
            "购买后商品应从商店列表移除");
    }

    [Test]
    public void ProceedToNextNode_Board_Has_Cards_After_Advance()
    {
        StartRunAndClearNode();
        var boardModel = TableNine.Interface.GetModel<IBoardModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();

        TableNine.Interface.SendCommand(new ProceedToNextNodeCommand());

        // 新节点应发牌，棋盘上应有卡
        var cardCount = 0;
        for (var i = 1; i <= 9; i++)
        {
            var uid = boardModel.GetCardAt(new BoardSlotNo(i));
            if (uid.HasValue)
            {
                cardCount++;
            }
        }

        Assert.That(cardCount, Is.GreaterThan(0), "推进到新节点后棋盘上应有卡片");
    }

    [Test]
    public void EndToEnd_Clear_HelpReward_Room_Proceeds_To_Next_Node()
    {
        StartRunAndClearNode();
        var flowModel = TableNine.Interface.GetModel<IFlowModel>();
        var runModel = TableNine.Interface.GetModel<IRunModel>();

        Assert.That(flowModel.Phase.Value, Is.EqualTo(FlowPhase.HelpRewardChoosing));

        TableNine.Interface.SendCommand(new SkipHelpRewardCommand());
        Assert.That(flowModel.Phase.Value, Is.EqualTo(FlowPhase.RoomChoosing));

        var nodeBefore = runModel.NodeInLayer.Value;
        TableNine.Interface.SendCommand(new ChooseRoomCommand(DefaultGameConfigFactory.RoomGoldId));
        Assert.That(runModel.NodeInLayer.Value, Is.EqualTo(nodeBefore + 1), "选房间后应推进到下一节点");
        Assert.That(flowModel.Phase.Value, Is.EqualTo(FlowPhase.PlayerControl));
    }

    [Test]
    public void GoldChangedEvent_Fired_On_MonsterKill()
    {
        StartRun(42);
        var events = new List<GoldChangedEvent>();
        var unReg = TableNine.Interface.RegisterEvent<GoldChangedEvent>(events.Add);

        // 找到一只怪物并击杀
        var boardModel = TableNine.Interface.GetModel<IBoardModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();

        CardUid? monsterUid = null;
        for (var i = 1; i <= 9; i++)
        {
            var uid = boardModel.GetCardAt(new BoardSlotNo(i));
            if (uid.HasValue && collectionModel.TryGetCard(uid.Value, out var rt) && rt.CardType == CardType.Monster)
            {
                monsterUid = uid.Value;
                break;
            }
        }

        if (!monsterUid.HasValue)
        {
            unReg.UnRegister();
            Assert.Inconclusive("棋盘上没有怪物。");
            return;
        }

        TableNine.Interface.SendCommand(new KillMonsterCommand(monsterUid.Value));

        unReg.UnRegister();
        Assert.That(events.Count, Is.GreaterThan(0), "击杀怪物应触发 GoldChangedEvent");
        Assert.That(events[events.Count - 1].Delta, Is.EqualTo(RewardConstants.MonsterKillGold),
            "金币增量应为 MonsterKillGold");
    }

    [Test]
    public void Relic_Stat_Bonus_Applied_To_EffectiveStats()
    {
        StartRun(42);
        var relicSystem = TableNine.Interface.GetSystem<IRelicSystem>();
        var statSystem = TableNine.Interface.GetSystem<IStatSystem>();

        var statsBefore = statSystem.GetEffectivePlayerStats();

        // 添加木盾（DEF+2）和木剑（ATK+2）
        relicSystem.AddRelic(DefaultGameConfigFactory.RelicWoodShieldId);
        relicSystem.AddRelic(DefaultGameConfigFactory.RelicWoodSwordId);

        var statsAfter = statSystem.GetEffectivePlayerStats();

        Assert.That(statsAfter.Defense - statsBefore.Defense, Is.EqualTo(2), "木盾应 +2 防御");
        Assert.That(statsAfter.Attack - statsBefore.Attack, Is.EqualTo(2), "木剑应 +2 攻击");
    }

    // ========================
    // 辅助方法
    // ========================

    private static void StartRun(int seed)
    {
        TableNine.InitArchitecture();
        TableNine.Interface.SendCommand(new StartNewRunCommand(seedOverride: seed));
    }

    /// <summary>
    /// 开始一局并清空当前节点的所有怪物，使流程进入 ClearReady。
    /// </summary>
    private static void StartRunAndClearNode()
    {
        StartRun(42);
        var boardSystem = TableNine.Interface.GetSystem<IBoardSystem>();
        var boardModel = TableNine.Interface.GetModel<IBoardModel>();
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();

        // 清空战斗牌堆
        deckModel.BattleDrawPile.Clear();
        TableNine.Interface.GetSystem<IDeckSystem>().UpdateNextBattlePreview();

        // 移除棋盘上所有怪物
        for (var i = 1; i <= 9; i++)
        {
            var slot = new BoardSlotNo(i);
            var uid = boardModel.GetCardAt(slot);
            if (!uid.HasValue || uid.Value.Equals(playerModel.PlayerCardUid))
                continue;

            var runtime = collectionModel.GetCard(uid.Value);
            if (runtime.CardType == CardType.Monster)
            {
                boardSystem.RemoveCardAt(slot);
                collectionModel.RemoveCard(uid.Value);
            }
        }

        TableNine.Interface.SendCommand(new CheckClearConditionCommand());
    }

    private static CardUid FindFirstHelpCard()
    {
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();

        for (var i = 0; i < deckModel.OwnedHelpCards.Count; i++)
        {
            var uid = deckModel.OwnedHelpCards[i];
            if (collectionModel.TryGetCard(uid, out var rt) && rt.CardType == CardType.Help)
                return uid;
        }

        Assert.Fail("找不到帮助卡");
        return default;
    }

    private static CardUid FindFirstHelpCardExcluding(CardUid excludeUid)
    {
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();

        for (var i = 0; i < deckModel.OwnedHelpCards.Count; i++)
        {
            var uid = deckModel.OwnedHelpCards[i];
            if (uid.Equals(excludeUid))
                continue;
            if (collectionModel.TryGetCard(uid, out var rt) && rt.CardType == CardType.Help)
                return uid;
        }

        Assert.Fail("找不到排除指定 UID 的帮助卡");
        return default;
    }

    /// <summary>
    /// 将帮助卡组填充到指定数量（使用多种帮助卡避免同名上限）。
    /// </summary>
    private static void FillHelpDeckToCapacity(int targetCount)
    {
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var configModel = TableNine.Interface.GetModel<IConfigModel>();

        deckModel.OwnedHelpCards.Clear();
        deckModel.HelpCardStates.Clear();

        var allHelp = configModel.GetAllHelpCardDefinitions();
        var cardIndex = 0;

        for (var i = 0; i < targetCount; i++)
        {
            var def = allHelp[cardIndex % allHelp.Count];
            var rt = collectionModel.CreateCard(def);
            deckModel.OwnedHelpCards.Add(rt.Uid);
            deckModel.HelpCardStates[rt.Uid.Value] = new HelpCardState
            {
                Uid = rt.Uid,
                DefinitionId = def.CardId
            };
            cardIndex++;
        }
    }
}
