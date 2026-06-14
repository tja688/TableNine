using System.Collections.Generic;

public static class CardDefinitionMigration
{
    public static void MigrateHelpCardSemantics(IList<CardDefinition> cards)
    {
        for (var i = 0; i < cards.Count; i++)
        {
            MigrateHelpCardSemantics(cards[i]);
        }
    }

    public static void MigrateHelpCardSemantics(CardDefinition card)
    {
        if (card == null || card.CardType != CardType.Help || card.RestoreAfterNodeAuthoritative)
        {
            return;
        }

        card.RestoreAfterNode = !card.LegacyIsPermanentRemoveOnUse;
    }

    public static void MarkAuthoritativeRestoreAfterNode(IList<CardDefinition> cards)
    {
        for (var i = 0; i < cards.Count; i++)
        {
            MarkAuthoritativeRestoreAfterNode(cards[i]);
        }
    }

    public static void MarkAuthoritativeRestoreAfterNode(CardDefinition card)
    {
        if (card == null || card.CardType != CardType.Help)
        {
            return;
        }

        card.RestoreAfterNodeAuthoritative = true;
    }

    /// <summary>
    /// Builds a help card that still carries the legacy IsPermanentRemoveOnUse serialized value.
    /// New configs should set RestoreAfterNode directly instead.
    /// </summary>
    public static CardDefinition CreateLegacyHelpCardDefinition(string cardId, bool isPermanentRemoveOnUse)
    {
        return new CardDefinition
        {
            CardId = cardId,
            CardType = CardType.Help,
            LegacyIsPermanentRemoveOnUse = isPermanentRemoveOnUse
        };
    }
}

public static class CardDeckDefinitionMigration
{
    public static void EnsureDecksAndAssignments(GameConfigSet config)
    {
        if (config == null)
        {
            return;
        }

        EnsureDeck(config, GameConfigIds.DeckCommonId, "通用牌组", "所有职业共享的帮助卡与导师卡",
            CardDeckFaction.Player, CardDeckKind.Common);
        EnsureDeck(config, GameConfigIds.DeckClownId, "小丑牌组", "小丑职业专属帮助卡",
            CardDeckFaction.Player, CardDeckKind.Class);
        EnsureDeck(config, GameConfigIds.DeckWeakEliteId, "弱精英牌组", "每层一至三节点使用的怪物牌组",
            CardDeckFaction.Monster, CardDeckKind.WeakElite);
        EnsureDeck(config, GameConfigIds.DeckStrongEliteId, "强精英牌组", "每层四至六节点使用的怪物牌组",
            CardDeckFaction.Monster, CardDeckKind.StrongElite);
        EnsureDeck(config, GameConfigIds.DeckBossId, "层主牌组", "每层七至九节点使用的怪物牌组",
            CardDeckFaction.Monster, CardDeckKind.Boss);

        for (var i = 0; i < config.Characters.Count; i++)
        {
            var character = config.Characters[i];
            if (string.IsNullOrWhiteSpace(character.CommonDeckId))
            {
                character.CommonDeckId = GameConfigIds.DeckCommonId;
            }

            if (string.IsNullOrWhiteSpace(character.ClassDeckId))
            {
                character.ClassDeckId = GameConfigIds.DeckClownId;
            }
        }

        for (var i = 0; i < config.Cards.Count; i++)
        {
            var card = config.Cards[i];
            if (card == null || !string.IsNullOrWhiteSpace(card.DeckId))
            {
                continue;
            }

            card.DeckId = ResolveDeckId(card);
        }

        for (var i = 0; i < config.MonsterDeckRules.Count; i++)
        {
            var rule = config.MonsterDeckRules[i];
            if (rule != null && string.IsNullOrWhiteSpace(rule.SourceDeckId))
            {
                rule.SourceDeckId = ResolveRuleDeckId(rule.NodeInLayer);
            }
        }
    }

    private static void EnsureDeck(
        GameConfigSet config,
        string deckId,
        string displayName,
        string description,
        CardDeckFaction faction,
        CardDeckKind kind)
    {
        for (var i = 0; i < config.CardDecks.Count; i++)
        {
            if (config.CardDecks[i].DeckId == deckId)
            {
                return;
            }
        }

        config.CardDecks.Add(new CardDeckDefinition
        {
            DeckId = deckId,
            DisplayName = displayName,
            Description = description,
            Faction = faction,
            DeckKind = kind
        });
    }

    private static string ResolveDeckId(CardDefinition card)
    {
        if (card.CardId == GameConfigIds.HelpThrowingKnifeId)
        {
            return GameConfigIds.DeckClownId;
        }

        if (card.CardType == CardType.Help || card.CardType == CardType.Tutor)
        {
            return GameConfigIds.DeckCommonId;
        }

        if (card.CardType == CardType.Monster)
        {
            switch (card.MonsterLevel)
            {
                case MonsterLevel.Level1:
                case MonsterLevel.Level2:
                case MonsterLevel.Level3:
                    return GameConfigIds.DeckWeakEliteId;
                case MonsterLevel.Level4:
                case MonsterLevel.Elite:
                    return GameConfigIds.DeckStrongEliteId;
                case MonsterLevel.Boss:
                    return GameConfigIds.DeckBossId;
            }
        }

        return GameConfigIds.DeckCommonId;
    }

    private static string ResolveRuleDeckId(int nodeInLayer)
    {
        if (nodeInLayer <= 3)
        {
            return GameConfigIds.DeckWeakEliteId;
        }

        if (nodeInLayer <= 6)
        {
            return GameConfigIds.DeckStrongEliteId;
        }

        return GameConfigIds.DeckBossId;
    }
}
