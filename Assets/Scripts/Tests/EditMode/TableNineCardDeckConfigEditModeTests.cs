#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using QFramework;
using UnityEngine;

[Category(TableNineTestCategories.RegressionTests)]
public sealed class TableNineCardDeckConfigEditModeTests
{
    private readonly List<Object> mCreatedObjects = new List<Object>();

    [SetUp]
    public void SetUp()
    {
        TableNine.ResetForTests();
    }

    [TearDown]
    public void TearDown()
    {
        TableNine.ResetForTests();
        for (var i = 0; i < mCreatedObjects.Count; i++)
        {
            if (mCreatedObjects[i] != null)
            {
                Object.DestroyImmediate(mCreatedObjects[i]);
            }
        }

        mCreatedObjects.Clear();
    }

    [Test]
    public void DefaultConfig_Creates_Design_Decks_And_Assigns_Core_Cards()
    {
        var config = DefaultGameConfigFactory.Create();

        Assert.That(config.CardDecks.Count, Is.EqualTo(5));
        Assert.That(FindDeck(config, GameConfigIds.DeckCommonId).DeckKind, Is.EqualTo(CardDeckKind.Common));
        Assert.That(FindDeck(config, GameConfigIds.DeckClownId).Faction, Is.EqualTo(CardDeckFaction.Player));
        Assert.That(FindCard(config, GameConfigIds.HelpPotionId).DeckId, Is.EqualTo(GameConfigIds.DeckCommonId));
        Assert.That(FindCard(config, GameConfigIds.HelpThrowingKnifeId).DeckId, Is.EqualTo(GameConfigIds.DeckClownId));
        Assert.That(FindCard(config, GameConfigIds.MonsterSpade2Id).DeckId, Is.EqualTo(GameConfigIds.DeckWeakEliteId));
        Assert.That(FindCard(config, GameConfigIds.MonsterSpade5Id).DeckId, Is.EqualTo(GameConfigIds.DeckStrongEliteId));
        Assert.That(FindCard(config, GameConfigIds.MonsterSpadeBossId).DeckId, Is.EqualTo(GameConfigIds.DeckBossId));
    }

    [Test]
    public void ConfigModel_Filters_Playable_Help_Cards_By_Common_And_Class_Decks()
    {
        var master = CreateMasterConfig();
        TableNine.ConfigureGameConfig(master);
        TableNine.InitArchitecture();

        var configModel = TableNine.Interface.GetModel<IConfigModel>();
        var cards = configModel.GetPlayableHelpCardDefinitions("hero");
        var ids = new HashSet<string>();
        for (var i = 0; i < cards.Count; i++)
        {
            ids.Add(cards[i].CardId);
        }

        Assert.That(ids.Contains("common_help"), Is.True);
        Assert.That(ids.Contains("class_help"), Is.True);
        Assert.That(ids.Contains("other_class_help"), Is.False);
    }

    [Test]
    public void ConfigModel_Resolves_Card_Image_And_Face_Back_Overrides()
    {
        var defaultFace = CreateSprite("default_face");
        var defaultBack = CreateSprite("default_back");
        var icon = CreateSprite("icon");
        var overrideFace = CreateSprite("override_face");

        var master = CreateMasterConfig(defaultFace, defaultBack, icon, overrideFace);
        TableNine.ConfigureGameConfig(master);
        TableNine.InitArchitecture();

        var configModel = TableNine.Interface.GetModel<IConfigModel>();

        Assert.That(configModel.GetCardMainImage("common_help"), Is.EqualTo(icon));
        Assert.That(configModel.GetEffectiveCardFaceImage("common_help"), Is.EqualTo(overrideFace));
        Assert.That(configModel.GetEffectiveCardBackImage("common_help"), Is.EqualTo(defaultBack));
        Assert.That(configModel.GetEffectiveCardFaceImage("class_help"), Is.EqualTo(defaultFace));
    }

    private TableNineGameConfig CreateMasterConfig(
        Sprite defaultFace = null,
        Sprite defaultBack = null,
        Sprite icon = null,
        Sprite overrideFace = null)
    {
        var cardConfig = ScriptableObject.CreateInstance<TableNineCardConfig>();
        mCreatedObjects.Add(cardConfig);
        cardConfig.CardDecks.Add(new CardDeckDefinition
        {
            DeckId = "common",
            DisplayName = "Common",
            Faction = CardDeckFaction.Player,
            DeckKind = CardDeckKind.Common,
            DefaultFaceImage = defaultFace,
            DefaultBackImage = defaultBack
        });
        cardConfig.CardDecks.Add(new CardDeckDefinition
        {
            DeckId = "class",
            DisplayName = "Class",
            Faction = CardDeckFaction.Player,
            DeckKind = CardDeckKind.Class,
            DefaultFaceImage = defaultFace,
            DefaultBackImage = defaultBack
        });
        cardConfig.CardDecks.Add(new CardDeckDefinition
        {
            DeckId = "other_class",
            DisplayName = "Other Class",
            Faction = CardDeckFaction.Player,
            DeckKind = CardDeckKind.Class
        });
        cardConfig.Cards.Add(new CardDefinition
        {
            CardId = "common_help",
            DisplayName = "Common Help",
            DeckId = "common",
            Image = icon,
            FaceImageOverride = overrideFace,
            CardType = CardType.Help,
            Quality = CardQuality.White,
            EffectGraphId = "eg_help_potion"
        });
        cardConfig.Cards.Add(new CardDefinition
        {
            CardId = "class_help",
            DisplayName = "Class Help",
            DeckId = "class",
            CardType = CardType.Help,
            Quality = CardQuality.White,
            EffectGraphId = "eg_help_potion"
        });
        cardConfig.Cards.Add(new CardDefinition
        {
            CardId = "other_class_help",
            DisplayName = "Other Class Help",
            DeckId = "other_class",
            CardType = CardType.Help,
            Quality = CardQuality.White,
            EffectGraphId = "eg_help_potion"
        });

        var characterConfig = ScriptableObject.CreateInstance<TableNineCharacterConfig>();
        mCreatedObjects.Add(characterConfig);
        characterConfig.Characters.Add(new CharacterDefinition
        {
            CharacterId = "hero",
            DisplayName = "Hero",
            CommonDeckId = "common",
            ClassDeckId = "class"
        });

        var master = ScriptableObject.CreateInstance<TableNineGameConfig>();
        mCreatedObjects.Add(master);
        master.CharacterConfig = characterConfig;
        master.CardConfig = cardConfig;
        return master;
    }

    private Sprite CreateSprite(string name)
    {
        var texture = new Texture2D(1, 1);
        texture.name = name + "_texture";
        mCreatedObjects.Add(texture);
        var sprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
        sprite.name = name;
        mCreatedObjects.Add(sprite);
        return sprite;
    }

    private static CardDeckDefinition FindDeck(GameConfigSet config, string deckId)
    {
        for (var i = 0; i < config.CardDecks.Count; i++)
        {
            if (config.CardDecks[i].DeckId == deckId)
            {
                return config.CardDecks[i];
            }
        }

        return null;
    }

    private static CardDefinition FindCard(GameConfigSet config, string cardId)
    {
        for (var i = 0; i < config.Cards.Count; i++)
        {
            if (config.Cards[i].CardId == cardId)
            {
                return config.Cards[i];
            }
        }

        return null;
    }
}
#endif
