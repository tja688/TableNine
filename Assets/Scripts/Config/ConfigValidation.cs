using System.Collections.Generic;

public static class ConfigValidator
{
    public static List<string> Validate(GameConfigSet config)
    {
        var errors = new List<string>();
        var cardIds = new HashSet<string>();
        var skillIds = new HashSet<string>();
        var characterIds = new HashSet<string>();
        var relicIds = new HashSet<string>();
        var roomIds = new HashSet<string>();

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

        for (var i = 0; i < config.Relics.Count; i++)
        {
            var relic = config.Relics[i];
            if (string.IsNullOrWhiteSpace(relic.RelicId) || !relicIds.Add(relic.RelicId))
            {
                errors.Add($"Duplicate or empty relicId: {relic.RelicId}");
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

            if (rule.NodeInLayer == 5)
            {
                var eliteId = DefaultGameConfigFactory.GetEliteMonsterId(rule.Layer);
                if (!rule.MandatoryMonsterCardIds.Contains(eliteId))
                {
                    errors.Add($"Layer {rule.Layer} node 5 must include elite monster {eliteId}.");
                }
            }

            if (rule.NodeInLayer == 9)
            {
                var bossId = DefaultGameConfigFactory.GetBossMonsterId(rule.Layer);
                if (!rule.MandatoryMonsterCardIds.Contains(bossId))
                {
                    errors.Add($"Layer {rule.Layer} node 9 must include boss monster {bossId}.");
                }
            }
        }

        var expectedRuleCount = 27;
        if (config.MonsterDeckRules.Count != expectedRuleCount)
        {
            errors.Add($"Expected {expectedRuleCount} monster deck rules (3 layers x 9 nodes), got {config.MonsterDeckRules.Count}.");
        }

        for (var layer = 1; layer <= 3; layer++)
        {
            for (var node = 1; node <= 9; node++)
            {
                var hasRule = false;
                for (var i = 0; i < config.MonsterDeckRules.Count; i++)
                {
                    var rule = config.MonsterDeckRules[i];
                    if (rule.Layer == layer && rule.NodeInLayer == node)
                    {
                        hasRule = true;
                        break;
                    }
                }

                if (!hasRule)
                {
                    errors.Add($"Missing monster deck rule for layer {layer} node {node}.");
                }
            }
        }

        for (var i = 0; i < DefaultGameConfigFactory.PlaytestHelpCardIds.Length; i++)
        {
            var helpId = DefaultGameConfigFactory.PlaytestHelpCardIds[i];
            if (!cardIds.Contains(helpId))
            {
                errors.Add($"Missing playtest help card: {helpId}");
            }
        }

        for (var i = 0; i < config.Rooms.Count; i++)
        {
            var room = config.Rooms[i];
            if (string.IsNullOrWhiteSpace(room.RoomId) || !roomIds.Add(room.RoomId))
            {
                errors.Add($"Duplicate or empty roomId: {room.RoomId}");
            }

            if (room.RoomType == RoomType.None)
            {
                errors.Add($"Room {room.RoomId} has RoomType None.");
            }

            if (!string.IsNullOrEmpty(room.InjectCardId) && !cardIds.Contains(room.InjectCardId))
            {
                errors.Add($"Room {room.RoomId} references missing injectCardId: {room.InjectCardId}");
            }
        }

        return errors;
    }

    public static List<string> CollectWarnings(GameConfigSet config)
    {
        var warnings = new List<string>();

        for (var i = 0; i < config.Cards.Count; i++)
        {
            var card = config.Cards[i];
            if (card.CardType != CardType.Help)
            {
                continue;
            }

            if (!card.IsPermanentRemoveOnUse)
            {
                continue;
            }

            warnings.Add($"Card {card.CardId}: IsPermanentRemoveOnUse is deprecated; configure RestoreAfterNode instead.");

            if (card.RestoreAfterNode)
            {
                warnings.Add($"Card {card.CardId}: IsPermanentRemoveOnUse conflicts with RestoreAfterNode.");
            }
        }

        return warnings;
    }
}
