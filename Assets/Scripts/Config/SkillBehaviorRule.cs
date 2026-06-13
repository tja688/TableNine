using System;
using System.Collections.Generic;

public enum SkillBehaviorKind
{
    EffectiveStatAttackBonus,
    EffectiveStatDefenseBonus,
    EffectiveStatDamageReduction,
    EffectiveStatGrantFirstStrike,
    AuraAttackBonusFromOtherMonster,
    AuraDefenseBonusFromOtherMonster,
    DirectDamageOnPlaced,
    StatDeltaOnMoved,
    ReducePlayerArmorOnMoved,
    HealAllBoardMonstersOnMoved,
    StatDeltaOnMonsterKilled,
    ReflectCombatArmorLoss,
    HealSelfOnCombatAfterResolved
}

[Serializable]
public sealed class SkillBehaviorRule
{
    public string RuleId;
    public string SkillId;
    public SkillTrigger Trigger;
    public string ConditionKey;
    public SkillBehaviorKind BehaviorKind;
    public int IntValue;
    public int IntValue2;
    public StatType StatType;
    public string CauseId;
    public bool IgnoreArmor;
    public string AuraSourceSkillId;
}

public static class SkillBehaviorConditionKeys
{
    public const string Always = "always";
    public const string AtSlot = "at_slot";
    public const string AtAmbushSlot = "at_ambush_slot";
    public const string OnTopRow = "on_top_row";
    public const string OnBottomRow = "on_bottom_row";
    public const string OnLeftColumn = "on_left_column";
    public const string AuraClubCubTopRow = "aura_club_cub_top_row";
    public const string AuraProtectionLeftColumn = "aura_protection_left_column";
    public const string ExcludeSelfSkill = "exclude_self_skill";
}

public static class SkillBehaviorRuleDefaults
{
    public static List<SkillBehaviorRule> Build()
    {
        return new List<SkillBehaviorRule>
        {
            Rule("spade_cub_attack", GameConfigIds.SkillSpadeCubId, SkillBehaviorKind.EffectiveStatAttackBonus,
                SkillBehaviorConditionKeys.AtSlot, slot: 6, amount: 2),
            Rule("spade_cub_first_strike", GameConfigIds.SkillSpadeCubId, SkillBehaviorKind.EffectiveStatGrantFirstStrike,
                SkillBehaviorConditionKeys.AtSlot, slot: 6),
            Rule("club_cub_aura_attack", GameConfigIds.SkillClubCubId, SkillBehaviorKind.AuraAttackBonusFromOtherMonster,
                SkillBehaviorConditionKeys.AuraClubCubTopRow, amount: 2, auraSource: GameConfigIds.SkillClubCubId),
            Rule("protection_aura_defense", GameConfigIds.SkillProtectionAuraId, SkillBehaviorKind.AuraDefenseBonusFromOtherMonster,
                SkillBehaviorConditionKeys.AuraProtectionLeftColumn, amount: 2, auraSource: GameConfigIds.SkillProtectionAuraId),
            Rule("hard_skin_reduction", GameConfigIds.SkillHardSkinId, SkillBehaviorKind.EffectiveStatDamageReduction,
                SkillBehaviorConditionKeys.Always, amount: 1),

            Rule("ambush_damage", GameConfigIds.SkillAmbushId, SkillBehaviorKind.DirectDamageOnPlaced,
                SkillBehaviorConditionKeys.AtAmbushSlot, amount: 3, causeId: GameConfigIds.SkillAmbushId, ignoreArmor: true),
            Rule("heart_cub_max_hp", GameConfigIds.SkillHeartCubId, SkillBehaviorKind.StatDeltaOnMoved,
                SkillBehaviorConditionKeys.AtSlot, slot: 8, amount: 2, stat: StatType.MaxHp, causeId: "skill_heart_cub"),
            Rule("diamond_cub_defense", GameConfigIds.SkillDiamondCubId, SkillBehaviorKind.StatDeltaOnMoved,
                SkillBehaviorConditionKeys.AtSlot, slot: 4, amount: 2, stat: StatType.Defense, causeId: "skill_diamond_cub"),
            Rule("armor_breaker", GameConfigIds.SkillArmorBreakerId, SkillBehaviorKind.ReducePlayerArmorOnMoved,
                SkillBehaviorConditionKeys.OnTopRow, amount: 2, causeId: "skill_armor_breaker"),
            Rule("medic_heal", GameConfigIds.SkillMedicId, SkillBehaviorKind.HealAllBoardMonstersOnMoved,
                SkillBehaviorConditionKeys.OnBottomRow, amount: 4, causeId: "skill_medic"),
            Rule("revenge_attack", GameConfigIds.SkillRevengeId, SkillBehaviorKind.StatDeltaOnMonsterKilled,
                SkillBehaviorConditionKeys.Always, amount: 2, stat: StatType.Attack, causeId: "skill_revenge"),
            Rule("sharp_shield_reflect", GameConfigIds.SkillSharpShieldId, SkillBehaviorKind.ReflectCombatArmorLoss,
                SkillBehaviorConditionKeys.OnLeftColumn),
            Rule("loving_body_heal", GameConfigIds.SkillLovingBodyId, SkillBehaviorKind.HealSelfOnCombatAfterResolved,
                SkillBehaviorConditionKeys.OnBottomRow, amount: 1)
        };
    }

    private static SkillBehaviorRule Rule(
        string ruleId,
        string skillId,
        SkillBehaviorKind kind,
        string conditionKey,
        int slot = 0,
        int amount = 0,
        StatType stat = StatType.Attack,
        string causeId = null,
        bool ignoreArmor = false,
        string auraSource = null)
    {
        return new SkillBehaviorRule
        {
            RuleId = ruleId,
            SkillId = skillId,
            Trigger = MapTrigger(kind),
            ConditionKey = conditionKey,
            BehaviorKind = kind,
            IntValue = amount,
            IntValue2 = slot,
            StatType = stat,
            CauseId = causeId ?? skillId,
            IgnoreArmor = ignoreArmor,
            AuraSourceSkillId = auraSource
        };
    }

    private static SkillTrigger MapTrigger(SkillBehaviorKind kind)
    {
        switch (kind)
        {
            case SkillBehaviorKind.DirectDamageOnPlaced:
                return SkillTrigger.OnCardPlaced;
            case SkillBehaviorKind.StatDeltaOnMoved:
            case SkillBehaviorKind.ReducePlayerArmorOnMoved:
            case SkillBehaviorKind.HealAllBoardMonstersOnMoved:
                return SkillTrigger.OnCardMoved;
            case SkillBehaviorKind.StatDeltaOnMonsterKilled:
                return SkillTrigger.OnMonsterKilled;
            case SkillBehaviorKind.ReflectCombatArmorLoss:
            case SkillBehaviorKind.HealSelfOnCombatAfterResolved:
                return SkillTrigger.OnAfterCombat;
            default:
                return SkillTrigger.OnComputeEffectiveStats;
        }
    }
}
