using System;
using System.Collections.Generic;

public enum EffectSource
{
    HelpCard,
    Skill,
    Relic,
    Room,
    Tutor
}

[Serializable]
public sealed class EffectGraphDefinition
{
    public string EffectGraphId;
    public List<EffectAtomDefinition> Atoms = new List<EffectAtomDefinition>();
}

[Serializable]
public sealed class EffectAtomDefinition
{
    public string AtomType;
    public List<EffectAtomParameter> Parameters = new List<EffectAtomParameter>();
}

public sealed class EffectContext
{
    public EffectSource Source;
    public CardUid? Caster;
    public List<CardUid> Targets = new List<CardUid>();
    public BoardSlotNo? SourceSlot;
    public Dictionary<string, object> Blackboard = new Dictionary<string, object>();
    public List<string> Tags = new List<string>();
}

public static class EffectAtomTypes
{
    public const string Damage = "DamageAtom";
    public const string Heal = "HealAtom";
    public const string AddGold = "AddGoldAtom";
    public const string ModifyStat = "ModifyStatAtom";
    public const string ConsumeHelpCard = "ConsumeHelpCardAtom";
    public const string OpenChoiceOverlay = "OpenChoiceOverlayAtom";
    public const string OpenTargeting = "OpenTargetingAtom";
    public const string AddCardToHelpDeck = "AddCardToHelpDeckAtom";
    public const string InjectCardToBattleDeck = "InjectCardToBattleDeckAtom";
    public const string MoveBoard = "MoveBoardAtom";
    public const string SwapCards = "SwapCardsAtom";
    public const string RemoveCard = "RemoveCardAtom";
    public const string ApplyStatus = "ApplyStatusAtom";
    public const string BloodConvertReward = "BloodConvertRewardAtom";
    public const string ReflectParallelDamage = "ReflectParallelDamageAtom";
}

internal static class EffectAtomParams
{
    public static string Get(EffectAtomDefinition atom, string key, string defaultValue = "")
    {
        return EffectAtomSerializationUtility.GetParameter(atom, key, defaultValue);
    }

    public static int GetInt(EffectAtomDefinition atom, string key, int defaultValue = 0)
    {
        return int.TryParse(Get(atom, key), out var value) ? value : defaultValue;
    }

    public static bool GetBool(EffectAtomDefinition atom, string key, bool defaultValue = false)
    {
        var raw = Get(atom, key);
        if (string.IsNullOrEmpty(raw))
        {
            return defaultValue;
        }

        return raw == "1" || raw.Equals("true", StringComparison.OrdinalIgnoreCase);
    }
}
