using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 运行期生成的兜底按钮覆盖层：用 <c>标准Button组件.prefab</c> 渲染一组「标题 + 若干按钮」。
/// 用于流程中暂时没有专属精致 UI 的模态选择（选房间、宝箱选遗物、商店、失败/胜利重开、跳过等）。
/// 由 <see cref="GameFlowDirector"/> 调度，不自行监听领域事件。
/// </summary>
public sealed class FlowChoiceButtonOverlay : MonoBehaviour
{
    public readonly struct Option
    {
        public Option(string label, Action onClick, bool interactable = true)
        {
            Label = label;
            OnClick = onClick;
            Interactable = interactable;
        }

        public string Label { get; }
        public Action OnClick { get; }
        public bool Interactable { get; }
    }

    private const string ButtonPrefabPath = "Assets/Prefabs/UI/标准Button组件.prefab";

    [SerializeField] private GameObject mButtonPrefab;
    [SerializeField] private int mSortingOrder = 5000;
    [SerializeField] private float mButtonWidth = 320f;
    [SerializeField] private float mButtonHeight = 56f;
    [SerializeField] private float mButtonSpacing = 14f;

    private Canvas mCanvas;
    private RectTransform mPanel;
    private Image mBackdrop;
    private TMP_Text mTitleText;
    private RectTransform mButtonContainer;
    private readonly List<GameObject> mSpawnedButtons = new List<GameObject>();

    /// <summary>
    /// 展示一组按钮；title 可为空。重复调用会先清空旧内容。
    /// dimBackground=false 用于和世界空间卡牌选择共存时只挂按钮、不遮挡画面；
    /// anchorBottom=true 把按钮列贴到屏幕底部（典型为「跳过」）。
    /// </summary>
    public void Show(string title, IReadOnlyList<Option> options, bool dimBackground = true, bool anchorBottom = false)
    {
        EnsureBuilt();
        ClearButtons();

        if (mBackdrop != null)
        {
            mBackdrop.color = new Color(0f, 0f, 0f, dimBackground ? 0.55f : 0f);
            mBackdrop.raycastTarget = dimBackground;
        }

        ApplyContainerAnchor(anchorBottom);

        if (mTitleText != null)
        {
            mTitleText.text = title ?? string.Empty;
            mTitleText.gameObject.SetActive(!string.IsNullOrEmpty(title));
        }

        if (options != null)
        {
            for (var i = 0; i < options.Count; i++)
            {
                SpawnButton(options[i]);
            }
        }

        gameObject.SetActive(true);
    }

    private void ApplyContainerAnchor(bool anchorBottom)
    {
        if (mButtonContainer == null)
        {
            return;
        }

        if (anchorBottom)
        {
            mButtonContainer.anchorMin = new Vector2(0.5f, 0f);
            mButtonContainer.anchorMax = new Vector2(0.5f, 0f);
            mButtonContainer.pivot = new Vector2(0.5f, 0f);
            mButtonContainer.anchoredPosition = new Vector2(0f, 80f);
        }
        else
        {
            mButtonContainer.anchorMin = new Vector2(0.5f, 0.5f);
            mButtonContainer.anchorMax = new Vector2(0.5f, 0.5f);
            mButtonContainer.pivot = new Vector2(0.5f, 0.5f);
            mButtonContainer.anchoredPosition = new Vector2(0f, -40f);
        }
    }

    public void Hide()
    {
        ClearButtons();
        if (gameObject.activeSelf)
        {
            gameObject.SetActive(false);
        }
    }

    private void SpawnButton(Option option)
    {
        GameObject instance = null;
        if (mButtonPrefab != null)
        {
            instance = Instantiate(mButtonPrefab, mButtonContainer);
        }

        if (instance == null)
        {
            instance = BuildProceduralButton(mButtonContainer);
        }

        instance.SetActive(true);
        var rect = instance.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.sizeDelta = new Vector2(mButtonWidth, mButtonHeight);
            rect.localScale = Vector3.one;
        }

        var label = instance.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = option.Label;
            label.enableAutoSizing = false;
            label.fontSize = 22f;
            label.alignment = TextAlignmentOptions.Center;
        }
        else
        {
            var legacy = instance.GetComponentInChildren<Text>(true);
            if (legacy != null)
            {
                legacy.text = option.Label;
            }
        }

        var button = instance.GetComponent<Button>();
        if (button == null)
        {
            button = instance.AddComponent<Button>();
        }

        button.interactable = option.Interactable;
        button.onClick.RemoveAllListeners();
        var callback = option.OnClick;
        button.onClick.AddListener(() => callback?.Invoke());

        mSpawnedButtons.Add(instance);
    }

    private void ClearButtons()
    {
        for (var i = 0; i < mSpawnedButtons.Count; i++)
        {
            if (mSpawnedButtons[i] != null)
            {
                Destroy(mSpawnedButtons[i]);
            }
        }

        mSpawnedButtons.Clear();
    }

    private void EnsureBuilt()
    {
        if (mCanvas != null)
        {
            return;
        }

        ResolveButtonPrefab();

        mCanvas = gameObject.GetComponent<Canvas>();
        if (mCanvas == null)
        {
            mCanvas = gameObject.AddComponent<Canvas>();
        }

        mCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        mCanvas.sortingOrder = mSortingOrder;

        if (gameObject.GetComponent<CanvasScaler>() == null)
        {
            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        if (gameObject.GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        var panelObject = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(transform, false);
        mPanel = panelObject.GetComponent<RectTransform>();
        mPanel.anchorMin = Vector2.zero;
        mPanel.anchorMax = Vector2.one;
        mPanel.offsetMin = Vector2.zero;
        mPanel.offsetMax = Vector2.zero;
        mBackdrop = panelObject.GetComponent<Image>();
        mBackdrop.color = new Color(0f, 0f, 0f, 0.55f);
        mBackdrop.raycastTarget = true;

        var titleObject = new GameObject("Title", typeof(RectTransform));
        titleObject.transform.SetParent(mPanel, false);
        var titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -120f);
        titleRect.sizeDelta = new Vector2(900f, 80f);
        mTitleText = titleObject.AddComponent<TextMeshProUGUI>();
        mTitleText.alignment = TextAlignmentOptions.Center;
        mTitleText.fontSize = 34f;
        mTitleText.color = Color.white;

        var containerObject = new GameObject("Buttons", typeof(RectTransform));
        containerObject.transform.SetParent(mPanel, false);
        mButtonContainer = containerObject.GetComponent<RectTransform>();
        mButtonContainer.anchorMin = new Vector2(0.5f, 0.5f);
        mButtonContainer.anchorMax = new Vector2(0.5f, 0.5f);
        mButtonContainer.pivot = new Vector2(0.5f, 0.5f);
        mButtonContainer.anchoredPosition = new Vector2(0f, -40f);
        mButtonContainer.sizeDelta = new Vector2(mButtonWidth + 40f, 600f);
        var layout = containerObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = mButtonSpacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        var fitter = containerObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private void ResolveButtonPrefab()
    {
        if (mButtonPrefab != null)
        {
            return;
        }

#if UNITY_EDITOR
        mButtonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPrefabPath);
#endif
    }

    private GameObject BuildProceduralButton(Transform parent)
    {
        var buttonObject = new GameObject("FallbackButton", typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        var image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.27f, 0.22f, 0.16f, 0.95f);

        var labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(buttonObject.transform, false);
        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
        var label = labelObject.AddComponent<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.fontSize = 22f;

        return buttonObject;
    }
}
