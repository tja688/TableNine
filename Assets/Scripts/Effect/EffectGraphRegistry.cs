using System.Collections.Generic;

public static class EffectGraphRegistry
{
    private static readonly Dictionary<string, EffectGraphDefinition> Graphs = new Dictionary<string, EffectGraphDefinition>();
    private static readonly Dictionary<string, string> CardToGraphId = new Dictionary<string, string>();

    static EffectGraphRegistry()
    {
        BuildGraphs();
        BuildCardMappings();
    }

    public static void AssignToConfig(GameConfigSet config)
    {
        for (var i = 0; i < config.Cards.Count; i++)
        {
            var card = config.Cards[i];
            if (card.CardType != CardType.Help)
            {
                continue;
            }

            if (CardToGraphId.TryGetValue(card.CardId, out var graphId))
            {
                card.EffectGraphId = graphId;
            }
        }
    }

    public static bool TryGetGraph(string effectGraphId, out EffectGraphDefinition graph)
    {
        return Graphs.TryGetValue(effectGraphId, out graph);
    }

    public static bool ContainsGraph(string effectGraphId)
    {
        return !string.IsNullOrEmpty(effectGraphId) && Graphs.ContainsKey(effectGraphId);
    }

    public static IReadOnlyList<EffectGraphDefinition> ExportAllGraphs()
    {
        var result = new List<EffectGraphDefinition>(Graphs.Count);
        foreach (var pair in Graphs)
        {
            result.Add(EffectAtomSerializationUtility.CloneGraph(pair.Value));
        }

        return result;
    }

    public static IReadOnlyList<HelpCardEffectMapping> ExportHelpCardMappings()
    {
        var result = new List<HelpCardEffectMapping>(CardToGraphId.Count);
        foreach (var pair in CardToGraphId)
        {
            result.Add(new HelpCardEffectMapping
            {
                CardId = pair.Key,
                EffectGraphId = pair.Value
            });
        }

        return result;
    }

    private static void BuildCardMappings()
    {
        Map(GameConfigIds.HelpPotionId, "eg_help_potion");
        Map(GameConfigIds.HelpThrowingKnifeId, "eg_help_throwing_knife");
        Map(GameConfigIds.HelpCommonChestId, "eg_help_common_chest");
        Map(GameConfigIds.HelpAttributeUpId, "eg_help_attribute_up");
        Map(GameConfigIds.HelpGoldCardId, "eg_help_gold_card");
        Map(GameConfigIds.HelpChestCardId, "eg_help_chest_card");
        Map(GameConfigIds.HelpBlessingId, "eg_help_blessing");
        Map(GameConfigIds.HelpBandageId, "eg_help_bandage");
        Map(GameConfigIds.HelpBlueChestId, "eg_help_blue_chest");
        Map(GameConfigIds.HelpGoldChestId, "eg_help_gold_chest");
        Map(GameConfigIds.HelpFireballId, "eg_help_fireball");
        Map(GameConfigIds.HelpSpinWheelId, "eg_help_spin_wheel");
        Map(GameConfigIds.HelpViolenceId, "eg_help_violence");
        Map(GameConfigIds.HelpBoulderId, "eg_help_boulder");
        Map(GameConfigIds.HelpBombId, "eg_help_bomb");
        Map(GameConfigIds.HelpSwapId, "eg_help_swap");
        Map(GameConfigIds.HelpSmasherId, "eg_help_smasher");
        Map(GameConfigIds.HelpFoodId, "eg_help_food");
        Map(GameConfigIds.HelpHealingSpringId, "eg_help_healing_spring");
        Map(GameConfigIds.HelpCrashTutorialId, "eg_help_crash_tutorial");
        Map(GameConfigIds.HelpWatchtowerId, "eg_help_watchtower");
        Map(GameConfigIds.HelpMultiplierTowerId, "eg_help_multiplier_tower");
        Map(GameConfigIds.HelpDurableShieldId, "eg_help_durable_shield");
        Map(GameConfigIds.HelpShieldStrikeTutorialId, "eg_help_shield_strike_tutorial");
        Map(GameConfigIds.HelpTeleportId, "eg_help_teleport");
        Map(GameConfigIds.HelpKidnapId, "eg_help_kidnap");
        Map(GameConfigIds.HelpBearTrapId, "eg_help_bear_trap");
        Map(GameConfigIds.HelpBloodConvertId, "eg_help_blood_convert");
    }

    private static void Map(string cardId, string graphId)
    {
        CardToGraphId[cardId] = graphId;
    }

    private static void BuildGraphs()
    {
        Register("eg_help_potion",
            Heal(10),
            Consume());

        Register("eg_help_throwing_knife",
            Targeting(6, GameConfigIds.HelpThrowingKnifeId, DescriptionPanelTextKeys.MsgThrowingKnifeSelect));

        Register("eg_help_fireball",
            TargetingPlayerAttack(GameConfigIds.HelpFireballId, DescriptionPanelTextKeys.MsgThrowingKnifeSelect));

        Register("eg_help_attribute_up",
            Overlay("attribute"));

        Register("eg_help_common_chest",
            Overlay("chest", ChestTier.Normal),
            Consume());

        Register("eg_help_chest_card",
            Overlay("chest", ChestTier.Normal),
            Consume());

        Register("eg_help_blue_chest",
            Overlay("chest", ChestTier.Blue),
            Consume());

        Register("eg_help_gold_chest",
            Overlay("chest", ChestTier.Gold),
            Consume());

        Register("eg_help_gold_card",
            Gold(50),
            Consume());

        Register("eg_help_blessing",
            Status("blessing_shield"),
            Consume());

        Register("eg_help_bandage",
            Heal(5),
            Consume());

        Register("eg_help_food",
            HealFull(),
            Consume());

        Register("eg_help_healing_spring",
            Consume());

        Register("eg_help_bomb",
            Damage(4, "all_monsters", GameConfigIds.HelpBombId),
            Consume());

        Register("eg_help_spin_wheel",
            Move("rotate_counterclockwise"),
            Consume());

        Register("eg_help_crash_tutorial",
            TargetingPlayerCurrentHp(GameConfigIds.HelpCrashTutorialId, DescriptionPanelTextKeys.MsgThrowingKnifeSelect));

        Register("eg_help_boulder",
            Consume());

        Register("eg_help_smasher",
            TargetingReduceArmor(10, GameConfigIds.HelpSmasherId, DescriptionPanelTextKeys.MsgThrowingKnifeSelect));

        Register("eg_help_durable_shield",
            ModifyArmor(5),
            Consume());

        Register("eg_help_shield_strike_tutorial",
            TargetingPlayerCurrentArmor(
                GameConfigIds.HelpShieldStrikeTutorialId,
                DescriptionPanelTextKeys.MsgThrowingKnifeSelect));

        Register("eg_help_teleport",
            Targeting(0, GameConfigIds.HelpTeleportId, DescriptionPanelTextKeys.MsgThrowingKnifeSelect, "teleport_to_deck"));

        Register("eg_help_kidnap",
            Targeting(0, GameConfigIds.HelpKidnapId, DescriptionPanelTextKeys.MsgThrowingKnifeSelect, "kidnap"));

        Register("eg_help_violence",
            Status("violence_attack"),
            Consume());

        Register("eg_help_swap",
            Targeting(0, GameConfigIds.HelpSwapId, DescriptionPanelTextKeys.MsgThrowingKnifeSelect, "swap"));

        Register("eg_help_watchtower",
            Status("tower_watch"),
            Consume());

        Register("eg_help_multiplier_tower",
            Status("tower_multiplier"),
            Consume());

        Register("eg_help_bear_trap",
            Consume());

        Register("eg_help_blood_convert",
            Modify(StatType.MaxHp, -5),
            Atom(EffectAtomTypes.BloodConvertReward),
            Consume());

        Register("eg_relic_thorn_armor",
            Atom(EffectAtomTypes.ReflectParallelDamage,
                ("mode", "fixed"),
                ("amount", CombatConstants.ThornArmorDamage.ToString()),
                ("causeId", CombatConstants.CauseThornArmor),
                ("damageType", DamageType.Relic.ToString())));

        Register("eg_skill_thorn_skin_reflect",
            Atom(EffectAtomTypes.ReflectParallelDamage,
                ("mode", "attacker_attack"),
                ("causeId", CombatConstants.CauseThornSkin),
                ("damageType", DamageType.Skill.ToString())));
    }

    private static void Register(string graphId, params EffectAtomDefinition[] atoms)
    {
        Graphs[graphId] = new EffectGraphDefinition
        {
            EffectGraphId = graphId,
            Atoms = new List<EffectAtomDefinition>(atoms)
        };
    }

    private static EffectAtomDefinition Atom(string type, params (string key, string value)[] parameters)
    {
        var atom = new EffectAtomDefinition { AtomType = type };
        for (var i = 0; i < parameters.Length; i++)
        {
            EffectAtomSerializationUtility.SetParameter(atom, parameters[i].key, parameters[i].value);
        }

        return atom;
    }

    private static EffectAtomDefinition Heal(int amount)
    {
        return Atom(EffectAtomTypes.Heal, ("amount", amount.ToString()), ("target", "player"));
    }

    private static EffectAtomDefinition HealFull()
    {
        return Atom(EffectAtomTypes.Heal, ("amount", "0"), ("target", "player"), ("full", "true"));
    }

    private static EffectAtomDefinition Gold(int amount)
    {
        return Atom(EffectAtomTypes.AddGold, ("amount", amount.ToString()));
    }

    private static EffectAtomDefinition Consume()
    {
        return Atom(EffectAtomTypes.ConsumeHelpCard);
    }

    private static EffectAtomDefinition Modify(StatType stat, int delta)
    {
        return Atom(EffectAtomTypes.ModifyStat,
            ("stat", stat.ToString()),
            ("delta", delta.ToString()),
            ("target", "player"));
    }

    private static EffectAtomDefinition Damage(int amount, string scope, string causeId)
    {
        return Atom(EffectAtomTypes.Damage,
            ("amount", amount.ToString()),
            ("scope", scope),
            ("causeId", causeId));
    }

    private static EffectAtomDefinition Overlay(string overlayKind, ChestTier chestTier = ChestTier.Normal)
    {
        return Atom(EffectAtomTypes.OpenChoiceOverlay,
            ("overlayKind", overlayKind),
            ("chestTier", chestTier.ToString()));
    }

    private static EffectAtomDefinition Targeting(int damage, string causeId, string messageKey, string mode = "damage")
    {
        return Atom(EffectAtomTypes.OpenTargeting,
            ("mode", mode),
            ("damage", damage.ToString()),
            ("causeId", causeId),
            ("messageKey", messageKey));
    }

    private static EffectAtomDefinition TargetingPlayerAttack(string causeId, string messageKey)
    {
        return Atom(EffectAtomTypes.OpenTargeting,
            ("mode", "player_attack"),
            ("damage", "0"),
            ("causeId", causeId),
            ("messageKey", messageKey));
    }

    private static EffectAtomDefinition TargetingPlayerCurrentHp(string causeId, string messageKey)
    {
        return Atom(EffectAtomTypes.OpenTargeting,
            ("mode", "player_current_hp"),
            ("damage", "0"),
            ("causeId", causeId),
            ("messageKey", messageKey));
    }

    private static EffectAtomDefinition TargetingReduceArmor(int armorReduction, string causeId, string messageKey)
    {
        return Atom(EffectAtomTypes.OpenTargeting,
            ("mode", "reduce_armor"),
            ("damage", armorReduction.ToString()),
            ("causeId", causeId),
            ("messageKey", messageKey));
    }

    private static EffectAtomDefinition TargetingPlayerCurrentArmor(string causeId, string messageKey)
    {
        return Atom(EffectAtomTypes.OpenTargeting,
            ("mode", "player_current_armor"),
            ("damage", "0"),
            ("causeId", causeId),
            ("messageKey", messageKey));
    }

    private static EffectAtomDefinition ModifyArmor(int delta)
    {
        return Atom(EffectAtomTypes.ModifyStat,
            ("stat", nameof(StatType.Armor)),
            ("delta", delta.ToString()),
            ("target", "player"));
    }

    private static EffectAtomDefinition Move(string mode)
    {
        return Atom(EffectAtomTypes.MoveBoard, ("mode", mode));
    }

    private static EffectAtomDefinition Status(string status)
    {
        return Atom(EffectAtomTypes.ApplyStatus, ("status", status));
    }
}
