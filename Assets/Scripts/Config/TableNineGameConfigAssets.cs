using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class EffectAtomParameter
{
    public string Key;
    public string Value;
}

[Serializable]
public sealed class HelpCardEffectMapping
{
    public string CardId;
    public string EffectGraphId;
}

public sealed class GameConfigRuntimeBundle
{
    public GameConfigSet Core { get; } = new GameConfigSet();
    public List<EffectGraphDefinition> EffectGraphs { get; } = new List<EffectGraphDefinition>();
    public List<SkillEffectBinding> SkillBindings { get; } = new List<SkillEffectBinding>();
    public List<SkillBehaviorRule> SkillBehaviorRules { get; } = new List<SkillBehaviorRule>();
}

[CreateAssetMenu(fileName = "TableNineCharacterConfig", menuName = "TableNine/Game Config/Character Config")]
public sealed class TableNineCharacterConfig : ScriptableObject
{
    public List<CharacterDefinition> Characters = new List<CharacterDefinition>();
}

[CreateAssetMenu(fileName = "TableNineCardConfig", menuName = "TableNine/Game Config/Card Config")]
public sealed class TableNineCardConfig : ScriptableObject
{
    public List<CardDefinition> Cards = new List<CardDefinition>();
    public List<MonsterDeckRuleDefinition> MonsterDeckRules = new List<MonsterDeckRuleDefinition>();
}

[CreateAssetMenu(fileName = "TableNineSkillConfig", menuName = "TableNine/Game Config/Skill Config")]
public sealed class TableNineSkillConfig : ScriptableObject
{
    public List<SkillDefinition> Skills = new List<SkillDefinition>();
    public List<SkillEffectBinding> SkillBindings = new List<SkillEffectBinding>();
    public List<SkillBehaviorRule> SkillBehaviorRules = new List<SkillBehaviorRule>();
}

[CreateAssetMenu(fileName = "TableNineRelicConfig", menuName = "TableNine/Game Config/Relic Config")]
public sealed class TableNineRelicConfig : ScriptableObject
{
    public List<RelicDefinition> Relics = new List<RelicDefinition>();
    public List<RoomDefinition> Rooms = new List<RoomDefinition>();
}

[CreateAssetMenu(fileName = "TableNineEffectConfig", menuName = "TableNine/Game Config/Effect Config")]
public sealed class TableNineEffectConfig : ScriptableObject
{
    public List<EffectGraphDefinition> EffectGraphs = new List<EffectGraphDefinition>();
    public List<HelpCardEffectMapping> HelpCardEffectMappings = new List<HelpCardEffectMapping>();
}

[CreateAssetMenu(fileName = "TableNineGameConfig", menuName = "TableNine/Game Config/Master Config")]
public sealed class TableNineGameConfig : ScriptableObject
{
    public TableNineCharacterConfig CharacterConfig;
    public TableNineCardConfig CardConfig;
    public TableNineSkillConfig SkillConfig;
    public TableNineRelicConfig RelicConfig;
    public TableNineEffectConfig EffectConfig;

    public GameConfigRuntimeBundle ToRuntimeBundle()
    {
        var bundle = new GameConfigRuntimeBundle();
        if (CharacterConfig != null)
        {
            bundle.Core.Characters.AddRange(CharacterConfig.Characters);
        }

        if (CardConfig != null)
        {
            bundle.Core.Cards.AddRange(CardConfig.Cards);
            bundle.Core.MonsterDeckRules.AddRange(CardConfig.MonsterDeckRules);
        }

        if (SkillConfig != null)
        {
            bundle.Core.Skills.AddRange(SkillConfig.Skills);
            bundle.SkillBindings.AddRange(SkillConfig.SkillBindings);
            bundle.SkillBehaviorRules.AddRange(SkillConfig.SkillBehaviorRules);
        }

        if (RelicConfig != null)
        {
            bundle.Core.Relics.AddRange(RelicConfig.Relics);
            bundle.Core.Rooms.AddRange(RelicConfig.Rooms);
        }

        if (EffectConfig != null)
        {
            bundle.EffectGraphs.AddRange(EffectConfig.EffectGraphs);
            ApplyHelpCardEffectMappings(bundle);
        }

        CardDefinitionMigration.MigrateHelpCardSemantics(bundle.Core.Cards);
        return bundle;
    }

    private void ApplyHelpCardEffectMappings(GameConfigRuntimeBundle bundle)
    {
        if (EffectConfig == null)
        {
            return;
        }

        var mappings = EffectConfig.HelpCardEffectMappings;
        for (var i = 0; i < mappings.Count; i++)
        {
            var mapping = mappings[i];
            if (string.IsNullOrEmpty(mapping.CardId) || string.IsNullOrEmpty(mapping.EffectGraphId))
            {
                continue;
            }

            for (var j = 0; j < bundle.Core.Cards.Count; j++)
            {
                var card = bundle.Core.Cards[j];
                if (card.CardType == CardType.Help && card.CardId == mapping.CardId)
                {
                    card.EffectGraphId = mapping.EffectGraphId;
                }
            }
        }
    }
}

public static class EffectAtomSerializationUtility
{
    public static EffectAtomDefinition CloneAtom(EffectAtomDefinition source)
    {
        if (source == null)
        {
            return null;
        }

        var clone = new EffectAtomDefinition { AtomType = source.AtomType };
        CopyParameters(source, clone);
        return clone;
    }

    public static EffectGraphDefinition CloneGraph(EffectGraphDefinition source)
    {
        if (source == null)
        {
            return null;
        }

        var clone = new EffectGraphDefinition { EffectGraphId = source.EffectGraphId };
        for (var i = 0; i < source.Atoms.Count; i++)
        {
            clone.Atoms.Add(CloneAtom(source.Atoms[i]));
        }

        return clone;
    }

    public static void CopyParameters(EffectAtomDefinition source, EffectAtomDefinition target)
    {
        target.Parameters.Clear();
        for (var i = 0; i < source.Parameters.Count; i++)
        {
            var parameter = source.Parameters[i];
            target.Parameters.Add(new EffectAtomParameter
            {
                Key = parameter.Key,
                Value = parameter.Value
            });
        }
    }

    public static void SetParameter(EffectAtomDefinition atom, string key, string value)
    {
        for (var i = 0; i < atom.Parameters.Count; i++)
        {
            if (atom.Parameters[i].Key == key)
            {
                atom.Parameters[i].Value = value;
                return;
            }
        }

        atom.Parameters.Add(new EffectAtomParameter { Key = key, Value = value });
    }

    public static string GetParameter(EffectAtomDefinition atom, string key, string defaultValue = "")
    {
        for (var i = 0; i < atom.Parameters.Count; i++)
        {
            if (atom.Parameters[i].Key == key)
            {
                return atom.Parameters[i].Value ?? defaultValue;
            }
        }

        return defaultValue;
    }
}
