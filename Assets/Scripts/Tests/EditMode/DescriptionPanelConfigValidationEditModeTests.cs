using System.Collections.Generic;
using NUnit.Framework;

[Category(TableNineTestCategories.RegressionTests)]
public sealed class DescriptionPanelConfigValidationEditModeTests
{
    [Test]
    public void SanitizeDescription_TruncatesOverLimitText()
    {
        var description = new string('卡', DescriptionPanelTextRules.MaxLength + 4);
        var modified = DescriptionPanelConfigValidation.SanitizeDescription("help_test", ref description, out var message);

        Assert.That(modified, Is.True);
        Assert.That(description.Length, Is.EqualTo(DescriptionPanelTextRules.MaxLength));
        Assert.That(message, Does.Contain("help_test"));
    }

    [Test]
    public void SanitizeDescription_RemovesLineBreaks()
    {
        var description = "飞刀\n待命";
        var modified = DescriptionPanelConfigValidation.SanitizeDescription("help_knife", ref description, out var message);

        Assert.That(modified, Is.True);
        Assert.That(description, Is.EqualTo("飞刀待命"));
        Assert.That(message, Does.Contain("line breaks"));
    }

    [Test]
    public void CollectErrors_FlagsInvalidDescriptions()
    {
        var config = new GameConfigSet();
        config.Cards.Add(new CardDefinition
        {
            CardId = "card_bad",
            Description = new string('超', DescriptionPanelTextRules.MaxLength + 1)
        });

        var errors = new List<string>();
        DescriptionPanelConfigValidation.CollectErrors(config, errors);

        Assert.That(errors.Count, Is.GreaterThan(0));
        Assert.That(errors[0], Does.Contain("card_bad"));
    }

    [Test]
    public void ProductionBundle_DescriptionsAreWithinLimit()
    {
        var bundle = TableNineTestConfig.LoadProductionRuntimeBundle();
        var errors = new List<string>();
        DescriptionPanelConfigValidation.CollectErrors(bundle.Core, errors);

        Assert.That(errors, Is.Empty, string.Join("\n", errors));
    }
}
