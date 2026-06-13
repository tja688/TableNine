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
