using System.Collections.Generic;
using QFramework;
using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 临时测试：复刻 React Bits Dock 卡牌栏（近距放大 + 弹簧动画）。
/// 纯 Card.prefab + 真实 SO 烘焙链路；悬停时走 DescriptionText 描述；点击选中，按住后轻拖或长按可拖动。
/// 不需要时整对象删除。
/// </summary>
[DefaultExecutionOrder(235)]
public sealed class DockCardsWorldDemo : MonoBehaviour, IController
{
    private const string StandardCardPrefabPath = "Assets/Prefabs/Cards/Card.prefab";
    private const string DockBackgroundSpritePath = "Assets/Arts/External/StoreAssets/经典卡牌/blank_card.png";
    private const string DockCardsSortingLayerName = "Cards_Drag";
    private const string DockBackgroundSortingLayerName = "BG_Table";

    [Header("Dock (React Bits defaults)")]
    [SerializeField] private int mCardCount = 5;
    [SerializeField] private float mBaseItemSizePixels = 50f;
    [SerializeField] private float mMagnificationPixels = 70f;
    [SerializeField] private float mProximityPixels = 200f;
    [SerializeField] private float mItemGapPixels = 16f;
    [SerializeField] private float mSpringStiffness = 150f;
    [SerializeField] private float mSpringDamping = 12f;
    [SerializeField] private float mHoverLiftPixels = 18f;

    [Header("Selection & Drag")]
    [SerializeField] private float mLongPressDuration = 0.35f;
    [SerializeField] private float mDragStartThresholdPixels = 16f;
    [SerializeField] private float mDragReturnDuration = 0.25f;

    [Header("Assets")]
    [SerializeField] private GameObject mCardPrefab;
    [SerializeField] private GameObject mCardFaceTemplate;
    [SerializeField] private GameObject mPlayerCardTemplate;
    [SerializeField] private float mWorldCardHeight = BakedCardRenderDataFactory.CanonicalWorldCardHeight;
    [SerializeField] private int mBaseSortingOrder = 440;

    [Header("Dock Placement")]
    [SerializeField] private float mViewportY = 0.1f;
    [SerializeField] private Camera mCamera;
    [SerializeField] private string mDefaultHint = "悬停放大查看描述；点击选中，按住后轻拖或长按可拖动";

    [Header("Dock Background")]
    [SerializeField] private Sprite mDockBackgroundSprite;
    [SerializeField] [Range(0f, 1f)] private float mDockBackgroundAlpha = 0.33333334f;
    [SerializeField] private Rect mDockBackgroundCollapsedRectLocal = new Rect(-5.8f, -1.95f, 11.6f, 2.3f);
    [SerializeField] private Rect mDockBackgroundExpandedRectLocal = new Rect(-6.25f, -1.95f, 12.5f, 7.4f);
    [SerializeField] private int mDockBackgroundSortingOrder = 1;

    private readonly List<DockCardEntry> mCards = new List<DockCardEntry>();

    private Transform mDockRoot;
    private Transform mDockBackgroundRoot;
    private BakedCardFaceComposer mComposer;
    private TMP_Text mDescriptionText;
    private string mDefaultDescription;
    private int mHoveredIndex = -1;
    private int mSelectedIndex = -1;
    private DockCardEntry mDraggingEntry;
    private Vector3 mDragPointerOffset;

    private bool mPressTracking;
    private int mPressIndex = -1;
    private float mPressStartedAt;
    private Vector2 mPressStartedScreen;

    private float mSlotWidthWorld;
    private float mMagnificationRatio = 1.4f;
    private float mProximityWorld;
    private float mItemGapWorld;
    private float mHoverLiftWorld;
    private SpriteRenderer mDockBackgroundRenderer;
    private Vector2 mDockBackgroundCurrentCenter;
    private Vector2 mDockBackgroundCenterVelocity;
    private float mDockBackgroundCurrentWidth;
    private float mDockBackgroundWidthVelocity;
    private float mDockBackgroundCurrentHeight;
    private float mDockBackgroundHeightVelocity;
    private DockCardEntry mReturningEntry;

    public IArchitecture GetArchitecture()
    {
        return TableNine.Interface;
    }

    private void Awake()
    {
#if UNITY_EDITOR
        if (mCardPrefab == null)
        {
            mCardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StandardCardPrefabPath);
        }

        if (mDockBackgroundSprite == null)
        {
            mDockBackgroundSprite = AssetDatabase.LoadAssetAtPath<Sprite>(DockBackgroundSpritePath);
        }
#endif

        if (mCardPrefab == null)
        {
            Debug.LogError("[DockCardsWorldDemo] Card prefab is missing.");
            enabled = false;
            return;
        }

        if (mCamera == null)
        {
            mCamera = Camera.main;
        }

        if (mDescriptionText == null)
        {
            mDescriptionText = FindDescriptionText();
        }

        if (mDescriptionText != null)
        {
            mDescriptionText.enableWordWrapping = true;
            mDescriptionText.overflowMode = TextOverflowModes.Overflow;
            mDefaultDescription = mDescriptionText.text;
        }

        mDockRoot = new GameObject("DockCardsRoot").transform;
        mDockRoot.SetParent(transform, false);

        CreateDockBackground();
    }

    private void Start()
    {
        if (!enabled)
        {
            return;
        }

        if (!TableNine.IsInitialized)
        {
            TableNine.InitArchitecture();
        }

        RefreshWorldMetrics();
        mComposer = new BakedCardFaceComposer(mCardFaceTemplate, mPlayerCardTemplate);
        BuildCards();
    }

    private void OnDestroy()
    {
        if (mComposer != null)
        {
            mComposer.Dispose();
            mComposer = null;
        }
    }

    private void Update()
    {
        if (mCamera == null)
        {
            return;
        }

        RefreshWorldMetrics();
        UpdateDockBackground(Time.deltaTime);

        if (mCards.Count == 0)
        {
            mHoveredIndex = -1;
            UpdateDescriptionPanel();
            return;
        }

        UpdateHoveredIndex();
        ApplyDockLayout(Time.deltaTime);

        if (mReturningEntry != null)
        {
            TickReturnDrag(Time.deltaTime);
            UpdateDescriptionPanel();
            return;
        }

        if (mDraggingEntry != null)
        {
            UpdateDrag();
            if (Input.GetMouseButtonUp(0))
            {
                HandleDragRelease();
            }

            UpdateDescriptionPanel();
            return;
        }

        HandlePointerInput();
        UpdateDescriptionPanel();
    }

    private void HandlePointerInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            mPressIndex = mHoveredIndex;
            if (mPressIndex >= 0)
            {
                mPressTracking = true;
                mPressStartedAt = Time.unscaledTime;
                mPressStartedScreen = Input.mousePosition;
            }
            else
            {
                mPressTracking = false;
            }

            return;
        }

        if (mPressTracking && Input.GetMouseButton(0))
        {
            if (mPressIndex >= 0)
            {
                var heldLongEnough = Time.unscaledTime - mPressStartedAt >= mLongPressDuration;
                var draggedFarEnough = Vector2.Distance(mPressStartedScreen, Input.mousePosition) >= mDragStartThresholdPixels;
                if (heldLongEnough || draggedFarEnough)
                {
                    mSelectedIndex = mPressIndex;
                    BeginDrag(mCards[mPressIndex]);
                    ResetPressTracking();
                }
            }

            return;
        }

        if (Input.GetMouseButtonUp(0))
        {
            if (mPressTracking && mPressIndex >= 0)
            {
                mSelectedIndex = mPressIndex;
            }

            ResetPressTracking();
        }
    }

    private void BuildCards()
    {
        mCards.Clear();
        var definitions = PickDemoCards(mCardCount);
        if (definitions.Count == 0)
        {
            Debug.LogWarning("[DockCardsWorldDemo] No cards available in config.");
            return;
        }

        var anchor = GetDockAnchorWorld();
        var totalWidth = ComputeTotalDockWidth(definitions.Count);
        var startX = anchor.x - totalWidth * 0.5f;

        for (var i = 0; i < definitions.Count; i++)
        {
            var definition = definitions[i];
            var renderData = BakedCardRenderDataFactory.CreateFromCardDefinition(this.GetModel<IConfigModel>(), definition);
            if (renderData == null)
            {
                continue;
            }

            var sprites = mComposer.ComposeSet(renderData, BakedCardRenderDataFactory.StandardPixelsPerUnit);
            if (!sprites.HasFace)
            {
                continue;
            }

            var wrapper = new GameObject($"DockCard_{definition.CardId}");
            wrapper.transform.SetParent(mDockRoot, false);
            wrapper.transform.position = new Vector3(startX + GetSlotCenterOffset(i), anchor.y, 0f);

            var cardObject = Instantiate(mCardPrefab, wrapper.transform, false);
            cardObject.name = "Card";

            var cardView = cardObject.GetComponent<CardView>();
            if (cardView == null)
            {
                cardView = cardObject.AddComponent<CardView>();
            }

            cardView.Initialize();
            cardView.SetTargetWorldHeight(mWorldCardHeight);
            cardView.ShowBaked(sprites);

            var entry = new DockCardEntry
            {
                Wrapper = wrapper.transform,
                CardView = cardView,
                Collider = cardObject.GetComponent<Collider2D>(),
                Definition = definition,
                BaseWorldPosition = wrapper.transform.position,
                CurrentX = wrapper.transform.position.x,
                CurrentScale = 1f,
                TargetScale = 1f,
                CurrentLift = 0f,
                TargetLift = 0f,
                SortingOrder = mBaseSortingOrder + i
            };

            ApplySortingOrder(entry, entry.SortingOrder);
            mCards.Add(entry);
        }

        CacheSlotMetrics();
        RepositionAllCards(true);
        SnapDockBackgroundLayout();
    }

    private void RepositionAllCards(bool snapImmediately = false)
    {
        if (mCards.Count == 0)
        {
            return;
        }

        var anchor = GetDockAnchorWorld();
        var totalWidth = ComputeTotalDockWidth(mCards.Count);
        var startX = anchor.x - totalWidth * 0.5f;

        for (var i = 0; i < mCards.Count; i++)
        {
            var entry = mCards[i];
            if (entry.Wrapper == null)
            {
                continue;
            }

            var slotCenterX = startX + GetSlotCenterOffset(i);
            entry.BaseWorldPosition = new Vector3(slotCenterX, anchor.y, 0f);
            if (snapImmediately)
            {
                entry.CurrentX = slotCenterX;
                entry.XVelocity = 0f;
                entry.Wrapper.position = entry.BaseWorldPosition;
            }
        }
    }

    private void CacheSlotMetrics()
    {
        if (mCards.Count > 0 && mCards[0].Collider != null)
        {
            mSlotWidthWorld = mCards[0].Collider.bounds.size.x;
        }
        else
        {
            mSlotWidthWorld = mWorldCardHeight * 0.75f;
        }
    }

    private void ApplyDockLayout(float deltaTime)
    {
        var pointerWorld = Vector3.zero;
        var hasActiveHover = mHoveredIndex >= 0 && TryGetPointerWorld(out pointerWorld);

        var anchor = GetDockAnchorWorld();
        var totalWidth = ComputeTotalDockWidth(mCards.Count);
        var startX = anchor.x - totalWidth * 0.5f;

        for (var i = 0; i < mCards.Count; i++)
        {
            var entry = mCards[i];
            if (entry.Wrapper == null)
            {
                continue;
            }

            var slotCenterX = startX + GetSlotCenterOffset(i);
            if (entry == mDraggingEntry)
            {
                entry.BaseWorldPosition = new Vector3(slotCenterX, anchor.y, 0f);
                continue;
            }

            var normalized = 0f;
            if (hasActiveHover)
            {
                var mouseDistance = pointerWorld.x - slotCenterX;
                normalized = 1f - Mathf.Clamp01(Mathf.Abs(mouseDistance) / Mathf.Max(0.01f, mProximityWorld));
            }

            var targetScale = Mathf.Lerp(1f, mMagnificationRatio, normalized);
            var targetLift = mHoverLiftWorld * normalized;

            entry.TargetScale = targetScale;
            entry.TargetLift = targetLift;
            entry.BaseWorldPosition = new Vector3(slotCenterX, anchor.y, 0f);
            entry.CurrentX = SpringMath.Step(
                ref entry.CurrentX,
                ref entry.XVelocity,
                entry.BaseWorldPosition.x,
                mSpringStiffness,
                mSpringDamping,
                deltaTime);

            entry.CurrentScale = SpringMath.Step(
                ref entry.CurrentScale,
                ref entry.ScaleVelocity,
                entry.TargetScale,
                mSpringStiffness,
                mSpringDamping,
                deltaTime);
            entry.CurrentLift = SpringMath.Step(
                ref entry.CurrentLift,
                ref entry.LiftVelocity,
                entry.TargetLift,
                mSpringStiffness,
                mSpringDamping,
                deltaTime);

            var sortBoost = Mathf.RoundToInt(normalized * 30f) + (i == mSelectedIndex ? 15 : 0);
            ApplySortingOrder(entry, entry.SortingOrder + sortBoost);

            if (entry == mReturningEntry)
            {
                continue;
            }

            var scaleMultiplier = entry.CurrentScale;
            entry.Wrapper.position = new Vector3(entry.CurrentX, entry.BaseWorldPosition.y + entry.CurrentLift, 0f);
            entry.Wrapper.localScale = Vector3.one * scaleMultiplier;
        }
    }

    private void ResetPressTracking()
    {
        mPressTracking = false;
        mPressIndex = -1;
    }

    private void BeginDrag(DockCardEntry entry)
    {
        if (entry == null || !TryGetPointerWorld(out var pointerWorld))
        {
            return;
        }

        mReturningEntry = null;
        mDraggingEntry = entry;
        mDragPointerOffset = entry.Wrapper.position - pointerWorld;
        ApplySortingOrder(entry, mBaseSortingOrder + mCards.Count + 60);
    }

    private void UpdateDrag()
    {
        if (mDraggingEntry?.Wrapper == null || !TryGetPointerWorld(out var pointerWorld))
        {
            return;
        }

        mDraggingEntry.Wrapper.position = pointerWorld + mDragPointerOffset;
    }

    private void BeginReturnDrag()
    {
        if (mDraggingEntry == null)
        {
            return;
        }

        mReturningEntry = mDraggingEntry;
        mDraggingEntry = null;
        mReturningEntry.ReturnElapsed = 0f;
        mReturningEntry.ReturnStartPosition = mReturningEntry.Wrapper.position;
        mReturningEntry.ReturnStartScale = mReturningEntry.Wrapper.localScale;
    }

    private void HandleDragRelease()
    {
        if (mDraggingEntry == null)
        {
            return;
        }

        if (ShouldReturnDraggedCardOnRelease())
        {
            BeginReturnDrag();
            return;
        }

        PlayDraggedCard();
    }

    private void TickReturnDrag(float deltaTime)
    {
        var entry = mReturningEntry;
        if (entry?.Wrapper == null)
        {
            mReturningEntry = null;
            return;
        }

        entry.ReturnElapsed += deltaTime;
        var duration = Mathf.Max(0.05f, mDragReturnDuration);
        var t = Mathf.Clamp01(entry.ReturnElapsed / duration);
        var eased = EaseOutBack(t);
        var targetPos = entry.BaseWorldPosition + new Vector3(0f, entry.CurrentLift, 0f);
        var targetScale = Vector3.one * entry.CurrentScale;
        entry.Wrapper.position = Vector3.LerpUnclamped(entry.ReturnStartPosition, targetPos, eased);
        entry.Wrapper.localScale = Vector3.LerpUnclamped(entry.ReturnStartScale, targetScale, Mathf.Clamp01(1f - Mathf.Pow(1f - t, 3f)));

        if (t >= 1f)
        {
            entry.Wrapper.position = targetPos;
            entry.Wrapper.localScale = targetScale;
            entry.CurrentX = targetPos.x;
            entry.XVelocity = 0f;
            mReturningEntry = null;
        }
    }

    private void UpdateHoveredIndex()
    {
        mHoveredIndex = -1;
        if (!TryGetPointerWorld(out var pointerWorld))
        {
            return;
        }

        for (var i = mCards.Count - 1; i >= 0; i--)
        {
            var entry = mCards[i];
            if (entry.Collider != null && entry.Collider.OverlapPoint(pointerWorld))
            {
                mHoveredIndex = i;
                return;
            }
        }
    }

    private void UpdateDescriptionPanel()
    {
        if (mDescriptionText == null || UIGameplayPanel.IsSidePanelHovered)
        {
            return;
        }

        var entry = GetDescriptionEntry();
        if (entry?.Definition == null)
        {
            mDescriptionText.text = !string.IsNullOrWhiteSpace(mDefaultHint)
                ? DescriptionPanelTextRules.Clamp(mDefaultHint)
                : mDefaultDescription;
            return;
        }

        var text = CardPreviewDescriptionComposer.Compose(this.GetModel<IConfigModel>(), entry.Definition);
        if (string.IsNullOrWhiteSpace(text))
        {
            text = entry.Definition.DisplayName ?? string.Empty;
        }

        mDescriptionText.text = DescriptionPanelTextRules.Clamp(text);
    }

    private DockCardEntry GetDescriptionEntry()
    {
        if (mDraggingEntry != null)
        {
            return mDraggingEntry;
        }

        if (mReturningEntry != null)
        {
            return mReturningEntry;
        }

        if (mHoveredIndex >= 0 && mHoveredIndex < mCards.Count)
        {
            return mCards[mHoveredIndex];
        }

        return null;
    }

    private void PlayDraggedCard()
    {
        var entry = mDraggingEntry;
        if (entry == null)
        {
            return;
        }

        var removedIndex = mCards.IndexOf(entry);
        if (removedIndex < 0)
        {
            mDraggingEntry = null;
            return;
        }

        mDraggingEntry = null;
        mCards.RemoveAt(removedIndex);

        if (mSelectedIndex == removedIndex)
        {
            mSelectedIndex = -1;
        }
        else if (mSelectedIndex > removedIndex)
        {
            mSelectedIndex--;
        }

        if (mPressIndex == removedIndex)
        {
            mPressIndex = -1;
        }
        else if (mPressIndex > removedIndex)
        {
            mPressIndex--;
        }

        if (entry.Wrapper != null)
        {
            Destroy(entry.Wrapper.gameObject);
        }

        ReindexCards();
        RepositionAllCards();
        UpdateHoveredIndex();

        if (mCards.Count == 0)
        {
            SnapDockBackgroundLayout();
        }
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        var inv = t - 1f;
        return 1f + c3 * inv * inv * inv + c1 * inv * inv;
    }

    private void RefreshWorldMetrics()
    {
        mMagnificationRatio = mMagnificationPixels / Mathf.Max(1f, mBaseItemSizePixels);
        mProximityWorld = ScreenPixelsToWorld(mProximityPixels);
        mItemGapWorld = ScreenPixelsToWorld(mItemGapPixels);
        mHoverLiftWorld = ScreenPixelsToWorld(mHoverLiftPixels);

        if (mSlotWidthWorld <= 0f)
        {
            mSlotWidthWorld = mWorldCardHeight * 0.75f;
        }
    }

    private float ScreenPixelsToWorld(float pixels)
    {
        if (mCamera == null)
        {
            return pixels * 0.01f;
        }

        var depth = Mathf.Abs(mCamera.transform.position.z);
        var origin = mCamera.ScreenToWorldPoint(new Vector3(0f, 0f, depth));
        var offset = mCamera.ScreenToWorldPoint(new Vector3(pixels, 0f, depth));
        return Mathf.Abs(offset.x - origin.x);
    }

    private Vector3 GetDockAnchorWorld()
    {
        if (mCamera == null)
        {
            return transform.position;
        }

        var depth = Mathf.Abs(mCamera.transform.position.z);
        return mCamera.ViewportToWorldPoint(new Vector3(0.5f, mViewportY, depth));
    }

    private float ComputeTotalDockWidth(int count)
    {
        if (count <= 0)
        {
            return 0f;
        }

        return count * mSlotWidthWorld + (count - 1) * mItemGapWorld;
    }

    private float GetSlotCenterOffset(int index)
    {
        return mSlotWidthWorld * 0.5f + index * (mSlotWidthWorld + mItemGapWorld);
    }

    private void ReindexCards()
    {
        for (var i = 0; i < mCards.Count; i++)
        {
            var entry = mCards[i];
            if (entry == null)
            {
                continue;
            }

            entry.SortingOrder = mBaseSortingOrder + i;
            ApplySortingOrder(entry, entry.SortingOrder);
        }
    }

    private List<CardDefinition> PickDemoCards(int count)
    {
        var config = this.GetModel<IConfigModel>();
        var pool = new List<CardDefinition>();
        AddCards(pool, config.GetCardsByType(CardType.Monster));
        AddCards(pool, config.GetCardsByType(CardType.Help));
        AddCards(pool, config.GetCardsByType(CardType.Tutor));

        var picked = new List<CardDefinition>();
        for (var i = 0; i < count && pool.Count > 0; i++)
        {
            picked.Add(pool[i % pool.Count]);
        }

        return picked;
    }

    private static void AddCards(List<CardDefinition> pool, IReadOnlyList<CardDefinition> cards)
    {
        if (cards == null)
        {
            return;
        }

        for (var i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            if (card == null || card.CardType is CardType.Player or CardType.Room)
            {
                continue;
            }

            pool.Add(card);
        }
    }

    private bool TryGetPointerWorld(out Vector3 worldPoint)
    {
        worldPoint = Vector3.zero;
        if (mCamera == null)
        {
            return false;
        }

        var screen = Input.mousePosition;
        worldPoint = mCamera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, Mathf.Abs(mCamera.transform.position.z)));
        worldPoint.z = 0f;
        return true;
    }

    private static TMP_Text FindDescriptionText()
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

    private bool ShouldReturnDraggedCardOnRelease()
    {
        return IsPointerInsideDockBackground() || DoesDraggedCardOverlapDockBackground();
    }

    private bool IsPointerInsideDockBackground()
    {
        if (!TryGetPointerWorld(out var pointerWorld))
        {
            return false;
        }

        return GetDockBackgroundWorldRect().Contains(new Vector2(pointerWorld.x, pointerWorld.y));
    }

    private bool DoesDraggedCardOverlapDockBackground()
    {
        if (mDraggingEntry?.Collider == null || mDockBackgroundRenderer == null)
        {
            return false;
        }

        var cardBounds = mDraggingEntry.Collider.bounds;
        if (cardBounds.size.sqrMagnitude <= Mathf.Epsilon)
        {
            return false;
        }

        return GetDockBackgroundWorldRect().Overlaps(GetWorldRect(cardBounds), true);
    }

    private void UpdateDockBackground(float deltaTime)
    {
        if (mDockBackgroundRenderer == null)
        {
            return;
        }

        var hasCards = mCards.Count > 0;
        mDockBackgroundRenderer.enabled = hasCards && mDockBackgroundRenderer.sprite != null;
        if (!hasCards || mDockBackgroundRenderer.sprite == null)
        {
            return;
        }

        var targetRect = GetDockBackgroundTargetRect(IsDockBackgroundExpanded());
        mDockBackgroundCurrentCenter.x = SpringMath.Step(
            ref mDockBackgroundCurrentCenter.x,
            ref mDockBackgroundCenterVelocity.x,
            targetRect.center.x,
            mSpringStiffness,
            mSpringDamping,
            deltaTime);
        mDockBackgroundCurrentCenter.y = SpringMath.Step(
            ref mDockBackgroundCurrentCenter.y,
            ref mDockBackgroundCenterVelocity.y,
            targetRect.center.y,
            mSpringStiffness,
            mSpringDamping,
            deltaTime);
        mDockBackgroundCurrentWidth = SpringMath.Step(
            ref mDockBackgroundCurrentWidth,
            ref mDockBackgroundWidthVelocity,
            targetRect.width,
            mSpringStiffness,
            mSpringDamping,
            deltaTime);
        mDockBackgroundCurrentHeight = SpringMath.Step(
            ref mDockBackgroundCurrentHeight,
            ref mDockBackgroundHeightVelocity,
            targetRect.height,
            mSpringStiffness,
            mSpringDamping,
            deltaTime);

        ApplyDockBackgroundRect(Rect.MinMaxRect(
            mDockBackgroundCurrentCenter.x - mDockBackgroundCurrentWidth * 0.5f,
            mDockBackgroundCurrentCenter.y - mDockBackgroundCurrentHeight * 0.5f,
            mDockBackgroundCurrentCenter.x + mDockBackgroundCurrentWidth * 0.5f,
            mDockBackgroundCurrentCenter.y + mDockBackgroundCurrentHeight * 0.5f));
    }

    private void SnapDockBackgroundLayout()
    {
        if (mDockBackgroundRenderer == null)
        {
            return;
        }

        if (mCards.Count <= 0 || mDockBackgroundRenderer.sprite == null)
        {
            mDockBackgroundRenderer.enabled = false;
            return;
        }

        mDockBackgroundRenderer.enabled = true;
        var rect = GetDockBackgroundTargetRect(false);
        mDockBackgroundCurrentCenter = rect.center;
        mDockBackgroundCenterVelocity = Vector2.zero;
        mDockBackgroundCurrentWidth = rect.width;
        mDockBackgroundCurrentHeight = rect.height;
        mDockBackgroundWidthVelocity = 0f;
        mDockBackgroundHeightVelocity = 0f;
        ApplyDockBackgroundRect(rect);
    }

    private void CreateDockBackground()
    {
        mDockBackgroundRoot = new GameObject("DockBackground").transform;
        mDockBackgroundRoot.SetParent(mDockRoot, false);
        mDockBackgroundRoot.SetAsFirstSibling();

        mDockBackgroundRenderer = mDockBackgroundRoot.gameObject.AddComponent<SpriteRenderer>();
        mDockBackgroundRenderer.drawMode = SpriteDrawMode.Simple;
        mDockBackgroundRenderer.sprite = mDockBackgroundSprite;
        mDockBackgroundRenderer.color = new Color(1f, 1f, 1f, mDockBackgroundAlpha);
        mDockBackgroundRenderer.sortingLayerName = DockBackgroundSortingLayerName;
        mDockBackgroundRenderer.sortingOrder = mDockBackgroundSortingOrder;
        mDockBackgroundRenderer.enabled = mDockBackgroundSprite != null;

        if (mDockBackgroundSprite == null)
        {
            Debug.LogWarning("[DockCardsWorldDemo] Dock background sprite is missing.");
        }
    }

    private bool IsDockBackgroundExpanded()
    {
        return mHoveredIndex >= 0 || mDraggingEntry != null || mReturningEntry != null;
    }

    private Rect GetDockBackgroundTargetRect(bool expanded)
    {
        return GetDockBackgroundWorldRect(expanded ? mDockBackgroundExpandedRectLocal : mDockBackgroundCollapsedRectLocal);
    }

    private void ApplyDockBackgroundRect(Rect rect)
    {
        if (mDockBackgroundRoot == null || mDockBackgroundRenderer?.sprite == null)
        {
            return;
        }

        var spriteSize = mDockBackgroundRenderer.sprite.bounds.size;
        if (spriteSize.x <= Mathf.Epsilon || spriteSize.y <= Mathf.Epsilon)
        {
            return;
        }

        mDockBackgroundRoot.position = new Vector3(rect.center.x, rect.center.y, 0f);
        mDockBackgroundRoot.localScale = new Vector3(rect.width / spriteSize.x, rect.height / spriteSize.y, 1f);
    }

    private Rect GetDockBackgroundWorldRect()
    {
        if (mDockBackgroundRenderer == null || !mDockBackgroundRenderer.enabled)
        {
            return Rect.zero;
        }

        var bounds = mDockBackgroundRenderer.bounds;
        return Rect.MinMaxRect(bounds.min.x, bounds.min.y, bounds.max.x, bounds.max.y);
    }

    private Rect GetDockBackgroundWorldRect(Rect localRect)
    {
        var anchor = GetDockAnchorWorld();
        return Rect.MinMaxRect(
            anchor.x + localRect.xMin,
            anchor.y + localRect.yMin,
            anchor.x + localRect.xMax,
            anchor.y + localRect.yMax);
    }

    private static Rect GetWorldRect(Bounds worldBounds)
    {
        return Rect.MinMaxRect(worldBounds.min.x, worldBounds.min.y, worldBounds.max.x, worldBounds.max.y);
    }

    private static void ApplySortingOrder(DockCardEntry entry, int order)
    {
        if (entry?.CardView?.DisplayAdapter == null)
        {
            return;
        }

        var pivot = entry.CardView.DisplayAdapter.VisualPivot;
        if (pivot == null)
        {
            return;
        }

        var renderers = pivot.GetComponentsInChildren<SpriteRenderer>(true);
        for (var i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingLayerName = DockCardsSortingLayerName;
            renderers[i].sortingOrder = order;
        }
    }

    private sealed class DockCardEntry
    {
        public Transform Wrapper;
        public CardView CardView;
        public Collider2D Collider;
        public CardDefinition Definition;
        public Vector3 BaseWorldPosition;
        public float CurrentX;
        public float XVelocity;
        public float CurrentScale = 1f;
        public float TargetScale = 1f;
        public float ScaleVelocity;
        public float CurrentLift;
        public float TargetLift;
        public float LiftVelocity;
        public int SortingOrder;
        public float ReturnElapsed;
        public Vector3 ReturnStartPosition;
        public Vector3 ReturnStartScale;
    }
}
