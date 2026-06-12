using System.Collections.Generic;

public static partial class DefaultGameConfigFactory
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
    public const string HelpBlessingId = "help_blessing";
    public const string HelpBandageId = "help_bandage";

    public const string MonsterColorlessId = "monster_colorless";
    public const string MonsterSpade2Id = "monster_spade_2";
    public const string MonsterHeart2Id = "monster_heart_2";
    public const string MonsterDiamond2Id = "monster_diamond_2";
    public const string MonsterClub2Id = "monster_club_2";

    // Relic IDs
    public const string RelicWoodShieldId = "relic_wood_shield";
    public const string RelicWoodSwordId = "relic_wood_sword";
    public const string RelicWoodArmorId = "relic_wood_armor";
    public const string RelicLivingFleshId = "relic_living_flesh";
    public const string RelicThornArmorId = "relic_thorn_armor";
    public const string RelicPhoenixFeatherId = "relic_phoenix_feather";

    // Room IDs
    public const string RoomGoldId = "room_gold";
    public const string RoomChestId = "room_chest";
    public const string RoomAttributeId = "room_attribute";
    public const string RoomShopId = "room_shop";

    // Help card IDs for room injection
    public const string HelpGoldCardId = "help_gold_card";
    public const string HelpChestCardId = "help_chest_card";

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
            CardId = HelpGoldCardId,
            DisplayName = "金币卡",
            CardType = CardType.Help,
            Quality = CardQuality.White,
            Price = 30,
            IsPermanentRemoveOnUse = true,
            RestoreAfterNode = false
        });
        config.Cards.Add(new CardDefinition
        {
            CardId = HelpChestCardId,
            DisplayName = "宝箱卡",
            CardType = CardType.Help,
            Quality = CardQuality.Blue,
            Price = 100,
            IsPermanentRemoveOnUse = true,
            RestoreAfterNode = false
        });
        config.Cards.Add(new CardDefinition
        {
            CardId = HelpBlessingId,
            DisplayName = "祝福",
            CardType = CardType.Help,
            Quality = CardQuality.Gold,
            Price = 200,
            IsPermanentRemoveOnUse = true
        });
        config.Cards.Add(new CardDefinition
        {
            CardId = HelpBandageId,
            DisplayName = "绷带",
            CardType = CardType.Help,
            Quality = CardQuality.White,
            Price = 15,
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

        AddLayer1MonstersAndSkills(config);
        AddPlaytestContent(config);
        AllLayersMonsterDeckRules.Populate(config);

        // Relic definitions
        config.Relics.Add(new RelicDefinition
        {
            RelicId = RelicWoodShieldId,
            DisplayName = "木盾",
            Quality = CardQuality.White,
            StatAttackBonus = 0,
            StatDefenseBonus = 2,
            StatMaxHpBonus = 0
        });
        config.Relics.Add(new RelicDefinition
        {
            RelicId = RelicWoodSwordId,
            DisplayName = "木剑",
            Quality = CardQuality.White,
            StatAttackBonus = 2,
            StatDefenseBonus = 0,
            StatMaxHpBonus = 0
        });
        config.Relics.Add(new RelicDefinition
        {
            RelicId = RelicWoodArmorId,
            DisplayName = "木甲",
            Quality = CardQuality.White,
            StatAttackBonus = 0,
            StatDefenseBonus = 0,
            StatMaxHpBonus = 3
        });
        config.Relics.Add(new RelicDefinition
        {
            RelicId = RelicLivingFleshId,
            DisplayName = "活肉",
            Quality = CardQuality.Blue,
            StatAttackBonus = 0,
            StatDefenseBonus = 0,
            StatMaxHpBonus = 5
        });
        config.Relics.Add(new RelicDefinition
        {
            RelicId = RelicThornArmorId,
            DisplayName = "荆棘甲",
            Quality = CardQuality.Blue,
            StatAttackBonus = 0,
            StatDefenseBonus = 1,
            StatMaxHpBonus = 0,
            TriggerDescription = "受到攻击时反弹1点伤害"
        });
        config.Relics.Add(new RelicDefinition
        {
            RelicId = RelicPhoenixFeatherId,
            DisplayName = "凤凰羽毛",
            Quality = CardQuality.Gold,
            StatAttackBonus = 0,
            StatDefenseBonus = 0,
            StatMaxHpBonus = 0,
            IsOneShot = true,
            TriggerDescription = "受到致命伤害时防止死亡，然后消耗"
        });

        // Room definitions
        config.Rooms.Add(new RoomDefinition
        {
            RoomId = RoomGoldId,
            DisplayName = "金币房间",
            RoomType = RoomType.Gold,
            RewardGold = 30
        });
        config.Rooms.Add(new RoomDefinition
        {
            RoomId = RoomChestId,
            DisplayName = "宝箱房间",
            RoomType = RoomType.Chest,
            InjectCardId = HelpChestCardId
        });
        config.Rooms.Add(new RoomDefinition
        {
            RoomId = RoomAttributeId,
            DisplayName = "属性房间",
            RoomType = RoomType.Attribute,
            InjectCardId = HelpAttributeUpId
        });
        config.Rooms.Add(new RoomDefinition
        {
            RoomId = RoomShopId,
            DisplayName = "商店房间",
            RoomType = RoomType.Shop
        });

        CardDefinitionMigration.MigrateHelpCardSemantics(config.Cards);
        return config;
    }
}
