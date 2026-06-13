using System.Collections.Generic;

public static class HelpCardSystemTagConfig
{
    private static readonly Dictionary<string, HelpCardSystemTag> TagsByCardId = new Dictionary<string, HelpCardSystemTag>
    {
        { GameConfigIds.HelpPotionId, HelpCardSystemTag.Recovery },
        { GameConfigIds.HelpBlessingId, HelpCardSystemTag.Defense },
        { GameConfigIds.HelpThrowingKnifeId, HelpCardSystemTag.DirectDamage },
        { GameConfigIds.HelpFireballId, HelpCardSystemTag.DirectDamage },
        { GameConfigIds.HelpSpinWheelId, HelpCardSystemTag.Displacement },
        { GameConfigIds.HelpViolenceId, HelpCardSystemTag.Attack },
        { GameConfigIds.HelpBoulderId, HelpCardSystemTag.DirectDamage },
        { GameConfigIds.HelpBombId, HelpCardSystemTag.DirectDamage },
        { GameConfigIds.HelpSwapId, HelpCardSystemTag.Displacement },
        { GameConfigIds.HelpSmasherId, HelpCardSystemTag.DirectDamage },
        { GameConfigIds.HelpDurableShieldId, HelpCardSystemTag.Defense },
        { GameConfigIds.HelpBearTrapId, HelpCardSystemTag.DirectDamage },
        { GameConfigIds.HelpTeleportId, HelpCardSystemTag.Displacement },
        { GameConfigIds.HelpBloodConvertId, HelpCardSystemTag.Displacement },
        { GameConfigIds.HelpGoldCardId, HelpCardSystemTag.Economy },
        { GameConfigIds.HelpFoodId, HelpCardSystemTag.Recovery },
        { GameConfigIds.HelpCommonChestId, HelpCardSystemTag.Economy },
        { GameConfigIds.HelpHealingSpringId, HelpCardSystemTag.Recovery },
        { GameConfigIds.HelpCrashTutorialId, HelpCardSystemTag.Hp },
        { GameConfigIds.HelpShieldStrikeTutorialId, HelpCardSystemTag.Defense },
        { GameConfigIds.HelpKidnapId, HelpCardSystemTag.Defense },
        { GameConfigIds.HelpBlueChestId, HelpCardSystemTag.Economy },
        { GameConfigIds.HelpWatchtowerId, HelpCardSystemTag.DirectDamage },
        { GameConfigIds.HelpMultiplierTowerId, HelpCardSystemTag.Special },
        { GameConfigIds.HelpAttributeUpId, HelpCardSystemTag.Special },
        { GameConfigIds.HelpGoldChestId, HelpCardSystemTag.Special },
        { GameConfigIds.HelpChestCardId, HelpCardSystemTag.Economy },
        { GameConfigIds.HelpBandageId, HelpCardSystemTag.Recovery }
    };

    public static void Apply(GameConfigSet config)
    {
        for (var i = 0; i < config.Cards.Count; i++)
        {
            var card = config.Cards[i];
            if (card.CardType != CardType.Help)
            {
                continue;
            }

            if (TagsByCardId.TryGetValue(card.CardId, out var tag))
            {
                card.SystemTag = tag;
            }
        }
    }
}
