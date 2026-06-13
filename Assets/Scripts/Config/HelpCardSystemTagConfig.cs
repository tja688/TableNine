using System.Collections.Generic;

public static class HelpCardSystemTagConfig
{
    private static readonly Dictionary<string, HelpCardSystemTag> TagsByCardId = new Dictionary<string, HelpCardSystemTag>
    {
        { DefaultGameConfigFactory.HelpPotionId, HelpCardSystemTag.Recovery },
        { DefaultGameConfigFactory.HelpBlessingId, HelpCardSystemTag.Defense },
        { DefaultGameConfigFactory.HelpThrowingKnifeId, HelpCardSystemTag.DirectDamage },
        { DefaultGameConfigFactory.HelpFireballId, HelpCardSystemTag.DirectDamage },
        { DefaultGameConfigFactory.HelpSpinWheelId, HelpCardSystemTag.Displacement },
        { DefaultGameConfigFactory.HelpViolenceId, HelpCardSystemTag.Attack },
        { DefaultGameConfigFactory.HelpBoulderId, HelpCardSystemTag.DirectDamage },
        { DefaultGameConfigFactory.HelpBombId, HelpCardSystemTag.DirectDamage },
        { DefaultGameConfigFactory.HelpSwapId, HelpCardSystemTag.Displacement },
        { DefaultGameConfigFactory.HelpSmasherId, HelpCardSystemTag.DirectDamage },
        { DefaultGameConfigFactory.HelpDurableShieldId, HelpCardSystemTag.Defense },
        { DefaultGameConfigFactory.HelpBearTrapId, HelpCardSystemTag.DirectDamage },
        { DefaultGameConfigFactory.HelpTeleportId, HelpCardSystemTag.Displacement },
        { DefaultGameConfigFactory.HelpBloodConvertId, HelpCardSystemTag.Displacement },
        { DefaultGameConfigFactory.HelpGoldCardId, HelpCardSystemTag.Economy },
        { DefaultGameConfigFactory.HelpFoodId, HelpCardSystemTag.Recovery },
        { DefaultGameConfigFactory.HelpCommonChestId, HelpCardSystemTag.Economy },
        { DefaultGameConfigFactory.HelpHealingSpringId, HelpCardSystemTag.Recovery },
        { DefaultGameConfigFactory.HelpCrashTutorialId, HelpCardSystemTag.Hp },
        { DefaultGameConfigFactory.HelpShieldStrikeTutorialId, HelpCardSystemTag.Defense },
        { DefaultGameConfigFactory.HelpKidnapId, HelpCardSystemTag.Defense },
        { DefaultGameConfigFactory.HelpBlueChestId, HelpCardSystemTag.Economy },
        { DefaultGameConfigFactory.HelpWatchtowerId, HelpCardSystemTag.DirectDamage },
        { DefaultGameConfigFactory.HelpMultiplierTowerId, HelpCardSystemTag.Special },
        { DefaultGameConfigFactory.HelpAttributeUpId, HelpCardSystemTag.Special },
        { DefaultGameConfigFactory.HelpGoldChestId, HelpCardSystemTag.Special },
        { DefaultGameConfigFactory.HelpChestCardId, HelpCardSystemTag.Economy },
        { DefaultGameConfigFactory.HelpBandageId, HelpCardSystemTag.Recovery }
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
