#if UNITY_EDITOR
using UnityEngine;

public sealed class WarmConsoleThemePalette
{
    public Color RootBg { get; }
    public Color ContentBg { get; }
    public Color SidebarBg { get; }
    public Color HeaderBg { get; }
    public Color StatCardBg { get; }
    public Color SectionCardBg { get; }

    public Color AccentStrong { get; }
    public Color AccentGoldValue { get; }
    public Color AccentMid { get; }
    public Color AccentWeak { get; }

    public Color TextPrimary { get; }
    public Color TextSecondary { get; }
    public Color TextTertiary { get; }
    public Color TextPath { get; }
    public Color TextChecklist { get; }

    public Color Divider { get; }
    public Color NavNormalBg { get; }
    public Color NavSelectedBg { get; }
    public Color NavStripeNormal { get; }

    public Color PageTitle { get; }
    public Color PageDesc { get; }

    private WarmConsoleThemePalette(
        Color rootBg, Color contentBg, Color sidebarBg, Color headerBg,
        Color statCardBg, Color sectionCardBg,
        Color accentStrong, Color accentGoldValue, Color accentMid, Color accentWeak,
        Color textPrimary, Color textSecondary, Color textTertiary,
        Color textPath, Color textChecklist,
        Color divider, Color navNormalBg, Color navSelectedBg, Color navStripeNormal,
        Color pageTitle, Color pageDesc)
    {
        RootBg = rootBg;
        ContentBg = contentBg;
        SidebarBg = sidebarBg;
        HeaderBg = headerBg;
        StatCardBg = statCardBg;
        SectionCardBg = sectionCardBg;
        AccentStrong = accentStrong;
        AccentGoldValue = accentGoldValue;
        AccentMid = accentMid;
        AccentWeak = accentWeak;
        TextPrimary = textPrimary;
        TextSecondary = textSecondary;
        TextTertiary = textTertiary;
        TextPath = textPath;
        TextChecklist = textChecklist;
        Divider = divider;
        NavNormalBg = navNormalBg;
        NavSelectedBg = navSelectedBg;
        NavStripeNormal = navStripeNormal;
        PageTitle = pageTitle;
        PageDesc = pageDesc;
    }

    public static WarmConsoleThemePalette Character => Create(
        root: (0.10f, 0.085f, 0.07f),
        content: (0.09f, 0.075f, 0.06f),
        sidebar: (0.12f, 0.095f, 0.08f),
        header: (0.13f, 0.10f, 0.08f),
        statCard: (0.16f, 0.12f, 0.09f),
        sectionCard: (0.15f, 0.12f, 0.095f),
        accentStrong: (0.86f, 0.64f, 0.28f),
        accentGold: (0.94f, 0.75f, 0.40f),
        accentMid: (0.83f, 0.62f, 0.26f),
        accentWeak: (0.56f, 0.40f, 0.18f),
        navSelected: (0.31f, 0.22f, 0.12f));

    public static WarmConsoleThemePalette Card => Create(
        root: (0.08f, 0.10f, 0.10f),
        content: (0.07f, 0.09f, 0.09f),
        sidebar: (0.10f, 0.12f, 0.11f),
        header: (0.11f, 0.13f, 0.12f),
        statCard: (0.12f, 0.16f, 0.15f),
        sectionCard: (0.11f, 0.15f, 0.14f),
        accentStrong: (0.45f, 0.78f, 0.68f),
        accentGold: (0.55f, 0.88f, 0.78f),
        accentMid: (0.38f, 0.70f, 0.60f),
        accentWeak: (0.22f, 0.42f, 0.38f),
        navSelected: (0.14f, 0.28f, 0.26f));

    public static WarmConsoleThemePalette Skill => Create(
        root: (0.10f, 0.08f, 0.11f),
        content: (0.09f, 0.07f, 0.10f),
        sidebar: (0.12f, 0.095f, 0.13f),
        header: (0.13f, 0.10f, 0.14f),
        statCard: (0.16f, 0.12f, 0.17f),
        sectionCard: (0.15f, 0.11f, 0.16f),
        accentStrong: (0.72f, 0.55f, 0.92f),
        accentGold: (0.82f, 0.68f, 0.98f),
        accentMid: (0.62f, 0.48f, 0.82f),
        accentWeak: (0.38f, 0.28f, 0.52f),
        navSelected: (0.26f, 0.18f, 0.34f));

    public static WarmConsoleThemePalette Relic => Create(
        root: (0.11f, 0.08f, 0.08f),
        content: (0.10f, 0.07f, 0.07f),
        sidebar: (0.13f, 0.095f, 0.09f),
        header: (0.14f, 0.10f, 0.10f),
        statCard: (0.17f, 0.12f, 0.11f),
        sectionCard: (0.16f, 0.11f, 0.10f),
        accentStrong: (0.88f, 0.58f, 0.52f),
        accentGold: (0.96f, 0.72f, 0.62f),
        accentMid: (0.78f, 0.50f, 0.44f),
        accentWeak: (0.48f, 0.30f, 0.26f),
        navSelected: (0.32f, 0.18f, 0.16f));

    public static WarmConsoleThemePalette Effect => Create(
        root: (0.09f, 0.10f, 0.08f),
        content: (0.08f, 0.09f, 0.07f),
        sidebar: (0.11f, 0.12f, 0.09f),
        header: (0.12f, 0.13f, 0.10f),
        statCard: (0.14f, 0.16f, 0.12f),
        sectionCard: (0.13f, 0.15f, 0.11f),
        accentStrong: (0.58f, 0.78f, 0.48f),
        accentGold: (0.68f, 0.88f, 0.58f),
        accentMid: (0.48f, 0.68f, 0.40f),
        accentWeak: (0.30f, 0.42f, 0.24f),
        navSelected: (0.18f, 0.28f, 0.16f));

    private static WarmConsoleThemePalette Create(
        (float r, float g, float b) root,
        (float r, float g, float b) content,
        (float r, float g, float b) sidebar,
        (float r, float g, float b) header,
        (float r, float g, float b) statCard,
        (float r, float g, float b) sectionCard,
        (float r, float g, float b) accentStrong,
        (float r, float g, float b) accentGold,
        (float r, float g, float b) accentMid,
        (float r, float g, float b) accentWeak,
        (float r, float g, float b) navSelected)
    {
        return new WarmConsoleThemePalette(
            C(root), C(content), C(sidebar), C(header), C(statCard), C(sectionCard),
            C(accentStrong), C(accentGold), C(accentMid), C(accentWeak),
            new Color(0.95f, 0.89f, 0.79f),
            new Color(0.79f, 0.73f, 0.67f),
            new Color(0.78f, 0.72f, 0.68f),
            new Color(0.73f, 0.70f, 0.66f),
            new Color(0.83f, 0.78f, 0.72f),
            new Color(0.22f, 0.18f, 0.14f),
            new Color(0.18f, 0.14f, 0.11f),
            C(navSelected),
            new Color(0.20f, 0.16f, 0.13f),
            new Color(0.95f, 0.90f, 0.80f),
            new Color(0.80f, 0.74f, 0.67f));
    }

    private static Color C((float r, float g, float b) rgb) => new Color(rgb.r, rgb.g, rgb.b);
}
#endif
