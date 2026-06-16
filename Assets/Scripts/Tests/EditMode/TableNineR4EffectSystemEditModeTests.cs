using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

[Category(TableNineTestCategories.NewRuleTests)]
public sealed class TableNineR4EffectSystemEditModeTests
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
    public void ConfigValidator_Requires_Help_Cards_To_Have_EffectGraphId()
    {
        var bundle = TableNineTestConfig.LoadProductionRuntimeBundle();
        var helpGraphErrors = ConfigValidator.Validate(bundle)
            .Where(error => error.Contains("EffectGraphId"))
            .ToList();

        Assert.That(helpGraphErrors, Is.Empty, string.Join("\n", helpGraphErrors));
        for (var i = 0; i < bundle.Core.Cards.Count; i++)
        {
            var card = bundle.Core.Cards[i];
            if (card.CardType != CardType.Help)
            {
                continue;
            }

            Assert.That(card.EffectGraphId, Is.Not.Empty, card.CardId);
            Assert.That(bundle.EffectGraphs.Any(graph => graph.EffectGraphId == card.EffectGraphId), Is.True, card.CardId);
        }
    }

    [Test]
    public void ConfigValidator_Fails_When_Help_Card_Missing_EffectGraphId()
    {
        var bundle = TableNineTestConfig.LoadProductionRuntimeBundle();
        var orphan = new CardDefinition
        {
            CardId = "help_orphan",
            DisplayName = "孤儿卡",
            CardType = CardType.Help,
            Quality = CardQuality.White
        };
        bundle.Core.Cards.Add(orphan);

        var errors = ConfigValidator.Validate(bundle);

        Assert.That(errors.Exists(e => e.Contains("help_orphan") && e.Contains("EffectGraphId")), Is.True);
    }

    [Test]
    public void UseHelpCard_Resolves_Potion_Via_EffectGraph()
    {
        StartRun(12345);
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var potionUid = MoveHelpCardToItemSlot(GameConfigIds.HelpPotionId);
        var playerRuntime = collectionModel.GetCard(playerModel.PlayerCardUid);
        playerRuntime.CurrentHp = 3;

        UseItemSlotHelpCard(potionUid);

        Assert.That(playerRuntime.CurrentHp, Is.EqualTo(playerRuntime.MaxHp));
        Assert.That(deckModel.HelpCardStates[potionUid.Value].IsPermanentlyRemoved, Is.True);
    }

    [Test]
    public void ThrowingKnife_Still_Targets_Any_Monster_Slot()
    {
        StartRun(12345);
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var knifeUid = MoveHelpCardToItemSlot(GameConfigIds.HelpThrowingKnifeId);
        var targetUid = MoveAnyMonsterToSlot(new BoardSlotNo(1));
        var targetRuntime = collectionModel.GetCard(targetUid);
        targetRuntime.CurrentHp = 7;

        UseItemSlotHelpCard(knifeUid);
        TableNine.Interface.SendCommand(new ClickBoardSlotCommand(new BoardSlotNo(1)));

        Assert.That(collectionModel.GetCard(targetUid).CurrentHp, Is.EqualTo(1));
        Assert.That(deckModel.HelpCardStates[knifeUid.Value].IsPermanentlyRemoved, Is.True);
        Assert.That(deckModel.PendingHelpCardAction.IsActive, Is.False);
    }

    [Test]
    public void HelpCardInteractionIntent_Distinguishes_Targeting_And_Immediate_Cards()
    {
        StartRun(12345);
        var configModel = TableNine.Interface.GetModel<IConfigModel>();

        var knifeIntent = HelpCardInteractionUtility.ResolveIntent(configModel.GetCardDefinition(GameConfigIds.HelpThrowingKnifeId));
        var potionIntent = HelpCardInteractionUtility.ResolveIntent(configModel.GetCardDefinition(GameConfigIds.HelpPotionId));
        var swapIntent = HelpCardInteractionUtility.ResolveIntent(configModel.GetCardDefinition(GameConfigIds.HelpSwapId));

        Assert.That(knifeIntent.Kind, Is.EqualTo(HelpCardPlayIntentKind.Targeting));
        Assert.That(knifeIntent.TargetingMode, Is.EqualTo("damage"));
        Assert.That(potionIntent.Kind, Is.EqualTo(HelpCardPlayIntentKind.Immediate));
        Assert.That(swapIntent.Kind, Is.EqualTo(HelpCardPlayIntentKind.SwapTarget));
    }

    [Test]
    public void ThrowingKnife_NonLethal_Does_Not_Fire_MonsterKilled()
    {
        StartRun(12345);
        var killed = new List<MonsterKilledEvent>();
        var unRegister = TableNine.Interface.RegisterEvent<MonsterKilledEvent>(killed.Add);
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var knifeUid = MoveHelpCardToItemSlot(GameConfigIds.HelpThrowingKnifeId);
        var targetUid = MoveAnyMonsterToSlot(new BoardSlotNo(1));
        var targetRuntime = collectionModel.GetCard(targetUid);
        targetRuntime.CurrentHp = 7;
        targetRuntime.CurrentArmor = 0;
        targetRuntime.BaseArmor = 0;

        UseItemSlotHelpCard(knifeUid);
        TableNine.Interface.SendCommand(new ResolveTargetingCommand(new BoardSlotNo(1)));

        Assert.That(killed, Is.Empty);
        Assert.That(collectionModel.TryGetCard(targetUid, out var runtime), Is.True);
        Assert.That(runtime.CurrentHp, Is.EqualTo(1));

        unRegister.UnRegister();
    }

    [Test]
    public void ThrowingKnife_Lethal_Fires_MonsterKilled_And_Clears_ItemSlot()
    {
        StartRun(12345);
        var killed = new List<MonsterKilledEvent>();
        var itemChanges = new List<ItemSlotChangedEvent>();
        var killedRegister = TableNine.Interface.RegisterEvent<MonsterKilledEvent>(killed.Add);
        var itemRegister = TableNine.Interface.RegisterEvent<ItemSlotChangedEvent>(itemChanges.Add);
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var knifeUid = MoveHelpCardToItemSlot(GameConfigIds.HelpThrowingKnifeId);
        var itemSlotIndex = collectionModel.GetCard(knifeUid).ItemSlotIndex.Value;
        var targetUid = MoveAnyMonsterToSlot(new BoardSlotNo(1));
        var targetRuntime = collectionModel.GetCard(targetUid);
        targetRuntime.CurrentHp = 1;
        targetRuntime.CurrentArmor = 0;
        targetRuntime.BaseArmor = 0;

        UseItemSlotHelpCard(knifeUid);
        TableNine.Interface.SendCommand(new ResolveTargetingCommand(new BoardSlotNo(1)));

        Assert.That(killed.Count(e => e.MonsterUid.Equals(targetUid)), Is.EqualTo(1));
        Assert.That(deckModel.ItemSlots[itemSlotIndex].HasValue, Is.False);
        Assert.That(itemChanges.Any(e => e.ItemSlotIndex == itemSlotIndex && !e.Uid.HasValue), Is.True);

        killedRegister.UnRegister();
        itemRegister.UnRegister();
    }

    [Test]
    public void Attribute_Defense_Choice_Syncs_Armor()
    {
        StartRun(12345);
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var cardUid = SpawnHelpCardToItemSlot(GameConfigIds.HelpAttributeUpId);
        var playerRuntime = collectionModel.GetCard(playerModel.PlayerCardUid);
        var defenseBefore = playerRuntime.BaseArmor;
        var armorBefore = playerRuntime.CurrentArmor;

        UseItemSlotHelpCard(cardUid);
        TableNine.Interface.SendCommand(new ResolveAttributeChoiceCommand(AttributeUpgradeChoice.Armor));

        Assert.That(playerRuntime.BaseArmor, Is.EqualTo(defenseBefore + 1));
        Assert.That(playerRuntime.CurrentArmor, Is.EqualTo(armorBefore + 1));
    }

    [Test]
    public void Gold_Card_Adds_50_Gold_And_Consumes()
    {
        StartRun(12345);
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var goldBefore = playerModel.Gold.Value;
        var uid = SpawnHelpCardToItemSlot(GameConfigIds.HelpGoldCardId);

        UseItemSlotHelpCard(uid);

        Assert.That(playerModel.Gold.Value, Is.EqualTo(goldBefore + 50));
        Assert.That(deckModel.HelpCardStates[uid.Value].IsPermanentlyRemoved, Is.True);
    }

    [Test]
    public void Blessing_Applies_Shield_Status_And_Consumes()
    {
        StartRun(12345);
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var uid = SpawnHelpCardToItemSlot(GameConfigIds.HelpBlessingId);

        UseItemSlotHelpCard(uid);

        Assert.That(deckModel.PendingHelpCardAction.Kind, Is.EqualTo(PendingHelpCardActionKind.BlessingShield));
        Assert.That(deckModel.HelpCardStates[uid.Value].IsPermanentlyRemoved, Is.True);
    }

    [Test]
    public void Fireball_Deals_Player_Attack_Damage_Via_Targeting()
    {
        StartRun(12345);
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var uid = SpawnHelpCardToItemSlot(GameConfigIds.HelpFireballId);
        var targetUid = MoveAnyMonsterToSlot(new BoardSlotNo(3));
        var targetRuntime = collectionModel.GetCard(targetUid);
        var playerAttack = collectionModel.GetCard(playerModel.PlayerCardUid).BaseAttack;
        targetRuntime.CurrentHp = 12;
        targetRuntime.CurrentArmor = 0;
        targetRuntime.BaseArmor = 0;

        UseItemSlotHelpCard(uid);
        TableNine.Interface.SendCommand(new ClickBoardSlotCommand(new BoardSlotNo(3)));

        Assert.That(collectionModel.GetCard(targetUid).CurrentHp, Is.EqualTo(12 - playerAttack));
    }

    [Test]
    public void Bomb_Damages_All_Monsters_By_4()
    {
        StartRun(12345);
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var boardModel = TableNine.Interface.GetModel<IBoardModel>();
        var uid = SpawnHelpCardToItemSlot(GameConfigIds.HelpBombId);
        for (var slot = 1; slot <= 9; slot++)
        {
            var boardSlot = new BoardSlotNo(slot);
            var cardUid = boardModel.GetCardAt(boardSlot);
            if (!cardUid.HasValue || !collectionModel.TryGetCard(cardUid.Value, out var runtime) || runtime.CardType != CardType.Monster)
            {
                continue;
            }

            runtime.CurrentHp = 10;
            runtime.CurrentArmor = 0;
        }

        UseItemSlotHelpCard(uid);

        for (var slot = 1; slot <= 9; slot++)
        {
            var boardSlot = new BoardSlotNo(slot);
            var cardUid = boardModel.GetCardAt(boardSlot);
            if (!cardUid.HasValue || !collectionModel.TryGetCard(cardUid.Value, out var runtime) || runtime.CardType != CardType.Monster)
            {
                continue;
            }

            Assert.That(runtime.CurrentHp, Is.EqualTo(6));
        }
    }

    [Test]
    public void Food_Card_Fully_Heals_Player()
    {
        StartRun(12345);
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var uid = SpawnHelpCardToItemSlot(GameConfigIds.HelpFoodId);
        var playerRuntime = collectionModel.GetCard(playerModel.PlayerCardUid);
        playerRuntime.CurrentHp = 1;

        UseItemSlotHelpCard(uid);

        Assert.That(playerRuntime.CurrentHp, Is.EqualTo(playerRuntime.MaxHp));
    }

    [Test]
    public void Swap_Card_Swaps_Two_Non_Player_Cards()
    {
        StartRun(12345);
        var boardModel = TableNine.Interface.GetModel<IBoardModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var boardSystem = TableNine.Interface.GetSystem<IBoardSystem>();
        var uid = SpawnHelpCardToItemSlot(GameConfigIds.HelpSwapId);
        var monsters = CollectMonsterUids(collectionModel, boardModel);
        Assert.That(monsters.Count, Is.GreaterThanOrEqualTo(2), "Need at least two monsters on board.");

        boardSystem.RemoveCardAt(new BoardSlotNo(1));
        boardSystem.RemoveCardAt(new BoardSlotNo(3));
        boardSystem.PlaceCard(monsters[0], new BoardSlotNo(1), CardPlacementSource.Refill);
        boardSystem.PlaceCard(monsters[1], new BoardSlotNo(3), CardPlacementSource.Refill);
        var firstUid = monsters[0];
        var secondUid = monsters[1];

        UseItemSlotHelpCard(uid);
        TableNine.Interface.SendCommand(new ClickBoardSlotCommand(new BoardSlotNo(1)));
        TableNine.Interface.SendCommand(new ClickBoardSlotCommand(new BoardSlotNo(3)));

        Assert.That(boardModel.GetCardAt(new BoardSlotNo(1)).Value, Is.EqualTo(secondUid));
        Assert.That(boardModel.GetCardAt(new BoardSlotNo(3)).Value, Is.EqualTo(firstUid));
        Assert.That(collectionModel.GetCard(firstUid).BoardSlot, Is.EqualTo(new BoardSlotNo(3)));
        Assert.That(collectionModel.GetCard(secondUid).BoardSlot, Is.EqualTo(new BoardSlotNo(1)));
    }

    [Test]
    public void Smasher_Reduces_Target_Armor_By_10()
    {
        StartRun(12345);
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var uid = SpawnHelpCardToItemSlot(GameConfigIds.HelpSmasherId);
        var targetUid = MoveAnyMonsterToSlot(new BoardSlotNo(2));
        var targetRuntime = collectionModel.GetCard(targetUid);
        targetRuntime.CurrentArmor = 12;

        UseItemSlotHelpCard(uid);
        TableNine.Interface.SendCommand(new ClickBoardSlotCommand(new BoardSlotNo(2)));

        Assert.That(targetRuntime.CurrentArmor, Is.EqualTo(2));
    }

    [Test]
    public void Spin_Wheel_Rotates_Counterclockwise()
    {
        StartRun(12345);
        var boardSystem = TableNine.Interface.GetSystem<IBoardSystem>();
        var boardModel = TableNine.Interface.GetModel<IBoardModel>();
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var uid = SpawnHelpCardToItemSlot(GameConfigIds.HelpSpinWheelId);

        boardSystem.RemoveCardAt(new BoardSlotNo(2));
        deckModel.BattleDrawPile.Clear();
        var before = CaptureRingDefinitionLayout(boardModel, TableNine.Interface.GetModel<ICollectionModel>());

        UseItemSlotHelpCard(uid);

        var expected = new string[before.Length];
        for (var i = 0; i < before.Length; i++)
        {
            expected[i] = before[(i + 1) % before.Length];
        }

        CollectionAssert.AreEqual(expected, CaptureRingDefinitionLayout(boardModel, TableNine.Interface.GetModel<ICollectionModel>()));
    }

    [Test]
    public void Chest_In_RoomChoosing_Returns_To_RoomChoosing_Not_Help_Reward()
    {
        StartRun(42);
        var flowModel = TableNine.Interface.GetModel<IFlowModel>();
        var rewardModel = TableNine.Interface.GetModel<IRewardModel>();
        var relicSystem = TableNine.Interface.GetSystem<IRelicSystem>();

        TableNine.Interface.SendCommand(new GenerateHelpRewardCommand());
        TableNine.Interface.SendCommand(new SkipHelpRewardCommand());
        Assert.That(flowModel.Phase.Value, Is.EqualTo(FlowPhase.RoomChoosing));

        var chestUid = SpawnHelpCardToItemSlot(GameConfigIds.HelpCommonChestId);
        UseItemSlotHelpCard(chestUid);

        Assert.That(flowModel.Phase.Value, Is.EqualTo(FlowPhase.ChestRewardChoosing));
        Assert.That(rewardModel.ChestRewardRelicIds.Count, Is.GreaterThan(0));

        var relicId = rewardModel.ChestRewardRelicIds[0];
        TableNine.Interface.SendCommand(new PickRelicRewardCommand(relicId));

        Assert.That(flowModel.Phase.Value, Is.EqualTo(FlowPhase.RoomChoosing));
        Assert.That(flowModel.Phase.Value, Is.Not.EqualTo(FlowPhase.HelpRewardChoosing));
        Assert.That(rewardModel.CurrentRewardSource, Is.EqualTo(RewardSource.None));
    }

    [Test]
    public void Gold_Chest_Uses_Gold_Tier_Quality_Pool()
    {
        StartRun(777);
        var rewardModel = TableNine.Interface.GetModel<IRewardModel>();
        var configModel = TableNine.Interface.GetModel<IConfigModel>();
        var relicSystem = TableNine.Interface.GetSystem<IRelicSystem>();

        relicSystem.GenerateChestRewardCandidates(ChestTier.Gold);

        Assert.That(rewardModel.ChestRewardRelicIds.Count, Is.GreaterThan(0));
        for (var i = 0; i < rewardModel.ChestRewardRelicIds.Count; i++)
        {
            var quality = configModel.GetRelicDefinition(rewardModel.ChestRewardRelicIds[i]).Quality;
            Assert.That(quality, Is.EqualTo(CardQuality.Blue).Or.EqualTo(CardQuality.Gold));
        }
    }

    [Test]
    public void UseHelpCard_Without_EffectGraph_Shows_NotConfigured_Message()
    {
        StartRun(12345);
        var configModel = TableNine.Interface.GetModel<IConfigModel>();
        var definition = configModel.GetCardDefinition(GameConfigIds.HelpFoodId);
        definition.EffectGraphId = null;
        var uid = SpawnHelpCardToItemSlot(GameConfigIds.HelpFoodId);

        LogAssert.Expect(UnityEngine.LogType.Error, "[UseHelpCardCommand] Help card help_food missing EffectGraphId.");
        var messages = new List<GameplayMessageEvent>();
        var unReg = TableNine.Interface.RegisterEvent<GameplayMessageEvent>(messages.Add);
        UseItemSlotHelpCard(uid);
        unReg.UnRegister();

        Assert.That(messages.Exists(m => m.Message.Contains(definition.DisplayName)), Is.True);
    }

    private static void StartRun(int seed)
    {
        TableNine.InitArchitecture();
        TableNine.Interface.SendCommand(new StartNewRunCommand(seedOverride: seed));
    }

    private static void UseItemSlotHelpCard(CardUid uid)
    {
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        TableNine.Interface.SendCommand(new ClickItemSlotCommand(collectionModel.GetCard(uid).ItemSlotIndex.Value));
    }

    private static CardUid MoveHelpCardToItemSlot(string definitionId)
    {
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        for (var i = 0; i < deckModel.OwnedHelpCards.Count; i++)
        {
            var uid = deckModel.OwnedHelpCards[i];
            if (collectionModel.GetCard(uid).DefinitionId != definitionId)
            {
                continue;
            }

            TableNine.Interface.SendCommand(new PickHelpCardToItemSlotCommand(uid));
            return uid;
        }

        Assert.Fail($"Help card not found: {definitionId}");
        return default;
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

    private static CardUid MoveAnyMonsterToSlot(BoardSlotNo slot)
    {
        var boardSystem = TableNine.Interface.GetSystem<IBoardSystem>();
        var boardModel = TableNine.Interface.GetModel<IBoardModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var currentAtSlot = boardModel.GetCardAt(slot);
        if (currentAtSlot.HasValue)
        {
            boardSystem.RemoveCardAt(slot);
        }

        for (var i = 1; i <= 9; i++)
        {
            var fromSlot = new BoardSlotNo(i);
            var uid = boardModel.GetCardAt(fromSlot);
            if (!uid.HasValue)
            {
                continue;
            }

            var runtime = collectionModel.GetCard(uid.Value);
            if (runtime.CardType != CardType.Monster)
            {
                continue;
            }

            boardSystem.RemoveCardAt(fromSlot);
            boardSystem.PlaceCard(uid.Value, slot, CardPlacementSource.Refill);
            return uid.Value;
        }

        Assert.Fail("Could not find a monster to move.");
        return default;
    }

    private static List<CardUid> CollectMonsterUids(ICollectionModel collectionModel, IBoardModel boardModel)
    {
        var monsters = new List<CardUid>();
        for (var slot = 1; slot <= 9; slot++)
        {
            var boardSlot = new BoardSlotNo(slot);
            var cardUid = boardModel.GetCardAt(boardSlot);
            if (!cardUid.HasValue)
            {
                continue;
            }

            if (collectionModel.GetCard(cardUid.Value).CardType == CardType.Monster)
            {
                monsters.Add(cardUid.Value);
            }
        }

        return monsters;
    }

    private static string[] CaptureRingDefinitionLayout(IBoardModel boardModel, ICollectionModel collectionModel)
    {
        var layout = new string[BoardSlotUtility.ClockwiseRing.Length];
        for (var i = 0; i < BoardSlotUtility.ClockwiseRing.Length; i++)
        {
            var uid = boardModel.GetCardAt(BoardSlotUtility.ClockwiseRing[i]);
            layout[i] = uid.HasValue ? collectionModel.GetCard(uid.Value).DefinitionId : string.Empty;
        }

        return layout;
    }
}
