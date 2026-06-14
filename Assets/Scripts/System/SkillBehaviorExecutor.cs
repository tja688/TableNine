using System.Collections.Generic;
using QFramework;
using UnityEngine;

public static class SkillBehaviorExecutor
{
    private static readonly int[] AmbushSlots = { 2, 4, 6, 8 };
    private static readonly int[] LeftColumnSlots = { 1, 4, 7 };
    private static readonly int[] TopRowSlots = { 1, 2, 3 };

    public static void ApplyEffectiveStatRules(
        CardRuntime runtime,
        BoardSlotNo? boardSlot,
        IBoardModel boardModel,
        ICollectionModel collectionModel,
        IReadOnlyList<SkillBehaviorRule> rules,
        ref int attack,
        ref int defense,
        ref int damageReduction,
        ref bool hasFirstStrike,
        IConfigModel configModel)
    {
        for (var i = 0; i < rules.Count; i++)
        {
            var rule = rules[i];
            if (rule.Trigger != SkillTrigger.OnComputeEffectiveStats || !runtime.HasSkill(rule.SkillId))
            {
                continue;
            }

            switch (rule.BehaviorKind)
            {
                case SkillBehaviorKind.EffectiveStatAttackBonus:
                    if (boardSlot.HasValue && MatchesSlot(boardSlot.Value, rule))
                    {
                        attack += rule.IntValue;
                    }

                    break;
                case SkillBehaviorKind.EffectiveStatArmorBonus:
                    if (boardSlot.HasValue && MatchesSlot(boardSlot.Value, rule))
                    {
                        defense += rule.IntValue;
                    }

                    break;
                case SkillBehaviorKind.EffectiveStatDamageReduction:
                    damageReduction += rule.IntValue;
                    break;
                case SkillBehaviorKind.EffectiveStatGrantFirstStrike:
                    if (boardSlot.HasValue && MatchesSlot(boardSlot.Value, rule))
                    {
                        hasFirstStrike = true;
                    }

                    break;
                case SkillBehaviorKind.AuraAttackBonusFromOtherMonster:
                    if (!runtime.HasSkill(rule.AuraSourceSkillId) &&
                        HasAuraMonster(boardModel, collectionModel, rule.AuraSourceSkillId, TopRowSlots))
                    {
                        attack += rule.IntValue;
                    }

                    break;
                case SkillBehaviorKind.AuraArmorBonusFromOtherMonster:
                    if (!runtime.HasSkill(rule.AuraSourceSkillId) &&
                        HasAuraMonster(boardModel, collectionModel, rule.AuraSourceSkillId, LeftColumnSlots))
                    {
                        defense += rule.IntValue;
                    }

                    break;
            }
        }

        for (var i = 0; i < runtime.SkillIds.Count; i++)
        {
            if (configModel.GetSkillDefinition(runtime.SkillIds[i]).GrantsFirstStrike)
            {
                hasFirstStrike = true;
            }
        }
    }

    public static void HandleEvent(
        SkillTrigger trigger,
        IReadOnlyList<SkillBehaviorRule> rules,
        CardRuntime runtime,
        IArchitecture architecture,
        BoardSlotNo? slot = null,
        BoardSlotNo? previousSlot = null,
        CardUid? killedMonsterUid = null,
        CombatContext combatContext = null)
    {
        if (runtime == null || runtime.CardType != CardType.Monster)
        {
            return;
        }

        for (var i = 0; i < rules.Count; i++)
        {
            var rule = rules[i];
            if (rule.Trigger != trigger || !runtime.HasSkill(rule.SkillId))
            {
                continue;
            }

            if (!MatchesEventCondition(rule, runtime, slot, previousSlot, killedMonsterUid, combatContext))
            {
                continue;
            }

            ExecuteRule(rule, runtime, architecture, slot, combatContext);
        }
    }

    private static void ExecuteRule(
        SkillBehaviorRule rule,
        CardRuntime runtime,
        IArchitecture architecture,
        BoardSlotNo? slot,
        CombatContext combatContext)
    {
        switch (rule.BehaviorKind)
        {
            case SkillBehaviorKind.DirectDamageOnPlaced:
                var playerModel = architecture.GetModel<IPlayerModel>();
                architecture.SendCommand(new ApplyDamageCommand(new DamageContext
                {
                    Target = playerModel.PlayerCardUid,
                    CauseId = rule.CauseId,
                    Type = DamageType.Skill,
                    RawAttack = rule.IntValue,
                    DamageBeforeArmor = rule.IntValue,
                    IgnoreArmor = rule.IgnoreArmor
                }));
                break;
            case SkillBehaviorKind.StatDeltaOnMoved:
                architecture.SendCommand(new ApplyStatChangeCommand(
                    runtime.Uid,
                    rule.StatType,
                    rule.IntValue,
                    rule.CauseId));
                break;
            case SkillBehaviorKind.ReducePlayerArmorOnMoved:
                var player = architecture.GetModel<IPlayerModel>();
                architecture.SendCommand(new ChangeArmorCommand(
                    player.PlayerCardUid,
                    -rule.IntValue,
                    rule.CauseId));
                break;
            case SkillBehaviorKind.HealAllBoardMonstersOnMoved:
                HealAllBoardMonsters(architecture, rule.IntValue, rule.CauseId);
                break;
            case SkillBehaviorKind.StatDeltaOnMonsterKilled:
                architecture.SendCommand(new ApplyStatChangeCommand(
                    runtime.Uid,
                    rule.StatType,
                    rule.IntValue,
                    rule.CauseId));
                break;
            case SkillBehaviorKind.ReflectCombatArmorLoss:
                if (combatContext == null)
                {
                    return;
                }

                var armorLost = SumArmorAbsorbed(combatContext, runtime.Uid);
                if (armorLost > 0)
                {
                    architecture.SendCommand(new ApplyDamageCommand(
                        combatContext.PlayerUid,
                        armorLost));
                }

                break;
            case SkillBehaviorKind.HealSelfOnCombatAfterResolved:
                if (combatContext == null || runtime.CurrentHp <= 0)
                {
                    return;
                }

                architecture.SendCommand(new ApplyEffectHealCommand(
                    runtime.Uid,
                    rule.IntValue,
                    false));
                break;
        }
    }

    private static bool MatchesEventCondition(
        SkillBehaviorRule rule,
        CardRuntime runtime,
        BoardSlotNo? slot,
        BoardSlotNo? previousSlot,
        CardUid? killedMonsterUid,
        CombatContext combatContext)
    {
        switch (rule.ConditionKey)
        {
            case SkillBehaviorConditionKeys.Always:
                return true;
            case SkillBehaviorConditionKeys.AtSlot:
                return slot.HasValue && slot.Value.Value == rule.IntValue2;
            case SkillBehaviorConditionKeys.AtAmbushSlot:
                return slot.HasValue && IsAmbushSlot(slot.Value);
            case SkillBehaviorConditionKeys.OnTopRow:
                return slot.HasValue && IsTopRowSlot(slot.Value);
            case SkillBehaviorConditionKeys.OnBottomRow:
                return slot.HasValue && BoardSlotUtility.IsBottomRow(slot.Value);
            case SkillBehaviorConditionKeys.OnLeftColumn:
                return slot.HasValue && IsLeftColumnSlot(slot.Value);
            default:
                return false;
        }
    }

    private static bool MatchesSlot(BoardSlotNo slot, SkillBehaviorRule rule)
    {
        return rule.ConditionKey == SkillBehaviorConditionKeys.AtSlot && slot.Value == rule.IntValue2;
    }

    private static bool HasAuraMonster(
        IBoardModel boardModel,
        ICollectionModel collectionModel,
        string auraSkillId,
        int[] slots)
    {
        for (var i = 0; i < slots.Length; i++)
        {
            var slot = new BoardSlotNo(slots[i]);
            var uid = boardModel.GetCardAt(slot);
            if (!uid.HasValue)
            {
                continue;
            }

            if (collectionModel.TryGetCard(uid.Value, out var auraRuntime) &&
                auraRuntime.CardType == CardType.Monster &&
                auraRuntime.HasSkill(auraSkillId))
            {
                return true;
            }
        }

        return false;
    }

    private static void HealAllBoardMonsters(
        IArchitecture architecture,
        int amount,
        string causeId)
    {
        var boardModel = architecture.GetModel<IBoardModel>();
        var collectionModel = architecture.GetModel<ICollectionModel>();
        for (var i = 0; i < BoardSlotUtility.AllSlots.Length; i++)
        {
            var slot = BoardSlotUtility.AllSlots[i];
            var uid = boardModel.GetCardAt(slot);
            if (!uid.HasValue)
            {
                continue;
            }

            if (!collectionModel.TryGetCard(uid.Value, out var monster) || monster.CardType != CardType.Monster)
            {
                continue;
            }

            architecture.SendCommand(new ApplyEffectHealCommand(uid.Value, amount, false));
        }
    }

    private static int SumArmorAbsorbed(CombatContext context, CardUid monsterUid)
    {
        var total = 0;
        total += SumGroup(context.FirstHitGroup, monsterUid);
        total += SumGroup(context.CounterHitGroup, monsterUid);
        return total;
    }

    private static int SumGroup(IReadOnlyList<DamageContext> group, CardUid monsterUid)
    {
        var total = 0;
        for (var i = 0; i < group.Count; i++)
        {
            var damage = group[i];
            if (damage.Target.Equals(monsterUid))
            {
                total += damage.ArmorAbsorbed;
            }
        }

        return total;
    }

    private static bool IsAmbushSlot(BoardSlotNo slot)
    {
        for (var i = 0; i < AmbushSlots.Length; i++)
        {
            if (AmbushSlots[i] == slot.Value)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsTopRowSlot(BoardSlotNo slot)
    {
        return BoardSlotUtility.IsTopRow(slot);
    }

    private static bool IsLeftColumnSlot(BoardSlotNo slot)
    {
        for (var i = 0; i < LeftColumnSlots.Length; i++)
        {
            if (LeftColumnSlots[i] == slot.Value)
            {
                return true;
            }
        }

        return false;
    }
}
