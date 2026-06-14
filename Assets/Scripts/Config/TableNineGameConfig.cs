using UnityEngine;

[CreateAssetMenu(fileName = "TableNineGameConfig", menuName = "TableNine/Game Config/Master Config")]
public sealed class TableNineGameConfig : ScriptableObject
{
    public TableNineCharacterConfig CharacterConfig;
    public TableNineCardConfig CardConfig;
    public TableNineSkillConfig SkillConfig;
    public TableNineRelicConfig RelicConfig;
    public TableNineEffectConfig EffectConfig;

    public GameConfigRuntimeBundle ToRuntimeBundle()
    {
        var bundle = new GameConfigRuntimeBundle();
        if (CharacterConfig != null)
        {
            bundle.Core.Characters.AddRange(CharacterConfig.Characters);
        }

        if (CardConfig != null)
        {
            bundle.Core.CardDecks.AddRange(CardConfig.CardDecks);
            bundle.Core.Cards.AddRange(CardConfig.Cards);
            bundle.Core.MonsterDeckRules.AddRange(CardConfig.MonsterDeckRules);
        }

        if (SkillConfig != null)
        {
            bundle.Core.Skills.AddRange(SkillConfig.Skills);
            bundle.SkillBindings.AddRange(SkillConfig.SkillBindings);
            bundle.SkillBehaviorRules.AddRange(SkillConfig.SkillBehaviorRules);
        }

        if (RelicConfig != null)
        {
            bundle.Core.Relics.AddRange(RelicConfig.Relics);
            bundle.Core.Rooms.AddRange(RelicConfig.Rooms);
        }

        if (EffectConfig != null)
        {
            bundle.EffectGraphs.AddRange(EffectConfig.EffectGraphs);
            ApplyHelpCardEffectMappings(bundle);
        }

        CardDeckDefinitionMigration.EnsureDecksAndAssignments(bundle.Core);
        CardDefinitionMigration.MigrateHelpCardSemantics(bundle.Core.Cards);
        return bundle;
    }

    private void ApplyHelpCardEffectMappings(GameConfigRuntimeBundle bundle)
    {
        if (EffectConfig == null)
        {
            return;
        }

        var mappings = EffectConfig.HelpCardEffectMappings;
        for (var i = 0; i < mappings.Count; i++)
        {
            var mapping = mappings[i];
            if (string.IsNullOrEmpty(mapping.CardId) || string.IsNullOrEmpty(mapping.EffectGraphId))
            {
                continue;
            }

            for (var j = 0; j < bundle.Core.Cards.Count; j++)
            {
                var card = bundle.Core.Cards[j];
                if (card.CardType == CardType.Help && card.CardId == mapping.CardId)
                {
                    card.EffectGraphId = mapping.EffectGraphId;
                }
            }
        }
    }
}
