using System.Collections.Generic;
using QFramework;
using UnityEngine;

public static class BakedCardRenderDataFactory
{
    public const float StandardPixelsPerUnit = 100f;
    public const BakedCardFaceSize StandardBakeSize = BakedCardFaceSize.Large;

    public static BakedCardFaceRenderData CreateRuntime(IController controller, CardViewData data)
    {
        if (controller == null || data == null || !TableNine.IsInitialized)
        {
            return null;
        }

        var config = controller.GetModel<IConfigModel>();
        if (config == null)
        {
            return null;
        }

        if (data.Type == CardType.Player)
        {
            return CreatePlayerRuntimeData(config, data);
        }

        if (!config.TryGetCardDefinition(data.DefinitionId, out var definition) || definition == null)
        {
            return null;
        }

        var renderData = CreateFromCardDefinition(config, definition);
        renderData.Size = StandardBakeSize;
        if (renderData.StatMode == BakedCardFaceStatMode.FullStats)
        {
            renderData.Life = data.CurrentHp;
            renderData.Attack = data.Attack;
            renderData.Defense = data.CurrentArmor;
        }

        if (data.Type == CardType.Monster)
        {
            renderData.EntryIconSprites = ResolveStatusSprites(config, data.StatusIconIds);
        }

        return renderData;
    }

    public static BakedCardFaceRenderData CreateFromCardDefinition(IConfigModel config, CardDefinition definition)
    {
        if (config == null || definition == null)
        {
            return null;
        }

        var renderData = new BakedCardFaceRenderData
        {
            DisplayName = definition.DisplayName,
            Life = definition.BaseHp,
            Attack = definition.BaseAttack,
            Defense = definition.BaseArmor,
            Template = BakedCardFaceTemplate.CardExample,
            Size = StandardBakeSize,
            StatMode = definition.CardType == CardType.Monster
                ? BakedCardFaceStatMode.FullStats
                : BakedCardFaceStatMode.MainIconOnly,
            FaceSprite = config.GetEffectiveCardFaceImage(definition.CardId),
            BackSprite = config.GetEffectiveCardBackImage(definition.CardId),
            MainIconSprite = ResolveCardMainIcon(config, definition)
        };

        if (definition.CardType == CardType.Monster)
        {
            renderData.EntryIconSprites = ResolveCardSkillSprites(config, definition.SkillIds);
            renderData.StampedIconCount = renderData.EntryIconSprites.Count;
        }

        return renderData;
    }

#if UNITY_EDITOR
    public static BakedCardFaceRenderData CreateEditorPreview(TableNineGameConfig masterConfig, CardDefinition definition)
    {
        if (masterConfig == null || definition == null)
        {
            return null;
        }

        var renderData = new BakedCardFaceRenderData
        {
            DisplayName = definition.DisplayName,
            Life = definition.BaseHp,
            Attack = definition.BaseAttack,
            Defense = definition.BaseArmor,
            Template = BakedCardFaceTemplate.CardExample,
            Size = StandardBakeSize,
            StatMode = definition.CardType == CardType.Monster
                ? BakedCardFaceStatMode.FullStats
                : BakedCardFaceStatMode.MainIconOnly,
            FaceSprite = ResolveEffectiveFace(masterConfig, definition),
            BackSprite = ResolveEffectiveBack(masterConfig, definition),
            MainIconSprite = ResolveEditorMainIcon(masterConfig, definition)
        };

        if (definition.CardType == CardType.Monster)
        {
            renderData.EntryIconSprites = ResolveEditorSkillSprites(masterConfig, definition.SkillIds);
            renderData.StampedIconCount = renderData.EntryIconSprites.Count;
        }

        return renderData;
    }
#endif

    private static BakedCardFaceRenderData CreatePlayerRuntimeData(IConfigModel config, CardViewData data)
    {
        var renderData = new BakedCardFaceRenderData
        {
            DisplayName = data.DisplayName,
            Life = data.CurrentHp,
            Attack = data.Attack,
            Defense = data.CurrentArmor,
            Template = BakedCardFaceTemplate.PlayerCard,
            Size = StandardBakeSize,
            StatMode = BakedCardFaceStatMode.FullStats,
            MainIconSprite = ResolvePlayerMainIcon(config, data.DefinitionId),
            EntryIconSprites = ResolveStatusSprites(config, data.StatusIconIds)
        };

        renderData.StampedIconCount = renderData.EntryIconSprites.Count;
        return renderData;
    }

    private static Sprite ResolveCardMainIcon(IConfigModel config, CardDefinition definition)
    {
        var sprite = config.GetCardMainImage(definition.CardId);
        if (sprite != null)
        {
            return sprite;
        }

        if (definition.CardType != CardType.Tutor || definition.SkillIds == null || definition.SkillIds.Count == 0)
        {
            return null;
        }

        return ResolveSkillSprite(config, definition.SkillIds[0]);
    }

    private static Sprite ResolvePlayerMainIcon(IConfigModel config, string characterId)
    {
        if (string.IsNullOrEmpty(characterId))
        {
            return null;
        }

        try
        {
            return config.GetCharacterDefinition(characterId)?.Image;
        }
        catch
        {
            return null;
        }
    }

    private static List<Sprite> ResolveCardSkillSprites(IConfigModel config, IReadOnlyList<string> skillIds)
    {
        var result = new List<Sprite>();
        if (skillIds == null)
        {
            return result;
        }

        for (var i = 0; i < skillIds.Count; i++)
        {
            var sprite = ResolveSkillSprite(config, skillIds[i]);
            if (sprite != null)
            {
                result.Add(sprite);
            }
        }

        return result;
    }

    private static List<Sprite> ResolveStatusSprites(IConfigModel config, IReadOnlyList<string> iconIds)
    {
        var result = new List<Sprite>();
        if (iconIds == null)
        {
            return result;
        }

        for (var i = 0; i < iconIds.Count; i++)
        {
            var sprite = ResolveStatusSprite(config, iconIds[i]);
            if (sprite != null)
            {
                result.Add(sprite);
            }
        }

        return result;
    }

    private static Sprite ResolveStatusSprite(IConfigModel config, string iconId)
    {
        var sprite = ResolveSkillSprite(config, iconId);
        if (sprite != null)
        {
            return sprite;
        }

        var relics = config.GetAllRelicDefinitions();
        for (var i = 0; i < relics.Count; i++)
        {
            if (relics[i] != null && relics[i].RelicId == iconId)
            {
                return relics[i].Image;
            }
        }

        return null;
    }

    private static Sprite ResolveSkillSprite(IConfigModel config, string skillId)
    {
        if (string.IsNullOrEmpty(skillId))
        {
            return null;
        }

        var skills = config.GetAllSkillDefinitions();
        for (var i = 0; i < skills.Count; i++)
        {
            if (skills[i] != null && skills[i].SkillId == skillId)
            {
                return skills[i].Image;
            }
        }

        return null;
    }

#if UNITY_EDITOR
    private static Sprite ResolveEffectiveFace(TableNineGameConfig masterConfig, CardDefinition definition)
    {
        if (definition.FaceImageOverride != null)
        {
            return definition.FaceImageOverride;
        }

        var decks = masterConfig.CardConfig != null ? masterConfig.CardConfig.CardDecks : null;
        if (decks == null)
        {
            return null;
        }

        for (var i = 0; i < decks.Count; i++)
        {
            if (decks[i] != null && decks[i].DeckId == definition.DeckId)
            {
                return decks[i].DefaultFaceImage;
            }
        }

        return null;
    }

    private static Sprite ResolveEffectiveBack(TableNineGameConfig masterConfig, CardDefinition definition)
    {
        if (definition.BackImageOverride != null)
        {
            return definition.BackImageOverride;
        }

        var decks = masterConfig.CardConfig != null ? masterConfig.CardConfig.CardDecks : null;
        if (decks == null)
        {
            return null;
        }

        for (var i = 0; i < decks.Count; i++)
        {
            if (decks[i] != null && decks[i].DeckId == definition.DeckId)
            {
                return decks[i].DefaultBackImage;
            }
        }

        return null;
    }

    private static Sprite ResolveEditorMainIcon(TableNineGameConfig masterConfig, CardDefinition definition)
    {
        if (definition.Image != null)
        {
            return definition.Image;
        }

        if (definition.CardType != CardType.Tutor || definition.SkillIds == null || definition.SkillIds.Count == 0)
        {
            return null;
        }

        return ResolveEditorSkillSprite(masterConfig, definition.SkillIds[0]);
    }

    private static List<Sprite> ResolveEditorSkillSprites(TableNineGameConfig masterConfig, IReadOnlyList<string> skillIds)
    {
        var result = new List<Sprite>();
        if (skillIds == null)
        {
            return result;
        }

        for (var i = 0; i < skillIds.Count; i++)
        {
            var sprite = ResolveEditorSkillSprite(masterConfig, skillIds[i]);
            if (sprite != null)
            {
                result.Add(sprite);
            }
        }

        return result;
    }

    private static Sprite ResolveEditorSkillSprite(TableNineGameConfig masterConfig, string skillId)
    {
        var skills = masterConfig.SkillConfig != null ? masterConfig.SkillConfig.Skills : null;
        if (skills == null || string.IsNullOrEmpty(skillId))
        {
            return null;
        }

        for (var i = 0; i < skills.Count; i++)
        {
            if (skills[i] != null && skills[i].SkillId == skillId)
            {
                return skills[i].Image;
            }
        }

        return null;
    }
#endif
}
