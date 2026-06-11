using NUnit.Framework;

public sealed class DescriptionPanelTextRulesEditModeTests
{
    [Test]
    public void RecommendedDefaults_AreWithinMaxLength()
    {
        var entries = DescriptionPanelTextDefaults.CreateRecommendedEntries();
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            Assert.That(
                DescriptionPanelTextRules.IsWithinLimit(entry.Text),
                Is.True,
                $"Key {entry.Key} exceeds {DescriptionPanelTextRules.MaxLength} chars: {entry.Text}");
            Assert.That(
                DescriptionPanelTextRules.ContainsLineBreak(entry.Text),
                Is.False,
                $"Key {entry.Key} contains line breaks: {entry.Text}");
        }
    }

    [Test]
    public void Format_SampleDynamicTexts_AreWithinMaxLength()
    {
        Assert.That(
            DescriptionPanelTextRules.IsWithinLimit(
                DescriptionPanelTextRules.Format(
                    DescriptionPanelTextDefaults.GetFallback(DescriptionPanelTextKeys.MsgThrowingKnifeHit),
                    "黑桃2")),
            Is.True);
        Assert.That(
            DescriptionPanelTextRules.IsWithinLimit(
                DescriptionPanelTextRules.Format(
                    DescriptionPanelTextDefaults.GetFallback(DescriptionPanelTextKeys.MsgHelpCardGained),
                    "普通宝箱卡")),
            Is.True);
        Assert.That(
            DescriptionPanelTextRules.IsWithinLimit(
                DescriptionPanelTextRules.Format(
                    DescriptionPanelTextDefaults.GetFallback(DescriptionPanelTextKeys.HudLevelClear),
                    1,
                    9)),
            Is.True);
    }

    [Test]
    public void Clamp_TruncatesOverLimitText()
    {
        var longText = new string('测', DescriptionPanelTextRules.MaxLength + 5);
        var clamped = DescriptionPanelTextRules.Clamp(longText);
        Assert.That(clamped.Length, Is.EqualTo(DescriptionPanelTextRules.MaxLength));
    }
}
