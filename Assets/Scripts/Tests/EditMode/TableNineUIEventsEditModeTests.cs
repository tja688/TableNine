using System.Collections.Generic;
using NUnit.Framework;
using QFramework;

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
        var cardUid = PickHelpCardToFirstItemSlot(DefaultGameConfigFactory.HelpAttributeUpId);

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
}
