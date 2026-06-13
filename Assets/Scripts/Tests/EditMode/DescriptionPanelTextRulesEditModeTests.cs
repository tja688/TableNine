using NUnit.Framework;

[Category(TableNineTestCategories.RegressionTests)]
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
    public void TrySanitize_RejectsLineBreakAndTruncation()
    {
        var longText = new string('测', DescriptionPanelTextRules.MaxLength + 3);
        Assert.That(
            DescriptionPanelTextRules.TrySanitize(
                $"前\n{longText}",
                out var sanitized,
                out var wasTruncated,
                out var hadLineBreak),
            Is.False);
        Assert.That(hadLineBreak, Is.True);
        Assert.That(wasTruncated, Is.True);
        Assert.That(sanitized.Length, Is.EqualTo(DescriptionPanelTextRules.MaxLength));
    }

    [Test]
    public void SanitizeForStorage_RemovesLineBreaksAndClamps()
    {
        var sanitized = DescriptionPanelTextRules.SanitizeForStorage("行1\n行2", out var wasModified);
        Assert.That(wasModified, Is.True);
        Assert.That(sanitized, Is.EqualTo("行1行2"));
        Assert.That(DescriptionPanelTextRules.IsWithinLimit(sanitized), Is.True);
    }

    [Test]
    public void Clamp_TruncatesOverLimitText()
    {
        var longText = new string('测', DescriptionPanelTextRules.MaxLength + 5);
        var clamped = DescriptionPanelTextRules.Clamp(longText);
        Assert.That(clamped.Length, Is.EqualTo(DescriptionPanelTextRules.MaxLength));
    }
}
