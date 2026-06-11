using UnityEngine;

/// <summary>
/// DescriptionPanel 文案运行时入口。由 GameplayBootstrap 注入配置资产。
/// </summary>
public static class DescriptionPanelTexts
{
    private static TableNineDescriptionPanelConfig sConfig;

    public static void Initialize(TableNineDescriptionPanelConfig config)
    {
        sConfig = config;
    }

    public static string Get(string key)
    {
        var text = sConfig != null ? sConfig.GetText(key) : DescriptionPanelTextDefaults.GetFallback(key);
        return DescriptionPanelTextRules.Clamp(text);
    }

    public static string Format(string key, params object[] args)
    {
        if (sConfig != null)
        {
            return sConfig.Format(key, args);
        }

        var format = DescriptionPanelTextDefaults.GetFallback(key);
        return DescriptionPanelTextRules.Clamp(DescriptionPanelTextRules.Format(format, args));
    }

    public static string Sanitize(string text)
    {
        return DescriptionPanelTextRules.Clamp(text ?? string.Empty);
    }

#if UNITY_EDITOR
    public static TableNineDescriptionPanelConfig EditorConfig => sConfig;
#endif
}
