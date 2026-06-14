using NUnit.Framework;
using QFramework;

[Category(TableNineTestCategories.RegressionTests)]
public sealed class TableNineStage2HelpCardDataEditModeTests
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
    public void Config_Level3_And_Level4_Monsters_Use_Rebalanced_Stats()
    {
        TableNine.InitArchitecture();
        var configModel = TableNine.Interface.GetModel<IConfigModel>();

        AssertMonsterStats(configModel, GameConfigIds.MonsterSpade4Id, 12, 6, 2);
        AssertMonsterStats(configModel, GameConfigIds.MonsterHeart4Id, 16, 4, 0);
        AssertMonsterStats(configModel, GameConfigIds.MonsterDiamond4Id, 8, 4, 8);
        AssertMonsterStats(configModel, GameConfigIds.MonsterClub4Id, 12, 4, 4);

        AssertMonsterStats(configModel, GameConfigIds.MonsterSpade5Id, 20, 7, 3);
        AssertMonsterStats(configModel, GameConfigIds.MonsterHeart5Id, 30, 0, 0);
        AssertMonsterStats(configModel, GameConfigIds.MonsterDiamond5Id, 12, 5, 13);
        AssertMonsterStats(configModel, GameConfigIds.MonsterClub5Id, 20, 5, 5);
    }

    [Test]
    public void Config_Colorless_Is_In_Level1_Pool_With_Lower_Difficulty_Stats()
    {
        TableNine.InitArchitecture();
        var configModel = TableNine.Interface.GetModel<IConfigModel>();

        CollectionAssert.Contains(GameConfigIds.Layer1Level1MonsterIds, GameConfigIds.MonsterColorlessId);
        AssertMonsterStats(configModel, GameConfigIds.MonsterColorlessId, 6, 2, 0);
    }

    [Test]
    public void Config_Help_Cards_Match_Stage2_Data()
    {
        TableNine.InitArchitecture();
        var configModel = TableNine.Interface.GetModel<IConfigModel>();

        AssertHelpCard(configModel, GameConfigIds.HelpAttributeUpId, "属性提升卡", CardQuality.Gold, 100);
        AssertHelpCard(configModel, GameConfigIds.HelpGoldCardId, "金币卡", CardQuality.Blue, 30);
        AssertHelpCard(configModel, GameConfigIds.HelpDurableShieldId, "耐用盾牌", CardQuality.White, 50);
        AssertHelpCard(configModel, GameConfigIds.HelpBearTrapId, "捕熊陷阱", CardQuality.White, 50);
        AssertHelpCard(configModel, GameConfigIds.HelpTeleportId, "传送卡", CardQuality.White, 30);
        AssertHelpCard(configModel, GameConfigIds.HelpBloodConvertId, "血液转换", CardQuality.White, 50);
        AssertHelpCard(configModel, GameConfigIds.HelpShieldStrikeTutorialId, "盾击教程", CardQuality.Blue, 80);
        AssertHelpCard(configModel, GameConfigIds.HelpKidnapId, "绑票", CardQuality.Blue, 100);

        Assert.That(
            configModel.TryGetEffectGraph(configModel.GetCardDefinition(GameConfigIds.HelpDurableShieldId).EffectGraphId, out _),
            Is.True);
        Assert.That(
            configModel.TryGetEffectGraph(configModel.GetCardDefinition(GameConfigIds.HelpShieldStrikeTutorialId).EffectGraphId, out _),
            Is.True);
        Assert.That(
            configModel.TryGetEffectGraph(configModel.GetCardDefinition(GameConfigIds.HelpTeleportId).EffectGraphId, out _),
            Is.True);
        Assert.That(
            configModel.TryGetEffectGraph(configModel.GetCardDefinition(GameConfigIds.HelpKidnapId).EffectGraphId, out _),
            Is.True);
        Assert.That(
            configModel.TryGetEffectGraph(configModel.GetCardDefinition(GameConfigIds.HelpBearTrapId).EffectGraphId, out _),
            Is.True);
        Assert.That(
            configModel.TryGetEffectGraph(configModel.GetCardDefinition(GameConfigIds.HelpBloodConvertId).EffectGraphId, out _),
            Is.True);
    }

    [Test]
    public void Bear_Trap_Damages_Refilled_Adjacent_Monster_And_Consumes()
    {
        StartRun(42);
        var boardSystem = TableNine.Interface.GetSystem<IBoardSystem>();
        var boardModel = TableNine.Interface.GetModel<IBoardModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var trapUid = SpawnHelpCardToItemSlot(GameConfigIds.HelpBearTrapId);

        var adjacentSlot = new BoardSlotNo(2);
        if (boardModel.GetCardAt(adjacentSlot).HasValue)
        {
            boardSystem.RemoveCardAt(adjacentSlot);
        }

        var monsterUid = SpawnMonster(GameConfigIds.MonsterHeart2Id);
        var monster = collectionModel.GetCard(monsterUid);
        monster.CurrentHp = 20;
        monster.CurrentArmor = 0;

        boardSystem.PlaceCard(monsterUid, adjacentSlot, CardPlacementSource.Refill);

        Assert.That(monster.CurrentHp, Is.EqualTo(10));
        Assert.That(deckModel.HelpCardStates[trapUid.Value].IsPermanentlyRemoved, Is.True);
    }

    [Test]
    public void Blood_Convert_Reduces_MaxHp_And_Consumes()
    {
        StartRun(4242);
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var player = collectionModel.GetCard(playerModel.PlayerCardUid);
        var maxHpBefore = player.MaxHp;
        var attackBefore = player.BaseAttack;

        var uid = SpawnHelpCardToItemSlot(GameConfigIds.HelpBloodConvertId);
        UseItemSlotHelpCard(uid);

        Assert.That(player.MaxHp, Is.EqualTo(maxHpBefore - 5));
        Assert.That(deckModel.HelpCardStates[uid.Value].IsPermanentlyRemoved, Is.True);
        Assert.That(
            player.BaseAttack > attackBefore ||
            player.BaseArmor > 1 ||
            playerModel.Gold.Value >= 50 ||
            playerModel.Relics.Count > 0,
            Is.True);
    }

    [Test]
    public void Durable_Shield_Grants_Five_Armor()
    {
        StartRun(42);
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var player = collectionModel.GetCard(playerModel.PlayerCardUid);
        player.CurrentArmor = 1;

        var uid = SpawnHelpCardToItemSlot(GameConfigIds.HelpDurableShieldId);
        UseItemSlotHelpCard(uid);

        Assert.That(player.CurrentArmor, Is.EqualTo(6));
    }

    [Test]
    public void Shield_Strike_Tutorial_Deals_Damage_Equal_To_Player_Armor()
    {
        StartRun(42);
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var targetUid = MoveAnyMonsterToSlot(new BoardSlotNo(3));
        var target = collectionModel.GetCard(targetUid);
        target.CurrentHp = 20;
        target.CurrentArmor = 0;
        target.BaseArmor = 0;

        TableNine.Interface.SendCommand(new ChangeArmorCommand(playerModel.PlayerCardUid, 3, "test_setup"));
        Assert.That(collectionModel.GetCard(playerModel.PlayerCardUid).CurrentArmor, Is.EqualTo(4));

        var uid = SpawnHelpCardToItemSlot(GameConfigIds.HelpShieldStrikeTutorialId);
        UseItemSlotHelpCard(uid);

        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        Assert.That(deckModel.PendingHelpCardAction.TargetingMode, Is.EqualTo("player_current_armor"));
        Assert.That(collectionModel.GetCard(playerModel.PlayerCardUid).CurrentArmor, Is.EqualTo(4));

        TableNine.Interface.SendCommand(new ClickBoardSlotCommand(new BoardSlotNo(3)));

        Assert.That(target.CurrentHp, Is.EqualTo(16));
    }

    [Test]
    public void Kidnap_Removes_Monster_And_Grants_Player_Monster_Armor()
    {
        StartRun(42);
        var boardModel = TableNine.Interface.GetModel<IBoardModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var player = collectionModel.GetCard(playerModel.PlayerCardUid);
        player.CurrentArmor = 0;

        var targetUid = MoveAnyMonsterToSlot(new BoardSlotNo(3));
        var target = collectionModel.GetCard(targetUid);
        target.CurrentArmor = 7;
        target.BaseArmor = 0;

        var uid = SpawnHelpCardToItemSlot(GameConfigIds.HelpKidnapId);
        UseItemSlotHelpCard(uid);

        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        Assert.That(deckModel.PendingHelpCardAction.TargetingMode, Is.EqualTo("kidnap"));
        Assert.That(boardModel.GetCardAt(new BoardSlotNo(3)).Value, Is.EqualTo(targetUid));

        TableNine.Interface.SendCommand(new ClickBoardSlotCommand(new BoardSlotNo(3)));

        Assert.That(collectionModel.TryGetCard(targetUid, out _), Is.False);
        var slotUid = boardModel.GetCardAt(new BoardSlotNo(3));
        Assert.That(!slotUid.HasValue || !slotUid.Value.Equals(targetUid), Is.True, "目标怪物不应仍留在格3");
        Assert.That(player.CurrentArmor, Is.EqualTo(7));
    }

    [Test]
    public void Teleport_Shuffles_Target_Back_Into_Battle_Deck()
    {
        StartRun(42);
        var boardModel = TableNine.Interface.GetModel<IBoardModel>();
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var targetUid = MoveAnyMonsterToSlot(new BoardSlotNo(3));
        var pileCountBefore = deckModel.BattleDrawPile.Count;

        var uid = SpawnHelpCardToItemSlot(GameConfigIds.HelpTeleportId);
        UseItemSlotHelpCard(uid);
        TableNine.Interface.SendCommand(new ClickBoardSlotCommand(new BoardSlotNo(3)));

        Assert.That(boardModel.GetCardAt(new BoardSlotNo(3)).HasValue, Is.False);
        Assert.That(deckModel.BattleDrawPile.Count, Is.EqualTo(pileCountBefore + 1));
        Assert.That(deckModel.BattleDrawPile, Does.Contain(targetUid));
    }

    private static void AssertMonsterStats(
        IConfigModel configModel,
        string cardId,
        int hp,
        int attack,
        int defense)
    {
        var definition = configModel.GetCardDefinition(cardId);
        Assert.That(definition.BaseHp, Is.EqualTo(hp), cardId);
        Assert.That(definition.BaseAttack, Is.EqualTo(attack), cardId);
        Assert.That(definition.BaseArmor, Is.EqualTo(defense), cardId);
    }

    private static void AssertHelpCard(
        IConfigModel configModel,
        string cardId,
        string displayName,
        CardQuality quality,
        int price)
    {
        var definition = configModel.GetCardDefinition(cardId);
        Assert.That(definition.DisplayName, Is.EqualTo(displayName), cardId);
        Assert.That(definition.Quality, Is.EqualTo(quality), cardId);
        Assert.That(definition.Price, Is.EqualTo(price), cardId);
        Assert.That(definition.RestoreAfterNode, Is.False, cardId);
    }

    private static CardUid SpawnMonster(string definitionId)
    {
        var configModel = TableNine.Interface.GetModel<IConfigModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        return collectionModel.CreateCard(configModel.GetCardDefinition(definitionId)).Uid;
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
        if (boardModel.GetCardAt(slot).HasValue)
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
}
