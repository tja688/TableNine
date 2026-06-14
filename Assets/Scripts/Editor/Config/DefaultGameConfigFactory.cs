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

        AddDefaultCardDecks(config);

        config.Characters.Add(new CharacterDefinition
        {
            CharacterId = CharacterImpId,
            DisplayName = "小鬼",
            Description = "关卡结束白帮助卡三选一",
            BaseHp = 10,
            BaseAttack = 3,
            BaseArmor = 1,
            CommonDeckId = GameConfigIds.DeckCommonId,
            ClassDeckId = GameConfigIds.DeckClownId,
            InitialSkillIds = new List<string> { SkillLightFootedId },
            InitialHelpCardIds = new List<string>
            {
                HelpPotionId,
                HelpPotionId,
                HelpPotionId,
                HelpCommonChestId,
                HelpThrowingKnifeId,
                HelpThrowingKnifeId,
                HelpThrowingKnifeId,
                HelpAttributeUpId
            }
        });

        config.Skills.Add(new SkillDefinition { SkillId = SkillLightFootedId, DisplayName = "轻车熟路", Description = "关卡结束白帮助卡三选一" });
        config.Skills.Add(new SkillDefinition { SkillId = SkillFirstStrikeId, DisplayName = "先攻", Description = "战斗时优先出手", GrantsFirstStrike = true });
        config.Skills.Add(new SkillDefinition
        {
            SkillId = SkillSpadeCubId,
            DisplayName = "黑桃幼崽",
            Description = "格6时攻击+2并获得先攻"
        });
        config.Skills.Add(new SkillDefinition
        {
            SkillId = SkillHeartCubId,
            DisplayName = "红桃幼崽",
            Description = "到格8永久+2生命上限"
        });
        config.Skills.Add(new SkillDefinition
        {
            SkillId = SkillDiamondCubId,
            DisplayName = "方块幼崽",
            Description = "到格4永久+2护甲"
        });
        config.Skills.Add(new SkillDefinition
        {
            SkillId = SkillClubCubId,
            DisplayName = "梅花幼崽",
            Description = "格1-3时其他怪攻击+2"
        });

        config.Cards.Add(new CardDefinition
        {
            CardId = HelpPotionId,
            DisplayName = "恢复药水",
            Description = "恢复10点生命",
            CardType = CardType.Help,
            Quality = CardQuality.White,
            Price = 30,
            RestoreAfterNode = false
        });
        config.Cards.Add(new CardDefinition
        {
            CardId = HelpThrowingKnifeId,
            DisplayName = "飞刀",
            Description = "对怪物造成6点伤害",
            CardType = CardType.Help,
            Quality = CardQuality.White,
            Price = 20,
            RestoreAfterNode = false
        });
        config.Cards.Add(new CardDefinition
        {
            CardId = HelpCommonChestId,
            DisplayName = "普通宝箱卡",
            Description = "开启普通宝箱奖励",
            CardType = CardType.Help,
            Quality = CardQuality.Blue,
            Price = 100,
            RestoreAfterNode = false
        });
        config.Cards.Add(new CardDefinition
        {
            CardId = HelpAttributeUpId,
            DisplayName = "属性提升卡",
            Description = "选择一项属性提升",
            CardType = CardType.Help,
            Quality = CardQuality.Gold,
            Price = 100,
            RestoreAfterNode = false
        });

        config.Cards.Add(new CardDefinition
        {
            CardId = HelpGoldCardId,
            DisplayName = "金币卡",
            Description = "获得金币奖励",
            CardType = CardType.Help,
            Quality = CardQuality.Blue,
            Price = 30,
            RestoreAfterNode = false
        });
        config.Cards.Add(new CardDefinition
        {
            CardId = HelpChestCardId,
            DisplayName = "宝箱卡",
            Description = "开启宝箱奖励",
            CardType = CardType.Help,
            Quality = CardQuality.Blue,
            Price = 100,
            RestoreAfterNode = false
        });
        config.Cards.Add(new CardDefinition
        {
            CardId = HelpBlessingId,
            DisplayName = "祝福",
            Description = "获得随机正面增益",
            CardType = CardType.Help,
            Quality = CardQuality.Gold,
            Price = 200,
            RestoreAfterNode = false
        });
        config.Cards.Add(new CardDefinition
        {
            CardId = HelpBandageId,
            DisplayName = "绷带",
            Description = "恢复少量生命",
            CardType = CardType.Help,
            Quality = CardQuality.White,
            Price = 15,
            RestoreAfterNode = false
        });

        config.Cards.Add(new CardDefinition
        {
            CardId = MonsterColorlessId,
            DisplayName = "无色卡",
            Description = "无花色普通怪物",
            CardType = CardType.Monster,
            MonsterLevel = MonsterLevel.Level1,
            BaseHp = 6,
            BaseAttack = 2,
            BaseArmor = 0
        });
        config.Cards.Add(new CardDefinition
        {
            CardId = MonsterSpade2Id,
            DisplayName = "黑桃2",
            Description = "黑桃幼崽怪物",
            CardType = CardType.Monster,
            MonsterLevel = MonsterLevel.Level1,
            Suit = Suit.Spade,
            Rank = 2,
            BaseHp = 2,
            BaseAttack = 3,
            BaseArmor = 1,
            SkillIds = new List<string> { SkillSpadeCubId }
        });
        config.Cards.Add(new CardDefinition
        {
            CardId = MonsterHeart2Id,
            DisplayName = "红桃2",
            Description = "红桃幼崽怪物",
            CardType = CardType.Monster,
            MonsterLevel = MonsterLevel.Level1,
            Suit = Suit.Heart,
            Rank = 2,
            BaseHp = 4,
            BaseAttack = 2,
            BaseArmor = 0,
            SkillIds = new List<string> { SkillHeartCubId }
        });
        config.Cards.Add(new CardDefinition
        {
            CardId = MonsterDiamond2Id,
            DisplayName = "方块2",
            Description = "方块幼崽怪物",
            CardType = CardType.Monster,
            MonsterLevel = MonsterLevel.Level1,
            Suit = Suit.Diamond,
            Rank = 2,
            BaseHp = 1,
            BaseAttack = 2,
            BaseArmor = 3,
            SkillIds = new List<string> { SkillDiamondCubId }
        });
        config.Cards.Add(new CardDefinition
        {
            CardId = MonsterClub2Id,
            DisplayName = "梅花2",
            Description = "梅花幼崽怪物",
            CardType = CardType.Monster,
            MonsterLevel = MonsterLevel.Level1,
            Suit = Suit.Club,
            Rank = 2,
            BaseHp = 2,
            BaseAttack = 2,
            BaseArmor = 2,
            SkillIds = new List<string> { SkillClubCubId }
        });

        AddLayer1MonstersAndSkills(config);
        AddPlaytestContent(config);
        AllLayersMonsterDeckRules.Populate(config);

        // Relic definitions
        config.Relics.Add(new RelicDefinition
        {
            RelicId = RelicWoodShieldId,
            DisplayName = "木盾",
            Description = "护甲+2",
            Quality = CardQuality.White,
            StatAttackBonus = 0,
            StatArmorBonus = 2,
            StatMaxHpBonus = 0
        });
        config.Relics.Add(new RelicDefinition
        {
            RelicId = RelicWoodSwordId,
            DisplayName = "木剑",
            Description = "攻击+2",
            Quality = CardQuality.White,
            StatAttackBonus = 2,
            StatArmorBonus = 0,
            StatMaxHpBonus = 0
        });
        config.Relics.Add(new RelicDefinition
        {
            RelicId = RelicWoodArmorId,
            DisplayName = "木甲",
            Description = "生命上限+3",
            Quality = CardQuality.White,
            StatAttackBonus = 0,
            StatArmorBonus = 0,
            StatMaxHpBonus = 3
        });
        config.Relics.Add(new RelicDefinition
        {
            RelicId = RelicLivingFleshId,
            DisplayName = "活肉",
            Description = "生命上限+5",
            Quality = CardQuality.Blue,
            StatAttackBonus = 0,
            StatArmorBonus = 0,
            StatMaxHpBonus = 5
        });
        config.Relics.Add(new RelicDefinition
        {
            RelicId = RelicThornArmorId,
            DisplayName = "荆棘甲",
            Description = "受击反弹1点伤害",
            Quality = CardQuality.Blue,
            StatAttackBonus = 0,
            StatArmorBonus = 1,
            StatMaxHpBonus = 0
        });
        config.Relics.Add(new RelicDefinition
        {
            RelicId = RelicPhoenixFeatherId,
            DisplayName = "凤凰羽毛",
            Description = "致命伤防止死亡并消耗",
            Quality = CardQuality.Gold,
            StatAttackBonus = 0,
            StatArmorBonus = 0,
            StatMaxHpBonus = 0,
            IsOneShot = true
        });

        // Room definitions
        config.Rooms.Add(new RoomDefinition
        {
            RoomId = RoomGoldId,
            DisplayName = "金币房间",
            Description = "获得50金币",
            RoomType = RoomType.Gold,
            RewardGold = 50
        });
        config.Rooms.Add(new RoomDefinition
        {
            RoomId = RoomChestId,
            DisplayName = "宝箱房间",
            Description = "注入宝箱卡奖励",
            RoomType = RoomType.Chest,
            InjectCardId = HelpChestCardId
        });
        config.Rooms.Add(new RoomDefinition
        {
            RoomId = RoomAttributeId,
            DisplayName = "温泉房",
            Description = "生命上限提升并回满",
            RoomType = RoomType.Attribute,
            StatMaxHpBonus = RewardConstants.AttributeRoomMaxHpBonus
        });
        config.Rooms.Add(new RoomDefinition
        {
            RoomId = RoomShopId,
            DisplayName = "商店房间",
            Description = "购买卡牌与遗物",
            RoomType = RoomType.Shop
        });

        AssignDefaultCardDecks(config);
        CardDefinitionMigration.MarkAuthoritativeRestoreAfterNode(config.Cards);
        HelpCardSystemTagConfig.Apply(config);
        EffectGraphRegistry.AssignToConfig(config);
        return config;
    }

    private static void AddDefaultCardDecks(GameConfigSet config)
    {
        config.CardDecks.Add(new CardDeckDefinition
        {
            DeckId = GameConfigIds.DeckCommonId,
            DisplayName = "通用牌组",
            Description = "所有职业共享的帮助卡",
            Faction = CardDeckFaction.Player,
            DeckKind = CardDeckKind.Common
        });
        config.CardDecks.Add(new CardDeckDefinition
        {
            DeckId = GameConfigIds.DeckClownId,
            DisplayName = "小丑牌组",
            Description = "小丑职业专属帮助卡",
            Faction = CardDeckFaction.Player,
            DeckKind = CardDeckKind.Class
        });
        config.CardDecks.Add(new CardDeckDefinition
        {
            DeckId = GameConfigIds.DeckTutorId,
            DisplayName = "导师牌组",
            Description = "导师奖励技能卡的专属卡面与卡背",
            Faction = CardDeckFaction.Player,
            DeckKind = CardDeckKind.Custom
        });
        config.CardDecks.Add(new CardDeckDefinition
        {
            DeckId = GameConfigIds.DeckWeakEliteId,
            DisplayName = "弱精英牌组",
            Description = "每层一至三节点使用的怪物牌组",
            Faction = CardDeckFaction.Monster,
            DeckKind = CardDeckKind.WeakElite
        });
        config.CardDecks.Add(new CardDeckDefinition
        {
            DeckId = GameConfigIds.DeckStrongEliteId,
            DisplayName = "强精英牌组",
            Description = "每层四至六节点使用的怪物牌组",
            Faction = CardDeckFaction.Monster,
            DeckKind = CardDeckKind.StrongElite
        });
        config.CardDecks.Add(new CardDeckDefinition
        {
            DeckId = GameConfigIds.DeckBossId,
            DisplayName = "层主牌组",
            Description = "每层七至九节点使用的怪物牌组",
            Faction = CardDeckFaction.Monster,
            DeckKind = CardDeckKind.Boss
        });
    }

    private static void AssignDefaultCardDecks(GameConfigSet config)
    {
        for (var i = 0; i < config.Cards.Count; i++)
        {
            var card = config.Cards[i];
            if (card == null || !string.IsNullOrWhiteSpace(card.DeckId))
            {
                continue;
            }

            if (card.CardId == HelpThrowingKnifeId)
            {
                card.DeckId = GameConfigIds.DeckClownId;
            }
            else if (card.CardType == CardType.Tutor)
            {
                card.DeckId = GameConfigIds.DeckTutorId;
            }
            else if (card.CardType == CardType.Help)
            {
                card.DeckId = GameConfigIds.DeckCommonId;
            }
            else if (card.CardType == CardType.Monster)
            {
                card.DeckId = GetDefaultMonsterDeckId(card.MonsterLevel);
            }
        }
    }

    private static string GetDefaultMonsterDeckId(MonsterLevel level)
    {
        switch (level)
        {
            case MonsterLevel.Level1:
            case MonsterLevel.Level2:
            case MonsterLevel.Level3:
                return GameConfigIds.DeckWeakEliteId;
            case MonsterLevel.Level4:
            case MonsterLevel.Elite:
                return GameConfigIds.DeckStrongEliteId;
            case MonsterLevel.Boss:
                return GameConfigIds.DeckBossId;
            default:
                return GameConfigIds.DeckCommonId;
        }
    }
}
