using System.Collections.Generic;

public static partial class DefaultGameConfigFactory
{
    public const string MonsterSpade3Id = "monster_spade_3";
    public const string MonsterHeart3Id = "monster_heart_3";
    public const string MonsterDiamond3Id = "monster_diamond_3";
    public const string MonsterClub3Id = "monster_club_3";

    public const string MonsterSpade4Id = "monster_spade_4";
    public const string MonsterHeart4Id = "monster_heart_4";
    public const string MonsterDiamond4Id = "monster_diamond_4";
    public const string MonsterClub4Id = "monster_club_4";

    public const string MonsterSpade5Id = "monster_spade_5";
    public const string MonsterHeart5Id = "monster_heart_5";
    public const string MonsterDiamond5Id = "monster_diamond_5";
    public const string MonsterClub5Id = "monster_club_5";

    public const string MonsterSpadeEliteId = "monster_spade_a";
    public const string MonsterSpadeBossId = "monster_spade_j";
    public const string MonsterHeartEliteId = "monster_heart_a";
    public const string MonsterHeartBossId = "monster_heart_j";
    public const string MonsterDiamondEliteId = "monster_diamond_a";
    public const string MonsterDiamondBossId = "monster_diamond_j";

    public const string HelpBlueChestId = "help_blue_chest";
    public const string HelpGoldChestId = "help_gold_chest";

    public const string SkillThornSkinId = "skill_thorn_skin";
    public const string SkillHardSkinId = "skill_hard_skin";
    public const string SkillBattleHardenedId = "skill_battle_hardened";
    public const string SkillArmoryId = "skill_armory";
    public const string SkillEvenHateId = "skill_even_hate";
    public const string SkillTowerChildId = "skill_tower_child";

    public static readonly string[] TutorSkillPoolIds =
    {
        SkillThornSkinId,
        SkillHardSkinId,
        SkillBattleHardenedId,
        SkillArmoryId,
        SkillEvenHateId,
        SkillTowerChildId,
        SkillFirstStrikeId
    };

    public static readonly string[] Layer1Level1MonsterIds =
    {
        MonsterColorlessId,
        MonsterSpade2Id,
        MonsterHeart2Id,
        MonsterDiamond2Id,
        MonsterClub2Id
    };

    public static readonly string[] Layer1Level2MonsterIds =
    {
        MonsterSpade3Id,
        MonsterHeart3Id,
        MonsterDiamond3Id,
        MonsterClub3Id
    };

    public static readonly string[] Layer1Level3MonsterIds =
    {
        MonsterSpade4Id,
        MonsterHeart4Id,
        MonsterDiamond4Id,
        MonsterClub4Id
    };

    public static readonly string[] Layer1Level4MonsterIds =
    {
        MonsterSpade5Id,
        MonsterHeart5Id,
        MonsterDiamond5Id,
        MonsterClub5Id
    };

    public static void AddLayer1MonstersAndSkills(GameConfigSet config)
    {
        config.Skills.Add(new SkillDefinition { SkillId = SkillThornSkinId, DisplayName = "刺皮" });
        config.Skills.Add(new SkillDefinition { SkillId = SkillHardSkinId, DisplayName = "硬皮" });
        config.Skills.Add(new SkillDefinition { SkillId = SkillBattleHardenedId, DisplayName = "历战" });
        config.Skills.Add(new SkillDefinition { SkillId = SkillArmoryId, DisplayName = "军械库" });
        config.Skills.Add(new SkillDefinition { SkillId = SkillEvenHateId, DisplayName = "偶数仇恨" });
        config.Skills.Add(new SkillDefinition { SkillId = SkillTowerChildId, DisplayName = "塔之子" });

        config.Cards.Add(new CardDefinition
        {
            CardId = HelpBlueChestId,
            DisplayName = "蓝色宝箱卡",
            CardType = CardType.Help,
            Quality = CardQuality.Gold,
            Price = 150,
            RestoreAfterNode = false
        });
        config.Cards.Add(new CardDefinition
        {
            CardId = HelpGoldChestId,
            DisplayName = "金色宝箱卡",
            CardType = CardType.Help,
            Quality = CardQuality.Red,
            Price = 400,
            RestoreAfterNode = false
        });

        AddMonster(config, MonsterSpade3Id, "黑桃3", MonsterLevel.Level2, Suit.Spade, 3, 9, 3, 1);
        AddMonster(config, MonsterHeart3Id, "红桃3", MonsterLevel.Level2, Suit.Heart, 3, 12, 3, 1);
        AddMonster(config, MonsterDiamond3Id, "方块3", MonsterLevel.Level2, Suit.Diamond, 3, 9, 1, 3);
        AddMonster(config, MonsterClub3Id, "梅花3", MonsterLevel.Level2, Suit.Club, 3, 9, 3, 1);

        AddMonster(config, MonsterSpade4Id, "黑桃4", MonsterLevel.Level3, Suit.Spade, 4, 16, 4, 2);
        AddMonster(config, MonsterHeart4Id, "红桃4", MonsterLevel.Level3, Suit.Heart, 4, 20, 4, 2);
        AddMonster(config, MonsterDiamond4Id, "方块4", MonsterLevel.Level3, Suit.Diamond, 4, 16, 2, 4);
        AddMonster(config, MonsterClub4Id, "梅花4", MonsterLevel.Level3, Suit.Club, 4, 16, 4, 2);

        AddMonster(config, MonsterSpade5Id, "黑桃5", MonsterLevel.Level4, Suit.Spade, 5, 25, 5, 3);
        AddMonster(config, MonsterHeart5Id, "红桃5", MonsterLevel.Level4, Suit.Heart, 5, 30, 5, 3);
        AddMonster(config, MonsterDiamond5Id, "方块5", MonsterLevel.Level4, Suit.Diamond, 5, 25, 3, 5);
        AddMonster(config, MonsterClub5Id, "梅花5", MonsterLevel.Level4, Suit.Club, 5, 25, 5, 3);

        config.Cards.Add(new CardDefinition
        {
            CardId = MonsterSpadeEliteId,
            DisplayName = "黑桃A",
            CardType = CardType.Monster,
            MonsterLevel = MonsterLevel.Elite,
            Suit = Suit.Spade,
            Rank = 1,
            BaseHp = 44,
            BaseAttack = 1,
            BaseDefense = 1
        });
        config.Cards.Add(new CardDefinition
        {
            CardId = MonsterSpadeBossId,
            DisplayName = "黑桃J",
            CardType = CardType.Monster,
            MonsterLevel = MonsterLevel.Boss,
            Suit = Suit.Spade,
            Rank = 11,
            BaseHp = 121,
            BaseAttack = 11,
            BaseDefense = 0
        });
    }

    private static void AddMonster(
        GameConfigSet config,
        string cardId,
        string displayName,
        MonsterLevel level,
        Suit suit,
        int rank,
        int hp,
        int attack,
        int defense)
    {
        config.Cards.Add(new CardDefinition
        {
            CardId = cardId,
            DisplayName = displayName,
            CardType = CardType.Monster,
            MonsterLevel = level,
            Suit = suit,
            Rank = rank,
            BaseHp = hp,
            BaseAttack = attack,
            BaseDefense = defense
        });
    }
}
