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
        config.Skills.Add(new SkillDefinition
        {
            SkillId = SkillThornSkinId,
            DisplayName = "刺皮",
            Description = "被攻击时反弹攻击者攻击力",
            HasRuntimeBinding = true,
            Trigger = SkillTrigger.OnModifyDamage,
            ConditionKey = "player_defender_monster_attacks",
            EffectGraphId = "eg_skill_thorn_skin_reflect"
        });
        config.Skills.Add(new SkillDefinition
        {
            SkillId = SkillHardSkinId,
            DisplayName = "硬皮",
            Description = "+10生命上限，清空关卡回血",
            HasRuntimeBinding = true,
            Trigger = SkillTrigger.OnNodeClear,
            ConditionKey = "always",
            EffectGraphId = "eg_skill_hard_skin_node_clear_heal",
            MaxHpOnAcquire = 10
        });
        config.Skills.Add(new SkillDefinition { SkillId = SkillBattleHardenedId, DisplayName = "历战", Description = "与敌战斗攻击+1，换敌复原" });
        config.Skills.Add(new SkillDefinition { SkillId = SkillArmoryId, DisplayName = "军械库", Description = "关卡结束选飞刀爆弹破击锤" });
        config.Skills.Add(new SkillDefinition { SkillId = SkillEvenHateId, DisplayName = "偶数仇恨", Description = "对战偶数怪造成双倍伤害" });
        config.Skills.Add(new SkillDefinition { SkillId = SkillTowerChildId, DisplayName = "塔之子", Description = "关卡开始放入倍增塔" });

        config.Cards.Add(new CardDefinition
        {
            CardId = HelpBlueChestId,
            DisplayName = "蓝色宝箱卡",
            Description = "开启蓝色宝箱奖励",
            CardType = CardType.Help,
            Quality = CardQuality.Gold,
            Price = 150,
            RestoreAfterNode = false
        });
        config.Cards.Add(new CardDefinition
        {
            CardId = HelpGoldChestId,
            DisplayName = "金色宝箱卡",
            Description = "开启金色宝箱奖励",
            CardType = CardType.Help,
            Quality = CardQuality.Red,
            Price = 400,
            RestoreAfterNode = false
        });

        AddMonster(config, MonsterSpade3Id, "黑桃3", MonsterLevel.Level2, Suit.Spade, 3, 6, 4, 2);
        AddMonster(config, MonsterHeart3Id, "红桃3", MonsterLevel.Level2, Suit.Heart, 3, 9, 3, 0);
        AddMonster(config, MonsterDiamond3Id, "方块3", MonsterLevel.Level2, Suit.Diamond, 3, 3, 0, 9);
        AddMonster(config, MonsterClub3Id, "梅花3", MonsterLevel.Level2, Suit.Club, 3, 6, 3, 3);

        AddMonster(config, MonsterSpade4Id, "黑桃4", MonsterLevel.Level3, Suit.Spade, 4, 12, 6, 2);
        AddMonster(config, MonsterHeart4Id, "红桃4", MonsterLevel.Level3, Suit.Heart, 4, 16, 4, 0);
        AddMonster(config, MonsterDiamond4Id, "方块4", MonsterLevel.Level3, Suit.Diamond, 4, 8, 4, 8);
        AddMonster(config, MonsterClub4Id, "梅花4", MonsterLevel.Level3, Suit.Club, 4, 12, 4, 4);

        AddMonster(config, MonsterSpade5Id, "黑桃5", MonsterLevel.Level4, Suit.Spade, 5, 20, 7, 3);
        AddMonster(config, MonsterHeart5Id, "红桃5", MonsterLevel.Level4, Suit.Heart, 5, 30, 0, 0);
        AddMonster(config, MonsterDiamond5Id, "方块5", MonsterLevel.Level4, Suit.Diamond, 5, 12, 5, 13);
        AddMonster(config, MonsterClub5Id, "梅花5", MonsterLevel.Level4, Suit.Club, 5, 20, 5, 5);

        config.Cards.Add(new CardDefinition
        {
            CardId = MonsterSpadeEliteId,
            DisplayName = "黑桃A",
            Description = "黑桃精英怪物",
            CardType = CardType.Monster,
            MonsterLevel = MonsterLevel.Elite,
            Suit = Suit.Spade,
            Rank = 1,
            BaseHp = 44,
            BaseAttack = 1,
            BaseArmor = 1
        });
        config.Cards.Add(new CardDefinition
        {
            CardId = MonsterSpadeBossId,
            DisplayName = "黑桃J",
            Description = "黑桃层主怪物",
            CardType = CardType.Monster,
            MonsterLevel = MonsterLevel.Boss,
            Suit = Suit.Spade,
            Rank = 11,
            BaseHp = 121,
            BaseAttack = 11,
            BaseArmor = 0
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
            Description = displayName + "怪物",
            CardType = CardType.Monster,
            MonsterLevel = level,
            Suit = suit,
            Rank = rank,
            BaseHp = hp,
            BaseAttack = attack,
            BaseArmor = defense
        });
    }
}
