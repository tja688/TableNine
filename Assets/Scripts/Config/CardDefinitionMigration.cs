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
        if (card == null || card.CardType != CardType.Help)
        {
            return;
        }

        card.RestoreAfterNode = !card.IsPermanentRemoveOnUse;
    }
}
