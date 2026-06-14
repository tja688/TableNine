#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;

/// <summary>
/// 效果配置编辑器展示文案，对照 Assets/Docs 帮助卡数据、遗物数据、技能数据。
/// 运行时 ID 保持不变，仅用于 Editor 窗口汉化展示。
/// </summary>
public static class EffectConfigDisplayCatalog
{
    public const string CategoryHelpActive = "援助卡·主动";
    public const string CategoryHelpPassive = "援助卡·被动";
    public const string CategoryRelic = "遗物";
    public const string CategorySkill = "技能";
    public const string CategoryOther = "其他";

    private static readonly Dictionary<string, GraphDisplayInfo> Graphs = new Dictionary<string, GraphDisplayInfo>
    {
        ["eg_help_potion"] = Info("恢复药水", CategoryHelpActive, "恢复10点生命，使用后消耗。"),
        ["eg_help_throwing_knife"] = Info("飞刀", CategoryHelpActive, "对目标造成6点伤害。"),
        ["eg_help_fireball"] = Info("火球术", CategoryHelpActive, "对目标造成等同于玩家攻击的伤害。"),
        ["eg_help_attribute_up"] = Info("属性提升卡", CategoryHelpActive, "选择攻击+1 / 护甲+1 / 生命+2。"),
        ["eg_help_common_chest"] = Info("普通宝箱卡", CategoryHelpActive, "三选一遗物（白65% / 蓝30% / 金5%）。"),
        ["eg_help_chest_card"] = Info("宝箱卡", CategoryHelpActive, "三选一遗物（白65% / 蓝30% / 金5%）。"),
        ["eg_help_blue_chest"] = Info("蓝色宝箱卡", CategoryHelpActive, "三选一遗物（白50% / 蓝50% / 金10%）。"),
        ["eg_help_gold_chest"] = Info("金色宝箱卡", CategoryHelpActive, "三选一遗物（蓝50% / 金50%）。"),
        ["eg_help_gold_card"] = Info("金币卡", CategoryHelpActive, "获得50金币。"),
        ["eg_help_blessing"] = Info("庇佑魔法卡", CategoryHelpActive, "下次受伤归零（仅首段）。"),
        ["eg_help_bandage"] = Info("绷带", CategoryHelpActive, "恢复5点生命。"),
        ["eg_help_food"] = Info("食品卡", CategoryHelpActive, "将玩家血量回满。"),
        ["eg_help_healing_spring"] = Info("治疗泉", CategoryHelpActive, "点击后消耗；被动回血见被动效果。"),
        ["eg_help_bomb"] = Info("爆弹", CategoryHelpActive, "对所有怪物造成4点伤害。"),
        ["eg_help_spin_wheel"] = Info("旋转轮", CategoryHelpActive, "逆时针旋转棋盘一次。"),
        ["eg_help_crash_tutorial"] = Info("撞击教程", CategoryHelpActive, "对目标造成等同于当前血量的伤害。"),
        ["eg_help_boulder"] = Info("滚石", CategoryHelpActive, "点击消耗；格3击杀见被动效果。"),
        ["eg_help_smasher"] = Info("破击锤", CategoryHelpActive, "将目标护甲降低10点。"),
        ["eg_help_durable_shield"] = Info("耐用盾牌", CategoryHelpActive, "玩家获得5点护甲。"),
        ["eg_help_shield_strike_tutorial"] = Info("盾击教程", CategoryHelpActive, "对目标造成等同于当前护甲的伤害。"),
        ["eg_help_teleport"] = Info("传送卡", CategoryHelpActive, "将目标卡牌洗回战斗卡组。"),
        ["eg_help_kidnap"] = Info("绑票", CategoryHelpActive, "移除非精英怪物并获得其护甲。"),
        ["eg_help_violence"] = Info("暴力卡", CategoryHelpActive, "当前总攻击翻倍，互动一次后复原。"),
        ["eg_help_swap"] = Info("交换卡", CategoryHelpActive, "交换两张非玩家卡的位置。"),
        ["eg_help_watchtower"] = Info("瞭望塔", CategoryHelpActive, "点击消耗；棋盘/道具格伤害见被动。"),
        ["eg_help_multiplier_tower"] = Info("倍增塔", CategoryHelpActive, "点击消耗；格1/道具格双倍见被动。"),
        ["eg_help_bear_trap"] = Info("捕熊陷阱", CategoryHelpActive, "点击消耗；相邻补牌伤害见被动。"),
        ["eg_help_blood_convert"] = Info("血液转换", CategoryHelpActive, "扣5生命上限并随机获得奖励。"),

        ["eg_passive_healing_spring_adjacent"] = Info("治疗泉·相邻回血", CategoryHelpPassive, "移动到玩家正交相邻格时恢复2生命。"),
        ["eg_passive_healing_spring_combat"] = Info("治疗泉·战斗回血", CategoryHelpPassive, "处于道具牌格时，每次战斗恢复1生命。"),
        ["eg_passive_boulder_kill"] = Info("滚石·格3击杀", CategoryHelpPassive, "移动到格3时移除非精英格6怪物。"),
        ["eg_passive_bear_trap_refill"] = Info("捕熊陷阱·补牌触发", CategoryHelpPassive, "相邻格补入怪物时造成10点伤害。"),

        ["eg_relic_thorn_armor"] = Info("荆棘甲·反伤", CategoryRelic, "玩家攻击怪物时，对平行列怪物造成2点反伤。"),
        ["eg_relic_living_flesh_heal"] = Info("活肉·用卡回血", CategoryRelic, "使用帮助卡时恢复1点生命。"),

        ["eg_skill_thorn_skin_reflect"] = Info("刺皮·反伤", CategorySkill, "被攻击时按攻击者攻击力反伤。"),
        ["eg_skill_hard_skin_node_clear_heal"] = Info("硬皮·通关回血", CategorySkill, "关卡清除时恢复10点生命。"),
    };

    private static readonly Dictionary<string, string> CardNames = new Dictionary<string, string>
    {
        [GameConfigIds.HelpPotionId] = "恢复药水",
        [GameConfigIds.HelpThrowingKnifeId] = "飞刀",
        [GameConfigIds.HelpCommonChestId] = "普通宝箱卡",
        [GameConfigIds.HelpAttributeUpId] = "属性提升卡",
        [GameConfigIds.HelpGoldCardId] = "金币卡",
        [GameConfigIds.HelpChestCardId] = "宝箱卡",
        [GameConfigIds.HelpBlessingId] = "庇佑魔法卡",
        [GameConfigIds.HelpBandageId] = "绷带",
        [GameConfigIds.HelpBlueChestId] = "蓝色宝箱卡",
        [GameConfigIds.HelpGoldChestId] = "金色宝箱卡",
        [GameConfigIds.HelpFireballId] = "火球术",
        [GameConfigIds.HelpSpinWheelId] = "旋转轮",
        [GameConfigIds.HelpViolenceId] = "暴力卡",
        [GameConfigIds.HelpBoulderId] = "滚石",
        [GameConfigIds.HelpBombId] = "爆弹",
        [GameConfigIds.HelpSwapId] = "交换卡",
        [GameConfigIds.HelpSmasherId] = "破击锤",
        [GameConfigIds.HelpFoodId] = "食品卡",
        [GameConfigIds.HelpHealingSpringId] = "治疗泉",
        [GameConfigIds.HelpCrashTutorialId] = "撞击教程",
        [GameConfigIds.HelpWatchtowerId] = "瞭望塔",
        [GameConfigIds.HelpMultiplierTowerId] = "倍增塔",
        [GameConfigIds.HelpDurableShieldId] = "耐用盾牌",
        [GameConfigIds.HelpBearTrapId] = "捕熊陷阱",
        [GameConfigIds.HelpTeleportId] = "传送卡",
        [GameConfigIds.HelpBloodConvertId] = "血液转换",
        [GameConfigIds.HelpShieldStrikeTutorialId] = "盾击教程",
        [GameConfigIds.HelpKidnapId] = "绑票",
        [GameConfigIds.RelicThornArmorId] = "荆棘甲",
        [GameConfigIds.RelicLivingFleshId] = "活肉",
        [GameConfigIds.SkillThornSkinId] = "刺皮",
        [GameConfigIds.SkillHardSkinId] = "硬皮",
    };

    private static readonly Dictionary<string, (string name, string desc)> AtomTypes =
        new Dictionary<string, (string, string)>
        {
            [EffectAtomTypes.Damage] = ("造成伤害", "对指定范围目标结算伤害。"),
            [EffectAtomTypes.Heal] = ("治疗", "恢复目标生命值。"),
            [EffectAtomTypes.AddGold] = ("获得金币", "为玩家增加金币。"),
            [EffectAtomTypes.ModifyStat] = ("修改属性", "增减攻击/护甲/生命/当前护甲等。"),
            [EffectAtomTypes.ConsumeHelpCard] = ("消耗援助卡", "使用后从卡组永久移除（或按卡面规则）。"),
            [EffectAtomTypes.OpenChoiceOverlay] = ("打开选择界面", "弹出宝箱/属性提升等选项框。"),
            [EffectAtomTypes.OpenTargeting] = ("打开目标选择", "让玩家选择棋盘目标并执行模式逻辑。"),
            [EffectAtomTypes.AddCardToHelpDeck] = ("加入帮助卡组", "将指定卡加入帮助卡组。"),
            [EffectAtomTypes.InjectCardToBattleDeck] = ("注入战斗卡组", "将指定卡洗入战斗卡组。"),
            [EffectAtomTypes.MoveBoard] = ("棋盘移动", "旋转或移动棋盘布局。"),
            [EffectAtomTypes.SwapCards] = ("交换卡牌", "互换两张卡所在格子。"),
            [EffectAtomTypes.RemoveCard] = ("移除卡牌", "从棋盘或卡组移除卡牌。"),
            [EffectAtomTypes.ApplyStatus] = ("施加状态", "赋予祝福、暴力、塔状态等。"),
            [EffectAtomTypes.BloodConvertReward] = ("血液转换奖励", "随机发放攻击/护甲/金币/遗物。"),
            [EffectAtomTypes.ReflectParallelDamage] = ("平行列反伤", "对平行列其他目标反射伤害。"),
        };

    private static readonly Dictionary<string, (string label, string desc)> Parameters =
        new Dictionary<string, (string, string)>
        {
            ["amount"] = ("数值", "伤害、治疗或金币的具体数值。"),
            ["target"] = ("目标", "player=玩家，targets=已选目标等。"),
            ["full"] = ("回满", "true 时忽略 amount 并回满生命。"),
            ["scope"] = ("作用范围", "all_monsters / board_slot_monster / targets 等。"),
            ["causeId"] = ("来源标识", "日志与战斗归因用的配置 ID。"),
            ["stat"] = ("属性类型", "Attack / Armor / MaxHp / CurrentArmor 等。"),
            ["delta"] = ("变化量", "属性增减的数值。"),
            ["overlayKind"] = ("界面类型", "chest=宝箱，attribute=属性提升。"),
            ["chestTier"] = ("宝箱品质", "Normal / Blue / Gold。"),
            ["mode"] = ("交互模式", "damage / player_attack / teleport_to_deck 等。"),
            ["damage"] = ("伤害参数", "目标选择模式使用的伤害或护甲削减值。"),
            ["messageKey"] = ("提示文案键", "目标选择 UI 使用的说明文本键。"),
            ["status"] = ("状态标识", "blessing_shield / violence_attack / tower_watch 等。"),
            ["slot"] = ("棋盘格子", "1–9 的格子编号。"),
            ["damageType"] = ("伤害类型", "Relic / Skill / HelpCard 等归因分类。"),
            ["ignoreArmor"] = ("忽略护甲", "结算时是否无视护甲。"),
        };

    public static readonly string[] CategoryOrder =
    {
        CategoryHelpActive,
        CategoryHelpPassive,
        CategoryRelic,
        CategorySkill,
        CategoryOther
    };

    public struct GraphDisplayInfo
    {
        public string DisplayName;
        public string Category;
        public string Summary;
    }

    public static GraphDisplayInfo GetGraphInfo(string graphId)
    {
        if (!string.IsNullOrEmpty(graphId) && Graphs.TryGetValue(graphId, out var info))
        {
            return info;
        }

        return new GraphDisplayInfo
        {
            DisplayName = graphId ?? "未命名效果",
            Category = InferCategory(graphId),
            Summary = "未在设计目录中登记，请对照 Assets/Docs 补充。"
        };
    }

    public static string GetGraphDisplayName(string graphId) => GetGraphInfo(graphId).DisplayName;

    public static string GetGraphCategory(string graphId) => GetGraphInfo(graphId).Category;

    public static string GetGraphSummary(string graphId) => GetGraphInfo(graphId).Summary;

    public static string GetAtomDisplayName(string atomType)
    {
        if (string.IsNullOrEmpty(atomType))
        {
            return "未指定原子";
        }

        return AtomTypes.TryGetValue(atomType, out var pair) ? pair.name : atomType;
    }

    public static string GetAtomDescription(string atomType)
    {
        if (string.IsNullOrEmpty(atomType))
        {
            return string.Empty;
        }

        return AtomTypes.TryGetValue(atomType, out var pair) ? pair.desc : string.Empty;
    }

    public static (string label, string desc) GetParameterLabel(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return ("参数", string.Empty);
        }

        return Parameters.TryGetValue(key, out var pair) ? pair : (key, string.Empty);
    }

    public static string GetCardDisplayName(string cardId)
    {
        if (string.IsNullOrEmpty(cardId))
        {
            return "未指定卡牌";
        }

        if (CardNames.TryGetValue(cardId, out var name))
        {
            return name;
        }

        var fromAsset = TryResolveCardNameFromAsset(cardId);
        return string.IsNullOrEmpty(fromAsset) ? cardId : fromAsset;
    }

    private static string TryResolveCardNameFromAsset(string cardId)
    {
        var cardConfig = AssetDatabase.LoadAssetAtPath<TableNineCardConfig>(
            $"{TableNineGameConfigSync.ConfigFolderPath}/TableNineCardConfig.asset");
        if (cardConfig?.Cards == null)
        {
            return null;
        }

        for (var i = 0; i < cardConfig.Cards.Count; i++)
        {
            if (cardConfig.Cards[i].CardId == cardId)
            {
                return cardConfig.Cards[i].DisplayName;
            }
        }

        return null;
    }

    private static string InferCategory(string graphId)
    {
        if (string.IsNullOrEmpty(graphId))
        {
            return CategoryOther;
        }

        if (graphId.StartsWith("eg_help_")) return CategoryHelpActive;
        if (graphId.StartsWith("eg_passive_")) return CategoryHelpPassive;
        if (graphId.StartsWith("eg_relic_")) return CategoryRelic;
        if (graphId.StartsWith("eg_skill_")) return CategorySkill;
        return CategoryOther;
    }

    private static GraphDisplayInfo Info(string displayName, string category, string summary)
    {
        return new GraphDisplayInfo { DisplayName = displayName, Category = category, Summary = summary };
    }
}
#endif
