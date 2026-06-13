using System.Collections.Generic;
using UnityEngine;

public static class DescriptionPanelConfigValidation
{
    public static void CollectErrors(GameConfigSet config, List<string> errors)
    {
        if (config == null)
        {
            return;
        }

        for (var i = 0; i < config.Characters.Count; i++)
        {
            CollectEntryErrors("Character", config.Characters[i].CharacterId, config.Characters[i].Description, errors);
        }

        for (var i = 0; i < config.Cards.Count; i++)
        {
            CollectEntryErrors("Card", config.Cards[i].CardId, config.Cards[i].Description, errors);
        }

        for (var i = 0; i < config.Skills.Count; i++)
        {
            CollectEntryErrors("Skill", config.Skills[i].SkillId, config.Skills[i].Description, errors);
        }

        for (var i = 0; i < config.Relics.Count; i++)
        {
            CollectEntryErrors("Relic", config.Relics[i].RelicId, config.Relics[i].Description, errors);
        }

        for (var i = 0; i < config.Rooms.Count; i++)
        {
            CollectEntryErrors("Room", config.Rooms[i].RoomId, config.Rooms[i].Description, errors);
        }
    }

    public static void SanitizeCharacterConfig(TableNineCharacterConfig config)
    {
        if (config == null)
        {
            return;
        }

        for (var i = 0; i < config.Characters.Count; i++)
        {
            var entry = config.Characters[i];
            if (entry == null)
            {
                continue;
            }

            SanitizeAndLog("Character", entry.CharacterId, ref entry.Description);
        }
    }

    public static void SanitizeCardConfig(TableNineCardConfig config)
    {
        if (config == null)
        {
            return;
        }

        for (var i = 0; i < config.Cards.Count; i++)
        {
            var entry = config.Cards[i];
            if (entry == null)
            {
                continue;
            }

            SanitizeAndLog("Card", entry.CardId, ref entry.Description);
        }
    }

    public static void SanitizeSkillConfig(TableNineSkillConfig config)
    {
        if (config == null)
        {
            return;
        }

        for (var i = 0; i < config.Skills.Count; i++)
        {
            var entry = config.Skills[i];
            if (entry == null)
            {
                continue;
            }

            SanitizeAndLog("Skill", entry.SkillId, ref entry.Description);
        }
    }

    public static void SanitizeRelicConfig(TableNineRelicConfig config)
    {
        if (config == null)
        {
            return;
        }

        for (var i = 0; i < config.Relics.Count; i++)
        {
            var entry = config.Relics[i];
            if (entry == null)
            {
                continue;
            }

            SanitizeAndLog("Relic", entry.RelicId, ref entry.Description);
        }

        for (var i = 0; i < config.Rooms.Count; i++)
        {
            var entry = config.Rooms[i];
            if (entry == null)
            {
                continue;
            }

            SanitizeAndLog("Room", entry.RoomId, ref entry.Description);
        }
    }

    public static bool SanitizeDescription(string ownerId, ref string description, out string message)
    {
        message = null;
        var original = description ?? string.Empty;
        var sanitized = DescriptionPanelTextRules.SanitizeForStorage(original, out var wasModified);
        if (!wasModified)
        {
            return false;
        }

        description = sanitized;
        message = BuildModificationMessage(ownerId, original, sanitized);
        return true;
    }

    private static void SanitizeAndLog(string kind, string ownerId, ref string description)
    {
        if (!SanitizeDescription(ownerId, ref description, out var message))
        {
            return;
        }

        Debug.LogError($"[DescriptionPanelConfig] ({kind}) {message}");
    }

    private static void CollectEntryErrors(string kind, string ownerId, string description, List<string> errors)
    {
        var text = description ?? string.Empty;
        ownerId = string.IsNullOrEmpty(ownerId) ? "(unknown)" : ownerId;
        if (DescriptionPanelTextRules.ContainsLineBreak(text))
        {
            errors.Add($"{kind} {ownerId} Description contains line breaks.");
        }

        if (!DescriptionPanelTextRules.IsWithinLimit(text))
        {
            errors.Add($"{kind} {ownerId} Description exceeds {DescriptionPanelTextRules.MaxLength} chars ({text.Length}).");
        }
    }

    private static string BuildModificationMessage(string ownerId, string original, string sanitized)
    {
        ownerId = string.IsNullOrEmpty(ownerId) ? "(unknown)" : ownerId;
        if (DescriptionPanelTextRules.ContainsLineBreak(original)
            && DescriptionPanelTextRules.CountLength(DescriptionPanelTextRules.RemoveLineBreaks(original)) > DescriptionPanelTextRules.MaxLength)
        {
            return $"{ownerId} Description had line breaks and exceeded {DescriptionPanelTextRules.MaxLength} chars; truncated from {original.Length} to {sanitized.Length}.";
        }

        if (DescriptionPanelTextRules.ContainsLineBreak(original))
        {
            return $"{ownerId} Description had line breaks; removed before save.";
        }

        return $"{ownerId} Description exceeded {DescriptionPanelTextRules.MaxLength} chars; truncated from {original.Length} to {sanitized.Length}.";
    }
}
