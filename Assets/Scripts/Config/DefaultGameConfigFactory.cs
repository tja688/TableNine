using System.Collections.Generic;

public static class DefaultGameConfigFactory
{
    public const string CharacterImpId = "character_imp";
    public const string SkillLightFootedId = "skill_light_footed";
    public const string SkillFirstStrikeId = "skill_first_strike";
    public const string SkillSpadeCubId = "skill_spade_cub";
    public const string SkillHeartCubId = "skill_heart_cub";
    public const string SkillDiamondCubId = "skill_diamond_cub";
    public const string SkillClubCubId = "skill_club_cub";

    public const string HelpPotionId = "help_potion";
    public const string HelpThrowingKnifeId = "help_throwing_knife";
    public const string HelpCommonChestId = "help_common_chest";
    public const string HelpAttributeUpId = "help_attribute_up";

    public const string MonsterColorlessId = "monster_colorless";
    public const string MonsterSpade2Id = "monster_spade_2";
    public const string MonsterHeart2Id = "monster_heart_2";
    public const string MonsterDiamond2Id = "monster_diamond_2";
    public const string MonsterClub2Id = "monster_club_2";

    public static GameConfigSet Create()
    {
        var config = new GameConfigSet();

        config.Characters.Add(new CharacterDefinition
        {
            CharacterId = CharacterImpId,
            DisplayName = "小鬼",
            BaseHp = 10,
            BaseAttack = 3,
            BaseDefense = 1,
            InitialSkillIds = new List<string> { SkillLightFootedId },
            InitialHelpCardIds = new List<string>
            {
                HelpPotionId,
                HelpPotionId,
                HelpPotionId,
                HelpCommonChestId,
                HelpAttributeUpId,
                HelpThrowingKnifeId,
                HelpThrowingKnifeId,
                HelpThrowingKnifeId
            }
        });

        config.Skills.Add(new SkillDefinition { SkillId = SkillLightFootedId, DisplayName = "轻车熟路" });
        config.Skills.Add(new SkillDefinition { SkillId = SkillFirstStrikeId, DisplayName = "先攻", GrantsFirstStrike = true });
        config.Skills.Add(new SkillDefinition { SkillId = SkillSpadeCubId, DisplayName = "黑桃幼崽" });
        config.Skills.Add(new SkillDefinition { SkillId = SkillHeartCubId, DisplayName = "红桃幼崽" });
        config.Skills.Add(new SkillDefinition { SkillId = SkillDiamondCubId, DisplayName = "方块幼崽" });
        config.Skills.Add(new SkillDefinition { SkillId = SkillClubCubId, DisplayName = "梅花幼崽" });

        config.Cards.Add(new CardDefinition
        {
            CardId = HelpPotionId,
            DisplayName = "恢复药水",
            CardType = CardType.Help,
            Quality = CardQuality.White,
            Price = 30,
            IsPermanentRemoveOnUse = true
        });
        config.Cards.Add(new CardDefinition
        {
            CardId = HelpThrowingKnifeId,
            DisplayName = "飞刀",
            CardType = CardType.Help,
            Quality = CardQuality.White,
            Price = 20,
            IsPermanentRemoveOnUse = true
        });
        config.Cards.Add(new CardDefinition
        {
            CardId = HelpCommonChestId,
            DisplayName = "普通宝箱卡",
            CardType = CardType.Help,
            Quality = CardQuality.Blue,
            Price = 100,
            IsPermanentRemoveOnUse = true
        });
        config.Cards.Add(new CardDefinition
        {
            CardId = HelpAttributeUpId,
            DisplayName = "属性提升卡",
            CardType = CardType.Help,
            Quality = CardQuality.Blue,
            Price = 100,
            IsPermanentRemoveOnUse = true
        });

        config.Cards.Add(new CardDefinition
        {
            CardId = MonsterColorlessId,
            DisplayName = "无色卡",
            CardType = CardType.Monster,
            MonsterLevel = MonsterLevel.Level1,
            BaseHp = 6,
            BaseAttack = 2,
            BaseDefense = 0
        });
        config.Cards.Add(new CardDefinition
        {
            CardId = MonsterSpade2Id,
            DisplayName = "黑桃2",
            CardType = CardType.Monster,
            MonsterLevel = MonsterLevel.Level1,
            Suit = Suit.Spade,
            Rank = 2,
            BaseHp = 4,
            BaseAttack = 2,
            BaseDefense = 0,
            SkillIds = new List<string> { SkillSpadeCubId }
        });
        config.Cards.Add(new CardDefinition
        {
            CardId = MonsterHeart2Id,
            DisplayName = "红桃2",
            CardType = CardType.Monster,
            MonsterLevel = MonsterLevel.Level1,
            Suit = Suit.Heart,
            Rank = 2,
            BaseHp = 6,
            BaseAttack = 2,
            BaseDefense = 0,
            SkillIds = new List<string> { SkillHeartCubId }
        });
        config.Cards.Add(new CardDefinition
        {
            CardId = MonsterDiamond2Id,
            DisplayName = "方块2",
            CardType = CardType.Monster,
            MonsterLevel = MonsterLevel.Level1,
            Suit = Suit.Diamond,
            Rank = 2,
            BaseHp = 4,
            BaseAttack = 2,
            BaseDefense = 0,
            SkillIds = new List<string> { SkillDiamondCubId }
        });
        config.Cards.Add(new CardDefinition
        {
            CardId = MonsterClub2Id,
            DisplayName = "梅花2",
            CardType = CardType.Monster,
            MonsterLevel = MonsterLevel.Level1,
            Suit = Suit.Club,
            Rank = 2,
            BaseHp = 4,
            BaseAttack = 2,
            BaseDefense = 0,
            SkillIds = new List<string> { SkillClubCubId, SkillFirstStrikeId }
        });

        config.MonsterDeckRules.Add(new MonsterDeckRuleDefinition
        {
            Layer = 1,
            NodeInLayer = 1,
            TotalCardCount = 10,
            AllowedMonsterCardIds = new List<string>
            {
                MonsterColorlessId,
                MonsterSpade2Id,
                MonsterHeart2Id,
                MonsterDiamond2Id,
                MonsterClub2Id
            }
        });

        config.MonsterDeckRules.Add(new MonsterDeckRuleDefinition
        {
            Layer = 1,
            NodeInLayer = 2,
            TotalCardCount = 11,
            AllowedMonsterCardIds = new List<string>
            {
                MonsterColorlessId,
                MonsterSpade2Id,
                MonsterHeart2Id,
                MonsterDiamond2Id,
                MonsterClub2Id
            }
        });

        return config;
    }
}
