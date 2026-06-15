#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class StaggeredMenuViewBuilder
{
    private const string FontAssetPath = "Assets/Arts/Fronts/fengheguai/fusion-pixel-12px-proportional SDF.asset";

    [MenuItem("TableNine/UI/Build Staggered Menu In UIPopupPanel")]
    public static void BuildInSelectedPopupPanel()
    {
        var popup = Selection.activeGameObject != null
            ? Selection.activeGameObject.GetComponentInParent<UIPopupPanel>()
            : null;

        if (popup == null)
        {
            popup = Object.FindObjectOfType<UIPopupPanel>(true);
        }

        if (popup == null)
        {
            EditorUtility.DisplayDialog("Staggered Menu", "请先选中场景或 Prefab 中的 UIPopupPanel。", "OK");
            return;
        }

        BuildUnder(popup.transform);
        EditorUtility.SetDirty(popup.gameObject);
    }

    public static StaggeredMenuView BuildUnder(Transform popupRoot)
    {
        var existing = popupRoot.Find("StaggeredMenu");
        if (existing != null)
        {
            Object.DestroyImmediate(existing.gameObject);
        }

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (font == null)
        {
            font = Resources.FindObjectsOfTypeAll<TMP_FontAsset>()[0];
        }

        var root = CreateRect("StaggeredMenu", popupRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        root.SetAsLastSibling();

        var clickAway = CreateImage("ClickAwayBlocker", root, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
            new Color(0f, 0f, 0f, 0f));
        clickAway.raycastTarget = false;

        var preLayersRoot = CreateRect("PreLayers", root, new Vector2(1f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        preLayersRoot.sizeDelta = new Vector2(162f, 0f);
        preLayersRoot.pivot = new Vector2(1f, 0.5f);

        var preLayerColors = new[]
        {
            new Color32(0xB4, 0x97, 0xCF, 0xFF),
            new Color32(0x52, 0x27, 0xFF, 0xFF)
        };
        var preLayers = new RectTransform[preLayerColors.Length];
        for (var i = 0; i < preLayerColors.Length; i++)
        {
            var layer = CreateImage($"PreLayer{i}", preLayersRoot, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                preLayerColors[i]);
            layer.raycastTarget = false;
            preLayers[i] = layer.rectTransform;
            preLayers[i].anchoredPosition = new Vector2(162f, 0f);
        }

        var panel = CreateImage("MenuPanel", root, new Vector2(1f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero,
            Color.white);
        panel.rectTransform.sizeDelta = new Vector2(162f, 0f);
        panel.rectTransform.pivot = new Vector2(1f, 0.5f);
        panel.rectTransform.anchoredPosition = new Vector2(162f, 0f);

        var panelInner = CreateRect("PanelInner", panel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 1f),
            new Vector2(12f, 12f), new Vector2(-12f, -48f));

        var header = CreateRect("Header", root, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(8f, -8f),
            new Vector2(-8f, -24f));

        var logoText = CreateTmp("LogoText", header, "TABLE NINE", 8, TextAlignmentOptions.MidlineLeft, font,
            Color.white);
        StretchLeft(logoText.rectTransform, 0f, 0f, 0.55f, 1f);

        var toggleButton = CreateImage("ToggleButton", header, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-72f, 0f), new Vector2(72f, 18f), new Color(1f, 1f, 1f, 0f));
        toggleButton.gameObject.AddComponent<StaggeredMenuTextButton>();

        var toggleTextWrap = CreateRect("ToggleTextWrap", toggleButton.rectTransform, new Vector2(0f, 0.5f),
            new Vector2(0f, 0.5f), new Vector2(0f, -6f), new Vector2(44f, 12f));
        toggleTextWrap.gameObject.AddComponent<RectMask2D>();

        var toggleTextInner = CreateRect("ToggleTextInner", toggleTextWrap, new Vector2(0f, 1f), new Vector2(0f, 1f),
            Vector2.zero, new Vector2(44f, 24f));
        toggleTextInner.pivot = new Vector2(0f, 1f);

        var toggleLine0 = CreateTmp("Line0", toggleTextInner, "Menu", 8, TextAlignmentOptions.MidlineLeft, font,
            Color.white);
        SetTopLine(toggleLine0.rectTransform, 0f, 12f);
        var toggleLine1 = CreateTmp("Line1", toggleTextInner, "Close", 8, TextAlignmentOptions.MidlineLeft, font,
            Color.white);
        SetTopLine(toggleLine1.rectTransform, 12f, 12f);
        toggleLine1.gameObject.SetActive(true);

        var toggleIcon = CreateRect("ToggleIcon", toggleButton.rectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
            new Vector2(-7f, -7f), new Vector2(14f, 14f));

        CreateIconLine("PlusH", toggleIcon, new Vector2(0f, 0f), new Vector2(1f, 0f), 2f);
        CreateIconLine("PlusV", toggleIcon, new Vector2(0f, 0f), new Vector2(0f, 1f), 2f);

        var menuList = CreateRect("MenuItems", panelInner, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, -8f), new Vector2(0f, -96f));
        var menuLayout = menuList.gameObject.AddComponent<VerticalLayoutGroup>();
        menuLayout.spacing = 4f;
        menuLayout.childAlignment = TextAnchor.UpperLeft;
        menuLayout.childControlWidth = true;
        menuLayout.childControlHeight = true;
        menuLayout.childForceExpandWidth = true;
        menuLayout.childForceExpandHeight = false;

        var menuLabels = new[] { "HOME", "ABOUT", "SERVICES", "CONTACT" };
        var menuBindings = new List<StaggeredMenuView.MenuItemBinding>();
        for (var i = 0; i < menuLabels.Length; i++)
        {
            var wrap = CreateRect($"Item{i}", menuList, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero,
                new Vector2(0f, 26f));
            wrap.gameObject.AddComponent<LayoutElement>().preferredHeight = 26f;
            wrap.gameObject.AddComponent<RectMask2D>();

            var labelRect = CreateRect("LabelRect", wrap, new Vector2(0f, 0f), new Vector2(1f, 1f), Vector2.zero,
                Vector2.zero);
            var label = CreateTmp("Text", labelRect, menuLabels[i], 18, TextAlignmentOptions.BottomLeft, font,
                Color.black);
            Stretch(label.rectTransform);
            label.gameObject.AddComponent<StaggeredMenuTextButton>();

            var number = CreateTmp("Number", wrap, (i + 1).ToString("00"), 8, TextAlignmentOptions.TopRight, font,
                new Color32(0x52, 0x27, 0xFF, 0xFF));
            number.rectTransform.anchorMin = new Vector2(1f, 1f);
            number.rectTransform.anchorMax = new Vector2(1f, 1f);
            number.rectTransform.pivot = new Vector2(1f, 1f);
            number.rectTransform.anchoredPosition = new Vector2(-4f, -2f);
            number.rectTransform.sizeDelta = new Vector2(24f, 12f);
            var numberColor = number.color;
            numberColor.a = 0f;
            number.color = numberColor;

            menuBindings.Add(new StaggeredMenuView.MenuItemBinding
            {
                Wrapper = wrap,
                LabelRect = labelRect,
                Label = label,
                Number = number
            });
        }

        var socials = CreateRect("Socials", panelInner, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 8f),
            new Vector2(0f, 52f));
        var socialLayout = socials.gameObject.AddComponent<VerticalLayoutGroup>();
        socialLayout.spacing = 6f;
        socialLayout.childAlignment = TextAnchor.LowerLeft;
        socialLayout.childControlWidth = true;
        socialLayout.childControlHeight = true;
        socialLayout.childForceExpandWidth = true;
        socialLayout.childForceExpandHeight = false;

        var socialTitle = CreateTmp("SocialsTitle", socials, "SOCIALS", 8, TextAlignmentOptions.BottomLeft, font,
            new Color32(0xFF, 0x6B, 0x6B, 0xFF));
        socialTitle.gameObject.AddComponent<LayoutElement>().preferredHeight = 12f;
        var socialTitleColor = socialTitle.color;
        socialTitleColor.a = 0f;
        socialTitle.color = socialTitleColor;

        var linksRoot = CreateRect("Links", socials, new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero,
            new Vector2(0f, 20f));
        linksRoot.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;
        var linksLayout = linksRoot.gameObject.AddComponent<HorizontalLayoutGroup>();
        linksLayout.spacing = 10f;
        linksLayout.childAlignment = TextAnchor.MiddleLeft;
        linksLayout.childControlWidth = false;
        linksLayout.childControlHeight = true;
        linksLayout.childForceExpandWidth = false;
        linksLayout.childForceExpandHeight = false;

        var socialLabels = new[] { "Twitter", "GitHub", "LinkedIn" };
        var socialLinks = new List<TMP_Text>();
        for (var i = 0; i < socialLabels.Length; i++)
        {
            var link = CreateTmp($"Text{i}", linksRoot, socialLabels[i], 9, TextAlignmentOptions.MidlineLeft, font,
                Color.black);
            link.gameObject.AddComponent<LayoutElement>().preferredWidth = 56f;
            link.gameObject.AddComponent<StaggeredMenuTextButton>();
            var linkColor = link.color;
            linkColor.a = 0f;
            link.color = linkColor;
            var rt = link.rectTransform;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, 25f);
            socialLinks.Add(link);
        }

        var view = root.gameObject.AddComponent<StaggeredMenuView>();
        view.EditorAssign(
            preLayersRoot,
            preLayers,
            panel.rectTransform,
            toggleButton.rectTransform,
            toggleTextInner,
            toggleIcon,
            toggleLine0,
            menuBindings.ToArray(),
            socialTitle,
            socialLinks.ToArray(),
            clickAway,
            logoText);

        return view;
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = parent.gameObject.layer;
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = sizeDelta;
        rt.localScale = Vector3.one;
        return rt;
    }

    private static Image CreateImage(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 anchoredPos, Vector2 sizeDelta, Color color)
    {
        var rt = CreateRect(name, parent, anchorMin, anchorMax, anchoredPos, sizeDelta);
        var image = rt.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        return image;
    }

    private static TMP_Text CreateTmp(string name, Transform parent, string text, float fontSize,
        TextAlignmentOptions align, TMP_FontAsset font, Color color)
    {
        var rt = CreateRect(name, parent, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero,
            new Vector2(120f, fontSize + 4f));
        var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.font = font;
        tmp.fontSize = fontSize;
        tmp.alignment = align;
        tmp.text = text;
        tmp.color = color;
        tmp.raycastTarget = true;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Overflow;
        return tmp;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void StretchLeft(RectTransform rt, float minX, float minY, float maxX, float maxY)
    {
        rt.anchorMin = new Vector2(minX, minY);
        rt.anchorMax = new Vector2(maxX, maxY);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private static void SetTopLine(RectTransform rt, float y, float height)
    {
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(0f, -y);
        rt.sizeDelta = new Vector2(0f, height);
    }

    private static void CreateIconLine(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, float thickness)
    {
        var line = CreateImage(name, parent, anchorMin, anchorMax, Vector2.zero, Vector2.zero, Color.white);
        line.raycastTarget = false;
        if (anchorMin.x == anchorMax.x)
        {
            line.rectTransform.sizeDelta = new Vector2(thickness, 0f);
        }
        else
        {
            line.rectTransform.sizeDelta = new Vector2(0f, thickness);
        }
    }
}
#endif
