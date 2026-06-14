using System.Collections.Generic;

public static class SidePanelDescriptionComposer
{
    public static string ComposeSkill(IConfigModel config, string skillId)
    {
        if (config == null || string.IsNullOrWhiteSpace(skillId))
        {
            return string.Empty;
        }

        var skill = config.GetSkillDefinition(skillId);
        if (skill == null)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        AppendIfPresent(parts, skill.DisplayName);
        AppendIfPresent(parts, skill.Description);
        return DescriptionPanelTextRules.FormatDisplayLines(parts);
    }

    public static string ComposeRelic(IConfigModel config, string relicId)
    {
        if (config == null || string.IsNullOrWhiteSpace(relicId))
        {
            return string.Empty;
        }

        var relic = config.GetRelicDefinition(relicId);
        if (relic == null)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        AppendIfPresent(parts, relic.DisplayName);
        AppendIfPresent(parts, relic.Description);
        return DescriptionPanelTextRules.FormatDisplayLines(parts);
    }

    private static void AppendIfPresent(List<string> parts, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        parts.Add(value.Trim());
    }
}
