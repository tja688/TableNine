using NUnit.Framework;
using QFramework;

[Category(TableNineTestCategories.RegressionTests)]
public sealed class TableNineStage1MonsterSkillEditModeTests
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
    public void Config_Level1_Monsters_Use_Rebalanced_Stats()
    {
        TableNine.InitArchitecture();
        var configModel = TableNine.Interface.GetModel<IConfigModel>();

        AssertMonsterStats(configModel, GameConfigIds.MonsterSpade2Id, 2, 3, 1);
        AssertMonsterStats(configModel, GameConfigIds.MonsterHeart2Id, 4, 2, 0);
        AssertMonsterStats(configModel, GameConfigIds.MonsterDiamond2Id, 1, 2, 3);
        AssertMonsterStats(configModel, GameConfigIds.MonsterClub2Id, 2, 2, 2);
    }

    [Test]
    public void Config_Level2_Monsters_Use_Rebalanced_Stats_And_Ambush_On_Club3()
    {
        TableNine.InitArchitecture();
        var configModel = TableNine.Interface.GetModel<IConfigModel>();

        AssertMonsterStats(configModel, GameConfigIds.MonsterSpade3Id, 6, 4, 2);
        AssertMonsterStats(configModel, GameConfigIds.MonsterHeart3Id, 9, 3, 0);
        AssertMonsterStats(configModel, GameConfigIds.MonsterDiamond3Id, 3, 0, 9);
        AssertMonsterStats(configModel, GameConfigIds.MonsterClub3Id, 6, 3, 3);

        var club3 = configModel.GetCardDefinition(GameConfigIds.MonsterClub3Id);
        Assert.That(club3.SkillIds, Contains.Item(GameConfigIds.SkillAmbushId));
        Assert.That(club3.SkillIds, Does.Not.Contain(GameConfigIds.SkillCallFriendsId));
    }

    [Test]
    public void Config_Heart5_Is_Pure_Summoner_Profile()
    {
        TableNine.InitArchitecture();
        var configModel = TableNine.Interface.GetModel<IConfigModel>();
        AssertMonsterStats(configModel, GameConfigIds.MonsterHeart5Id, 30, 0, 0);
    }

    [Test]
    public void SpadeCub_At_Slot6_Grants_Attack_Bonus_And_FirstStrike()
    {
        StartRun(42);
        var monster = SpawnMonster(GameConfigIds.MonsterSpade2Id);
        PlaceMonster(monster, new BoardSlotNo(6));

        var stats = TableNine.Interface.GetSystem<IStatSystem>().GetEffectiveMonsterStats(monster);
        Assert.That(stats.Attack, Is.EqualTo(5));
        Assert.That(stats.HasFirstStrike, Is.True);
    }

    [Test]
    public void ClubCub_In_TopRow_Buffs_Other_Monster_Attack()
    {
        StartRun(42);
        var clubCub = SpawnMonster(GameConfigIds.MonsterClub2Id);
        var spadeCub = SpawnMonster(GameConfigIds.MonsterSpade2Id);
        PlaceMonster(clubCub, new BoardSlotNo(1));
        PlaceMonster(spadeCub, new BoardSlotNo(2));

        var stats = TableNine.Interface.GetSystem<IStatSystem>().GetEffectiveMonsterStats(spadeCub);
        Assert.That(stats.Attack, Is.EqualTo(5));
    }

    [Test]
    public void Ambush_Deals_Three_Damage_When_Monster_Placed_On_Ambush_Slot()
    {
        StartRun(42);
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var player = collectionModel.GetCard(playerModel.PlayerCardUid);
        var hpBefore = player.CurrentHp;

        var ambushMonster = SpawnMonster(GameConfigIds.MonsterClub3Id);
        PlaceMonster(ambushMonster, new BoardSlotNo(4));

        Assert.That(player.CurrentHp, Is.EqualTo(hpBefore - 3));
    }

    [Test]
    public void Revenge_Gains_Two_Attack_When_Another_Monster_Is_Killed()
    {
        StartRun(42);
        var revengeMonster = SpawnMonster(GameConfigIds.MonsterSpade4Id);
        var victim = SpawnMonster(GameConfigIds.MonsterHeart2Id);
        PlaceMonster(revengeMonster, new BoardSlotNo(2));

        var attackBefore = TableNine.Interface.GetModel<ICollectionModel>().GetCard(revengeMonster).BaseAttack;
        TableNine.Interface.SendCommand(new KillMonsterCommand(victim));

        var attackAfter = TableNine.Interface.GetModel<ICollectionModel>().GetCard(revengeMonster).BaseAttack;
        Assert.That(attackAfter, Is.EqualTo(attackBefore + 2));
    }

    [Test]
    public void HeartCub_Gains_MaxHp_When_Rotated_Into_Slot8()
    {
        StartRun(42);
        ClearBoardExceptPlayer();
        var boardSystem = TableNine.Interface.GetSystem<IBoardSystem>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var monster = SpawnMonster(GameConfigIds.MonsterHeart2Id);
        PlaceMonster(monster, new BoardSlotNo(9));
        var maxHpBefore = collectionModel.GetCard(monster).MaxHp;

        boardSystem.RotateClockwise();

        Assert.That(collectionModel.GetCard(monster).BoardSlot.Value.Value, Is.EqualTo(8));
        Assert.That(collectionModel.GetCard(monster).MaxHp, Is.EqualTo(maxHpBefore + 2));
    }

    [Test]
    public void DiamondCub_Gains_Defense_When_Rotated_Into_Slot4()
    {
        StartRun(42);
        ClearBoardExceptPlayer();
        var boardSystem = TableNine.Interface.GetSystem<IBoardSystem>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var monster = SpawnMonster(GameConfigIds.MonsterDiamond2Id);
        PlaceMonster(monster, new BoardSlotNo(7));
        var defenseBefore = collectionModel.GetCard(monster).BaseArmor;

        boardSystem.RotateClockwise();

        Assert.That(collectionModel.GetCard(monster).BoardSlot.Value.Value, Is.EqualTo(4));
        Assert.That(collectionModel.GetCard(monster).BaseArmor, Is.EqualTo(defenseBefore + 2));
    }

    [Test]
    public void ArmorBreaker_Reduces_Player_Armor_When_Moved_To_Top_Row()
    {
        StartRun(42);
        ClearBoardExceptPlayer();
        var boardSystem = TableNine.Interface.GetSystem<IBoardSystem>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var playerModel = TableNine.Interface.GetModel<IPlayerModel>();
        var player = collectionModel.GetCard(playerModel.PlayerCardUid);
        var armorBefore = player.CurrentArmor;

        var monster = SpawnMonster(GameConfigIds.MonsterSpade3Id);
        PlaceMonster(monster, new BoardSlotNo(4));
        boardSystem.RotateClockwise();

        Assert.That(player.CurrentArmor, Is.EqualTo(System.Math.Max(0, armorBefore - 2)));
    }

    [Test]
    public void Medic_Heals_All_Monsters_When_Moved_To_Bottom_Row()
    {
        StartRun(42);
        ClearBoardExceptPlayer();
        var boardSystem = TableNine.Interface.GetSystem<IBoardSystem>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var medic = SpawnMonster(GameConfigIds.MonsterHeart4Id);
        PlaceMonster(medic, new BoardSlotNo(8));
        collectionModel.GetCard(medic).CurrentHp = 5;

        boardSystem.RotateClockwise();

        Assert.That(collectionModel.GetCard(medic).BoardSlot.Value.Value, Is.EqualTo(7));
        Assert.That(collectionModel.GetCard(medic).CurrentHp, Is.EqualTo(9));
    }

    private static void ClearBoardExceptPlayer()
    {
        var boardModel = TableNine.Interface.GetModel<IBoardModel>();
        var boardSystem = TableNine.Interface.GetSystem<IBoardSystem>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();

        for (var i = 1; i <= 9; i++)
        {
            var slot = new BoardSlotNo(i);
            if (slot.Value == boardModel.PlayerSlot.Value)
            {
                continue;
            }

            var uid = boardModel.GetCardAt(slot);
            if (!uid.HasValue)
            {
                continue;
            }

            boardSystem.RemoveCardAt(slot);
            if (collectionModel.TryGetCard(uid.Value, out var runtime) && runtime.CardType == CardType.Monster)
            {
                collectionModel.RemoveCard(uid.Value);
            }
        }
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

    private static void StartRun(int seed)
    {
        TableNine.InitArchitecture();
        TableNine.Interface.SendCommand(new StartNewRunCommand(seedOverride: seed));
    }

    private static CardUid SpawnMonster(string definitionId)
    {
        var configModel = TableNine.Interface.GetModel<IConfigModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        return collectionModel.CreateCard(configModel.GetCardDefinition(definitionId)).Uid;
    }

    private static void PlaceMonster(CardUid uid, BoardSlotNo slot)
    {
        var boardSystem = TableNine.Interface.GetSystem<IBoardSystem>();
        var boardModel = TableNine.Interface.GetModel<IBoardModel>();
        if (boardModel.GetCardAt(slot).HasValue)
        {
            boardSystem.RemoveCardAt(slot);
        }

        boardSystem.PlaceCard(uid, slot, CardPlacementSource.Refill);
    }
}
