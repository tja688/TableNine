using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using QFramework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(110)]
public sealed class UIGameplayPanel : MonoBehaviour, IController
{
    private enum SidePanelEntryKind
    {
        None,
        Skill,
        Relic
    }

    private sealed class SidePanelIconSlot
    {
        public Image Image;
        public string EntryId;
        public SidePanelEntryKind Kind;
    }

    private sealed class SkillFlowStripRuntime
    {
        public RectTransform Root;
        public RectTransform Overlay;
        public RectTransform OverlayTrack;
        public TMP_Text OverlayText;
        public CanvasGroup OverlayCanvasGroup;
        public bool OverlayVisible;
        public bool LastEnterFromTop;
        public float RepeatWidth;
        public Tween SlideTween;
        public Tween MarqueeTween;
    }

    private readonly List<IUnRegister> mEventRegisters = new List<IUnRegister>();

    [SerializeField] private TMP_Text mCharacterNameText;
    [SerializeField] private TMP_Text mLifeNumberText;
    [SerializeField] private TMP_Text mCoinNumberText;
    [SerializeField] private TMP_Text mSkillText;
    [SerializeField] private TMP_Text mRelicText;
    [SerializeField] private TMP_Text mDescriptionText;
    [SerializeField] private Transform mSkillIconRoot;
    [SerializeField] private Transform mRelicIconRoot;
    [SerializeField] private RectTransform mSkillPanelRoot;

    private SidePanelIconSlot[] mSkillSlots = System.Array.Empty<SidePanelIconSlot>();
    private SidePanelIconSlot[] mSkillFlowSlots = System.Array.Empty<SidePanelIconSlot>();
    private SidePanelIconSlot[] mRelicSlots = System.Array.Empty<SidePanelIconSlot>();
    private string mLastMessage;
    private bool mEventsRegistered;
    private SkillFlowStripRuntime mSkillFlowStrip;
    private string mSkillFlowActiveSkillId;
    private readonly StringBuilder mSkillFlowTextBuilder = new StringBuilder(128);

    public static bool IsSidePanelHovered { get; private set; }

    public IArchitecture GetArchitecture()
    {
        return TableNine.Interface;
    }

    private void Awake()
    {
        mLastMessage = DescriptionPanelTexts.Get(DescriptionPanelTextKeys.HudDefaultHint);
        AutoBind();
        BindSidePanelSlots();
    }

    private void OnEnable()
    {
        RegisterEvents();
        RefreshAll();
    }

    private void OnDisable()
    {
        UnregisterEvents();
        IsSidePanelHovered = false;
        HideSkillFlowMarquee(true);
    }

    private void Update()
    {
        if (TableNine.IsInitialized)
        {
            RefreshAll();
        }
    }

    private void LateUpdate()
    {
        if (mDescriptionText == null)
        {
            mDescriptionText = FindDescriptionTextInScene();
        }

        RefreshSidePanelHoverDescription();
    }

    private void AutoBind()
    {
        mCharacterNameText = mCharacterNameText != null ? mCharacterNameText : FindTmpText("CharacterName");
        mLifeNumberText = mLifeNumberText != null ? mLifeNumberText : FindTmpText("LifeNumber");
        mCoinNumberText = mCoinNumberText != null ? mCoinNumberText : FindTmpText("CoinNumber");
        mSkillText = mSkillText != null ? mSkillText : FindTmpText("SkillText");
        mRelicText = mRelicText != null ? mRelicText : FindTmpText("RelicText");
        mDescriptionText = mDescriptionText != null ? mDescriptionText : FindTmpText("DescriptionText");
        if (mDescriptionText == null)
        {
            mDescriptionText = FindDescriptionTextInScene();
        }
        mSkillIconRoot = mSkillIconRoot != null ? mSkillIconRoot : FindDeep(transform, "SkillPanel/Grids");
        mRelicIconRoot = mRelicIconRoot != null ? mRelicIconRoot : FindDeep(transform, "RelicPanel/Grids");
        mSkillPanelRoot = mSkillPanelRoot != null
            ? mSkillPanelRoot
            : FindDeep(transform, "SkillPanel") as RectTransform;
    }

    private void BindSidePanelSlots()
    {
        mSkillSlots = CollectIconSlots(mSkillIconRoot);
        mRelicSlots = CollectIconSlots(mRelicIconRoot);
        EnsureSkillFlowStrip();
    }

    private static SidePanelIconSlot[] CollectIconSlots(Transform gridsRoot)
    {
        if (gridsRoot == null)
        {
            return System.Array.Empty<SidePanelIconSlot>();
        }

        var slots = new List<SidePanelIconSlot>(gridsRoot.childCount);
        for (var i = 0; i < gridsRoot.childCount; i++)
        {
            var image = gridsRoot.GetChild(i).GetComponent<Image>();
            if (image == null)
            {
                continue;
            }

            image.preserveAspect = true;
            slots.Add(new SidePanelIconSlot { Image = image });
        }

        return slots.ToArray();
    }

    private void RegisterEvents()
    {
        if (mEventsRegistered || !TableNine.IsInitialized)
        {
            return;
        }

        mEventsRegistered = true;
        mEventRegisters.Add(this.RegisterEvent<GameplayMessageEvent>(evt =>
        {
            mLastMessage = DescriptionPanelTexts.Sanitize(evt.Message);
            RefreshDescription();
        }));

        mEventRegisters.Add(this.RegisterEvent<LevelClearReadyEvent>(evt =>
        {
            mLastMessage = DescriptionPanelTexts.Format(
                DescriptionPanelTextKeys.HudLevelClear,
                evt.Layer,
                evt.NodeInLayer);
            RefreshDescription();
        }));

        mEventRegisters.Add(this.RegisterEvent<MonsterKilledEvent>(_ =>
        {
            mLastMessage = DescriptionPanelTexts.Get(DescriptionPanelTextKeys.HudMonsterKilled);
            RefreshAll();
        }));

        mEventRegisters.Add(this.RegisterEvent<BattleDeckChangedEvent>(_ => RefreshAll()));
        mEventRegisters.Add(this.RegisterEvent<DamageAppliedEvent>(_ => RefreshAll()));
        mEventRegisters.Add(this.RegisterEvent<ItemSlotChangedEvent>(_ => RefreshAll()));
        mEventRegisters.Add(this.RegisterEvent<RelicAddedEvent>(_ => RefreshSidePanels()));
        mEventRegisters.Add(this.RegisterEvent<RelicDiscardedEvent>(_ => RefreshSidePanels()));
        mEventRegisters.Add(this.RegisterEvent<RelicStatsChangedEvent>(_ => RefreshSidePanels()));
        mEventRegisters.Add(this.RegisterEvent<TutorSkillChosenEvent>(_ => RefreshSidePanels()));
    }

    private void UnregisterEvents()
    {
        for (var i = 0; i < mEventRegisters.Count; i++)
        {
            mEventRegisters[i].UnRegister();
        }

        mEventRegisters.Clear();
        mEventsRegistered = false;
    }

    private void RefreshAll()
    {
        RefreshPlayer();
        RefreshSidePanels();
        RefreshDescription();
    }

    private void RefreshPlayer()
    {
        if (!TableNine.IsInitialized || !this.GetModel<IRunModel>().IsRunActive.Value)
        {
            if (mLifeNumberText != null) mLifeNumberText.text = "x--";
            if (mCoinNumberText != null) mCoinNumberText.text = "x0";
            return;
        }

        var runModel = this.GetModel<IRunModel>();
        var playerModel = this.GetModel<IPlayerModel>();
        var configModel = this.GetModel<IConfigModel>();
        var stats = this.SendQuery(new GetEffectivePlayerStatsQuery());
        var characterDefinition = configModel.GetCharacterDefinition(runModel.CharacterId);

        if (mCharacterNameText != null)
        {
            mCharacterNameText.text = characterDefinition != null ? characterDefinition.DisplayName : "玩家";
        }

        if (mLifeNumberText != null)
        {
            mLifeNumberText.text = $"x{stats.CurrentHp}";
        }

        if (mCoinNumberText != null)
        {
            mCoinNumberText.text = $"x{playerModel.Gold.Value}";
        }
    }

    private void RefreshSidePanels()
    {
        if (!TableNine.IsInitialized || !this.GetModel<IRunModel>().IsRunActive.Value)
        {
            ClearSidePanelSlots(mSkillSlots);
            ClearSidePanelSlots(mSkillFlowSlots);
            ClearSidePanelSlots(mRelicSlots);
            HideSkillFlowMarquee(true);
            return;
        }

        RefreshSkillIcons();
        RefreshSkillFlowIcons();
        RefreshRelicIcons();
    }

    private void RefreshSkillIcons()
    {
        var playerModel = this.GetModel<IPlayerModel>();
        var configModel = this.GetModel<IConfigModel>();

        for (var i = 0; i < mSkillSlots.Length; i++)
        {
            var slot = mSkillSlots[i];
            if (i >= playerModel.SkillIds.Count)
            {
                ClearSlot(slot);
                continue;
            }

            var skillId = playerModel.SkillIds[i];
            var definition = configModel.GetSkillDefinition(skillId);
            SetSlot(slot, definition?.Image, skillId, SidePanelEntryKind.Skill);
        }
    }

    private void RefreshRelicIcons()
    {
        var playerModel = this.GetModel<IPlayerModel>();
        var configModel = this.GetModel<IConfigModel>();
        var writeIndex = 0;

        for (var i = 0; i < playerModel.Relics.Count && writeIndex < mRelicSlots.Length; i++)
        {
            var relic = playerModel.Relics[i];
            if (relic == null || relic.IsConsumed)
            {
                continue;
            }

            var definition = configModel.GetRelicDefinition(relic.RelicId);
            SetSlot(mRelicSlots[writeIndex], definition?.Image, relic.RelicId, SidePanelEntryKind.Relic);
            writeIndex++;
        }

        for (var i = writeIndex; i < mRelicSlots.Length; i++)
        {
            ClearSlot(mRelicSlots[i]);
        }
    }

    private void RefreshSkillFlowIcons()
    {
        if (mSkillFlowSlots == null || mSkillFlowSlots.Length == 0)
        {
            return;
        }

        var playerModel = this.GetModel<IPlayerModel>();
        var configModel = this.GetModel<IConfigModel>();

        for (var i = 0; i < mSkillFlowSlots.Length; i++)
        {
            var slot = mSkillFlowSlots[i];
            if (i >= playerModel.SkillIds.Count)
            {
                ClearSlot(slot);
                continue;
            }

            var skillId = playerModel.SkillIds[i];
            var definition = configModel.GetSkillDefinition(skillId);
            SetSlot(slot, definition?.Image, skillId, SidePanelEntryKind.Skill);
        }
    }

    private static void SetSlot(SidePanelIconSlot slot, Sprite sprite, string entryId, SidePanelEntryKind kind)
    {
        if (slot?.Image == null)
        {
            return;
        }

        slot.EntryId = entryId;
        slot.Kind = kind;
        slot.Image.sprite = sprite;
        slot.Image.color = sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        slot.Image.raycastTarget = sprite != null;
    }

    private static void ClearSlot(SidePanelIconSlot slot)
    {
        if (slot == null)
        {
            return;
        }

        slot.EntryId = null;
        slot.Kind = SidePanelEntryKind.None;
        if (slot.Image == null)
        {
            return;
        }

        slot.Image.sprite = null;
        slot.Image.color = new Color(1f, 1f, 1f, 0f);
        slot.Image.raycastTarget = false;
    }

    private static void ClearSidePanelSlots(SidePanelIconSlot[] slots)
    {
        if (slots == null)
        {
            return;
        }

        for (var i = 0; i < slots.Length; i++)
        {
            ClearSlot(slots[i]);
        }
    }

    private void RefreshSidePanelHoverDescription()
    {
        IsSidePanelHovered = false;
        if (mDescriptionText == null)
        {
            mDescriptionText = FindDescriptionTextInScene();
        }

        if (mDescriptionText == null || !TableNine.IsInitialized || !this.GetModel<IRunModel>().IsRunActive.Value)
        {
            return;
        }

        var hoveredSlot = TryGetHoveredSlot();
        if (hoveredSlot == null)
        {
            HideSkillFlowMarquee();
            return;
        }

        var description = ComposeHoverDescription(hoveredSlot);
        if (string.IsNullOrWhiteSpace(description))
        {
            HideSkillFlowMarquee();
            return;
        }

        UpdateSkillFlowMarquee(hoveredSlot);
        IsSidePanelHovered = true;
        mDescriptionText.text = description;
    }

    private SidePanelIconSlot TryGetHoveredSlot()
    {
        var hoveredFlow = TryGetHoveredSlot(mSkillFlowSlots);
        if (hoveredFlow != null)
        {
            return hoveredFlow;
        }

        var hovered = TryGetHoveredSlot(mSkillSlots);
        if (hovered != null)
        {
            return hovered;
        }

        return TryGetHoveredSlot(mRelicSlots);
    }

    private static SidePanelIconSlot TryGetHoveredSlot(SidePanelIconSlot[] slots)
    {
        if (slots == null)
        {
            return null;
        }

        for (var i = 0; i < slots.Length; i++)
        {
            var slot = slots[i];
            if (slot == null || slot.Kind == SidePanelEntryKind.None || slot.Image == null)
            {
                continue;
            }

            if (IsPointerOver(slot.Image.rectTransform))
            {
                return slot;
            }
        }

        return null;
    }

    private static bool IsPointerOver(RectTransform rectTransform)
    {
        if (rectTransform == null || !rectTransform.gameObject.activeInHierarchy)
        {
            return false;
        }

        var canvas = rectTransform.GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return false;
        }

        Camera camera = null;
        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            camera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }

        if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, Input.mousePosition, camera))
        {
            return true;
        }

        var corners = new Vector3[4];
        rectTransform.GetWorldCorners(corners);
        var min = corners[0];
        var max = corners[2];
        for (var i = 1; i < corners.Length; i++)
        {
            min = Vector3.Min(min, corners[i]);
            max = Vector3.Max(max, corners[i]);
        }

        var screenMin = RectTransformUtility.WorldToScreenPoint(camera, min);
        var screenMax = RectTransformUtility.WorldToScreenPoint(camera, max);
        var pointer = Input.mousePosition;
        return pointer.x >= screenMin.x && pointer.x <= screenMax.x
               && pointer.y >= screenMin.y && pointer.y <= screenMax.y;
    }

    private static TMP_Text FindDescriptionTextInScene()
    {
        var texts = Object.FindObjectsOfType<TMP_Text>(true);
        for (var i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].name == "DescriptionText")
            {
                return texts[i];
            }
        }

        return null;
    }

    private string ComposeHoverDescription(SidePanelIconSlot slot)
    {
        if (slot == null || string.IsNullOrWhiteSpace(slot.EntryId))
        {
            return string.Empty;
        }

        var configModel = this.GetModel<IConfigModel>();
        return slot.Kind switch
        {
            SidePanelEntryKind.Skill => SidePanelDescriptionComposer.ComposeSkill(configModel, slot.EntryId),
            SidePanelEntryKind.Relic => SidePanelDescriptionComposer.ComposeRelic(configModel, slot.EntryId),
            _ => string.Empty
        };
    }

    private void EnsureSkillFlowStrip()
    {
        if (mSkillPanelRoot == null || mSkillIconRoot == null || mSkillSlots.Length == 0)
        {
            return;
        }

        if (mSkillFlowStrip != null && mSkillFlowSlots.Length > 0)
        {
            return;
        }

        var gridRect = mSkillIconRoot as RectTransform;
        if (gridRect == null)
        {
            return;
        }

        const int flowSlotCount = 5;
        const float slotGap = 2f;
        const float stripGap = 6f;

        var sampleSlot = mSkillSlots[0]?.Image != null
            ? mSkillSlots[0].Image.rectTransform
            : null;
        var iconSize = sampleSlot != null ? Mathf.Max(16f, sampleSlot.rect.width) : 20f;
        var stripWidth = flowSlotCount * iconSize + (flowSlotCount - 1) * slotGap;
        var stripHeight = iconSize;

        var rootTransform = FindDeep(mSkillPanelRoot, "SkillFlowStrip") as RectTransform;
        if (rootTransform == null)
        {
            rootTransform = CreateUiRect("SkillFlowStrip", mSkillPanelRoot);
        }

        rootTransform.anchorMin = new Vector2(1f, 0.5f);
        rootTransform.anchorMax = new Vector2(1f, 0.5f);
        rootTransform.pivot = new Vector2(0.5f, 0.5f);
        rootTransform.sizeDelta = new Vector2(stripWidth, stripHeight);
        rootTransform.anchoredPosition = new Vector2(
            gridRect.anchoredPosition.x,
            gridRect.anchoredPosition.y - (gridRect.rect.height * 0.5f + stripGap + stripHeight * 0.5f));

        var iconRow = CreateOrFindChildRect(rootTransform, "IconRow");
        iconRow.anchorMin = Vector2.zero;
        iconRow.anchorMax = Vector2.one;
        iconRow.offsetMin = Vector2.zero;
        iconRow.offsetMax = Vector2.zero;

        var flowLayout = iconRow.GetComponent<HorizontalLayoutGroup>();
        if (flowLayout == null)
        {
            flowLayout = iconRow.gameObject.AddComponent<HorizontalLayoutGroup>();
        }

        flowLayout.childAlignment = TextAnchor.MiddleCenter;
        flowLayout.childControlWidth = false;
        flowLayout.childControlHeight = false;
        flowLayout.childForceExpandWidth = false;
        flowLayout.childForceExpandHeight = false;
        flowLayout.spacing = slotGap;
        flowLayout.padding = new RectOffset(0, 0, 0, 0);

        var slots = new SidePanelIconSlot[flowSlotCount];
        for (var i = 0; i < flowSlotCount; i++)
        {
            var slotRect = CreateOrFindChildRect(iconRow, $"FlowIcon_{i}");
            slotRect.sizeDelta = new Vector2(iconSize, iconSize);
            var layoutElement = slotRect.GetComponent<LayoutElement>();
            if (layoutElement == null)
            {
                layoutElement = slotRect.gameObject.AddComponent<LayoutElement>();
            }

            layoutElement.preferredWidth = iconSize;
            layoutElement.preferredHeight = iconSize;

            var icon = slotRect.GetComponent<Image>();
            if (icon == null)
            {
                icon = slotRect.gameObject.AddComponent<Image>();
            }

            icon.color = new Color(1f, 1f, 1f, 0f);
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            slots[i] = new SidePanelIconSlot
            {
                Image = icon,
                Kind = SidePanelEntryKind.Skill
            };
        }

        var overlay = CreateOrFindChildRect(rootTransform, "MarqueeOverlay");
        overlay.anchorMin = Vector2.zero;
        overlay.anchorMax = Vector2.one;
        overlay.offsetMin = Vector2.zero;
        overlay.offsetMax = Vector2.zero;

        var overlayImage = overlay.GetComponent<Image>();
        if (overlayImage == null)
        {
            overlayImage = overlay.gameObject.AddComponent<Image>();
        }

        overlayImage.color = new Color(1f, 1f, 1f, 0.9f);
        overlayImage.raycastTarget = false;

        var mask = overlay.GetComponent<RectMask2D>();
        if (mask == null)
        {
            mask = overlay.gameObject.AddComponent<RectMask2D>();
        }

        mask.padding = Vector4.zero;

        var canvasGroup = overlay.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = overlay.gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        var track = CreateOrFindChildRect(overlay, "MarqueeTrack");
        track.anchorMin = new Vector2(0f, 0f);
        track.anchorMax = new Vector2(0f, 1f);
        track.pivot = new Vector2(0f, 0.5f);
        track.anchoredPosition = Vector2.zero;
        track.sizeDelta = new Vector2(stripWidth, 0f);

        var overlayTextRect = CreateOrFindChildRect(track, "MarqueeText");
        overlayTextRect.anchorMin = Vector2.zero;
        overlayTextRect.anchorMax = Vector2.one;
        overlayTextRect.offsetMin = new Vector2(4f, 0f);
        overlayTextRect.offsetMax = new Vector2(-4f, 0f);

        var overlayText = overlayTextRect.GetComponent<TMP_Text>();
        if (overlayText == null)
        {
            overlayText = overlayTextRect.gameObject.AddComponent<TextMeshProUGUI>();
        }

        if (mSkillText != null)
        {
            overlayText.font = mSkillText.font;
            overlayText.fontMaterial = mSkillText.fontSharedMaterial;
            overlayText.fontSize = Mathf.Max(9f, mSkillText.fontSize - 1f);
        }
        else if (mDescriptionText != null)
        {
            overlayText.font = mDescriptionText.font;
            overlayText.fontMaterial = mDescriptionText.fontSharedMaterial;
            overlayText.fontSize = Mathf.Max(9f, mDescriptionText.fontSize - 1f);
        }

        overlayText.alignment = TextAlignmentOptions.MidlineLeft;
        overlayText.enableWordWrapping = false;
        overlayText.overflowMode = TextOverflowModes.Overflow;
        overlayText.color = Color.black;
        overlayText.raycastTarget = false;
        overlayText.text = string.Empty;

        overlay.anchoredPosition = new Vector2(0f, stripHeight + 2f);

        mSkillFlowSlots = slots;
        mSkillFlowStrip = new SkillFlowStripRuntime
        {
            Root = rootTransform,
            Overlay = overlay,
            OverlayTrack = track,
            OverlayText = overlayText,
            OverlayCanvasGroup = canvasGroup
        };
    }

    private void UpdateSkillFlowMarquee(SidePanelIconSlot hoveredSlot)
    {
        if (mSkillFlowStrip == null || hoveredSlot == null || hoveredSlot.Kind != SidePanelEntryKind.Skill)
        {
            HideSkillFlowMarquee();
            return;
        }

        var configModel = this.GetModel<IConfigModel>();
        var definition = configModel.GetSkillDefinition(hoveredSlot.EntryId);
        if (definition == null || string.IsNullOrWhiteSpace(definition.DisplayName))
        {
            HideSkillFlowMarquee();
            return;
        }

        if (mSkillFlowActiveSkillId == hoveredSlot.EntryId && mSkillFlowStrip.OverlayVisible)
        {
            return;
        }

        mSkillFlowActiveSkillId = hoveredSlot.EntryId;
        mSkillFlowStrip.LastEnterFromTop = IsPointerAboveCenter(mSkillFlowStrip.Root);
        ConfigureSkillFlowText(definition.DisplayName);
        ShowSkillFlowMarquee();
    }

    private void ConfigureSkillFlowText(string displayName)
    {
        if (mSkillFlowStrip?.OverlayText == null || string.IsNullOrWhiteSpace(displayName))
        {
            return;
        }

        const int repeatCount = 4;
        mSkillFlowTextBuilder.Clear();
        for (var i = 0; i < repeatCount; i++)
        {
            if (i > 0)
            {
                mSkillFlowTextBuilder.Append("   ");
            }

            mSkillFlowTextBuilder.Append(displayName);
        }

        var overlayText = mSkillFlowStrip.OverlayText;
        overlayText.text = mSkillFlowTextBuilder.ToString();
        overlayText.ForceMeshUpdate();

        var repeatWidth = Mathf.Max(
            overlayText.preferredWidth / repeatCount,
            mSkillFlowStrip.Root.rect.width);
        mSkillFlowStrip.RepeatWidth = repeatWidth;
        mSkillFlowStrip.OverlayTrack.sizeDelta = new Vector2(repeatWidth * 2f, 0f);
        mSkillFlowStrip.OverlayTrack.anchoredPosition = Vector2.zero;
    }

    private void ShowSkillFlowMarquee()
    {
        if (mSkillFlowStrip == null)
        {
            return;
        }

        KillSkillFlowTweens();

        var rootHeight = Mathf.Max(1f, mSkillFlowStrip.Root.rect.height);
        var startY = mSkillFlowStrip.LastEnterFromTop ? rootHeight + 2f : -rootHeight - 2f;
        if (!mSkillFlowStrip.OverlayVisible)
        {
            mSkillFlowStrip.Overlay.anchoredPosition = new Vector2(0f, startY);
            mSkillFlowStrip.OverlayCanvasGroup.alpha = 0f;
        }

        mSkillFlowStrip.OverlayVisible = true;
        mSkillFlowStrip.SlideTween = DOTween.Sequence()
            .Join(TweenAnchorPos(mSkillFlowStrip.Overlay, Vector2.zero, 0.22f, Ease.OutCubic))
            .Join(TweenCanvasGroupAlpha(mSkillFlowStrip.OverlayCanvasGroup, 1f, 0.18f, Ease.OutCubic));

        if (mSkillFlowStrip.RepeatWidth <= mSkillFlowStrip.Root.rect.width + 1f)
        {
            return;
        }

        var duration = Mathf.Max(4f, mSkillFlowStrip.RepeatWidth / 20f);
        mSkillFlowStrip.MarqueeTween = TweenAnchorPosX(
                mSkillFlowStrip.OverlayTrack,
                -mSkillFlowStrip.RepeatWidth,
                duration,
                Ease.Linear)
            .SetLoops(-1, LoopType.Restart);
    }

    private void HideSkillFlowMarquee(bool immediate = false)
    {
        if (mSkillFlowStrip == null)
        {
            return;
        }

        mSkillFlowActiveSkillId = null;
        if (!mSkillFlowStrip.OverlayVisible && !immediate)
        {
            return;
        }

        KillSkillFlowTweens();
        mSkillFlowStrip.OverlayVisible = false;

        if (immediate)
        {
            mSkillFlowStrip.Overlay.anchoredPosition = new Vector2(0f, mSkillFlowStrip.Root.rect.height + 2f);
            mSkillFlowStrip.OverlayCanvasGroup.alpha = 0f;
            return;
        }

        var endY = mSkillFlowStrip.LastEnterFromTop
            ? mSkillFlowStrip.Root.rect.height + 2f
            : -mSkillFlowStrip.Root.rect.height - 2f;
        mSkillFlowStrip.SlideTween = DOTween.Sequence()
            .Join(TweenAnchorPosY(mSkillFlowStrip.Overlay, endY, 0.18f, Ease.InCubic))
            .Join(TweenCanvasGroupAlpha(mSkillFlowStrip.OverlayCanvasGroup, 0f, 0.16f, Ease.InCubic));
    }

    private void KillSkillFlowTweens()
    {
        if (mSkillFlowStrip == null)
        {
            return;
        }

        mSkillFlowStrip.SlideTween?.Kill();
        mSkillFlowStrip.SlideTween = null;
        mSkillFlowStrip.MarqueeTween?.Kill();
        mSkillFlowStrip.MarqueeTween = null;
    }

    private static bool IsPointerAboveCenter(RectTransform rectTransform)
    {
        if (rectTransform == null)
        {
            return true;
        }

        var canvas = rectTransform.GetComponentInParent<Canvas>();
        Camera camera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            camera = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rectTransform,
                Input.mousePosition,
                camera,
                out var localPoint))
        {
            return true;
        }

        return localPoint.y >= 0f;
    }

    private static Tween TweenAnchorPos(
        RectTransform target,
        Vector2 endValue,
        float duration,
        Ease ease)
    {
        return DOTween.To(
                () => target.anchoredPosition,
                value => target.anchoredPosition = value,
                endValue,
                duration)
            .SetEase(ease);
    }

    private static Tween TweenAnchorPosX(
        RectTransform target,
        float endX,
        float duration,
        Ease ease)
    {
        return DOTween.To(
                () => target.anchoredPosition,
                value => target.anchoredPosition = value,
                new Vector2(endX, target.anchoredPosition.y),
                duration)
            .SetEase(ease);
    }

    private static Tween TweenAnchorPosY(
        RectTransform target,
        float endY,
        float duration,
        Ease ease)
    {
        return DOTween.To(
                () => target.anchoredPosition,
                value => target.anchoredPosition = value,
                new Vector2(target.anchoredPosition.x, endY),
                duration)
            .SetEase(ease);
    }

    private static Tween TweenCanvasGroupAlpha(
        CanvasGroup canvasGroup,
        float endAlpha,
        float duration,
        Ease ease)
    {
        return DOTween.To(
                () => canvasGroup.alpha,
                value => canvasGroup.alpha = value,
                endAlpha,
                duration)
            .SetEase(ease);
    }

    private static RectTransform CreateUiRect(string name, Transform parent)
    {
        var child = new GameObject(name, typeof(RectTransform));
        child.layer = parent.gameObject.layer;
        var rectTransform = child.GetComponent<RectTransform>();
        rectTransform.SetParent(parent, false);
        rectTransform.localScale = Vector3.one;
        return rectTransform;
    }

    private static RectTransform CreateOrFindChildRect(Transform parent, string childName)
    {
        var existing = FindDirectChild(parent, childName) as RectTransform;
        return existing != null ? existing : CreateUiRect(childName, parent);
    }

    private static Transform FindDirectChild(Transform parent, string childName)
    {
        if (parent == null)
        {
            return null;
        }

        for (var i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

    private void RefreshDescription()
    {
        if (mDescriptionText == null)
        {
            mDescriptionText = FindDescriptionTextInScene();
        }

        if (mDescriptionText == null)
        {
            return;
        }

        if (!TableNine.IsInitialized || !this.GetModel<IRunModel>().IsRunActive.Value)
        {
            mDescriptionText.text = DescriptionPanelTexts.Get(DescriptionPanelTextKeys.HudPreparing);
            return;
        }

        var deckModel = this.GetModel<IDeckModel>();
        var flowModel = this.GetModel<IFlowModel>();
        if (deckModel.PendingHelpCardAction.Kind == PendingHelpCardActionKind.ThrowingKnifeTarget)
        {
            mDescriptionText.text = DescriptionPanelTexts.Get(DescriptionPanelTextKeys.HudThrowingKnifeReady);
            return;
        }

        if (deckModel.PendingHelpCardAction.Kind == PendingHelpCardActionKind.AttributeChoice)
        {
            mDescriptionText.text = DescriptionPanelTexts.Get(DescriptionPanelTextKeys.HudAttributeChoice);
            return;
        }

        if (flowModel.Phase.Value == FlowPhase.ClearReady)
        {
            mDescriptionText.text = DescriptionPanelTexts.Get(DescriptionPanelTextKeys.HudNodeClearReady);
            return;
        }

        mDescriptionText.text = DescriptionPanelTexts.Sanitize(mLastMessage);
    }

    private TMP_Text FindTmpText(string childName)
    {
        var child = FindDeep(transform, childName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private static Transform FindDeep(Transform root, string childPath)
    {
        if (root == null || string.IsNullOrWhiteSpace(childPath))
        {
            return null;
        }

        var segments = childPath.Split('/');
        var current = root;
        for (var i = 0; i < segments.Length; i++)
        {
            current = FindChildByName(current, segments[i]);
            if (current == null)
            {
                return null;
            }
        }

        return current;
    }

    private static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == childName)
        {
            return root;
        }

        for (var i = 0; i < root.childCount; i++)
        {
            var result = FindDeep(root.GetChild(i), childName);
            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}
