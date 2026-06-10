using System.Collections.Generic;

public static class ConfigValidator
{
    public static List<string> Validate(GameConfigSet config)
    {
        var errors = new List<string>();
        var cardIds = new HashSet<string>();
        var skillIds = new HashSet<string>();
        var characterIds = new HashSet<string>();

        for (var i = 0; i < config.Cards.Count; i++)
        {
            var card = config.Cards[i];
            if (string.IsNullOrWhiteSpace(card.CardId) || !cardIds.Add(card.CardId))
            {
                errors.Add($"Duplicate or empty cardId: {card.CardId}");
            }
        }

        for (var i = 0; i < config.Skills.Count; i++)
        {
            var skill = config.Skills[i];
            if (string.IsNullOrWhiteSpace(skill.SkillId) || !skillIds.Add(skill.SkillId))
            {
                errors.Add($"Duplicate or empty skillId: {skill.SkillId}");
            }
        }

        for (var i = 0; i < config.Characters.Count; i++)
        {
            var character = config.Characters[i];
            if (string.IsNullOrWhiteSpace(character.CharacterId) || !characterIds.Add(character.CharacterId))
            {
                errors.Add($"Duplicate or empty characterId: {character.CharacterId}");
            }

            for (var j = 0; j < character.InitialSkillIds.Count; j++)
            {
                if (!skillIds.Contains(character.InitialSkillIds[j]))
                {
                    errors.Add($"Character {character.CharacterId} references missing skillId: {character.InitialSkillIds[j]}");
                }
            }

            for (var j = 0; j < character.InitialHelpCardIds.Count; j++)
            {
                if (!cardIds.Contains(character.InitialHelpCardIds[j]))
                {
                    errors.Add($"Character {character.CharacterId} references missing helpCardId: {character.InitialHelpCardIds[j]}");
                }
            }
        }

        for (var i = 0; i < config.Cards.Count; i++)
        {
            var card = config.Cards[i];
            for (var j = 0; j < card.SkillIds.Count; j++)
            {
                if (!skillIds.Contains(card.SkillIds[j]))
                {
                    errors.Add($"Card {card.CardId} references missing skillId: {card.SkillIds[j]}");
                }
            }
        }

        for (var i = 0; i < config.MonsterDeckRules.Count; i++)
        {
            var rule = config.MonsterDeckRules[i];
            if (rule.TotalCardCount != 9 + rule.NodeInLayer)
            {
                errors.Add($"Monster deck rule {rule.Layer}-{rule.NodeInLayer} total count must equal 9 + node.");
            }

            if (rule.AllowedMonsterCardIds.Count == 0)
            {
                errors.Add($"Monster deck rule {rule.Layer}-{rule.NodeInLayer} has no allowed monster cards.");
            }

            for (var j = 0; j < rule.AllowedMonsterCardIds.Count; j++)
            {
                var cardId = rule.AllowedMonsterCardIds[j];
                if (!cardIds.Contains(cardId))
                {
                    errors.Add($"Monster deck rule {rule.Layer}-{rule.NodeInLayer} references missing cardId: {cardId}");
                }
            }
        }

        return errors;
    }
}
