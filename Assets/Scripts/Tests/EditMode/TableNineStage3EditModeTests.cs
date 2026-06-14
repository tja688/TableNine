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

        AssertHelpCardTag(configModel, GameConfigIds.HelpPotionId, HelpCardSystemTag.Recovery);
        AssertHelpCardTag(configModel, GameConfigIds.HelpDurableShieldId, HelpCardSystemTag.Armor);
        AssertHelpCardTag(configModel, GameConfigIds.HelpThrowingKnifeId, HelpCardSystemTag.DirectDamage);
        AssertHelpCardTag(configModel, GameConfigIds.HelpTeleportId, HelpCardSystemTag.Displacement);
        AssertHelpCardTag(configModel, GameConfigIds.HelpAttributeUpId, HelpCardSystemTag.Special);
    }

    [Test]
    public void Imp_Initial_Help_Deck_Includes_Attribute_Up_Card()
    {
        TableNine.InitArchitecture();
        var configModel = TableNine.Interface.GetModel<IConfigModel>();
        var character = configModel.GetCharacterDefinition(GameConfigIds.CharacterImpId);

        CollectionAssert.Contains(character.InitialHelpCardIds, GameConfigIds.HelpAttributeUpId);
        Assert.That(character.InitialHelpCardIds.Count, Is.EqualTo(8));
    }

    [Test]
    public void CanAddHelpCard_BypassDeckCapacity_Allows_Over_Capacity()
    {
        StartRun(42);
        var rewardSystem = TableNine.Interface.GetSystem<IRewardSystem>();
        FillHelpDeckToCapacity(12);

        Assert.That(rewardSystem.CanAddHelpCard(GameConfigIds.HelpBlessingId), Is.False);
        Assert.That(
            rewardSystem.CanAddHelpCard(GameConfigIds.HelpBlessingId, HelpCardAddPolicy.BypassDeckCapacity),
            Is.True);
        Assert.That(
            rewardSystem.TryAddHelpCard(GameConfigIds.HelpBlessingId, HelpCardAddPolicy.BypassDeckCapacity),
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
        Assert.That(rewardSystem.TryAddHelpCard(GameConfigIds.HelpBlessingId, HelpCardAddPolicy.BypassDeckCapacity), Is.True);
        Assert.That(rewardSystem.TryAddHelpCard(GameConfigIds.HelpBandageId, HelpCardAddPolicy.BypassDeckCapacity), Is.True);
        Assert.That(deckModel.CountActiveHelpCards(), Is.EqualTo(14));

        rewardSystem.TrimHelpDeckOverflow();

        Assert.That(deckModel.CountActiveHelpCards(), Is.EqualTo(12));
        Assert.That(deckModel.CountHelpCardsById(GameConfigIds.HelpBlessingId), Is.EqualTo(0));
        Assert.That(deckModel.CountHelpCardsById(GameConfigIds.HelpBandageId), Is.EqualTo(0));
    }

    [Test]
    public void Golden_Chest_Relic_Adds_Gold_Chest_Cards_With_Bypass()
    {
        StartRun(42);
        var relicSystem = TableNine.Interface.GetSystem<IRelicSystem>();
        var deckModel = TableNine.Interface.GetModel<IDeckModel>();

        FillHelpDeckToCapacity(12);
        Assert.That(relicSystem.AddRelic(GameConfigIds.RelicGoldenChestId), Is.True);
        Assert.That(deckModel.CountHelpCardsById(GameConfigIds.HelpGoldChestId), Is.EqualTo(2));
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
            GameConfigIds.HelpFireballId,
            GameConfigIds.HelpViolenceId,
            GameConfigIds.HelpBoulderId,
            GameConfigIds.HelpBombId,
            GameConfigIds.HelpSwapId,
            GameConfigIds.HelpSmasherId,
            GameConfigIds.HelpFoodId,
            GameConfigIds.HelpHealingSpringId,
            GameConfigIds.HelpCrashTutorialId,
            GameConfigIds.HelpWatchtowerId,
            GameConfigIds.HelpDurableShieldId,
            GameConfigIds.HelpTeleportId,
            GameConfigIds.HelpKidnapId,
            GameConfigIds.HelpShieldStrikeTutorialId,
            GameConfigIds.HelpBearTrapId
        };

        var index = 0;
        while (deckModel.CountActiveHelpCards() < targetCount && index < fillers.Length * 3)
        {
            rewardSystem.TryAddHelpCard(fillers[index % fillers.Length]);
            index++;
        }
    }
}
