public static class DescriptionPanelTexts
{
    public static string Get(string key)
    {
        return DescriptionPanelTextRules.Clamp(DescriptionPanelTextDefaults.GetFallback(key));
    }

    public static string Format(string key, params object[] args)
    {
        var format = DescriptionPanelTextDefaults.GetFallback(key);
        return DescriptionPanelTextRules.Clamp(DescriptionPanelTextRules.Format(format, args));
    }

    public static string Sanitize(string text)
    {
        return DescriptionPanelTextRules.Clamp(text ?? string.Empty);
    }
}
