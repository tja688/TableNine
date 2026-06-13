using System.Collections.Generic;
using NUnit.Framework;
using QFramework;

[Category(TableNineTestCategories.RegressionTests)]
public sealed class TableNineM1EditModeTests
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
    public void NewRun_Creates_Unique_HelpCard_Uids()
    {
        StartRun(12345);
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var unique = new HashSet<int>();

        Assert.That(deckModel.OwnedHelpCards.Count, Is.EqualTo(7));
        for (var i = 0; i < deckModel.OwnedHelpCards.Count; i++)
        {
            unique.Add(deckModel.OwnedHelpCards[i].Value);
        }

        Assert.That(unique.Count, Is.EqualTo(7));
    }

    [Test]
    public void NewRun_Places_Player_In_Slot5()
    {
        StartRun(12345);
        var boardModel = TableNine.Interface.GetModel<IBoardModel>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();

        Assert.That(boardModel.GetCardAt(new BoardSlotNo(5)), Is.EqualTo(playerModel.PlayerCardUid));
    }

    [Test]
    public void OpeningDeal_Places_Three_Help_Three_Demon_Two_Battle()
    {
        TableNine.InitArchitecture();
        var placements = new List<CardPlacedEvent>();
        var unRegister = TableNine.Interface.RegisterEvent<CardPlacedEvent>(placements.Add);

        TableNine.Interface.SendCommand(new StartNewRunCommand(seedOverride: 12345));

        unRegister.UnRegister();

        Assert.That(placements.Count, Is.EqualTo(8));
        Assert.That(CountPlacements(placements, CardPlacementSource.OpeningHelp), Is.EqualTo(3));
        Assert.That(CountPlacements(placements, CardPlacementSource.OpeningDemon), Is.EqualTo(3));
        Assert.That(CountPlacements(placements, CardPlacementSource.OpeningBattle), Is.EqualTo(2));

        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        Assert.That(deckModel.BattleDrawPile.Count, Is.EqualTo(9));
        Assert.That(deckModel.NextBattleCardPreview.Value.IsEmpty, Is.False);
    }

    [Test]
    public void SameSeed_Produces_Same_Opening_Layout()
    {
        StartRun(24680);
        var firstLayout = CaptureBoardDefinitionLayout();
        var firstPreview = TableNine.Interface.GetModel<IDeckModel>().NextBattleCardPreview.Value.DefinitionId;

        TableNine.ResetForTests();

        StartRun(24680);
        var secondLayout = CaptureBoardDefinitionLayout();
        var secondPreview = TableNine.Interface.GetModel<IDeckModel>().NextBattleCardPreview.Value.DefinitionId;

        CollectionAssert.AreEqual(firstLayout, secondLayout);
        Assert.That(secondPreview, Is.EqualTo(firstPreview));
    }

    [Test]
    public void Orthogonal_Neighbor_Of_Center_Is_Interactable_And_Diagonal_Is_Not()
    {
        StartRun(12345);
        var boardSystem = TableNine.Interface.GetSystem<IBoardSystem>();
        boardSystem.RemoveCardAt(new BoardSlotNo(2));
        boardSystem.RemoveCardAt(new BoardSlotNo(1));

        var orthogonal = TableNine.Interface.SendQuery(new CanInteractBoardSlotQuery(new BoardSlotNo(2)));
        var diagonal = TableNine.Interface.SendQuery(new CanInteractBoardSlotQuery(new BoardSlotNo(1)));

        Assert.That(orthogonal.CanInteract, Is.True);
        Assert.That(orthogonal.Kind, Is.EqualTo(InteractionKind.EmptySlot));
        Assert.That(diagonal.CanInteract, Is.False);
    }

    [Test]
    public void RotateClockwise_Moves_Ring_Correctly()
    {
        StartRun(12345);
        var boardSystem = TableNine.Interface.GetSystem<IBoardSystem>();
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var before = CaptureRingDefinitionLayout();

        boardSystem.RemoveCardAt(new BoardSlotNo(2));
        deckModel.BattleDrawPile.Clear();
        TableNine.Interface.SendCommand(new ClickBoardSlotCommand(new BoardSlotNo(2)));

        var expected = new string[before.Length];
        for (var i = 0; i < before.Length; i++)
        {
            var fromIndex = (i - 1 + before.Length) % before.Length;
            expected[i] = fromIndex == 1 ? string.Empty : before[fromIndex];
        }

        CollectionAssert.AreEqual(expected, CaptureRingDefinitionLayout());
    }

    [Test]
    public void Refill_Multiple_Empty_Slots_Draws_Until_Filled()
    {
        StartRun(12345);
        var boardSystem = TableNine.Interface.GetSystem<IBoardSystem>();
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var battleCountBefore = deckModel.BattleDrawPile.Count;

        boardSystem.RemoveCardAt(new BoardSlotNo(2));
        boardSystem.RemoveCardAt(new BoardSlotNo(4));

        TableNine.Interface.SendCommand(new RequestRefillBoardCommand());

        Assert.That(TableNine.Interface.GetModel<IBoardModel>().GetEmptySlots().Count, Is.EqualTo(0));
        Assert.That(deckModel.BattleDrawPile.Count, Is.EqualTo(battleCountBefore - 2));
    }

    [Test]
    public void Refill_Debounce_Does_Not_Double_Draw_When_Already_Running()
    {
        StartRun(12345);
        var boardSystem = TableNine.Interface.GetSystem<IBoardSystem>();
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var battleCountBefore = deckModel.BattleDrawPile.Count;

        boardSystem.RemoveCardAt(new BoardSlotNo(2));
        deckModel.RefillRunning = true;

        TableNine.Interface.SendCommand(new RequestRefillBoardCommand());
        TableNine.Interface.SendCommand(new RequestRefillBoardCommand());

        Assert.That(deckModel.BattleDrawPile.Count, Is.EqualTo(battleCountBefore));
        Assert.That(deckModel.RefillPending, Is.True);
    }

    [Test]
    [Category(TableNineTestCategories.LegacyRuleTests)]
    public void Combat_Damage_Min_Zero_When_DamageReduction_High()
    {
        TableNine.InitArchitecture();
        var combatSystem = TableNine.Interface.GetSystem<ICombatSystem>();
        var damage = combatSystem.CalculateDamage(
            new EffectiveStats { Attack = 1, Defense = 0 },
            new EffectiveStats { Attack = 0, Defense = 5, DamageReduction = 5 });

        Assert.That(damage, Is.EqualTo(0));
    }

    [Test]
    public void Player_Attacks_First_When_No_FirstStrike()
    {
        TableNine.InitArchitecture();
        var combatSystem = TableNine.Interface.GetSystem<ICombatSystem>();
        var playerActsFirst = combatSystem.PlayerActsFirst(
            new EffectiveStats { HasFirstStrike = false },
            new EffectiveStats { HasFirstStrike = false });

        Assert.That(playerActsFirst, Is.True);
    }

    [Test]
    public void Dead_Monster_Does_Not_CounterAttack_And_Adds_5_Gold()
    {
        StartRun(12345);
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var adjacentMonsterUid = MoveAnyAdjacentMonsterWithoutFirstStrikeToSlot2();
        var monsterRuntime = collectionModel.GetCard(adjacentMonsterUid);
        monsterRuntime.CurrentHp = 1;
        monsterRuntime.BaseDefense = 0;
        monsterRuntime.CurrentArmor = 0;

        var goldBefore = playerModel.Gold.Value;
        var playerHpBefore = collectionModel.GetCard(playerModel.PlayerCardUid).CurrentHp;
        TableNine.Interface.SendCommand(new ClickBoardSlotCommand(new BoardSlotNo(2)));

        Assert.That(collectionModel.GetCard(playerModel.PlayerCardUid).CurrentHp, Is.EqualTo(playerHpBefore));
        Assert.That(playerModel.Gold.Value - goldBefore, Is.EqualTo(RewardConstants.MonsterKillGold));
        Assert.That(collectionModel.TryGetCard(adjacentMonsterUid, out _), Is.False);
    }

    [Test]
    public void CheckClearCondition_When_No_Monsters_Remaining_Enters_ClearReady()
    {
        StartRun(12345);
        var boardSystem = TableNine.Interface.GetSystem<IBoardSystem>();
        var boardModel = TableNine.Interface.GetModel<IBoardModel>();
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();

        deckModel.BattleDrawPile.Clear();
        TableNine.Interface.GetSystem<IDeckSystem>().UpdateNextBattlePreview();

        for (var i = 1; i <= 9; i++)
        {
            var slot = new BoardSlotNo(i);
            var uid = boardModel.GetCardAt(slot);
            if (!uid.HasValue || uid.Value.Equals(TableNine.Interface.GetModel<IPlayerModel>().PlayerCardUid))
            {
                continue;
            }

            var runtime = collectionModel.GetCard(uid.Value);
            if (runtime.CardType != CardType.Monster)
            {
                continue;
            }

            boardSystem.RemoveCardAt(slot);
            collectionModel.RemoveCard(uid.Value);
        }

        TableNine.Interface.SendCommand(new CheckClearConditionCommand());

        Assert.That(TableNine.Interface.GetModel<IFlowModel>().Phase.Value, Is.EqualTo(FlowPhase.HelpRewardChoosing));
    }

    [Test]
    public void Potion_Heals_10_And_Permanently_Removes()
    {
        StartRun(12345);
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var potionUid = MoveHelpCardToItemSlot(GameConfigIds.HelpPotionId);
        var playerRuntime = collectionModel.GetCard(playerModel.PlayerCardUid);
        playerRuntime.CurrentHp = 3;

        TableNine.Interface.SendCommand(new ClickItemSlotCommand(collectionModel.GetCard(potionUid).ItemSlotIndex.Value));

        Assert.That(playerRuntime.CurrentHp, Is.EqualTo(playerRuntime.MaxHp));
        Assert.That(deckModel.HelpCardStates[potionUid.Value].IsPermanentlyRemoved, Is.True);
        Assert.That(collectionModel.GetCard(potionUid).ItemSlotIndex.HasValue, Is.False);
    }

    [Test]
    public void ThrowingKnife_Deals_6_Damage_Without_Adjacency_And_Permanently_Removes()
    {
        StartRun(12345);
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var knifeUid = MoveHelpCardToItemSlot(GameConfigIds.HelpThrowingKnifeId);
        var targetUid = MoveAnyMonsterToSlot(new BoardSlotNo(1));
        var targetRuntime = collectionModel.GetCard(targetUid);
        targetRuntime.CurrentHp = 7;

        TableNine.Interface.SendCommand(new ClickItemSlotCommand(collectionModel.GetCard(knifeUid).ItemSlotIndex.Value));
        TableNine.Interface.SendCommand(new ClickBoardSlotCommand(new BoardSlotNo(1)));

        Assert.That(collectionModel.GetCard(targetUid).CurrentHp, Is.EqualTo(1));
        Assert.That(deckModel.HelpCardStates[knifeUid.Value].IsPermanentlyRemoved, Is.True);
        Assert.That(deckModel.PendingHelpCardAction.IsActive, Is.False);
    }

    [Test]
    public void AttributeCard_Attack_Option_Adds_1_Attack()
    {
        StartRun(12345);
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var flowModel = TableNine.Interface.GetModel<IFlowModel>();
        var playerRuntime = collectionModel.GetCard(TableNine.Interface.GetModel<IPlayerModel>().PlayerCardUid);
        var cardUid = SpawnHelpCardToItemSlot(GameConfigIds.HelpAttributeUpId);

        TableNine.Interface.SendCommand(new ClickItemSlotCommand(collectionModel.GetCard(cardUid).ItemSlotIndex.Value));

        Assert.That(deckModel.PendingHelpCardAction.Kind, Is.EqualTo(PendingHelpCardActionKind.AttributeChoice));
        Assert.That(flowModel.HasLock(InputLockReason.OverlayVisible), Is.True);

        TableNine.Interface.SendCommand(new ResolveAttributeChoiceCommand(AttributeUpgradeChoice.Attack));

        Assert.That(playerRuntime.BaseAttack, Is.EqualTo(4));
        Assert.That(deckModel.HelpCardStates[cardUid.Value].IsPermanentlyRemoved, Is.True);
        Assert.That(flowModel.HasLock(InputLockReason.OverlayVisible), Is.False);
    }

    private static void StartRun(int seed)
    {
        TableNine.InitArchitecture();
        TableNine.Interface.SendCommand(new StartNewRunCommand(seedOverride: seed));
    }

    private static int CountPlacements(List<CardPlacedEvent> placements, CardPlacementSource source)
    {
        var count = 0;
        for (var i = 0; i < placements.Count; i++)
        {
            if (placements[i].Source == source)
            {
                count++;
            }
        }

        return count;
    }

    private static string[] CaptureBoardDefinitionLayout()
    {
        var boardModel = TableNine.Interface.GetModel<IBoardModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var layout = new string[9];
        for (var i = 1; i <= 9; i++)
        {
            var uid = boardModel.GetCardAt(new BoardSlotNo(i));
            layout[i - 1] = uid.HasValue ? collectionModel.GetCard(uid.Value).DefinitionId : string.Empty;
        }

        return layout;
    }

    private static string[] CaptureRingDefinitionLayout()
    {
        var boardModel = TableNine.Interface.GetModel<IBoardModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var layout = new string[BoardSlotUtility.ClockwiseRing.Length];
        for (var i = 0; i < BoardSlotUtility.ClockwiseRing.Length; i++)
        {
            var uid = boardModel.GetCardAt(BoardSlotUtility.ClockwiseRing[i]);
            layout[i] = uid.HasValue ? collectionModel.GetCard(uid.Value).DefinitionId : string.Empty;
        }

        return layout;
    }

    private static CardUid MoveAnyAdjacentMonsterWithoutFirstStrikeToSlot2()
    {
        var boardSystem = TableNine.Interface.GetSystem<IBoardSystem>();
        var boardModel = TableNine.Interface.GetModel<IBoardModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var currentAtTwo = boardModel.GetCardAt(new BoardSlotNo(2));
        if (currentAtTwo.HasValue)
        {
            boardSystem.RemoveCardAt(new BoardSlotNo(2));
        }

        for (var i = 1; i <= 9; i++)
        {
            var slot = new BoardSlotNo(i);
            var uid = boardModel.GetCardAt(slot);
            if (!uid.HasValue)
            {
                continue;
            }

            var runtime = collectionModel.GetCard(uid.Value);
            if (runtime.CardType == CardType.Monster && !runtime.HasSkill(GameConfigIds.SkillFirstStrikeId))
            {
                boardSystem.RemoveCardAt(slot);
                boardSystem.PlaceCard(uid.Value, new BoardSlotNo(2), CardPlacementSource.Refill);
                return uid.Value;
            }
        }

        Assert.Fail("Could not find a non-first-strike monster to move next to the player.");
        return default;
    }

    private static CardUid MoveHelpCardToItemSlot(string definitionId)
    {
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        for (var i = 0; i < deckModel.OwnedHelpCards.Count; i++)
        {
            var uid = deckModel.OwnedHelpCards[i];
            var runtime = collectionModel.GetCard(uid);
            if (runtime.DefinitionId != definitionId)
            {
                continue;
            }

            TableNine.Interface.SendCommand(new PickHelpCardToItemSlotCommand(uid));
            return uid;
        }

        Assert.Fail($"Could not find help card {definitionId}.");
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

    private static CardUid MoveAnyMonsterToSlot(BoardSlotNo targetSlot)
    {
        var boardSystem = TableNine.Interface.GetSystem<IBoardSystem>();
        var boardModel = TableNine.Interface.GetModel<IBoardModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var currentTarget = boardModel.GetCardAt(targetSlot);
        if (currentTarget.HasValue)
        {
            boardSystem.RemoveCardAt(targetSlot);
        }

        for (var i = 1; i <= 9; i++)
        {
            var slot = new BoardSlotNo(i);
            if (slot.Equals(targetSlot))
            {
                continue;
            }

            var uid = boardModel.GetCardAt(slot);
            if (!uid.HasValue)
            {
                continue;
            }

            var runtime = collectionModel.GetCard(uid.Value);
            if (runtime.CardType != CardType.Monster)
            {
                continue;
            }

            boardSystem.RemoveCardAt(slot);
            boardSystem.PlaceCard(uid.Value, targetSlot, CardPlacementSource.Refill);
            return uid.Value;
        }

        Assert.Fail($"Could not find a monster to move to slot {targetSlot.Value}.");
        return default;
    }
}
