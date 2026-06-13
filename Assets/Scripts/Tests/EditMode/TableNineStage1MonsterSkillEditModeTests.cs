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

        AssertMonsterStats(configModel, DefaultGameConfigFactory.MonsterSpade2Id, 2, 3, 1);
        AssertMonsterStats(configModel, DefaultGameConfigFactory.MonsterHeart2Id, 4, 2, 0);
        AssertMonsterStats(configModel, DefaultGameConfigFactory.MonsterDiamond2Id, 1, 2, 3);
        AssertMonsterStats(configModel, DefaultGameConfigFactory.MonsterClub2Id, 2, 2, 2);
    }

    [Test]
    public void Config_Level2_Monsters_Use_Rebalanced_Stats_And_Ambush_On_Club3()
    {
        TableNine.InitArchitecture();
        var configModel = TableNine.Interface.GetModel<IConfigModel>();

        AssertMonsterStats(configModel, DefaultGameConfigFactory.MonsterSpade3Id, 6, 4, 2);
        AssertMonsterStats(configModel, DefaultGameConfigFactory.MonsterHeart3Id, 9, 3, 0);
        AssertMonsterStats(configModel, DefaultGameConfigFactory.MonsterDiamond3Id, 3, 0, 9);
        AssertMonsterStats(configModel, DefaultGameConfigFactory.MonsterClub3Id, 6, 3, 3);

        var club3 = configModel.GetCardDefinition(DefaultGameConfigFactory.MonsterClub3Id);
        Assert.That(club3.SkillIds, Contains.Item(DefaultGameConfigFactory.SkillAmbushId));
        Assert.That(club3.SkillIds, Does.Not.Contain(DefaultGameConfigFactory.SkillCallFriendsId));
    }

    [Test]
    public void Config_Heart5_Is_Pure_Summoner_Profile()
    {
        TableNine.InitArchitecture();
        var configModel = TableNine.Interface.GetModel<IConfigModel>();
        AssertMonsterStats(configModel, DefaultGameConfigFactory.MonsterHeart5Id, 30, 0, 0);
    }

    [Test]
    public void SpadeCub_At_Slot6_Grants_Attack_Bonus_And_FirstStrike()
    {
        StartRun(42);
        var monster = SpawnMonster(DefaultGameConfigFactory.MonsterSpade2Id);
        PlaceMonster(monster, new BoardSlotNo(6));

        var stats = TableNine.Interface.GetSystem<IStatSystem>().GetEffectiveMonsterStats(monster);
        Assert.That(stats.Attack, Is.EqualTo(5));
        Assert.That(stats.HasFirstStrike, Is.True);
    }

    [Test]
    public void ClubCub_In_TopRow_Buffs_Other_Monster_Attack()
    {
        StartRun(42);
        var clubCub = SpawnMonster(DefaultGameConfigFactory.MonsterClub2Id);
        var spadeCub = SpawnMonster(DefaultGameConfigFactory.MonsterSpade2Id);
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

        var ambushMonster = SpawnMonster(DefaultGameConfigFactory.MonsterClub3Id);
        PlaceMonster(ambushMonster, new BoardSlotNo(4));

        Assert.That(player.CurrentHp, Is.EqualTo(hpBefore - 3));
    }

    [Test]
    public void Revenge_Gains_Two_Attack_When_Another_Monster_Is_Killed()
    {
        StartRun(42);
        var revengeMonster = SpawnMonster(DefaultGameConfigFactory.MonsterSpade4Id);
        var victim = SpawnMonster(DefaultGameConfigFactory.MonsterHeart2Id);
        PlaceMonster(revengeMonster, new BoardSlotNo(2));

        var attackBefore = TableNine.Interface.GetModel<ICollectionModel>().GetCard(revengeMonster).BaseAttack;
        TableNine.Interface.SendCommand(new KillMonsterCommand(victim));

        var attackAfter = TableNine.Interface.GetModel<ICollectionModel>().GetCard(revengeMonster).BaseAttack;
        Assert.That(attackAfter, Is.EqualTo(attackBefore + 2));
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
        Assert.That(definition.BaseDefense, Is.EqualTo(defense), cardId);
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
