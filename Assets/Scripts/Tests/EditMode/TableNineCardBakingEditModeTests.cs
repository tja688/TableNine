using NUnit.Framework;
using QFramework;

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
