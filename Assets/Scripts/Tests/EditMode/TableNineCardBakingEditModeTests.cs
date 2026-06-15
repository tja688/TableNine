using NUnit.Framework;
using QFramework;
using UnityEngine;
using UnityEngine.TestTools;

[Category(TableNineTestCategories.RegressionTests)]
public sealed class TableNineCardBakingEditModeTests
{
    [SetUp]
    public void SetUp()
    {
        TableNine.ResetForTests();
        TableNineTestConfig.EnsureProductionConfigLoaded();
    }

    [TearDown]
    public void TearDown()
    {
        TableNine.ResetForTests();
    }

    [Test]
    public void DefinitionRenderData_Uses_Effective_Deck_Sprites_For_Monsters()
    {
        TableNine.InitArchitecture();
        var config = TableNine.Interface.GetModel<IConfigModel>();
        var monster = GetFirstCard(config, CardType.Monster);

        var renderData = BakedCardRenderDataFactory.CreateFromCardDefinition(config, monster);

        Assert.That(renderData, Is.Not.Null);
        Assert.That(renderData.Template, Is.EqualTo(BakedCardFaceTemplate.CardExample));
        Assert.That(renderData.StatMode, Is.EqualTo(BakedCardFaceStatMode.FullStats));
        Assert.That(renderData.FaceSprite, Is.SameAs(config.GetEffectiveCardFaceImage(monster.CardId)));
        Assert.That(renderData.BackSprite, Is.SameAs(config.GetEffectiveCardBackImage(monster.CardId)));
        Assert.That(renderData.MainIconSprite, Is.SameAs(config.GetCardMainImage(monster.CardId)));
    }

    [Test]
    public void RuntimeRenderData_Uses_Current_Monster_Stats()
    {
        TableNine.InitArchitecture();
        TableNine.Interface.SendCommand(new StartNewRunCommand(seedOverride: 73));

        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        var monsterUid = FindMonsterOnBoard();
        var runtime = collectionModel.GetCard(monsterUid);
        runtime.CurrentHp = 9;
        runtime.CurrentArmor = 4;
        runtime.BaseAttack = 11;

        var controller = new TestController();
        var cardData = CardViewDataFactory.Create(controller, monsterUid);
        var renderData = BakedCardRenderDataFactory.CreateRuntime(controller, cardData);

        Assert.That(renderData, Is.Not.Null);
        Assert.That(renderData.Life, Is.EqualTo(9));
        Assert.That(renderData.Attack, Is.EqualTo(11));
        Assert.That(renderData.Defense, Is.EqualTo(4));
        Assert.That(renderData.Size, Is.EqualTo(BakedCardRenderDataFactory.StandardBakeSize));
    }

    [Test]
    public void PlayerRuntimeRenderData_Uses_PlayerCard_Template_And_Composes_Face()
    {
        TableNine.InitArchitecture();
        TableNine.Interface.SendCommand(new StartNewRunCommand(seedOverride: 73));

        var controller = new TestController();
        var playerUid = TableNine.Interface.GetModel<IPlayerModel>().PlayerCardUid;
        var cardData = CardViewDataFactory.Create(controller, playerUid);
        var renderData = BakedCardRenderDataFactory.CreateRuntime(controller, cardData);

        Assert.That(renderData, Is.Not.Null);
        Assert.That(renderData.Template, Is.EqualTo(BakedCardFaceTemplate.PlayerCard));
        Assert.That(renderData.StatMode, Is.EqualTo(BakedCardFaceStatMode.FullStats));
        Assert.That(renderData.DisplayName, Is.Not.Empty);

        var composer = new BakedCardFaceComposer();
        var sprites = composer.ComposeSet(renderData, BakedCardRenderDataFactory.StandardPixelsPerUnit);
        composer.Dispose();

        Assert.That(sprites.HasFace, Is.True);
        Assert.That(sprites.BackSprite, Is.Not.Null);

        var faceSprite = sprites.FaceSprite;
        var backSprite = sprites.BackSprite;
        BakedCardRuntimeSpriteUtility.Release(ref faceSprite);
        BakedCardRuntimeSpriteUtility.Release(ref backSprite);
    }

    [Test]
    public void TutorRenderData_Produces_A_Main_Icon()
    {
        TableNine.InitArchitecture();
        var config = TableNine.Interface.GetModel<IConfigModel>();
        var tutors = config.GetCardsByType(CardType.Tutor);
        if (tutors == null || tutors.Count == 0)
        {
            Assert.Ignore("Production config does not contain tutor cards.");
        }

        var renderData = BakedCardRenderDataFactory.CreateFromCardDefinition(config, tutors[0]);

        Assert.That(renderData, Is.Not.Null);
        Assert.That(renderData.StatMode, Is.EqualTo(BakedCardFaceStatMode.MainIconOnly));
        Assert.That(renderData.MainIconSprite, Is.Not.Null);
    }

    [Test]
    public void HelpRenderData_Hides_Entry_Icon_Placeholders()
    {
#if UNITY_EDITOR
        TableNine.InitArchitecture();
        var config = TableNine.Interface.GetModel<IConfigModel>();
        var help = GetFirstCard(config, CardType.Help);
        var renderData = BakedCardRenderDataFactory.CreateFromCardDefinition(config, help);

        var template = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Cards/CardExample.prefab");
        Assert.That(template, Is.Not.Null);

        var root = Object.Instantiate(template);
        try
        {
            CardExampleFaceBinder.Apply(root.transform, renderData);
            var entryIcons = CardExampleFaceBinder.FindChild(root.transform, "EntryIcons");
            Assert.That(entryIcons, Is.Not.Null);
            Assert.That(entryIcons.gameObject.activeSelf, Is.False);

            for (var i = 0; i < entryIcons.childCount; i++)
            {
                var iconRenderer = entryIcons.GetChild(i).GetComponent<SpriteRenderer>();
                if (iconRenderer == null)
                {
                    continue;
                }

                Assert.That(iconRenderer.enabled, Is.False);
                Assert.That(iconRenderer.sprite, Is.Null);
            }
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
#else
        Assert.Ignore("Editor-only prefab binding test.");
#endif
    }

    [Test]
    public void HelpComposeSet_Produces_Face_Without_Entry_Icon_Container()
    {
        TableNine.InitArchitecture();
        var config = TableNine.Interface.GetModel<IConfigModel>();
        var help = GetFirstCard(config, CardType.Help);
        var renderData = BakedCardRenderDataFactory.CreateFromCardDefinition(config, help);
        var composer = new BakedCardFaceComposer();

        LogAssert.NoUnexpectedReceived();
        var sprites = composer.ComposeSet(renderData, BakedCardRenderDataFactory.StandardPixelsPerUnit);
        composer.Dispose();

        Assert.That(sprites.HasFace, Is.True);

        var faceSprite = sprites.FaceSprite;
        var backSprite = sprites.BackSprite;
        BakedCardRuntimeSpriteUtility.Release(ref faceSprite);
        BakedCardRuntimeSpriteUtility.Release(ref backSprite);
    }

    [Test]
    public void ComposeSet_Crops_Face_And_Back_And_Records_Content_Height()
    {
        TableNine.InitArchitecture();
        var config = TableNine.Interface.GetModel<IConfigModel>();
        var monster = GetFirstCard(config, CardType.Monster);
        var renderData = BakedCardRenderDataFactory.CreateFromCardDefinition(config, monster);
        var composer = new BakedCardFaceComposer();

        LogAssert.NoUnexpectedReceived();
        var sprites = composer.ComposeSet(renderData, BakedCardRenderDataFactory.StandardPixelsPerUnit);
        composer.Dispose();

        Assert.That(sprites.HasFace, Is.True);
        Assert.That(sprites.BackSprite, Is.Not.Null);
        Assert.That(sprites.ContentWorldHeight, Is.GreaterThan(0f));
        Assert.That(sprites.ContentFillRatio, Is.EqualTo(1f).Within(0.001f));
        Assert.That(sprites.FaceSprite.texture.width, Is.GreaterThan(0));
        Assert.That(sprites.FaceSprite.texture.height, Is.GreaterThan(0));
        Assert.That(sprites.FaceSprite.texture.height, Is.LessThan(384));

        var faceSprite = sprites.FaceSprite;
        var backSprite = sprites.BackSprite;
        BakedCardRuntimeSpriteUtility.Release(ref faceSprite);
        BakedCardRuntimeSpriteUtility.Release(ref backSprite);
    }

    [Test]
    public void CardDisplayAdapter_Fits_To_Canonical_World_Height()
    {
        var root = new GameObject("CardAdapterTest");
        try
        {
            var adapter = root.AddComponent<CardDisplayAdapter>();
            adapter.Initialize();
            adapter.SetTargetWorldHeight(BakedCardRenderDataFactory.CanonicalWorldCardHeight);

            var texture = new Texture2D(100, 150, TextureFormat.RGBA32, false);
            var pixels = new Color32[100 * 150];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = new Color32(255, 255, 255, 255);
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 100f, 150f), new Vector2(0.5f, 0.5f), 100f);
            adapter.ApplySprites(new BakedCardSpriteSet(sprite, null, 1.5f, 1f));

            var faceRenderer = root.GetComponentInChildren<SpriteRenderer>(true);
            Assert.That(faceRenderer, Is.Not.Null);
            var worldHeight = faceRenderer.bounds.size.y;
            Assert.That(
                worldHeight,
                Is.EqualTo(BakedCardRenderDataFactory.CanonicalWorldCardHeight).Within(0.05f));

            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void BakedCardFaceVisibility_Switches_On_Pivot_Y_Rotation()
    {
        Assert.That(BakedCardFaceVisibility.IsFaceVisible(0f), Is.True);
        Assert.That(BakedCardFaceVisibility.IsFaceVisible(45f), Is.True);
        Assert.That(BakedCardFaceVisibility.IsFaceVisible(120f), Is.False);
        Assert.That(BakedCardFaceVisibility.IsFaceVisible(180f), Is.False);
        Assert.That(BakedCardFaceVisibility.IsFaceVisible(300f), Is.True);
    }

    private static CardDefinition GetFirstCard(IConfigModel config, CardType cardType)
    {
        var cards = config.GetCardsByType(cardType);
        Assert.That(cards, Is.Not.Null.And.Count.GreaterThan(0), $"Expected at least one {cardType} card in config.");
        return cards[0];
    }

    private static CardUid FindMonsterOnBoard()
    {
        var boardModel = TableNine.Interface.GetModel<IBoardModel>();
        var collectionModel = TableNine.Interface.GetModel<ICollectionModel>();
        for (var i = 1; i <= 9; i++)
        {
            var uid = boardModel.GetCardAt(new BoardSlotNo(i));
            if (uid.HasValue &&
                collectionModel.TryGetCard(uid.Value, out var runtime) &&
                runtime.CardType == CardType.Monster)
            {
                return uid.Value;
            }
        }

        Assert.Fail("No monster found on board.");
        return default;
    }

    private sealed class TestController : IController
    {
        public IArchitecture GetArchitecture()
        {
            return TableNine.Interface;
        }
    }
}
