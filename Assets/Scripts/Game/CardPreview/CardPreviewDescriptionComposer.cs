using System.Collections.Generic;

public static class CardPreviewDescriptionComposer
{
    public static string Compose(IConfigModel config, CardDefinition card)
    {
        if (config == null || card == null)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        if (card.CardType == CardType.Tutor)
        {
            AppendTutorSkillDescriptions(config, card, parts);
        }
        else
        {
            AppendIfPresent(parts, card.Description);

            if (card.CardType == CardType.Monster)
            {
                AppendMonsterSkillDescriptions(config, card, parts);
            }
            else if (card.CardType == CardType.Help)
            {
                AppendHelpEffectDescriptions(config, card, parts);
            }
        }

        return DescriptionPanelTextRules.FormatDisplayLines(parts);
    }

    private static void AppendTutorSkillDescriptions(IConfigModel config, CardDefinition card, List<string> parts)
    {
        if (card.SkillIds == null)
        {
            return;
        }

        for (var i = 0; i < card.SkillIds.Count; i++)
        {
            var skill = config.GetSkillDefinition(card.SkillIds[i]);
            AppendIfPresent(parts, skill?.Description);
        }
    }

    private static void AppendMonsterSkillDescriptions(IConfigModel config, CardDefinition card, List<string> parts)
    {
        if (card.SkillIds == null)
        {
            return;
        }

        for (var i = 0; i < card.SkillIds.Count; i++)
        {
            var skill = config.GetSkillDefinition(card.SkillIds[i]);
            AppendIfPresent(parts, skill?.Description);
        }
    }

    private static void AppendHelpEffectDescriptions(IConfigModel config, CardDefinition card, List<string> parts)
    {
        if (!string.IsNullOrWhiteSpace(card.EffectGraphId)
            && config.TryGetEffectGraph(card.EffectGraphId, out var activeGraph))
        {
            AppendIfPresent(parts, SummarizeEffectGraph(activeGraph));
        }

        var passiveBindings = SkillEffectRegistry.AllBindings;
        for (var i = 0; i < passiveBindings.Count; i++)
        {
            var binding = passiveBindings[i];
            if (binding.OwnerKind != SkillOwnerKind.HelpCardPassive
                || binding.OwnerDefinitionId != card.CardId
                || string.IsNullOrWhiteSpace(binding.EffectGraphId))
            {
                continue;
            }

            if (!config.TryGetEffectGraph(binding.EffectGraphId, out var passiveGraph))
            {
                continue;
            }

            AppendIfPresent(parts, SummarizeEffectGraph(passiveGraph));
        }
    }

    private static string SummarizeEffectGraph(EffectGraphDefinition graph)
    {
        if (graph?.Atoms == null || graph.Atoms.Count == 0)
        {
            return string.Empty;
        }

        var builder = new System.Text.StringBuilder();
        for (var i = 0; i < graph.Atoms.Count; i++)
        {
            var summary = SummarizeAtom(graph.Atoms[i]);
            if (string.IsNullOrWhiteSpace(summary))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(summary);
        }

        return builder.ToString();
    }

    private static string SummarizeAtom(EffectAtomDefinition atom)
    {
        if (atom == null || string.IsNullOrWhiteSpace(atom.AtomType))
        {
            return string.Empty;
        }

        switch (atom.AtomType)
        {
            case EffectAtomTypes.Heal:
                if (EffectAtomParams.GetBool(atom, "full"))
                {
                    return "回满生命";
                }

                var healAmount = EffectAtomParams.GetInt(atom, "amount", 0);
                return healAmount > 0 ? $"恢复{healAmount}生命" : string.Empty;
            case EffectAtomTypes.Damage:
                var damageAmount = EffectAtomParams.GetInt(atom, "amount", 0);
                return damageAmount > 0 ? $"造成{damageAmount}伤害" : string.Empty;
            case EffectAtomTypes.ModifyStat:
                return SummarizeModifyStat(atom);
            case EffectAtomTypes.AddGold:
                var gold = EffectAtomParams.GetInt(atom, "amount", 0);
                return gold > 0 ? $"获得{gold}金币" : string.Empty;
            case EffectAtomTypes.OpenTargeting:
                var messageKey = EffectAtomParams.Get(atom, "messageKey");
                return string.IsNullOrWhiteSpace(messageKey)
                    ? string.Empty
                    : DescriptionPanelTexts.Get(messageKey);
            case EffectAtomTypes.OpenChoiceOverlay:
                return "打开选择界面";
            case EffectAtomTypes.MoveBoard:
                return "旋转棋盘";
            case EffectAtomTypes.ApplyStatus:
                return "获得状态效果";
            case EffectAtomTypes.ConsumeHelpCard:
                return string.Empty;
            default:
                return string.Empty;
        }
    }

    private static string SummarizeModifyStat(EffectAtomDefinition atom)
    {
        var stat = EffectAtomParams.Get(atom, "stat");
        var delta = EffectAtomParams.GetInt(atom, "delta", 0);
        if (delta == 0)
        {
            return string.Empty;
        }

        switch (stat)
        {
            case "Attack":
                return delta > 0 ? $"攻击+{delta}" : $"攻击{delta}";
            case "Armor":
            case "CurrentArmor":
                return delta > 0 ? $"护甲+{delta}" : $"护甲{delta}";
            case "MaxHp":
                return delta > 0 ? $"生命上限+{delta}" : $"生命上限{delta}";
            default:
                return string.Empty;
        }
    }

    private static void AppendIfPresent(List<string> parts, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var trimmed = value.Trim();
        if (parts.Count > 0 && parts[parts.Count - 1] == trimmed)
        {
            return;
        }

        parts.Add(trimmed);
    }
}
