using System.Collections.Generic;
using NUnit.Framework;
using QFramework;

[Category(TableNineTestCategories.NewRuleTests)]
public sealed class TableNineR2BasicRulesEditModeTests
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
    public void NodeStart_Fills_Player_Armor_From_Effective_Defense()
    {
        TableNine.InitArchitecture();
        TableNine.Interface.SendCommand(new StartNewRunCommand(seedOverride: 42));

        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var player = collectionModel.GetCard(playerModel.PlayerCardUid);
        var stats = TableNine.Interface.GetSystem<IStatSystem>().GetEffectivePlayerStats();

        Assert.That(player.CurrentArmor, Is.EqualTo(stats.Armor));
    }

    [Test]
    public void Next_Node_Start_Refills_Armor_From_Defense_Not_Carryover()
    {
        TableNine.InitArchitecture();
        TableNine.Interface.SendCommand(new StartNewRunCommand(seedOverride: 42));

        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var player = collectionModel.GetCard(playerModel.PlayerCardUid);
        player.CurrentArmor = 0;

        TableNine.Interface.SendCommand(new StartNodeCommand(1, 2));

        var stats = TableNine.Interface.GetSystem<IStatSystem>().GetEffectivePlayerStats();
        Assert.That(player.CurrentArmor, Is.EqualTo(stats.Armor));
    }

    [Test]
    public void Defense_Stat_Change_Also_Increases_Current_Armor()
    {
        TableNine.InitArchitecture();
        TableNine.Interface.SendCommand(new StartNewRunCommand(seedOverride: 7));

        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var player = collectionModel.GetCard(playerModel.PlayerCardUid);
        var defenseBefore = player.BaseArmor;
        var armorBefore = player.CurrentArmor;

        ArmorChangedEvent? armorEvent = null;
        StatsDirtyEvent? dirtyEvent = null;
        var unRegisterArmor = TableNine.Interface.RegisterEvent<ArmorChangedEvent>(e => armorEvent = e);
        var unRegisterDirty = TableNine.Interface.RegisterEvent<StatsDirtyEvent>(e => dirtyEvent = e);

        TableNine.Interface.SendCommand(new ApplyStatChangeCommand(
            playerModel.PlayerCardUid, StatType.Armor, 1, "test_defense_up"));

        unRegisterArmor.UnRegister();
        unRegisterDirty.UnRegister();

        Assert.That(player.BaseArmor, Is.EqualTo(defenseBefore + 1));
        Assert.That(player.CurrentArmor, Is.EqualTo(armorBefore + 1));
        Assert.That(armorEvent.HasValue, Is.True);
        Assert.That(armorEvent.Value.OldArmor, Is.EqualTo(armorBefore));
        Assert.That(armorEvent.Value.NewArmor, Is.EqualTo(armorBefore + 1));
        Assert.That(armorEvent.Value.CauseId, Is.EqualTo("test_defense_up"));
        Assert.That(dirtyEvent.HasValue, Is.True);
        Assert.That(dirtyEvent.Value.TargetUid, Is.EqualTo(playerModel.PlayerCardUid));
    }

    [Test]
    public void ApplyDamage_Absorbs_Armor_Before_Hp()
    {
        TableNine.InitArchitecture();
        TableNine.Interface.SendCommand(new StartNewRunCommand(seedOverride: 11));

        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var player = collectionModel.GetCard(playerModel.PlayerCardUid);
        player.CurrentArmor = 10;
        var hpBefore = player.CurrentHp;

        ArmorChangedEvent? armorEvent = null;
        StatsDirtyEvent? dirtyEvent = null;
        DamageAppliedEvent? damageEvent = null;
        var unRegisterArmor = TableNine.Interface.RegisterEvent<ArmorChangedEvent>(e => armorEvent = e);
        var unRegisterDirty = TableNine.Interface.RegisterEvent<StatsDirtyEvent>(e => dirtyEvent = e);
        var unRegisterDamage = TableNine.Interface.RegisterEvent<DamageAppliedEvent>(e => damageEvent = e);

        TableNine.Interface.SendCommand(new ApplyDamageCommand(playerModel.PlayerCardUid, 3));

        unRegisterArmor.UnRegister();
        unRegisterDirty.UnRegister();
        unRegisterDamage.UnRegister();

        Assert.That(player.CurrentArmor, Is.EqualTo(7));
        Assert.That(player.CurrentHp, Is.EqualTo(hpBefore));
        Assert.That(armorEvent.HasValue, Is.True);
        Assert.That(armorEvent.Value.OldArmor, Is.EqualTo(10));
        Assert.That(armorEvent.Value.NewArmor, Is.EqualTo(7));
        Assert.That(dirtyEvent.HasValue, Is.True);
        Assert.That(dirtyEvent.Value.TargetUid, Is.EqualTo(playerModel.PlayerCardUid));
        Assert.That(damageEvent.HasValue, Is.True);
        Assert.That(damageEvent.Value.Context.ArmorAbsorbed, Is.EqualTo(3));
        Assert.That(damageEvent.Value.Context.HpDamage, Is.EqualTo(0));
    }

    [Test]
    public void ApplyDamage_Partial_Armor_Then_Hp()
    {
        TableNine.InitArchitecture();
        TableNine.Interface.SendCommand(new StartNewRunCommand(seedOverride: 11));

        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var player = collectionModel.GetCard(playerModel.PlayerCardUid);
        player.CurrentArmor = 2;
        var hpBefore = player.CurrentHp;

        TableNine.Interface.SendCommand(new ApplyDamageCommand(playerModel.PlayerCardUid, 5));

        Assert.That(player.CurrentArmor, Is.EqualTo(0));
        Assert.That(player.CurrentHp, Is.EqualTo(hpBefore - 3));
    }

    [Test]
    public void ApplyDamage_IgnoreArmor_Skips_Armor()
    {
        TableNine.InitArchitecture();
        TableNine.Interface.SendCommand(new StartNewRunCommand(seedOverride: 11));

        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var player = collectionModel.GetCard(playerModel.PlayerCardUid);
        player.CurrentArmor = 5;
        var hpBefore = player.CurrentHp;

        var context = new DamageContext
        {
            Target = playerModel.PlayerCardUid,
            CauseId = "test_ignore_armor",
            Type = DamageType.Debug,
            RawAttack = 4,
            DamageBeforeArmor = 4,
            IgnoreArmor = true
        };
        TableNine.Interface.SendCommand(new ApplyDamageCommand(context));

        Assert.That(player.CurrentArmor, Is.EqualTo(5));
        Assert.That(player.CurrentHp, Is.EqualTo(hpBefore - 4));
    }

    [Test]
    public void Combat_Damage_Uses_DamageReduction_Not_Defense()
    {
        TableNine.InitArchitecture();
        var combatSystem = TableNine.Interface.GetSystem<ICombatSystem>();

        var damage = combatSystem.CalculateDamage(
            new EffectiveStats { Attack = 5, Armor = 0 },
            new EffectiveStats { Attack = 0, Armor = 99, DamageReduction = 0 });

        Assert.That(damage, Is.EqualTo(5));

        var reduced = combatSystem.CalculateDamage(
            new EffectiveStats { Attack = 5, Armor = 0 },
            new EffectiveStats { Attack = 0, Armor = 99, DamageReduction = 2 });

        Assert.That(reduced, Is.EqualTo(3));
    }

    [Test]
    public void Default_Help_Card_Used_Is_Permanently_Removed()
    {
        TableNine.InitArchitecture();
        TableNine.Interface.SendCommand(new StartNewRunCommand(seedOverride: 12345));

        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var potionUid = FindHelpCardOnBoardOrDeck(GameConfigIds.HelpPotionId);

        TableNine.Interface.SendCommand(new PickHelpCardToItemSlotCommand(potionUid));
        var itemSlot = collectionModel.GetCard(potionUid).ItemSlotIndex.Value;
        TableNine.Interface.SendCommand(new ClickItemSlotCommand(itemSlot));

        Assert.That(deckModel.HelpCardStates[potionUid.Value].IsPermanentlyRemoved, Is.True);
        Assert.That(deckModel.HelpCardStates[potionUid.Value].IsTemporarilyRemoved, Is.False);
    }

    [Test]
    public void RestoreAfterNode_Help_Card_Is_Temporarily_Removed()
    {
        TableNine.InitArchitecture();
        TableNine.Interface.SendCommand(new StartNewRunCommand(seedOverride: 42));

        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var configModel = TableNine.Interface.GetModel<IConfigModel>();
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();

        var definition = configModel.GetCardDefinition(GameConfigIds.HelpPotionId);
        definition.RestoreAfterNode = true;

        var uid = FindHelpCardOnBoardOrDeck(GameConfigIds.HelpPotionId);
        TableNine.Interface.SendCommand(new PickHelpCardToItemSlotCommand(uid));
        var itemSlot = collectionModel.GetCard(uid).ItemSlotIndex.Value;
        TableNine.Interface.SendCommand(new ClickItemSlotCommand(itemSlot));

        Assert.That(deckModel.HelpCardStates[uid.Value].IsTemporarilyRemoved, Is.True);
        Assert.That(deckModel.HelpCardStates[uid.Value].IsPermanentlyRemoved, Is.False);
    }

    [Test]
    public void ClearCondition_Triggers_HelpReward_Before_Room()
    {
        TableNine.InitArchitecture();
        TableNine.Interface.SendCommand(new StartNewRunCommand(seedOverride: 42));
        ClearAllMonsters();

        var helpEvents = new List<HelpRewardGeneratedEvent>();
        var roomEvents = new List<RoomChoiceRequestedEvent>();
        var helpReg = TableNine.Interface.RegisterEvent<HelpRewardGeneratedEvent>(helpEvents.Add);
        var roomReg = TableNine.Interface.RegisterEvent<RoomChoiceRequestedEvent>(roomEvents.Add);

        TableNine.Interface.SendCommand(new CheckClearConditionCommand());

        Assert.That(TableNine.Interface.GetModel<IFlowModel>().Phase.Value, Is.EqualTo(FlowPhase.HelpRewardChoosing));
        Assert.That(helpEvents.Count, Is.EqualTo(1));
        Assert.That(roomEvents.Count, Is.EqualTo(0));

        helpReg.UnRegister();
        roomReg.UnRegister();
    }

    [Test]
    public void HelpReward_Completion_Enters_RoomChoosing_With_Two_Candidates()
    {
        TableNine.InitArchitecture();
        TableNine.Interface.SendCommand(new StartNewRunCommand(seedOverride: 42));
        ClearAllMonsters();
        TableNine.Interface.SendCommand(new CheckClearConditionCommand());

        var roomEvents = new List<RoomChoiceRequestedEvent>();
        var roomReg = TableNine.Interface.RegisterEvent<RoomChoiceRequestedEvent>(roomEvents.Add);

        TableNine.Interface.SendCommand(new SkipHelpRewardCommand());

        Assert.That(TableNine.Interface.GetModel<IFlowModel>().Phase.Value, Is.EqualTo(FlowPhase.RoomChoosing));
        Assert.That(roomEvents.Count, Is.EqualTo(1));
        Assert.That(roomEvents[0].RoomIds.Count, Is.EqualTo(RewardConstants.RoomCandidateCount));

        roomReg.UnRegister();
    }

    [Test]
    public void ChooseRoom_Settles_Unused_Help_Cards_After_Room_Click()
    {
        TableNine.InitArchitecture();
        TableNine.Interface.SendCommand(new StartNewRunCommand(seedOverride: 42));
        ClearAllMonsters();
        TableNine.Interface.SendCommand(new CheckClearConditionCommand());
        TableNine.Interface.SendCommand(new SkipHelpRewardCommand());

        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var goldBefore = playerModel.Gold.Value;

        TableNine.Interface.SendCommand(new ChooseRoomCommand(GameConfigIds.RoomGoldId));

        Assert.That(playerModel.Gold.Value, Is.GreaterThan(goldBefore));
    }

    private static CardUid FindHelpCardOnBoardOrDeck(string cardId)
    {
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();

        for (var i = 0; i < deckModel.OwnedHelpCards.Count; i++)
        {
            var uid = deckModel.OwnedHelpCards[i];
            if (collectionModel.TryGetCard(uid, out var runtime) && runtime.DefinitionId == cardId)
            {
                return uid;
            }
        }

        Assert.Fail($"找不到帮助卡 {cardId}");
        return default;
    }

    private static void ClearAllMonsters()
    {
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
            if (runtime.CardType != CardType.Monster)
            {
                continue;
            }

            boardSystem.RemoveCardAt(slot);
            collectionModel.RemoveCard(uid.Value);
        }
    }
}
