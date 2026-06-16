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
    [SerializeField] private bool mUseRuntimeItemSlots = true;
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
    private readonly List<CardDefinition> mDemoCardSequence = new List<CardDefinition>();
    private readonly List<IUnRegister> mEventRegisters = new List<IUnRegister>();
    private readonly Dictionary<int, Vector3> mPickupOrigins = new Dictionary<int, Vector3>();
    private readonly Dictionary<int, float> mTargetFeedbackWeights = new Dictionary<int, float>();
    private readonly Dictionary<int, float> mTargetFeedbackVelocities = new Dictionary<int, float>();
    private readonly Dictionary<int, Vector3> mTargetFeedbackBaseScales = new Dictionary<int, Vector3>();

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
    private int mNextSpawnSequenceIndex;

    public bool UsesRuntimeItemSlots => mUseRuntimeItemSlots;
    public int VisibleCardCount => mCards.Count;

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

        mCardFaceTemplate = BakedCardPrefabRefs.ResolveCardExample(mCardFaceTemplate);
        mPlayerCardTemplate = BakedCardPrefabRefs.ResolvePlayerCard(mPlayerCardTemplate);

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
        RegisterRuntimeEvents();
        BuildCards();
    }

    private void OnDestroy()
    {
        for (var i = 0; i < mEventRegisters.Count; i++)
        {
            mEventRegisters[i].UnRegister();
        }

        mEventRegisters.Clear();
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
        HandleDebugInput();
        UpdateDockBackground(Time.deltaTime);
        UpdateTargetFeedback(Time.deltaTime);

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
        mDemoCardSequence.Clear();
        if (mUseRuntimeItemSlots)
        {
            BuildCardsFromRuntimeItemSlots();
            return;
        }

        mDemoCardSequence.AddRange(PickDemoCards(mCardCount));
        mNextSpawnSequenceIndex = 0;

        if (mDemoCardSequence.Count == 0)
        {
            Debug.LogWarning("[DockCardsWorldDemo] No cards available in config.");
            return;
        }

        var initialCount = Mathf.Min(mCardCount, mDemoCardSequence.Count);
        for (var i = 0; i < initialCount; i++)
        {
            TryAddNextDemoCard(false);
        }

        CacheSlotMetrics();
        RepositionAllCards(true);
        SnapDockBackgroundLayout();
    }

    private void RegisterRuntimeEvents()
    {
        if (!mUseRuntimeItemSlots)
        {
            return;
        }

        mEventRegisters.Add(this.RegisterEvent<CardRemovedEvent>(OnCardRemoved));
        mEventRegisters.Add(this.RegisterEvent<ItemSlotChangedEvent>(OnItemSlotChanged));
    }

    private void BuildCardsFromRuntimeItemSlots()
    {
        ClearExistingCardObjects();
        var deckModel = this.GetModel<IDeckModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var configModel = this.GetModel<IConfigModel>();
        for (var i = 0; i < deckModel.ItemSlots.Length; i++)
        {
            var uid = deckModel.ItemSlots[i];
            if (!uid.HasValue || !collectionModel.TryGetCard(uid.Value, out var runtime))
            {
                continue;
            }

            var definition = configModel.GetCardDefinition(runtime.DefinitionId);
            var entry = CreateDockCardEntry(definition, uid.Value, i, GetDockAnchorWorld());
            if (entry != null)
            {
                mCards.Add(entry);
            }
        }

        CacheSlotMetrics();
        ReindexCards();
        RepositionAllCards(true);
        SnapDockBackgroundLayout();
    }

    private void ClearExistingCardObjects()
    {
        for (var i = 0; i < mCards.Count; i++)
        {
            if (mCards[i]?.Wrapper != null)
            {
                Destroy(mCards[i].Wrapper.gameObject);
            }
        }

        mCards.Clear();
        mHoveredIndex = -1;
        mSelectedIndex = -1;
        mDraggingEntry = null;
        mReturningEntry = null;
        ResetAllTargetFeedback();
    }

    private void OnCardRemoved(CardRemovedEvent evt)
    {
        if (evt.Reason != RemoveReason.HelpCard)
        {
            return;
        }

        if (TryGetBoardSlotWorldPosition(evt.Slot, out var position))
        {
            mPickupOrigins[evt.Uid.Value] = position;
        }
    }

    private void OnItemSlotChanged(ItemSlotChangedEvent evt)
    {
        if (!mUseRuntimeItemSlots)
        {
            return;
        }

        if (evt.Uid.HasValue)
        {
            AddOrUpdateRuntimeCard(evt.ItemSlotIndex, evt.Uid.Value);
            return;
        }

        RemoveRuntimeCardAt(evt.ItemSlotIndex);
    }

    private void AddOrUpdateRuntimeCard(int itemSlotIndex, CardUid uid)
    {
        var existing = FindRuntimeEntry(itemSlotIndex, uid);
        if (existing != null)
        {
            existing.ItemSlotIndex = itemSlotIndex;
            return;
        }

        var collectionModel = this.GetModel<ICollectionModel>();
        var configModel = this.GetModel<IConfigModel>();
        if (!collectionModel.TryGetCard(uid, out var runtime))
        {
            return;
        }

        var definition = configModel.GetCardDefinition(runtime.DefinitionId);
        var spawnPosition = mPickupOrigins.TryGetValue(uid.Value, out var pickupOrigin)
            ? pickupOrigin
            : GetDockAnchorWorld();
        mPickupOrigins.Remove(uid.Value);

        var entry = CreateDockCardEntry(definition, uid, itemSlotIndex, spawnPosition);
        if (entry == null)
        {
            return;
        }

        mCards.Add(entry);
        CacheSlotMetrics();
        ReindexCards();
        RepositionAllCards();
        UpdateHoveredIndex();
    }

    private void RemoveRuntimeCardAt(int itemSlotIndex)
    {
        for (var i = mCards.Count - 1; i >= 0; i--)
        {
            var entry = mCards[i];
            if (entry == null || entry.ItemSlotIndex != itemSlotIndex)
            {
                continue;
            }

            if (entry == mDraggingEntry)
            {
                mDraggingEntry = null;
            }

            if (entry == mReturningEntry)
            {
                mReturningEntry = null;
            }

            ResetDraggedCardJitter(entry);
            if (entry.Wrapper != null)
            {
                Destroy(entry.Wrapper.gameObject);
            }

            mCards.RemoveAt(i);
            if (mSelectedIndex == i)
            {
                mSelectedIndex = -1;
            }
            else if (mSelectedIndex > i)
            {
                mSelectedIndex--;
            }

            ReindexCards();
            RepositionAllCards();
            UpdateHoveredIndex();
            return;
        }
    }

    private DockCardEntry FindRuntimeEntry(int itemSlotIndex, CardUid uid)
    {
        for (var i = 0; i < mCards.Count; i++)
        {
            var entry = mCards[i];
            if (entry != null &&
                (entry.ItemSlotIndex == itemSlotIndex ||
                 (entry.Uid.HasValue && entry.Uid.Value.Equals(uid))))
            {
                return entry;
            }
        }

        return null;
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
                entry.CurrentY = anchor.y;
                entry.XVelocity = 0f;
                entry.YVelocity = 0f;
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
            entry.CurrentY = SpringMath.Step(
                ref entry.CurrentY,
                ref entry.YVelocity,
                entry.BaseWorldPosition.y,
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
            entry.Wrapper.position = new Vector3(entry.CurrentX, entry.CurrentY + entry.CurrentLift, 0f);
            entry.Wrapper.localScale = Vector3.one * scaleMultiplier;
        }
    }

    private void ResetPressTracking()
    {
        mPressTracking = false;
        mPressIndex = -1;
    }

    private void HandleDebugInput()
    {
        if (mUseRuntimeItemSlots)
        {
            return;
        }

        if (!Input.GetKeyDown(KeyCode.Alpha3) && !Input.GetKeyDown(KeyCode.Keypad3))
        {
            return;
        }

        TryAddNextDemoCard(true);
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
        ApplyDraggedCardJitter(mDraggingEntry);
    }

    private void BeginReturnDrag()
    {
        if (mDraggingEntry == null)
        {
            return;
        }

        mReturningEntry = mDraggingEntry;
        mDraggingEntry = null;
        ResetDraggedCardJitter(mReturningEntry);
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

        if (mUseRuntimeItemSlots)
        {
            if (!TryPlayRuntimeDraggedCard())
            {
                BeginReturnDrag();
            }

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
            entry.CurrentY = entry.BaseWorldPosition.y;
            entry.XVelocity = 0f;
            entry.YVelocity = 0f;
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
            mNextSpawnSequenceIndex = 0;
            mHoveredIndex = -1;
        }
    }

    private bool TryPlayRuntimeDraggedCard()
    {
        var entry = mDraggingEntry;
        if (entry == null || !entry.Uid.HasValue)
        {
            return false;
        }

        if (entry.Intent.RequiresBoardTarget)
        {
            if (!TryFindValidBoardTargetUnderPointer(entry.Intent, out var targetSlot))
            {
                return false;
            }

            this.SendCommand(new UseHelpCardCommand(entry.Uid.Value));
            var deckModel = this.GetModel<IDeckModel>();
            if (deckModel.PendingHelpCardAction.Kind == PendingHelpCardActionKind.SwapTarget)
            {
                this.SendCommand(new ResolveSwapTargetCommand(targetSlot));
            }
            else if (deckModel.PendingHelpCardAction.Kind == PendingHelpCardActionKind.ThrowingKnifeTarget)
            {
                this.SendCommand(new ResolveTargetingCommand(targetSlot));
            }

            return FinishRuntimeDragAfterCommands(entry);
        }

        this.SendCommand(new UseHelpCardCommand(entry.Uid.Value));
        return FinishRuntimeDragAfterCommands(entry);
    }

    private bool FinishRuntimeDragAfterCommands(DockCardEntry entry)
    {
        ResetDraggedCardJitter(entry);
        if (IsRuntimeEntryStillInItemSlot(entry))
        {
            BeginReturnDragFor(entry);
        }
        else
        {
            mDraggingEntry = null;
        }

        return true;
    }

    private bool IsRuntimeEntryStillInItemSlot(DockCardEntry entry)
    {
        if (entry == null || !entry.Uid.HasValue)
        {
            return false;
        }

        var deckModel = this.GetModel<IDeckModel>();
        for (var i = 0; i < deckModel.ItemSlots.Length; i++)
        {
            if (deckModel.ItemSlots[i].HasValue && deckModel.ItemSlots[i].Value.Equals(entry.Uid.Value))
            {
                return true;
            }
        }

        return false;
    }

    private void BeginReturnDragFor(DockCardEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        mReturningEntry = entry;
        mDraggingEntry = null;
        mReturningEntry.ReturnElapsed = 0f;
        mReturningEntry.ReturnStartPosition = mReturningEntry.Wrapper.position;
        mReturningEntry.ReturnStartScale = mReturningEntry.Wrapper.localScale;
    }

    private bool TryFindValidBoardTargetUnderPointer(HelpCardPlayIntent intent, out BoardSlotNo slot)
    {
        slot = default;
        if (!TryGetPointerWorld(out var pointerWorld))
        {
            return false;
        }

        var slotViews = FindObjectsOfType<BoardSlotView>();
        var bestDistance = float.MaxValue;
        BoardSlotNo bestSlot = default;
        for (var i = 0; i < slotViews.Length; i++)
        {
            var slotView = slotViews[i];
            if (slotView == null || slotView.IsItemSlot || slotView.BoardSlotNo <= 0)
            {
                continue;
            }

            var candidate = new BoardSlotNo(slotView.BoardSlotNo);
            if (!HelpCardInteractionUtility.IsValidBoardTarget(this, intent, candidate))
            {
                continue;
            }

            var collider = slotView.GetComponent<Collider2D>();
            if (collider != null && collider.enabled && collider.OverlapPoint(pointerWorld))
            {
                slot = candidate;
                return true;
            }

            var distance = Vector2.Distance(pointerWorld, slotView.transform.position);
            if (distance < 0.8f && distance < bestDistance)
            {
                bestDistance = distance;
                bestSlot = candidate;
            }
        }

        if (bestDistance < float.MaxValue)
        {
            slot = bestSlot;
            return true;
        }

        return false;
    }

    private void UpdateTargetFeedback(float deltaTime)
    {
        BoardSlotNo? activeSlot = null;
        if (mDraggingEntry != null && mDraggingEntry.Intent.RequiresBoardTarget &&
            TryFindValidBoardTargetUnderPointer(mDraggingEntry.Intent, out var slot))
        {
            activeSlot = slot;
        }

        if (activeSlot.HasValue)
        {
            if (!mTargetFeedbackWeights.ContainsKey(activeSlot.Value.Value))
            {
                mTargetFeedbackWeights[activeSlot.Value.Value] = 0f;
                mTargetFeedbackVelocities[activeSlot.Value.Value] = 0f;
            }
        }

        var keys = new List<int>(mTargetFeedbackWeights.Keys);
        for (var i = 0; i < keys.Count; i++)
        {
            var key = keys[i];
            var target = activeSlot.HasValue && activeSlot.Value.Value == key ? 1f : 0f;
            var velocity = mTargetFeedbackVelocities.TryGetValue(key, out var existingVelocity) ? existingVelocity : 0f;
            var weight = Mathf.SmoothDamp(
                mTargetFeedbackWeights[key],
                target,
                ref velocity,
                0.08f,
                Mathf.Infinity,
                deltaTime);
            mTargetFeedbackWeights[key] = weight;
            mTargetFeedbackVelocities[key] = velocity;

            if (TryGetBoardCardView(new BoardSlotNo(key), out var view) && view != null)
            {
                if (!mTargetFeedbackBaseScales.ContainsKey(key))
                {
                    mTargetFeedbackBaseScales[key] = view.transform.localScale;
                }

                view.transform.localScale = mTargetFeedbackBaseScales[key] * (1f + weight * 0.08f);
            }

            if (weight <= 0.001f && target <= 0f)
            {
                ResetTargetFeedbackSlot(key);
            }
        }
    }

    private void ApplyDraggedCardJitter(DockCardEntry entry)
    {
        if (entry?.CardView?.DisplayAdapter?.VisualPivot == null)
        {
            return;
        }

        if (!entry.Intent.RequiresBoardTarget)
        {
            entry.CardView.DisplayAdapter.VisualPivot.localPosition = Vector3.zero;
            return;
        }

        var strength = TryFindValidBoardTargetUnderPointer(entry.Intent, out _) ? 0.035f : 0.012f;
        var jitter = UnityEngine.Random.insideUnitCircle * strength;
        entry.CardView.DisplayAdapter.VisualPivot.localPosition = new Vector3(jitter.x, jitter.y, 0f);
    }

    private static void ResetDraggedCardJitter(DockCardEntry entry)
    {
        if (entry?.CardView?.DisplayAdapter?.VisualPivot != null)
        {
            entry.CardView.DisplayAdapter.VisualPivot.localPosition = Vector3.zero;
        }
    }

    private void ResetAllTargetFeedback()
    {
        var keys = new List<int>(mTargetFeedbackWeights.Keys);
        for (var i = 0; i < keys.Count; i++)
        {
            ResetTargetFeedbackSlot(keys[i]);
        }

        mTargetFeedbackWeights.Clear();
        mTargetFeedbackVelocities.Clear();
        mTargetFeedbackBaseScales.Clear();
    }

    private void ResetTargetFeedbackSlot(int slotNo)
    {
        if (mTargetFeedbackBaseScales.TryGetValue(slotNo, out var scale) &&
            TryGetBoardCardView(new BoardSlotNo(slotNo), out var view) &&
            view != null)
        {
            view.transform.localScale = scale;
        }

        mTargetFeedbackWeights.Remove(slotNo);
        mTargetFeedbackVelocities.Remove(slotNo);
        mTargetFeedbackBaseScales.Remove(slotNo);
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
        if (mDockBackgroundRenderer == null || mDockBackgroundRenderer.sprite == null)
        {
            return;
        }

        var targetRect = GetDockBackgroundTargetRect(IsDockBackgroundExpanded());
        mDockBackgroundRenderer.enabled = true;
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

        if (mCards.Count == 0 && mDockBackgroundCurrentWidth <= 0.01f && mDockBackgroundCurrentHeight <= 0.01f)
        {
            mDockBackgroundRenderer.enabled = false;
        }
    }

    private void SnapDockBackgroundLayout()
    {
        if (mDockBackgroundRenderer == null)
        {
            return;
        }

        if (mDockBackgroundRenderer.sprite == null)
        {
            mDockBackgroundRenderer.enabled = false;
            return;
        }

        if (mCards.Count <= 0)
        {
            var zeroRect = GetDockBackgroundTargetRect(false);
            mDockBackgroundRenderer.enabled = false;
            mDockBackgroundCurrentCenter = zeroRect.center;
            mDockBackgroundCenterVelocity = Vector2.zero;
            mDockBackgroundCurrentWidth = 0f;
            mDockBackgroundCurrentHeight = 0f;
            mDockBackgroundWidthVelocity = 0f;
            mDockBackgroundHeightVelocity = 0f;
            ApplyDockBackgroundRect(zeroRect);
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
        var localRect = expanded ? mDockBackgroundExpandedRectLocal : mDockBackgroundCollapsedRectLocal;
        var visibilityScale = mCards.Count > 0 ? 1f : 0f;
        var widthScale = GetDockBackgroundWidthScale();
        return GetScaledDockBackgroundWorldRect(localRect, widthScale, visibilityScale);
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

    private Rect GetScaledDockBackgroundWorldRect(Rect localRect, float widthScale, float heightScale)
    {
        var anchor = GetDockAnchorWorld();
        var localCenter = localRect.center;
        var scaledHalfWidth = localRect.width * 0.5f * Mathf.Max(0f, widthScale);
        var scaledHalfHeight = localRect.height * 0.5f * Mathf.Max(0f, heightScale);
        return Rect.MinMaxRect(
            anchor.x + localCenter.x - scaledHalfWidth,
            anchor.y + localCenter.y - scaledHalfHeight,
            anchor.x + localCenter.x + scaledHalfWidth,
            anchor.y + localCenter.y + scaledHalfHeight);
    }

    private float GetDockBackgroundWidthScale()
    {
        if (mCardCount <= 0 || mCards.Count <= 0)
        {
            return 0f;
        }

        var referenceWidth = ComputeTotalDockWidth(mCardCount);
        if (referenceWidth <= Mathf.Epsilon)
        {
            return 0f;
        }

        return Mathf.Clamp01(ComputeTotalDockWidth(mCards.Count) / referenceWidth);
    }

    private bool TryAddNextDemoCard(bool animateFromCenter)
    {
        if (mCards.Count >= mCardCount)
        {
            return false;
        }

        if (mDemoCardSequence.Count == 0)
        {
            mDemoCardSequence.AddRange(PickDemoCards(mCardCount));
            if (mDemoCardSequence.Count == 0)
            {
                return false;
            }
        }

        if (mCards.Count == 0)
        {
            mNextSpawnSequenceIndex = 0;
        }

        var definition = mDemoCardSequence[mNextSpawnSequenceIndex % mDemoCardSequence.Count];
        mNextSpawnSequenceIndex++;
        var entry = CreateDockCardEntry(definition, animateFromCenter);
        if (entry == null)
        {
            return false;
        }

        mCards.Add(entry);
        CacheSlotMetrics();
        ReindexCards();
        RepositionAllCards(!animateFromCenter);
        UpdateHoveredIndex();
        if (!animateFromCenter)
        {
            SnapDockBackgroundLayout();
        }

        return true;
    }

    private DockCardEntry CreateDockCardEntry(CardDefinition definition, bool animateFromCenter)
    {
        return CreateDockCardEntry(definition, null, -1, animateFromCenter ? GetDockAnchorWorld() : GetDockAnchorWorld());
    }

    private DockCardEntry CreateDockCardEntry(CardDefinition definition, CardUid? uid, int itemSlotIndex, Vector3 spawnPosition)
    {
        if (definition == null)
        {
            return null;
        }

        var renderData = BakedCardRenderDataFactory.CreateFromCardDefinition(this.GetModel<IConfigModel>(), definition);
        if (renderData == null)
        {
            return null;
        }

        var sprites = mComposer.ComposeSet(renderData, BakedCardRenderDataFactory.StandardPixelsPerUnit);
        if (!sprites.HasFace)
        {
            return null;
        }

        var wrapper = new GameObject($"DockCard_{definition.CardId}_{mNextSpawnSequenceIndex}");
        wrapper.transform.SetParent(mDockRoot, false);
        wrapper.transform.position = new Vector3(spawnPosition.x, spawnPosition.y, 0f);

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
            Uid = uid,
            ItemSlotIndex = itemSlotIndex,
            Intent = HelpCardInteractionUtility.ResolveIntent(definition),
            BaseWorldPosition = wrapper.transform.position,
            CurrentX = wrapper.transform.position.x,
            CurrentY = wrapper.transform.position.y,
            CurrentScale = 1f,
            TargetScale = 1f,
            CurrentLift = 0f,
            TargetLift = 0f,
            SortingOrder = mBaseSortingOrder + mCards.Count
        };

        ApplySortingOrder(entry, entry.SortingOrder);
        return entry;
    }

    private static bool TryGetBoardCardView(BoardSlotNo slot, out CardView cardView)
    {
        var slotViews = FindObjectsOfType<BoardSlotView>();
        for (var i = 0; i < slotViews.Length; i++)
        {
            if (slotViews[i] != null &&
                !slotViews[i].IsItemSlot &&
                slotViews[i].BoardSlotNo == slot.Value &&
                slotViews[i].CardView != null)
            {
                cardView = slotViews[i].CardView;
                return true;
            }
        }

        cardView = null;
        return false;
    }

    private static bool TryGetBoardSlotWorldPosition(BoardSlotNo slot, out Vector3 worldPosition)
    {
        var slotViews = FindObjectsOfType<BoardSlotView>();
        for (var i = 0; i < slotViews.Length; i++)
        {
            if (slotViews[i] != null &&
                !slotViews[i].IsItemSlot &&
                slotViews[i].BoardSlotNo == slot.Value)
            {
                worldPosition = slotViews[i].transform.position;
                return true;
            }
        }

        worldPosition = Vector3.zero;
        return false;
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
        public CardUid? Uid;
        public int ItemSlotIndex = -1;
        public HelpCardPlayIntent Intent;
        public Vector3 BaseWorldPosition;
        public float CurrentX;
        public float XVelocity;
        public float CurrentY;
        public float YVelocity;
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
