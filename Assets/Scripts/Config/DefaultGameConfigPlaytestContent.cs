using System.Collections.Generic;

public static partial class DefaultGameConfigFactory
{
    public const string HelpFireballId = "help_fireball";
    public const string HelpSpinWheelId = "help_spin_wheel";
    public const string HelpViolenceId = "help_violence";
    public const string HelpBoulderId = "help_boulder";
    public const string HelpBombId = "help_bomb";
    public const string HelpSwapId = "help_swap";
    public const string HelpSmasherId = "help_smasher";
    public const string HelpFoodId = "help_food";
    public const string HelpHealingSpringId = "help_healing_spring";
    public const string HelpCrashTutorialId = "help_crash_tutorial";
    public const string HelpWatchtowerId = "help_watchtower";
    public const string HelpMultiplierTowerId = "help_multiplier_tower";

    public const string RelicIronShieldId = "relic_iron_shield";
    public const string RelicSharpSwordId = "relic_sharp_sword";
    public const string RelicVitalityCharmId = "relic_vitality_charm";
    public const string RelicBloodFangId = "relic_blood_fang";
    public const string RelicGoldSwordId = "relic_gold_sword";
    public const string RelicDragonArmorId = "relic_dragon_armor";
    public const string RelicGoldenChestId = "relic_golden_chest";
    public const string RelicBerserkerAxeId = "relic_berserker_axe";
    public const string RelicLuckyCoinId = "relic_lucky_coin";

    public const string SkillArmorBreakerId = "skill_armor_breaker";
    public const string SkillLovingBodyId = "skill_loving_body";
    public const string SkillSharpShieldId = "skill_sharp_shield";
    public const string SkillCallFriendsId = "skill_call_friends";
    public const string SkillRevengeId = "skill_revenge";
    public const string SkillMedicId = "skill_medic";
    public const string SkillProtectionAuraId = "skill_protection_aura";
    public const string SkillRelentlessPursuitId = "skill_relentless_pursuit";
    public const string SkillSideStrikeId = "skill_side_strike";
    public const string SkillHeartMotherId = "skill_heart_mother";
    public const string SkillUnbreakableId = "skill_unbreakable";
    public const string SkillVerticalLeverId = "skill_vertical_lever";
    public const string SkillDuelId = "skill_duel";
    public const string SkillWarDanceId = "skill_war_dance";
    public const string SkillViolenceId = "skill_violence";
    public const string SkillSpadeRoyaltyId = "skill_spade_royalty";

    public static readonly string[] PlaytestHelpCardIds =
    {
        HelpPotionId,
        HelpBlessingId,
        HelpThrowingKnifeId,
        HelpFireballId,
        HelpSpinWheelId,
        HelpViolenceId,
        HelpBoulderId,
        HelpBombId,
        HelpSwapId,
        HelpSmasherId,
        HelpAttributeUpId,
        HelpGoldCardId,
        HelpFoodId,
        HelpCommonChestId,
        HelpHealingSpringId,
        HelpCrashTutorialId,
        HelpBlueChestId,
        HelpWatchtowerId,
        HelpMultiplierTowerId,
        HelpGoldChestId
    };

    public static string GetEliteMonsterId(int layer)
    {
        switch (layer)
        {
            case 2: return MonsterHeartEliteId;
            case 3: return MonsterDiamondEliteId;
            default: return MonsterSpadeEliteId;
        }
    }

    public static string GetBossMonsterId(int layer)
    {
        switch (layer)
        {
            case 2: return MonsterHeartBossId;
            case 3: return MonsterDiamondBossId;
            default: return MonsterSpadeBossId;
        }
    }

    public static IReadOnlyList<string> GetLevelMonsterIds(int layer, MonsterLevel level)
    {
        switch (level)
        {
            case MonsterLevel.Level1:
                return layer == 1 ? Layer1Level1MonsterIds : GetScaledMonsterIds(Layer1Level1MonsterIds, layer);
            case MonsterLevel.Level2:
                return layer == 1 ? Layer1Level2MonsterIds : GetScaledMonsterIds(Layer1Level2MonsterIds, layer);
            case MonsterLevel.Level3:
                return layer == 1 ? Layer1Level3MonsterIds : GetScaledMonsterIds(Layer1Level3MonsterIds, layer);
            case MonsterLevel.Level4:
                return layer == 1 ? Layer1Level4MonsterIds : GetScaledMonsterIds(Layer1Level4MonsterIds, layer);
            default:
                return Layer1Level1MonsterIds;
        }
    }

    private static IReadOnlyList<string> GetScaledMonsterIds(IReadOnlyList<string> baseIds, int layer)
    {
        var suffix = layer == 2 ? "_l2" : "_l3";
        var result = new string[baseIds.Count];
        for (var i = 0; i < baseIds.Count; i++)
        {
            result[i] = baseIds[i] + suffix;
        }

        return result;
    }

    public static void AddPlaytestContent(GameConfigSet config)
    {
        AddPlaytestSkills(config);
        AddPlaytestHelpCards(config);
        AddPlaytestRelics(config);
        AddScaledLayerMonsters(config);
    }

    private static void AddPlaytestSkills(GameConfigSet config)
    {
        config.Skills.Add(new SkillDefinition { SkillId = SkillArmorBreakerId, DisplayName = "破防专家" });
        config.Skills.Add(new SkillDefinition { SkillId = SkillLovingBodyId, DisplayName = "爱之躯" });
        config.Skills.Add(new SkillDefinition { SkillId = SkillSharpShieldId, DisplayName = "尖盾" });
        config.Skills.Add(new SkillDefinition { SkillId = SkillCallFriendsId, DisplayName = "叫人！" });
        config.Skills.Add(new SkillDefinition { SkillId = SkillRevengeId, DisplayName = "复仇" });
        config.Skills.Add(new SkillDefinition { SkillId = SkillMedicId, DisplayName = "医疗兵" });
        config.Skills.Add(new SkillDefinition { SkillId = SkillProtectionAuraId, DisplayName = "防护光环" });
        config.Skills.Add(new SkillDefinition { SkillId = SkillRelentlessPursuitId, DisplayName = "不休追击" });
        config.Skills.Add(new SkillDefinition { SkillId = SkillSideStrikeId, DisplayName = "侧方打击" });
        config.Skills.Add(new SkillDefinition { SkillId = SkillHeartMotherId, DisplayName = "红桃之母" });
        config.Skills.Add(new SkillDefinition { SkillId = SkillUnbreakableId, DisplayName = "牢不可破" });
        config.Skills.Add(new SkillDefinition { SkillId = SkillVerticalLeverId, DisplayName = "竖向拉杆" });
        config.Skills.Add(new SkillDefinition { SkillId = SkillDuelId, DisplayName = "决斗" });
        config.Skills.Add(new SkillDefinition { SkillId = SkillWarDanceId, DisplayName = "战舞" });
        config.Skills.Add(new SkillDefinition { SkillId = SkillViolenceId, DisplayName = "暴力" });
        config.Skills.Add(new SkillDefinition { SkillId = SkillSpadeRoyaltyId, DisplayName = "黑桃皇室" });
    }

    private static void AddPlaytestHelpCards(GameConfigSet config)
    {
        config.Cards.Add(HelpCard(HelpFireballId, "火球术", CardQuality.White, 30, true));
        config.Cards.Add(HelpCard(HelpSpinWheelId, "旋转轮", CardQuality.White, 20, true));
        config.Cards.Add(HelpCard(HelpViolenceId, "暴力卡", CardQuality.White, 30, true));
        config.Cards.Add(HelpCard(HelpBoulderId, "滚石", CardQuality.White, 50, true));
        config.Cards.Add(HelpCard(HelpBombId, "爆弹", CardQuality.White, 50, true));
        config.Cards.Add(HelpCard(HelpSwapId, "交换卡", CardQuality.White, 50, true));
        config.Cards.Add(HelpCard(HelpSmasherId, "破击锤", CardQuality.White, 50, true));
        config.Cards.Add(HelpCard(HelpFoodId, "食品卡", CardQuality.Blue, 50, true));
        config.Cards.Add(HelpCard(HelpHealingSpringId, "治疗泉", CardQuality.Blue, 80, true));
        config.Cards.Add(HelpCard(HelpCrashTutorialId, "撞击教程", CardQuality.Blue, 80, true));
        config.Cards.Add(HelpCard(HelpWatchtowerId, "瞭望塔", CardQuality.Gold, 150, true));
        config.Cards.Add(HelpCard(HelpMultiplierTowerId, "倍增塔", CardQuality.Gold, 150, true));

        var blessing = config.Cards.Find(c => c.CardId == HelpBlessingId);
        if (blessing != null)
        {
            blessing.DisplayName = "庇佑魔法卡";
            blessing.Quality = CardQuality.White;
            blessing.Price = 20;
        }
    }

    private static CardDefinition HelpCard(string id, string name, CardQuality quality, int price, bool permanentRemove)
    {
        return new CardDefinition
        {
            CardId = id,
            DisplayName = name,
            CardType = CardType.Help,
            Quality = quality,
            Price = price,
            IsPermanentRemoveOnUse = permanentRemove,
            RestoreAfterNode = !permanentRemove
        };
    }

    private static void AddPlaytestRelics(GameConfigSet config)
    {
        config.Relics.Add(new RelicDefinition
        {
            RelicId = RelicLuckyCoinId,
            DisplayName = "幸运硬币",
            Quality = CardQuality.White,
            TriggerDescription = "击败精英或层主时将金币卡加入战斗牌组"
        });
        config.Relics.Add(new RelicDefinition
        {
            RelicId = RelicIronShieldId,
            DisplayName = "铁盾",
            Quality = CardQuality.Blue,
            StatDefenseBonus = 1,
            TriggerDescription = "受到的所有伤害减少1点"
        });
        config.Relics.Add(new RelicDefinition
        {
            RelicId = RelicSharpSwordId,
            DisplayName = "锐利长剑",
            Quality = CardQuality.Blue,
            StatAttackBonus = 1,
            TriggerDescription = "本关卡每移除一张怪物卡攻击+1"
        });
        config.Relics.Add(new RelicDefinition
        {
            RelicId = RelicVitalityCharmId,
            DisplayName = "活力护符",
            Quality = CardQuality.Blue,
            StatMaxHpBonus = 6,
            TriggerDescription = "每个关卡结束时恢复6血量"
        });
        config.Relics.Add(new RelicDefinition
        {
            RelicId = RelicBloodFangId,
            DisplayName = "嗜血之牙",
            Quality = CardQuality.Blue,
            StatAttackBonus = 1,
            TriggerDescription = "每次移除怪物卡时恢复2点生命"
        });
        config.Relics.Add(new RelicDefinition
        {
            RelicId = RelicGoldSwordId,
            DisplayName = "金剑",
            Quality = CardQuality.Gold,
            StatAttackBonus = 8,
            TriggerDescription = "每进行一次战斗，本遗物提供的攻击-1"
        });
        config.Relics.Add(new RelicDefinition
        {
            RelicId = RelicDragonArmorId,
            DisplayName = "龙鳞甲",
            Quality = CardQuality.Gold,
            StatDefenseBonus = 1,
            StatMaxHpBonus = 6,
            TriggerDescription = "所有怪物卡的攻击-1"
        });
        config.Relics.Add(new RelicDefinition
        {
            RelicId = RelicGoldenChestId,
            DisplayName = "金色宝箱",
            Quality = CardQuality.Gold,
            ExcludeFromPool = true,
            TriggerDescription = "获取时将两张金色宝箱卡加入帮助卡组"
        });
        config.Relics.Add(new RelicDefinition
        {
            RelicId = RelicBerserkerAxeId,
            DisplayName = "狂战士斧",
            Quality = CardQuality.Gold,
            StatAttackBonus = 1,
            TriggerDescription = "当前血量低于50%时攻击+3"
        });

        var livingFlesh = FindRelic(config, RelicLivingFleshId);
        if (livingFlesh != null)
        {
            livingFlesh.DisplayName = "活着的肉";
            livingFlesh.TriggerDescription = "使用帮助卡时恢复1血量";
        }

        var woodShield = FindRelic(config, RelicWoodShieldId);
        if (woodShield != null)
        {
            woodShield.StatDefenseBonus = 1;
        }

        var woodSword = FindRelic(config, RelicWoodSwordId);
        if (woodSword != null)
        {
            woodSword.StatAttackBonus = 1;
        }

        var woodArmor = FindRelic(config, RelicWoodArmorId);
        if (woodArmor != null)
        {
            woodArmor.StatMaxHpBonus = 2;
        }

        var phoenix = FindRelic(config, RelicPhoenixFeatherId);
        if (phoenix != null)
        {
            phoenix.StatMaxHpBonus = 8;
            phoenix.TriggerDescription = "受到致命伤害时恢复50%血量并消耗";
        }
    }

    private static RelicDefinition FindRelic(GameConfigSet config, string relicId)
    {
        for (var i = 0; i < config.Relics.Count; i++)
        {
            if (config.Relics[i].RelicId == relicId)
            {
                return config.Relics[i];
            }
        }

        return null;
    }

    private static void AddScaledLayerMonsters(GameConfigSet config)
    {
        AssignMonsterSkills(config);
        AddScaledMonsterSet(config, 2, 1.2f);
        AddScaledMonsterSet(config, 3, 1.5f);
        AddLayerElitesAndBosses(config);
    }

    private static void AddScaledMonsterSet(GameConfigSet config, int layer, float scale)
    {
        var suffix = layer == 2 ? "_l2" : "_l3";
        DuplicateScaledMonsters(config, Layer1Level2MonsterIds, suffix, scale, MonsterLevel.Level2);
        DuplicateScaledMonsters(config, Layer1Level3MonsterIds, suffix, scale, MonsterLevel.Level3);
        DuplicateScaledMonsters(config, Layer1Level4MonsterIds, suffix, scale, MonsterLevel.Level4);
    }

    private static void DuplicateScaledMonsters(
        GameConfigSet config,
        IReadOnlyList<string> sourceIds,
        string suffix,
        float scale,
        MonsterLevel level)
    {
        for (var i = 0; i < sourceIds.Count; i++)
        {
            var source = FindCard(config, sourceIds[i]);
            if (source == null)
            {
                continue;
            }

            config.Cards.Add(new CardDefinition
            {
                CardId = source.CardId + suffix,
                DisplayName = source.DisplayName + (suffix == "_l2" ? "·二层" : "·三层"),
                CardType = CardType.Monster,
                MonsterLevel = level,
                Suit = source.Suit,
                Rank = source.Rank,
                BaseHp = ScaleStat(source.BaseHp, scale),
                BaseAttack = ScaleStat(source.BaseAttack, scale),
                BaseDefense = ScaleStat(source.BaseDefense, scale),
                SkillIds = new List<string>(source.SkillIds)
            });
        }
    }

    private static CardDefinition FindCard(GameConfigSet config, string cardId)
    {
        for (var i = 0; i < config.Cards.Count; i++)
        {
            if (config.Cards[i].CardId == cardId)
            {
                return config.Cards[i];
            }
        }

        return null;
    }

    private static int ScaleStat(int value, float scale)
    {
        return MathfMax(1, (int)(value * scale));
    }

    private static int MathfMax(int a, int b)
    {
        return a > b ? a : b;
    }

    private static void AddLayerElitesAndBosses(GameConfigSet config)
    {
        config.Cards.Add(new CardDefinition
        {
            CardId = MonsterHeartEliteId,
            DisplayName = "红桃A",
            CardType = CardType.Monster,
            MonsterLevel = MonsterLevel.Elite,
            Suit = Suit.Heart,
            Rank = 1,
            BaseHp = 55,
            BaseAttack = 2,
            BaseDefense = 2,
            SkillIds = new List<string> { SkillDuelId, SkillWarDanceId }
        });
        config.Cards.Add(new CardDefinition
        {
            CardId = MonsterHeartBossId,
            DisplayName = "红桃J",
            CardType = CardType.Monster,
            MonsterLevel = MonsterLevel.Boss,
            Suit = Suit.Heart,
            Rank = 11,
            BaseHp = 150,
            BaseAttack = 12,
            BaseDefense = 1,
            SkillIds = new List<string> { SkillViolenceId, SkillSpadeRoyaltyId }
        });
        config.Cards.Add(new CardDefinition
        {
            CardId = MonsterDiamondEliteId,
            DisplayName = "方块A",
            CardType = CardType.Monster,
            MonsterLevel = MonsterLevel.Elite,
            Suit = Suit.Diamond,
            Rank = 1,
            BaseHp = 66,
            BaseAttack = 2,
            BaseDefense = 3,
            SkillIds = new List<string> { SkillDuelId, SkillWarDanceId }
        });
        config.Cards.Add(new CardDefinition
        {
            CardId = MonsterDiamondBossId,
            DisplayName = "方块J",
            CardType = CardType.Monster,
            MonsterLevel = MonsterLevel.Boss,
            Suit = Suit.Diamond,
            Rank = 11,
            BaseHp = 180,
            BaseAttack = 10,
            BaseDefense = 4,
            SkillIds = new List<string> { SkillViolenceId, SkillSpadeRoyaltyId }
        });
    }

    private static void AssignMonsterSkills(GameConfigSet config)
    {
        AssignSkills(config, MonsterSpade3Id, SkillArmorBreakerId);
        AssignSkills(config, MonsterHeart3Id, SkillLovingBodyId);
        AssignSkills(config, MonsterDiamond3Id, SkillSharpShieldId);
        AssignSkills(config, MonsterClub3Id, SkillCallFriendsId);
        AssignSkills(config, MonsterSpade4Id, SkillRevengeId);
        AssignSkills(config, MonsterHeart4Id, SkillMedicId);
        AssignSkills(config, MonsterDiamond4Id, SkillProtectionAuraId);
        AssignSkills(config, MonsterClub4Id, SkillRelentlessPursuitId);
        AssignSkills(config, MonsterSpade5Id, SkillSideStrikeId);
        AssignSkills(config, MonsterHeart5Id, SkillHeartMotherId);
        AssignSkills(config, MonsterDiamond5Id, SkillUnbreakableId);
        AssignSkills(config, MonsterClub5Id, SkillVerticalLeverId);
        AssignSkills(config, MonsterSpadeEliteId, SkillDuelId, SkillWarDanceId);
        AssignSkills(config, MonsterSpadeBossId, SkillViolenceId, SkillSpadeRoyaltyId);
    }

    private static void AssignSkills(GameConfigSet config, string cardId, params string[] skillIds)
    {
        var card = FindCard(config, cardId);
        if (card == null)
        {
            return;
        }

        card.SkillIds.Clear();
        card.SkillIds.AddRange(skillIds);
    }
}
