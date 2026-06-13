using System;
using System.Collections.Generic;

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
