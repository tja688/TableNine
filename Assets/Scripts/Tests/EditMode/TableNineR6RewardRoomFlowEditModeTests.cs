using System.Collections.Generic;
using NUnit.Framework;
using QFramework;
using UnityEngine;

/// <summary>
/// R6 奖励/房间/商店流程收束：清场→帮助奖励→房间→节点结束。
/// </summary>
[Category(TableNineTestCategories.NewRuleTests)]
public sealed class TableNineR6RewardRoomFlowEditModeTests
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

    [Test]
    public void HelpRewardSkipped_UIRouter_Opens_RoomChoice_Not_NextNodePrompt()
    {
        TableNineUIRuntimeTestRecorder.Reset();
        var router = CreateUIRouterForTests(out var registry);
        TableNineUIRuntimeTestRecorder.OpenedKeys.Clear();
        TableNineUIRuntimeTestRecorder.ClosedKeys.Clear();

        StartRunAndClearNode();
        TableNine.Interface.SendCommand(new SkipHelpRewardCommand());

        Assert.That(TableNineUIRuntimeTestRecorder.OpenedKeys, Does.Contain(TableNineUIKeys.RoomChoice));
        Assert.That(TableNineUIRuntimeTestRecorder.OpenedKeys, Does.Not.Contain(TableNineUIKeys.NextNodePrompt));
        Assert.That(TableNine.Interface.GetModel<IFlowModel>().Phase.Value, Is.EqualTo(FlowPhase.RoomChoosing));

        router.Dispose();
        Object.DestroyImmediate(registry);
        TableNineUIRuntimeTestRecorder.Reset();
    }

    [Test]
    public void HelpRewardPicked_UIRouter_Opens_RoomChoice_Not_NextNodePrompt()
    {
        TableNineUIRuntimeTestRecorder.Reset();
        var router = CreateUIRouterForTests(out var registry);
        TableNineUIRuntimeTestRecorder.OpenedKeys.Clear();
        TableNineUIRuntimeTestRecorder.ClosedKeys.Clear();

        StartRunAndClearNode();
        var rewardModel = TableNine.Interface.GetModel<IRewardModel>();
        var cardId = rewardModel.HelpRewardCardIds[0];
        TableNine.Interface.SendCommand(new PickHelpCardRewardCommand(cardId));

        Assert.That(TableNineUIRuntimeTestRecorder.OpenedKeys, Does.Contain(TableNineUIKeys.RoomChoice));
        Assert.That(TableNineUIRuntimeTestRecorder.OpenedKeys, Does.Not.Contain(TableNineUIKeys.NextNodePrompt));
        Assert.That(TableNine.Interface.GetModel<IFlowModel>().Phase.Value, Is.EqualTo(FlowPhase.RoomChoosing));

        router.Dispose();
        Object.DestroyImmediate(registry);
        TableNineUIRuntimeTestRecorder.Reset();
    }

    [Test]
    public void Clear_To_HelpReward_To_Room_To_NextNode()
    {
        StartRunAndClearNode();
        var flowModel = TableNine.Interface.GetModel<IFlowModel>();
        var runModel = TableNine.Interface.GetModel<IRunModel>();

        Assert.That(flowModel.Phase.Value, Is.EqualTo(FlowPhase.HelpRewardChoosing));

        TableNine.Interface.SendCommand(new SkipHelpRewardCommand());
        Assert.That(flowModel.Phase.Value, Is.EqualTo(FlowPhase.RoomChoosing));

        var nodeBefore = runModel.NodeInLayer.Value;
        TableNine.Interface.SendCommand(new ChooseRoomCommand(DefaultGameConfigFactory.RoomGoldId));

        Assert.That(runModel.NodeInLayer.Value, Is.EqualTo(nodeBefore + 1));
        Assert.That(flowModel.Phase.Value, Is.EqualTo(FlowPhase.PlayerControl));
    }

    [Test]
    public void SkipHelpReward_Gives_10_Gold_And_Enters_RoomChoosing()
    {
        StartRunAndClearNode();
        var flowModel = TableNine.Interface.GetModel<IFlowModel>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var rewardModel = TableNine.Interface.GetModel<IRewardModel>();

        var goldBefore = playerModel.Gold.Value;
        TableNine.Interface.SendCommand(new SkipHelpRewardCommand());

        Assert.That(playerModel.Gold.Value - goldBefore, Is.EqualTo(RewardConstants.SkipHelpRewardGold));
        Assert.That(flowModel.Phase.Value, Is.EqualTo(FlowPhase.RoomChoosing));
        Assert.That(flowModel.HasLock(InputLockReason.OverlayVisible), Is.False);
        Assert.That(rewardModel.RoomCandidateIds.Count, Is.EqualTo(RewardConstants.RoomCandidateCount));
    }

    [Test]
    public void RoomChoosing_Unused_HelpCard_On_Board_Settles_At_Node_End()
    {
        StartRunAndClearNode();
        TableNine.Interface.SendCommand(new SkipHelpRewardCommand());

        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var helpUid = FindFirstActiveHelpCard();
        foreach (var pair in deckModel.HelpCardStates)
        {
            pair.Value.IsOnBoard = false;
            pair.Value.IsInItemSlot = false;
        }

        deckModel.HelpCardStates[helpUid.Value].IsOnBoard = true;

        var settledEvents = new List<HelpCardsSettledEvent>();
        var unReg = TableNine.Interface.RegisterEvent<HelpCardsSettledEvent>(settledEvents.Add);

        TableNine.Interface.SendCommand(new ChooseRoomCommand(DefaultGameConfigFactory.RoomGoldId));

        unReg.UnRegister();

        Assert.That(settledEvents.Count, Is.EqualTo(1));
        Assert.That(settledEvents[0].UnusedCount, Is.EqualTo(1));
        Assert.That(settledEvents[0].GoldGained, Is.EqualTo(RewardConstants.UnusedHelpCardGold));
    }

    [Test]
    public void GoldRoom_Gives_50_Gold_And_Proceeds()
    {
        StartRunAndClearNode();
        TableNine.Interface.SendCommand(new SkipHelpRewardCommand());

        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var runModel = TableNine.Interface.GetModel<IRunModel>();
        var goldBefore = playerModel.Gold.Value;
        var nodeBefore = runModel.NodeInLayer.Value;

        TableNine.Interface.SendCommand(new ChooseRoomCommand(DefaultGameConfigFactory.RoomGoldId));

        Assert.That(playerModel.Gold.Value - goldBefore, Is.GreaterThanOrEqualTo(50));
        Assert.That(runModel.NodeInLayer.Value, Is.EqualTo(nodeBefore + 1));
    }

    [Test]
    public void AttributeRoom_Increases_MaxHp_And_Full_Heals()
    {
        StartRunAndClearNode();
        TableNine.Interface.SendCommand(new SkipHelpRewardCommand());

        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var playerRuntime = collectionModel.GetCard(playerModel.PlayerCardUid);
        playerRuntime.CurrentHp = 3;
        var maxHpBefore = playerRuntime.MaxHp;

        TableNine.Interface.SendCommand(new ChooseRoomCommand(DefaultGameConfigFactory.RoomAttributeId));

        Assert.That(playerRuntime.MaxHp, Is.EqualTo(maxHpBefore + RewardConstants.AttributeRoomMaxHpBonus));
        var effectiveMax = TableNine.Interface.GetSystem<IStatSystem>().GetEffectivePlayerStats().MaxHp;
        Assert.That(playerRuntime.CurrentHp, Is.EqualTo(effectiveMax));
        Assert.That(TableNine.Interface.GetModel<IRunModel>().NodeInLayer.Value, Is.EqualTo(2));
    }

    [Test]
    public void ChestRoom_PickRelic_Proceeds_To_Next_Node()
    {
        StartRunAndClearNode();
        TableNine.Interface.SendCommand(new SkipHelpRewardCommand());
        TableNine.Interface.SendCommand(new ChooseRoomCommand(DefaultGameConfigFactory.RoomChestId));

        var rewardModel = TableNine.Interface.GetModel<IRewardModel>();
        var relicId = rewardModel.ChestRewardRelicIds[0];
        TableNine.Interface.SendCommand(new PickRelicRewardCommand(relicId));

        Assert.That(TableNine.Interface.GetSystem<IRelicSystem>().HasRelic(relicId), Is.True);
        Assert.That(TableNine.Interface.GetModel<IRunModel>().NodeInLayer.Value, Is.EqualTo(2));
    }

    [Test]
    public void ShopRoom_CloseShop_Proceeds_To_Next_Node_Not_HelpReward()
    {
        StartRunAndClearNode();
        TableNine.Interface.SendCommand(new SkipHelpRewardCommand());
        TableNine.Interface.SendCommand(new ChooseRoomCommand(DefaultGameConfigFactory.RoomShopId));

        var flowModel = TableNine.Interface.GetModel<IFlowModel>();
        var runModel = TableNine.Interface.GetModel<IRunModel>();
        Assert.That(flowModel.Phase.Value, Is.EqualTo(FlowPhase.Shop));

        var nodeBefore = runModel.NodeInLayer.Value;
        TableNine.Interface.SendCommand(new CloseShopCommand());

        Assert.That(flowModel.Phase.Value, Is.Not.EqualTo(FlowPhase.HelpRewardChoosing));
        Assert.That(runModel.NodeInLayer.Value, Is.EqualTo(nodeBefore + 1));
    }

    [Test]
    public void Shop_DeleteHelpCard_Gives_10_Gold()
    {
        StartRun(42);
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var helpUid = FindFirstActiveHelpCard();
        var goldBefore = playerModel.Gold.Value;

        TableNine.Interface.SendCommand(new DeleteHelpCardForGoldCommand(helpUid));

        Assert.That(playerModel.Gold.Value - goldBefore, Is.EqualTo(RewardConstants.DeleteHelpCardGold));
    }

    [Test]
    public void Shop_DeleteHelpCard_Rejects_Temporarily_Removed_Cards()
    {
        StartRun(42);
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var helpUid = FindFirstActiveHelpCard();
        deckModel.HelpCardStates[helpUid.Value].IsTemporarilyRemoved = true;

        var goldBefore = playerModel.Gold.Value;
        var deleted = TableNine.Interface.GetSystem<IShopSystem>().DeleteHelpCardForGold(helpUid);

        Assert.That(deleted, Is.False);
        Assert.That(playerModel.Gold.Value, Is.EqualTo(goldBefore));
    }

    [Test]
    public void PickHelpReward_Does_Not_Settle_Unused_HelpCards()
    {
        StartRunAndClearNode();
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var rewardModel = TableNine.Interface.GetModel<IRewardModel>();

        var helpUid = FindFirstActiveHelpCard();
        deckModel.HelpCardStates[helpUid.Value].IsOnBoard = true;
        var goldBefore = playerModel.Gold.Value;

        if (rewardModel.HelpRewardCardIds.Count == 0)
        {
            rewardModel.AddHelpRewardCardId(DefaultGameConfigFactory.HelpBlessingId);
        }

        var cardId = rewardModel.HelpRewardCardIds[0];
        TableNine.Interface.SendCommand(new PickHelpCardRewardCommand(cardId));

        Assert.That(playerModel.Gold.Value, Is.EqualTo(goldBefore));
        Assert.That(TableNine.Interface.GetModel<IFlowModel>().Phase.Value, Is.EqualTo(FlowPhase.RoomChoosing));
    }

    private static void StartRun(int seed)
    {
        TableNine.Interface.SendCommand(new StartNewRunCommand(seedOverride: seed));
    }

    private static void StartRunAndClearNode()
    {
        StartRun(42);
        var boardSystem = TableNine.Interface.GetSystem<IBoardSystem>();
        var boardModel = TableNine.Interface.GetModel<IBoardModel>();
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();

        deckModel.BattleDrawPile.Clear();
        TableNine.Interface.GetSystem<IDeckSystem>().UpdateNextBattlePreview();

        for (var i = 1; i <= 9; i++)
        {
            var slot = new BoardSlotNo(i);
            var uid = boardModel.GetCardAt(slot);
            if (!uid.HasValue || uid.Value.Equals(playerModel.PlayerCardUid))
            {
                continue;
            }

            var runtime = collectionModel.GetCard(uid.Value);
            if (runtime.CardType == CardType.Monster)
            {
                boardSystem.RemoveCardAt(slot);
                collectionModel.RemoveCard(uid.Value);
            }
        }

        TableNine.Interface.SendCommand(new CheckClearConditionCommand());
    }

    private static TableNineUIRouter CreateUIRouterForTests(out TableNineUIPanelRegistry registry)
    {
        registry = ScriptableObject.CreateInstance<TableNineUIPanelRegistry>();
        TableNineUIRuntimeTestRecorder.SkipActualPanelOpen = true;

        var router = new TableNineUIRouter(registry);
        router.Start();
        return router;
    }

    private static CardUid FindFirstActiveHelpCard()
    {
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();

        for (var i = 0; i < deckModel.OwnedHelpCards.Count; i++)
        {
            var uid = deckModel.OwnedHelpCards[i];
            if (!deckModel.HelpCardStates.TryGetValue(uid.Value, out var state) ||
                state.IsPermanentlyRemoved ||
                state.IsTemporarilyRemoved)
            {
                continue;
            }

            if (collectionModel.TryGetCard(uid, out var runtime) && runtime.CardType == CardType.Help)
            {
                return uid;
            }
        }

        Assert.Fail("找不到可用帮助卡");
        return default;
    }
}
