using System.Collections.Generic;

public static class SkillEffectRegistry
{
    private static readonly List<SkillEffectBinding> Bindings = new List<SkillEffectBinding>();

    static SkillEffectRegistry()
    {
        BuildBindings();
        BuildPassiveEffectGraphs();
    }

    public static IReadOnlyList<SkillEffectBinding> AllBindings => Bindings;

    private static void BuildBindings()
    {
        BindRelic(DefaultGameConfigFactory.RelicThornArmorId, SkillTrigger.OnModifyDamage, "first_hit_player_attacks_monster", "eg_relic_thorn_armor");
        BindRelic(DefaultGameConfigFactory.RelicLivingFleshId, SkillTrigger.OnHelpCardUsed, "always", "eg_relic_living_flesh_heal");

        BindHelpPassive(DefaultGameConfigFactory.HelpHealingSpringId, SkillTrigger.OnCardMoved, "moved_to_adjacent_player", "eg_passive_healing_spring_adjacent");
        BindHelpPassive(DefaultGameConfigFactory.HelpHealingSpringId, SkillTrigger.OnAfterCombat, "in_item_slot", "eg_passive_healing_spring_combat");
        BindHelpPassive(DefaultGameConfigFactory.HelpBoulderId, SkillTrigger.OnCardMoved, "moved_to_slot_3_killable", "eg_passive_boulder_kill");
        BindHelpPassive(DefaultGameConfigFactory.HelpBearTrapId, SkillTrigger.OnCardMoved, "refilled_monster_adjacent_to_player", "eg_passive_bear_trap_refill");
    }

    private static void BuildPassiveEffectGraphs()
    {
        PassiveGraphs.Register("eg_passive_healing_spring_adjacent",
            PassiveGraphs.Heal(2));
        PassiveGraphs.Register("eg_passive_healing_spring_combat",
            PassiveGraphs.Heal(1));
        PassiveGraphs.Register("eg_relic_living_flesh_heal",
            PassiveGraphs.Heal(1));
        PassiveGraphs.Register("eg_passive_boulder_kill",
            PassiveGraphs.RemoveBoardSlotMonster(6),
            PassiveGraphs.ConsumeCaster());
        PassiveGraphs.Register("eg_passive_bear_trap_refill",
            PassiveGraphs.Damage(10, DefaultGameConfigFactory.HelpBearTrapId),
            PassiveGraphs.ConsumeCaster());
        PassiveGraphs.Register("eg_skill_hard_skin_node_clear_heal",
            PassiveGraphs.Heal(10));
    }

    public static bool IsKnownEffectGraph(string graphId)
    {
        return graphId == "eg_relic_thorn_armor" ||
               graphId == "eg_skill_thorn_skin_reflect" ||
               PassiveGraphs.TryGetGraph(graphId, out _);
    }

    private static void BindRelic(string relicId, SkillTrigger trigger, string condition, string graphId)
    {
        Bindings.Add(new SkillEffectBinding
        {
            BindingId = $"relic_{relicId}_{trigger}",
            OwnerKind = SkillOwnerKind.Relic,
            OwnerDefinitionId = relicId,
            Trigger = trigger,
            ConditionKey = condition,
            EffectGraphId = graphId
        });
    }

    private static void BindPlayerSkill(string skillId, SkillTrigger trigger, string condition, string graphId)
    {
        Bindings.Add(new SkillEffectBinding
        {
            BindingId = $"player_skill_{skillId}_{trigger}",
            OwnerKind = SkillOwnerKind.PlayerSkill,
            OwnerDefinitionId = skillId,
            Trigger = trigger,
            ConditionKey = condition,
            EffectGraphId = graphId
        });
    }

    private static void BindHelpPassive(string helpCardId, SkillTrigger trigger, string condition, string graphId)
    {
        Bindings.Add(new SkillEffectBinding
        {
            BindingId = $"help_{helpCardId}_{trigger}",
            OwnerKind = SkillOwnerKind.HelpCardPassive,
            OwnerDefinitionId = helpCardId,
            Trigger = trigger,
            ConditionKey = condition,
            EffectGraphId = graphId
        });
    }
}

internal static class PassiveGraphs
{
    private static readonly Dictionary<string, EffectGraphDefinition> Graphs = new Dictionary<string, EffectGraphDefinition>();

    public static void Register(string graphId, params EffectAtomDefinition[] atoms)
    {
        Graphs[graphId] = new EffectGraphDefinition
        {
            EffectGraphId = graphId,
            Atoms = new List<EffectAtomDefinition>(atoms)
        };
    }

    public static bool TryGetGraph(string graphId, out EffectGraphDefinition graph)
    {
        return Graphs.TryGetValue(graphId, out graph);
    }

    public static EffectAtomDefinition Heal(int amount)
    {
        return Atom(EffectAtomTypes.Heal, ("amount", amount.ToString()), ("target", "player"));
    }

    public static EffectAtomDefinition ConsumeCaster()
    {
        return Atom(EffectAtomTypes.ConsumeHelpCard);
    }

    public static EffectAtomDefinition Damage(int amount, string causeId)
    {
        return Atom(EffectAtomTypes.Damage,
            ("amount", amount.ToString()),
            ("scope", "targets"),
            ("causeId", causeId));
    }

    public static EffectAtomDefinition RemoveBoardSlotMonster(int slot)
    {
        return Atom(EffectAtomTypes.RemoveCard,
            ("scope", "board_slot_monster"),
            ("slot", slot.ToString()));
    }

    private static EffectAtomDefinition Atom(string type, params (string key, string value)[] parameters)
    {
        var atom = new EffectAtomDefinition { AtomType = type };
        for (var i = 0; i < parameters.Length; i++)
        {
            atom.Parameters[parameters[i].key] = parameters[i].value;
        }

        return atom;
    }
}
