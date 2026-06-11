using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class UIFallbackPanel : UIPanel, IController
{
    private RectTransform mRoot;
    private TableNineUIRequestPanelData mData;

    public IArchitecture GetArchitecture()
    {
        return TableNine.Interface;
    }

    protected override void OnInit(IUIData uiData = null)
    {
        EnsureRoot();
    }

    protected override void OnOpen(IUIData uiData = null)
    {
        mData = uiData as TableNineUIRequestPanelData ?? new TableNineUIRequestPanelData
        {
            Title = name,
            Message = "UI fallback opened without request data.",
            FallbackStrategy = TableNineUIFallbackStrategy.AtomicPopup
        };

        Rebuild();
    }

    protected override void OnClose()
    {
        ClearRoot();
        mData = null;
    }

    private void EnsureRoot()
    {
        if (mRoot != null)
        {
            return;
        }

        var rootObject = new GameObject("AtomicRoot", typeof(RectTransform));
        rootObject.transform.SetParent(transform, false);
        mRoot = rootObject.GetComponent<RectTransform>();
        Stretch(mRoot);
    }

    private void Rebuild()
    {
        EnsureRoot();
        ClearRoot();

        switch (mData.FallbackStrategy)
        {
            case TableNineUIFallbackStrategy.AtomicRoomChoice:
                BuildRoomChoice();
                break;
            case TableNineUIFallbackStrategy.AtomicShopList:
                BuildShopList();
                break;
            case TableNineUIFallbackStrategy.AtomicStatusPrompt:
                BuildStatusPrompt();
                break;
            case TableNineUIFallbackStrategy.AtomicChoiceList:
                BuildChoiceList("AtomicChoicePanel", 360f, 206f);
                break;
            case TableNineUIFallbackStrategy.AtomicPopup:
            default:
                BuildPopup();
                break;
        }
    }

    private void ClearRoot()
    {
        if (mRoot == null)
        {
            return;
        }

        for (var i = mRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(mRoot.GetChild(i).gameObject);
        }
    }

    private void BuildPopup()
    {
        AddDimmer(mData.BlocksGameplayInput);
        var panel = CreatePanel("AtomicPopupPanel", mRoot, new Vector2(320f, 118f), TextAnchor.MiddleCenter);
        var layout = AddVertical(panel, 8f, 14, 14, 12, 12);
        layout.childAlignment = TextAnchor.MiddleCenter;

        CreateText("Title", panel, SafeText(mData.Title, "提示"), 18, FontStyles.Bold, TextAlignmentOptions.Center);
        CreateText("Message", panel, mData.Message, 14, FontStyles.Normal, TextAlignmentOptions.Center, true);
        CreateButton("CloseButton", panel, SafeText(mData.CloseLabel, "确定"), () =>
        {
            mData.CloseAction?.Invoke(this);
            CloseSelf();
        }, 84f, 24f);
    }

    private void BuildChoiceList(string panelName, float width, float height)
    {
        AddDimmer(mData.BlocksGameplayInput);
        var panel = CreatePanel(panelName, mRoot, new Vector2(width, height), TextAnchor.MiddleCenter);
        AddVertical(panel, 6f, 12, 12, 10, 10);

        CreateText("Title", panel, SafeText(mData.Title, "选择"), 18, FontStyles.Bold, TextAlignmentOptions.Center);
        if (!string.IsNullOrWhiteSpace(mData.Message))
        {
            CreateText("Message", panel, mData.Message, 12, FontStyles.Normal, TextAlignmentOptions.Center, true);
        }

        BuildChoiceSection(panel, mData.Choices, mData.EmptyText, false);

        if (mData.SecondaryChoices.Count > 0)
        {
            CreateText("SecondaryTitle", panel, "其他操作", 12, FontStyles.Bold, TextAlignmentOptions.Left);
            BuildChoiceSection(panel, mData.SecondaryChoices, string.Empty, true);
        }

        AddCloseButtonIfNeeded(panel);
    }

    private void BuildRoomChoice()
    {
        var panel = CreatePanel("AtomicRoomChoicePanel", mRoot, new Vector2(384f, 78f), TextAnchor.LowerCenter);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 10f);

        AddVertical(panel, 4f, 8, 8, 8, 8);
        CreateText("Title", panel, SafeText(mData.Title, "选择房间"), 13, FontStyles.Bold, TextAlignmentOptions.Center);

        var row = CreateRect("Choices", panel);
        var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.spacing = 6f;
        rowLayout.childForceExpandWidth = true;
        rowLayout.childForceExpandHeight = true;
        rowLayout.childControlWidth = true;
        rowLayout.childControlHeight = true;
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 34f;

        for (var i = 0; i < mData.Choices.Count; i++)
        {
            CreateChoiceButton(row, mData.Choices[i], 78f, 30f);
        }
    }

    private void BuildShopList()
    {
        BuildChoiceList("AtomicShopPanel", 394f, 222f);
    }

    private void BuildStatusPrompt()
    {
        var panel = CreatePanel("AtomicStatusPromptPanel", mRoot, new Vector2(260f, 54f), TextAnchor.UpperCenter);
        var rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -10f);

        AddHorizontal(panel, 8f, 8, 8, 8, 8);
        CreateText("Message", panel, SafeText(mData.Message, mData.Title), 12, FontStyles.Normal, TextAlignmentOptions.MidlineLeft, true);
        AddCloseButtonIfNeeded(panel);
    }

    private void BuildChoiceSection(Transform parent, System.Collections.Generic.List<TableNineUIChoiceData> choices, string emptyText, bool compact)
    {
        if (choices.Count == 0)
        {
            if (!string.IsNullOrWhiteSpace(emptyText))
            {
                CreateText("EmptyText", parent, emptyText, 12, FontStyles.Italic, TextAlignmentOptions.Center);
            }

            return;
        }

        var contentParent = parent;
        if (choices.Count > 5)
        {
            contentParent = CreateScrollArea(parent, compact ? 54f : 112f);
        }

        for (var i = 0; i < choices.Count; i++)
        {
            CreateChoiceButton(contentParent, choices[i], 0f, compact ? 20f : 25f);
        }
    }

    private void AddCloseButtonIfNeeded(Transform parent)
    {
        if (string.IsNullOrWhiteSpace(mData.CloseLabel) && mData.CloseAction == null)
        {
            return;
        }

        CreateButton("CloseButton", parent, SafeText(mData.CloseLabel, "关闭"), () =>
        {
            mData.CloseAction?.Invoke(this);
            if (mData.CloseAction == null)
            {
                CloseSelf();
            }
        }, 86f, 24f);
    }

    private void AddDimmer(bool blocksInput)
    {
        if (!blocksInput)
        {
            return;
        }

        var dimmer = CreateRect("Dimmer", mRoot);
        Stretch(dimmer);
        dimmer.SetAsFirstSibling();
        var image = dimmer.gameObject.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.58f);
        image.raycastTarget = true;
    }

    // ── Prefab-aware 创建方法 ──

    private static Transform CreatePanel(string name, Transform parent, Vector2 size, TextAnchor anchor)
    {
        var rect = CreateRect(name, parent);
        rect.sizeDelta = size;

        switch (anchor)
        {
            case TextAnchor.LowerCenter:
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                break;
            case TextAnchor.UpperCenter:
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                break;
            default:
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                break;
        }

        rect.anchoredPosition = Vector2.zero;
        var image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.08f, 0.09f, 0.1f, 0.96f);
        image.raycastTarget = true;
        return rect;
    }

    private static VerticalLayoutGroup AddVertical(Transform target, float spacing, int left, int right, int top, int bottom)
    {
        var layout = target.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(left, right, top, bottom);
        layout.spacing = spacing;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        return layout;
    }

    private static HorizontalLayoutGroup AddHorizontal(Transform target, float spacing, int left, int right, int top, int bottom)
    {
        var layout = target.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(left, right, top, bottom);
        layout.spacing = spacing;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        return layout;
    }

    private TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        string text,
        float fontSize,
        FontStyles style,
        TextAlignmentOptions alignment,
        bool flexibleHeight = false)
    {
        if (mData != null && mData.FallbackTextPrefab != null)
        {
            var obj = UnityEngine.Object.Instantiate(mData.FallbackTextPrefab, parent, false);
            obj.name = name;
            var tmp = obj.GetComponent<TextMeshProUGUI>();
            tmp.text = text ?? string.Empty;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = alignment;
            tmp.color = new Color(0.93f, 0.91f, 0.84f, 1f);
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Ellipsis;
            var layout = obj.GetComponent<LayoutElement>();
            if (layout == null) layout = obj.AddComponent<LayoutElement>();
            layout.minHeight = flexibleHeight ? 24f : 18f;
            layout.preferredHeight = flexibleHeight ? 36f : 20f;
            return tmp;
        }

        var rect = CreateRect(name, parent);
        var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text ?? string.Empty;
        label.fontSize = fontSize;
        label.fontStyle = style;
        label.alignment = alignment;
        label.color = new Color(0.93f, 0.91f, 0.84f, 1f);
        label.enableWordWrapping = true;
        label.overflowMode = TextOverflowModes.Ellipsis;

        var layoutElem = rect.gameObject.AddComponent<LayoutElement>();
        layoutElem.minHeight = flexibleHeight ? 24f : 18f;
        layoutElem.preferredHeight = flexibleHeight ? 36f : 20f;
        return label;
    }

    private void CreateChoiceButton(Transform parent, TableNineUIChoiceData choice, float preferredWidth, float preferredHeight)
    {
        var label = choice.Label;
        if (!string.IsNullOrWhiteSpace(choice.MetaText))
        {
            label = $"{label}  {choice.MetaText}";
        }

        if (!string.IsNullOrWhiteSpace(choice.Description))
        {
            label = $"{label}\n{choice.Description}";
            preferredHeight = Mathf.Max(preferredHeight, 34f);
        }

        CreateButton($"Choice.{choice.Id}", parent, label, () =>
        {
            if (!choice.IsEnabled)
            {
                return;
            }

            choice.Action?.Invoke(this);
            if (mData.CloseOnChoice && choice.CloseAfterClick)
            {
                CloseSelf();
            }
        }, preferredWidth, preferredHeight, choice.IsEnabled);
    }

    private Button CreateButton(
        string name,
        Transform parent,
        string label,
        UnityEngine.Events.UnityAction action,
        float preferredWidth,
        float preferredHeight,
        bool interactable = true)
    {
        if (mData != null && mData.FallbackButtonPrefab != null)
        {
            var obj = UnityEngine.Object.Instantiate(mData.FallbackButtonPrefab, parent, false);
            obj.name = name;
            var button = obj.GetComponent<Button>();
            var image = obj.GetComponent<Image>();
            if (image != null)
            {
                image.color = interactable
                    ? new Color(0.24f, 0.31f, 0.36f, 1f)
                    : new Color(0.13f, 0.15f, 0.16f, 1f);
            }
            button.interactable = interactable;
            if (action != null) button.onClick.AddListener(action);

            var layout = obj.GetComponent<LayoutElement>();
            if (layout == null) layout = obj.AddComponent<LayoutElement>();
            if (preferredWidth > 0f) layout.preferredWidth = preferredWidth;
            layout.minHeight = preferredHeight;
            layout.preferredHeight = preferredHeight;

            var textObj = FindChildText(obj);
            if (textObj != null)
            {
                textObj.text = label ?? string.Empty;
                textObj.fontSize = label != null && label.Contains("\n") ? 9.5f : 11.5f;
                textObj.alignment = TextAlignmentOptions.Center;
                textObj.color = interactable
                    ? new Color(0.96f, 0.95f, 0.9f, 1f)
                    : new Color(0.54f, 0.56f, 0.56f, 1f);
                textObj.enableWordWrapping = true;
                textObj.overflowMode = TextOverflowModes.Ellipsis;
            }
            return button;
        }

        var rect = CreateRect(name, parent);
        var bgImage = rect.gameObject.AddComponent<Image>();
        bgImage.color = interactable
            ? new Color(0.24f, 0.31f, 0.36f, 1f)
            : new Color(0.13f, 0.15f, 0.16f, 1f);

        var btn = rect.gameObject.AddComponent<Button>();
        btn.interactable = interactable;
        btn.targetGraphic = bgImage;
        if (action != null)
        {
            btn.onClick.AddListener(action);
        }

        var le = rect.gameObject.AddComponent<LayoutElement>();
        if (preferredWidth > 0f)
        {
            le.preferredWidth = preferredWidth;
        }

        le.minHeight = preferredHeight;
        le.preferredHeight = preferredHeight;

        var textRect = CreateRect("Label", rect);
        Stretch(textRect);
        textRect.offsetMin = new Vector2(6f, 1f);
        textRect.offsetMax = new Vector2(-6f, -1f);

        var tmp = textRect.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = label ?? string.Empty;
        tmp.fontSize = label != null && label.Contains("\n") ? 9.5f : 11.5f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = interactable
            ? new Color(0.96f, 0.95f, 0.9f, 1f)
            : new Color(0.54f, 0.56f, 0.56f, 1f);
        tmp.enableWordWrapping = true;
        tmp.overflowMode = TextOverflowModes.Ellipsis;

        return btn;
    }

    private static Transform CreateScrollArea(Transform parent, float preferredHeight)
    {
        var viewport = CreateRect("ScrollViewport", parent);
        viewport.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.18f);
        var mask = viewport.gameObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        viewport.gameObject.AddComponent<LayoutElement>().preferredHeight = preferredHeight;

        var content = CreateRect("ScrollContent", viewport);
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;
        AddVertical(content, 4f, 4, 4, 4, 4);
        var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var scrollRect = viewport.gameObject.AddComponent<ScrollRect>();
        scrollRect.content = content;
        scrollRect.viewport = viewport;
        scrollRect.horizontal = false;
        scrollRect.vertical = true;

        return content;
    }

    private static TextMeshProUGUI FindChildText(GameObject parent)
    {
        var tmp = parent.GetComponentInChildren<TextMeshProUGUI>();
        return tmp;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        return obj.GetComponent<RectTransform>();
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    private static string SafeText(string value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }
}

public static class TableNineFallbackPanelPrefabFactory
{
    public static GameObject CreatePrefab()
    {
        var panelObject = new GameObject("UIFallbackPanelPrefab", typeof(RectTransform), typeof(UIFallbackPanel));
        var rect = panelObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        panelObject.SetActive(false);
        panelObject.hideFlags = HideFlags.HideAndDontSave;
        return panelObject;
    }
}
