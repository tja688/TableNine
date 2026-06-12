using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QFramework;

[Category(TableNineTestCategories.NewRuleTests)]
public sealed class TableNineR5SkillSystemEditModeTests
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
    public void Healing_Spring_Heals_When_Placed_Adjacent_To_Player()
    {
        StartRun(12345);
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var player = collectionModel.GetCard(playerModel.PlayerCardUid);
        player.CurrentHp = 5;

        var springUid = SpawnHelpCard(DefaultGameConfigFactory.HelpHealingSpringId);
        PlaceHelpCardOnBoard(springUid, new BoardSlotNo(2));

        Assert.That(player.CurrentHp, Is.EqualTo(7));
    }

    [Test]
    public void Healing_Spring_In_Item_Slot_Heals_After_Combat()
    {
        StartRun(12345);
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var player = collectionModel.GetCard(playerModel.PlayerCardUid);
        player.CurrentHp = 5;

        SpawnHelpCardToItemSlot(DefaultGameConfigFactory.HelpHealingSpringId);
        var monsterUid = MoveAdjacentMonsterWithoutFirstStrikeToSlot2();
        var monster = collectionModel.GetCard(monsterUid);
        monster.CurrentHp = 999;
        monster.CurrentArmor = 0;

        TableNine.Interface.SendCommand(new ClickBoardSlotCommand(new BoardSlotNo(2)));

        Assert.That(player.CurrentHp, Is.EqualTo(6));
    }

    [Test]
    public void Boulder_Removes_Non_Elite_Monster_At_Slot_6_When_Moved_To_Slot_3()
    {
        StartRun(12345);
        var boardModel = TableNine.Interface.GetModel<IBoardModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();

        var monsterUid = MoveAnyMonsterToSlot(new BoardSlotNo(6));
        collectionModel.GetCard(monsterUid).MonsterLevel = MonsterLevel.Level1;

        var boulderUid = SpawnHelpCard(DefaultGameConfigFactory.HelpBoulderId);
        PlaceHelpCardOnBoard(boulderUid, new BoardSlotNo(3));

        Assert.That(boardModel.GetCardAt(new BoardSlotNo(6)).HasValue, Is.False);
        Assert.That(collectionModel.TryGetCard(monsterUid, out _), Is.False);
        Assert.That(deckModel.HelpCardStates[boulderUid.Value].IsPermanentlyRemoved, Is.True);
    }

    [Test]
    public void Boulder_Does_Not_Remove_Elite_Monster_At_Slot_6()
    {
        StartRun(12345);
        var boardModel = TableNine.Interface.GetModel<IBoardModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();

        var monsterUid = MoveAnyMonsterToSlot(new BoardSlotNo(6));
        collectionModel.GetCard(monsterUid).MonsterLevel = MonsterLevel.Elite;

        var boulderUid = SpawnHelpCard(DefaultGameConfigFactory.HelpBoulderId);
        PlaceHelpCardOnBoard(boulderUid, new BoardSlotNo(3));

        Assert.That(boardModel.GetCardAt(new BoardSlotNo(6)).Value, Is.EqualTo(monsterUid));
        Assert.That(deckModel.HelpCardStates[boulderUid.Value].IsPermanentlyRemoved, Is.False);
    }

    [Test]
    public void Boulder_Click_Only_Consumes_Card()
    {
        StartRun(12345);
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var boulderUid = SpawnHelpCardToItemSlot(DefaultGameConfigFactory.HelpBoulderId);

        TableNine.Interface.SendCommand(new ClickItemSlotCommand(0));

        Assert.That(deckModel.HelpCardStates[boulderUid.Value].IsPermanentlyRemoved, Is.True);
    }

    [Test]
    public void Thorn_Armor_Still_Parallels_Player_First_Hit_Via_SkillSystem()
    {
        StartRun(42);
        var relicSystem = TableNine.Interface.GetSystem<IRelicSystem>();
        relicSystem.AddRelic(DefaultGameConfigFactory.RelicThornArmorId);
        var monsterUid = MoveAdjacentMonsterWithoutFirstStrikeToSlot2();

        CombatResolvedEvent? resolved = null;
        var unReg = TableNine.Interface.RegisterEvent<CombatResolvedEvent>(e => resolved = e);
        TableNine.Interface.SendCommand(new ClickBoardSlotCommand(new BoardSlotNo(2)));
        unReg.UnRegister();

        Assert.That(resolved.HasValue, Is.True);
        var firstGroup = resolved.Value.Context.FirstHitGroup;
        Assert.That(firstGroup.Exists(c => c.CauseId == CombatConstants.CauseThornArmor), Is.True);
    }

    [Test]
    public void Thorn_Skin_Still_Parallels_Monster_Counter_Via_SkillSystem()
    {
        StartRun(42);
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        playerModel.AddSkill(DefaultGameConfigFactory.SkillThornSkinId);
        var monsterUid = MoveAdjacentMonsterWithoutFirstStrikeToSlot2();
        var monster = TableNine.Interface.GetModel<ICollectionModel>().GetCard(monsterUid);
        monster.CurrentHp = 999;
        monster.BaseAttack = 3;

        CombatResolvedEvent? resolved = null;
        var unReg = TableNine.Interface.RegisterEvent<CombatResolvedEvent>(e => resolved = e);
        TableNine.Interface.SendCommand(new ClickBoardSlotCommand(new BoardSlotNo(2)));
        unReg.UnRegister();

        Assert.That(resolved.HasValue, Is.True);
        var counterGroup = resolved.Value.Context.CounterHitGroup;
        Assert.That(counterGroup.Exists(c => c.CauseId == CombatConstants.CauseThornSkin), Is.True);
    }

    [Test]
    public void Phoenix_Feather_Still_Prevents_Lethal_Death()
    {
        StartRun(42);
        var relicSystem = TableNine.Interface.GetSystem<IRelicSystem>();
        relicSystem.AddRelic(DefaultGameConfigFactory.RelicPhoenixFeatherId);
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var player = collectionModel.GetCard(playerModel.PlayerCardUid);
        player.CurrentHp = 1;
        player.CurrentArmor = 0;

        var monsterUid = MoveAdjacentMonsterWithFirstStrikeToSlot2();
        var monster = collectionModel.GetCard(monsterUid);
        monster.BaseAttack = 99;

        TableNine.Interface.SendCommand(new ClickBoardSlotCommand(new BoardSlotNo(2)));

        Assert.That(player.CurrentHp, Is.GreaterThan(0));
        Assert.That(relicSystem.HasRelic(DefaultGameConfigFactory.RelicPhoenixFeatherId), Is.False);
    }

    [Test]
    public void Wood_Set_Grants_Extra_Stats()
    {
        StartRun(1);
        var relicSystem = TableNine.Interface.GetSystem<IRelicSystem>();
        var statSystem = TableNine.Interface.GetSystem<IStatSystem>();
        var statsBefore = statSystem.GetEffectivePlayerStats();

        relicSystem.AddRelic(DefaultGameConfigFactory.RelicWoodShieldId);
        relicSystem.AddRelic(DefaultGameConfigFactory.RelicWoodSwordId);
        var statsWithTwoPieces = statSystem.GetEffectivePlayerStats();

        relicSystem.AddRelic(DefaultGameConfigFactory.RelicWoodArmorId);
        var statsWithFullSet = statSystem.GetEffectivePlayerStats();
        var configModel = TableNine.Interface.GetModel<IConfigModel>();
        var woodArmorHpBonus = configModel.GetRelicDefinition(DefaultGameConfigFactory.RelicWoodArmorId).StatMaxHpBonus;

        Assert.That(statsWithTwoPieces.Attack, Is.GreaterThan(statsBefore.Attack));
        Assert.That(statsWithFullSet.Attack - statsWithTwoPieces.Attack, Is.EqualTo(2));
        Assert.That(statsWithFullSet.Defense - statsWithTwoPieces.Defense, Is.EqualTo(2));
        Assert.That(statsWithFullSet.MaxHp - statsWithTwoPieces.MaxHp, Is.EqualTo(woodArmorHpBonus + 8));
    }

    [Test]
    public void Wood_Shield_Increases_Node_Start_Armor()
    {
        StartRun(1);
        var relicSystem = TableNine.Interface.GetSystem<IRelicSystem>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();

        relicSystem.AddRelic(DefaultGameConfigFactory.RelicWoodShieldId);
        TableNine.Interface.SendCommand(new StartNodeCommand(1, 2));

        var player = collectionModel.GetCard(playerModel.PlayerCardUid);
        var stats = TableNine.Interface.GetSystem<IStatSystem>().GetEffectivePlayerStats();
        Assert.That(player.CurrentArmor, Is.EqualTo(stats.Defense));
    }

    [Test]
    public void Hard_Skin_Reduces_Incoming_Damage()
    {
        StartRun(1);
        var statSystem = TableNine.Interface.GetSystem<IStatSystem>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var monsterUid = MoveAnyMonsterToSlot(new BoardSlotNo(2));
        var monster = collectionModel.GetCard(monsterUid);
        monster.SkillIds.Add(DefaultGameConfigFactory.SkillHardSkinId);

        var stats = statSystem.GetEffectiveMonsterStats(monsterUid);
        var combatSystem = TableNine.Interface.GetSystem<ICombatSystem>();
        var damage = combatSystem.CalculateDamage(new EffectiveStats { Attack = 3 }, stats);

        Assert.That(stats.DamageReduction, Is.EqualTo(1));
        Assert.That(damage, Is.EqualTo(2));
    }

    [Test]
    public void Living_Flesh_Heals_When_Using_Help_Card()
    {
        StartRun(12345);
        var relicSystem = TableNine.Interface.GetSystem<IRelicSystem>();
        relicSystem.AddRelic(DefaultGameConfigFactory.RelicLivingFleshId);
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var player = collectionModel.GetCard(playerModel.PlayerCardUid);
        player.CurrentHp = 5;

        var potionUid = SpawnHelpCardToItemSlot(DefaultGameConfigFactory.HelpPotionId);
        TableNine.Interface.SendCommand(new ClickItemSlotCommand(collectionModel.GetCard(potionUid).ItemSlotIndex.Value));

        Assert.That(player.CurrentHp, Is.GreaterThanOrEqualTo(6));
    }

    [Test]
    public void SkillSystem_Logs_Trigger_With_Owner_And_EffectGraph()
    {
        StartRun(42);
        var skillSystem = TableNine.Interface.GetSystem<ISkillSystem>();
        var relicSystem = TableNine.Interface.GetSystem<IRelicSystem>();
        relicSystem.AddRelic(DefaultGameConfigFactory.RelicThornArmorId);
        MoveAdjacentMonsterWithoutFirstStrikeToSlot2();

        TableNine.Interface.SendCommand(new ClickBoardSlotCommand(new BoardSlotNo(2)));

        Assert.That(skillSystem.RecentLogs.Any(log =>
            log.OwnerDefinitionId == DefaultGameConfigFactory.RelicThornArmorId &&
            log.Trigger == SkillTrigger.OnModifyDamage &&
            log.EffectGraphId == "eg_relic_thorn_armor"), Is.True);
    }

    [Test]
    public void SkillSystem_Blocks_Repeated_Same_Trigger_In_Same_Pass()
    {
        StartRun(12345);
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var player = collectionModel.GetCard(playerModel.PlayerCardUid);
        player.CurrentHp = 5;

        var context = new TriggerContext
        {
            OwnerUid = SpawnHelpCard(DefaultGameConfigFactory.HelpHealingSpringId),
            OwnerDefinitionId = DefaultGameConfigFactory.HelpHealingSpringId,
            CardSlot = new BoardSlotNo(2),
            PreviousSlot = null
        };

        TableNine.Interface.SendCommand(new DoubleSkillTriggerTestCommand(context));

        Assert.That(player.CurrentHp, Is.EqualTo(7));
    }

    private sealed class DoubleSkillTriggerTestCommand : AbstractCommand
    {
        public DoubleSkillTriggerTestCommand(TriggerContext context)
        {
            Context = context;
        }

        public TriggerContext Context { get; }

        protected override void OnExecute()
        {
            var skillSystem = this.GetSystem<ISkillSystem>();
            skillSystem.Trigger(SkillTrigger.OnCardMoved, Context, this);
            skillSystem.Trigger(SkillTrigger.OnCardMoved, Context, this);
        }
    }

    private static void StartRun(int seed)
    {
        TableNine.InitArchitecture();
        TableNine.Interface.SendCommand(new StartNewRunCommand(seedOverride: seed));
    }

    private static void PlaceHelpCardOnBoard(CardUid uid, BoardSlotNo slot)
    {
        var boardSystem = TableNine.Interface.GetSystem<IBoardSystem>();
        var boardModel = TableNine.Interface.GetModel<IBoardModel>();
        if (boardModel.GetCardAt(slot).HasValue)
        {
            boardSystem.RemoveCardAt(slot);
        }

        boardSystem.PlaceCard(uid, slot, CardPlacementSource.Refill);
    }

    private static CardUid SpawnHelpCard(string definitionId)
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
        return runtime.Uid;
    }

    private static CardUid SpawnHelpCardToItemSlot(string definitionId)
    {
        var uid = SpawnHelpCard(definitionId);
        TableNine.Interface.SendCommand(new PickHelpCardToItemSlotCommand(uid));
        return uid;
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

            if (collectionModel.GetCard(uid.Value).CardType == CardType.Monster)
            {
                boardSystem.RemoveCardAt(fromSlot);
                boardSystem.PlaceCard(uid.Value, slot, CardPlacementSource.Refill);
                return uid.Value;
            }
        }

        Assert.Fail("Could not find a monster to move.");
        return default;
    }

    private static CardUid MoveAdjacentMonsterWithoutFirstStrikeToSlot2()
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
            if (runtime.CardType == CardType.Monster && !runtime.HasSkill(DefaultGameConfigFactory.SkillFirstStrikeId))
            {
                boardSystem.RemoveCardAt(slot);
                boardSystem.PlaceCard(uid.Value, new BoardSlotNo(2), CardPlacementSource.Refill);
                return uid.Value;
            }
        }

        Assert.Fail("Could not find a non-first-strike monster.");
        return default;
    }

    private static CardUid MoveAdjacentMonsterWithFirstStrikeToSlot2()
    {
        var monsterUid = MoveAdjacentMonsterWithoutFirstStrikeToSlot2();
        var monster = TableNine.Interface.GetModel<ICollectionModel>().GetCard(monsterUid);
        if (!monster.HasSkill(DefaultGameConfigFactory.SkillFirstStrikeId))
        {
            monster.SkillIds.Add(DefaultGameConfigFactory.SkillFirstStrikeId);
        }

        return monsterUid;
    }
}
