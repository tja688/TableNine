using NUnit.Framework;
using QFramework;

[Category(TableNineTestCategories.RegressionTests)]
public sealed class TableNineStage3EditModeTests
{
    [SetUp]
    public void SetUp()
    {
        TableNine.ResetForTests();
    }

    [TearDown]
    public void TearDown()
    {
        TableNine.ResetForTests();
    }

    [Test]
    public void Config_Help_Cards_Have_System_Tags()
    {
        TableNine.InitArchitecture();
        var configModel = TableNine.Interface.GetModel<IConfigModel>();

        AssertHelpCardTag(configModel, DefaultGameConfigFactory.HelpPotionId, HelpCardSystemTag.Recovery);
        AssertHelpCardTag(configModel, DefaultGameConfigFactory.HelpDurableShieldId, HelpCardSystemTag.Defense);
        AssertHelpCardTag(configModel, DefaultGameConfigFactory.HelpThrowingKnifeId, HelpCardSystemTag.DirectDamage);
        AssertHelpCardTag(configModel, DefaultGameConfigFactory.HelpTeleportId, HelpCardSystemTag.Displacement);
        AssertHelpCardTag(configModel, DefaultGameConfigFactory.HelpAttributeUpId, HelpCardSystemTag.Special);
    }

    [Test]
    public void Imp_Initial_Help_Deck_Excludes_Attribute_Up_Card()
    {
        TableNine.InitArchitecture();
        var configModel = TableNine.Interface.GetModel<IConfigModel>();
        var character = configModel.GetCharacterDefinition(DefaultGameConfigFactory.CharacterImpId);

        CollectionAssert.DoesNotContain(character.InitialHelpCardIds, DefaultGameConfigFactory.HelpAttributeUpId);
        Assert.That(character.InitialHelpCardIds.Count, Is.EqualTo(7));
    }

    [Test]
    public void CanAddHelpCard_BypassDeckCapacity_Allows_Over_Capacity()
    {
        StartRun(42);
        var rewardSystem = TableNine.Interface.GetSystem<IRewardSystem>();
        FillHelpDeckToCapacity(12);

        Assert.That(rewardSystem.CanAddHelpCard(DefaultGameConfigFactory.HelpBlessingId), Is.False);
        Assert.That(
            rewardSystem.CanAddHelpCard(DefaultGameConfigFactory.HelpBlessingId, HelpCardAddPolicy.BypassDeckCapacity),
            Is.True);
        Assert.That(
            rewardSystem.TryAddHelpCard(DefaultGameConfigFactory.HelpBlessingId, HelpCardAddPolicy.BypassDeckCapacity),
            Is.True);

        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        Assert.That(deckModel.CountActiveHelpCards(), Is.EqualTo(13));
    }

    [Test]
    public void TrimHelpDeckOverflow_Removes_Last_Added_Cards_At_Node_End()
    {
        StartRun(42);
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var rewardSystem = TableNine.Interface.GetSystem<IRewardSystem>();

        FillHelpDeckToCapacity(12);
        Assert.That(rewardSystem.TryAddHelpCard(DefaultGameConfigFactory.HelpBlessingId, HelpCardAddPolicy.BypassDeckCapacity), Is.True);
        Assert.That(rewardSystem.TryAddHelpCard(DefaultGameConfigFactory.HelpBandageId, HelpCardAddPolicy.BypassDeckCapacity), Is.True);
        Assert.That(deckModel.CountActiveHelpCards(), Is.EqualTo(14));

        rewardSystem.TrimHelpDeckOverflow();

        Assert.That(deckModel.CountActiveHelpCards(), Is.EqualTo(12));
        Assert.That(deckModel.CountHelpCardsById(DefaultGameConfigFactory.HelpBlessingId), Is.EqualTo(0));
        Assert.That(deckModel.CountHelpCardsById(DefaultGameConfigFactory.HelpBandageId), Is.EqualTo(0));
    }

    [Test]
    public void Golden_Chest_Relic_Adds_Gold_Chest_Cards_With_Bypass()
    {
        StartRun(42);
        var relicSystem = TableNine.Interface.GetSystem<IRelicSystem>();
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();

        FillHelpDeckToCapacity(12);
        Assert.That(relicSystem.AddRelic(DefaultGameConfigFactory.RelicGoldenChestId), Is.True);
        Assert.That(deckModel.CountHelpCardsById(DefaultGameConfigFactory.HelpGoldChestId), Is.EqualTo(2));
        Assert.That(deckModel.CountActiveHelpCards(), Is.EqualTo(14));
    }

    [Test]
    public void HelpDeckMessages_Use_Deck_Capacity_Wording()
    {
        Assert.That(HelpDeckMessages.CapacityOrSameNameBlocked, Does.Contain("卡组上限"));
        Assert.That(HelpDeckMessages.CapacityOrSameNameBlocked, Does.Not.Contain("种类上限"));
    }

    private static void AssertHelpCardTag(IConfigModel configModel, string cardId, HelpCardSystemTag expectedTag)
    {
        var definition = configModel.GetCardDefinition(cardId);
        Assert.That(definition.SystemTag, Is.EqualTo(expectedTag), cardId);
        Assert.That(HelpCardSystemTagUtility.ToDisplayName(definition.SystemTag), Is.Not.Empty, cardId);
    }

    private static void StartRun(int seed)
    {
        TableNine.InitArchitecture();
        TableNine.Interface.SendCommand(new StartNewRunCommand(seedOverride: seed));
    }

    private static void FillHelpDeckToCapacity(int targetCount)
    {
        var rewardSystem = TableNine.Interface.GetSystem<IRewardSystem>();
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();
        var fillers = new[]
        {
            DefaultGameConfigFactory.HelpFireballId,
            DefaultGameConfigFactory.HelpViolenceId,
            DefaultGameConfigFactory.HelpBoulderId,
            DefaultGameConfigFactory.HelpBombId,
            DefaultGameConfigFactory.HelpSwapId,
            DefaultGameConfigFactory.HelpSmasherId,
            DefaultGameConfigFactory.HelpFoodId,
            DefaultGameConfigFactory.HelpHealingSpringId,
            DefaultGameConfigFactory.HelpCrashTutorialId,
            DefaultGameConfigFactory.HelpWatchtowerId,
            DefaultGameConfigFactory.HelpDurableShieldId,
            DefaultGameConfigFactory.HelpTeleportId,
            DefaultGameConfigFactory.HelpKidnapId,
            DefaultGameConfigFactory.HelpShieldStrikeTutorialId,
            DefaultGameConfigFactory.HelpBearTrapId
        };

        var index = 0;
        while (deckModel.CountActiveHelpCards() < targetCount && index < fillers.Length * 3)
        {
            rewardSystem.TryAddHelpCard(fillers[index % fillers.Length]);
            index++;
        }
    }
}
