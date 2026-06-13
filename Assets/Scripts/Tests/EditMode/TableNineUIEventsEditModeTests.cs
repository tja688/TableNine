using System.Collections.Generic;
using NUnit.Framework;
using QFramework;
using UnityEngine;

[Category(TableNineTestCategories.RegressionTests)]
public sealed class TableNineUIEventsEditModeTests
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
    public void AttributeCard_Requests_And_Resolves_Overlay_Event()
    {
        StartRun(12345);
        var requested = new List<AttributeChoiceRequestedEvent>();
        var resolved = new List<AttributeChoiceResolvedEvent>();
        var requestRegister = TableNine.Interface.RegisterEvent<AttributeChoiceRequestedEvent>(requested.Add);
        var resolveRegister = TableNine.Interface.RegisterEvent<AttributeChoiceResolvedEvent>(resolved.Add);
        var cardUid = SpawnHelpCardToItemSlot(DefaultGameConfigFactory.HelpAttributeUpId);

        TableNine.Interface.SendCommand(new ClickItemSlotCommand(0));

        Assert.That(requested.Count, Is.EqualTo(1));
        Assert.That(requested[0].HelpCardUid, Is.EqualTo(cardUid));
        Assert.That(TableNine.Interface.GetModel<IFlowModel>().HasLock(InputLockReason.OverlayVisible), Is.True);

        TableNine.Interface.SendCommand(new ResolveAttributeChoiceCommand(AttributeUpgradeChoice.Attack));

        Assert.That(resolved.Count, Is.EqualTo(1));
        Assert.That(resolved[0].HelpCardUid, Is.EqualTo(cardUid));
        Assert.That(resolved[0].Choice, Is.EqualTo(AttributeUpgradeChoice.Attack));
        Assert.That(TableNine.Interface.GetModel<IFlowModel>().HasLock(InputLockReason.OverlayVisible), Is.False);

        requestRegister.UnRegister();
        resolveRegister.UnRegister();
    }

    [Test]
    public void ItemSlotChangedEvent_Fires_When_Picked_And_Consumed()
    {
        StartRun(12345);
        var events = new List<ItemSlotChangedEvent>();
        var unRegister = TableNine.Interface.RegisterEvent<ItemSlotChangedEvent>(events.Add);
        var potionUid = FindHelpCard(DefaultGameConfigFactory.HelpPotionId);
        var playerRuntime = TableNine.Interface.GetModel<ICollectionModel>().GetCard(TableNine.Interface.GetModel<IPlayerModel>().PlayerCardUid);
        playerRuntime.CurrentHp = 1;

        TableNine.Interface.SendCommand(new PickHelpCardToItemSlotCommand(potionUid));
        TableNine.Interface.SendCommand(new ClickItemSlotCommand(0));

        Assert.That(events.Count, Is.EqualTo(2));
        Assert.That(events[0].ItemSlotIndex, Is.EqualTo(0));
        Assert.That(events[0].Uid, Is.EqualTo(potionUid));
        Assert.That(events[1].ItemSlotIndex, Is.EqualTo(0));
        Assert.That(events[1].Uid.HasValue, Is.False);

        unRegister.UnRegister();
    }

    [Test]
    public void ClearCondition_Requests_HelpReward_Before_Room()
    {
        StartRun(12345);
        RemoveAllMonsters();
        TableNine.Interface.GetModel<IDeckModel>().BattleDrawPile.Clear();

        var helpEvents = new List<HelpRewardGeneratedEvent>();
        var roomEvents = new List<RoomChoiceRequestedEvent>();
        var helpRegister = TableNine.Interface.RegisterEvent<HelpRewardGeneratedEvent>(helpEvents.Add);
        var roomRegister = TableNine.Interface.RegisterEvent<RoomChoiceRequestedEvent>(roomEvents.Add);

        TableNine.Interface.SendCommand(new CheckClearConditionCommand());

        Assert.That(helpEvents.Count, Is.EqualTo(1));
        Assert.That(roomEvents.Count, Is.EqualTo(0));
        Assert.That(TableNine.Interface.GetModel<IFlowModel>().Phase.Value, Is.EqualTo(FlowPhase.HelpRewardChoosing));

        helpRegister.UnRegister();
        roomRegister.UnRegister();
    }

    [Test]
    public void HelpReward_Completion_Requests_RoomChoice()
    {
        StartRun(12345);
        RemoveAllMonsters();
        TableNine.Interface.GetModel<IDeckModel>().BattleDrawPile.Clear();
        TableNine.Interface.SendCommand(new CheckClearConditionCommand());

        var roomEvents = new List<RoomChoiceRequestedEvent>();
        var roomRegister = TableNine.Interface.RegisterEvent<RoomChoiceRequestedEvent>(roomEvents.Add);

        TableNine.Interface.SendCommand(new SkipHelpRewardCommand());

        Assert.That(roomEvents.Count, Is.EqualTo(1));
        Assert.That(roomEvents[0].RoomIds.Count, Is.EqualTo(RewardConstants.RoomCandidateCount));
        Assert.That(TableNine.Interface.GetModel<IFlowModel>().Phase.Value, Is.EqualTo(FlowPhase.RoomChoosing));

        roomRegister.UnRegister();
    }

    [Test]
    public void ChooseRoom_Gold_Proceeds_To_Next_Node()
    {
        StartRun(12345);
        var flowModel = TableNine.Interface.GetModel<IFlowModel>();
        var runModel = TableNine.Interface.GetModel<IRunModel>();

        TableNine.Interface.SendCommand(new ChooseRoomCommand(DefaultGameConfigFactory.RoomGoldId));

        Assert.That(runModel.NodeInLayer.Value, Is.EqualTo(2));
        Assert.That(flowModel.Phase.Value, Is.EqualTo(FlowPhase.PlayerControl));
    }

    [Test]
    public void UIPanelRegistry_Resolves_Default_Fallback_Entries()
    {
        var registry = ScriptableObject.CreateInstance<TableNineUIPanelRegistry>();

        var helpRewardEntry = registry.GetEntryOrDefault(TableNineUIKeys.HelpReward);
        var roomChoiceEntry = registry.GetEntryOrDefault(TableNineUIKeys.RoomChoice);

        Assert.That(helpRewardEntry.PanelName, Is.EqualTo("UIHelpRewardPanel"));
        Assert.That(helpRewardEntry.FallbackStrategy, Is.EqualTo(TableNineUIFallbackStrategy.AtomicChoiceList));
        Assert.That(helpRewardEntry.BlocksGameplayInput, Is.True);
        Assert.That(roomChoiceEntry.FallbackStrategy, Is.EqualTo(TableNineUIFallbackStrategy.AtomicRoomChoice));
        Assert.That(roomChoiceEntry.BlocksGameplayInput, Is.False);

        Object.DestroyImmediate(registry);
    }

    private static void StartRun(int seed)
    {
        TableNine.InitArchitecture();
        TableNine.Interface.SendCommand(new StartNewRunCommand(seedOverride: seed));
    }

    private static CardUid PickHelpCardToFirstItemSlot(string cardId)
    {
        var uid = FindHelpCard(cardId);
        TableNine.Interface.SendCommand(new PickHelpCardToItemSlotCommand(uid));
        return uid;
    }

    private static CardUid SpawnHelpCardToItemSlot(string definitionId)
    {
        var configModel = TableNine.Interface.GetModel<IConfigModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var runtime = collectionModel.CreateCard(configModel.GetCardDefinition(definitionId));
        deckModel.OwnedHelpCards.Add(runtime.Uid);
        deckModel.HelpCardStates[runtime.Uid.Value] = new HelpCardState
        {
            Uid = runtime.Uid,
            DefinitionId = runtime.DefinitionId
        };
        TableNine.Interface.SendCommand(new PickHelpCardToItemSlotCommand(runtime.Uid));
        return runtime.Uid;
    }

    private static CardUid FindHelpCard(string cardId)
    {
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        foreach (var pair in collectionModel.Cards)
        {
            if (pair.Value.CardType == CardType.Help && pair.Value.DefinitionId == cardId)
            {
                return pair.Value.Uid;
            }
        }

        Assert.Fail($"Could not find help card {cardId}.");
        return default;
    }

    private static void RemoveAllMonsters()
    {
        var boardModel = TableNine.Interface.GetModel<IBoardModel>();
        var boardSystem = TableNine.Interface.GetSystem<IBoardSystem>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var monsterUids = new List<CardUid>();

        for (var i = 1; i <= 9; i++)
        {
            var slot = new BoardSlotNo(i);
            var uid = boardModel.GetCardAt(slot);
            if (uid.HasValue &&
                collectionModel.TryGetCard(uid.Value, out var runtime) &&
                runtime.CardType == CardType.Monster)
            {
                boardSystem.RemoveCardAt(slot);
                monsterUids.Add(uid.Value);
            }
        }

        for (var i = 0; i < monsterUids.Count; i++)
        {
            collectionModel.RemoveCard(monsterUids[i]);
        }
    }
}
