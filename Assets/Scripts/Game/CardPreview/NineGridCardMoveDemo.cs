using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using QFramework;
using TMPro;
using UnityEngine;

/// <summary>
/// 世界空间九宫格发牌与外圈跳格演示：按 1 按正式外圈顺序发牌，鼠标点击卡牌触发旋转。
/// </summary>
[DefaultExecutionOrder(260)]
public sealed class NineGridCardMoveDemo : MonoBehaviour, IController
{
    private const string DefaultBoardRootName = "NineGrid Main CardSlots";
    private const string DefaultDeckSlotName = "CardDeckSlot";
    private const string DefaultDeckLeftTextName = "DeckLeftText";
    private const string DefaultPlayerCardName = "PlayerCard";

    [Header("Input")]
    [SerializeField] private KeyCode mDealAllKey = KeyCode.Alpha1;

    [Header("Scene")]
    [SerializeField] private Transform mDeckSlot;
    [SerializeField] private string mDeckSlotName = DefaultDeckSlotName;
    [SerializeField] private Transform mBoardRoot;
    [SerializeField] private string mBoardRootName = DefaultBoardRootName;
    [SerializeField] private Transform mPlayerCard;
    [SerializeField] private string mPlayerCardName = DefaultPlayerCardName;
    [SerializeField] private TextMeshProUGUI mDeckLeftText;
    [SerializeField] private string mDeckLeftTextName = DefaultDeckLeftTextName;
    [SerializeField] private Vector3 mDeckWorldOffset = new Vector3(0f, 0f, -0.08f);
    [SerializeField] private Vector3 mCardWorldOffset = new Vector3(0f, 0f, -0.02f);

    [Header("Card Pipeline")]
    [SerializeField] private GameObject mCardPrefab;
    [SerializeField] private GameObject mCardFaceTemplate;
    [SerializeField] private GameObject mPlayerCardTemplate;
    [SerializeField] private float mWorldCardHeight = BakedCardRenderDataFactory.CanonicalWorldCardHeight;
    [SerializeField] private string mSortingLayerName = "Cards_Front";
    [SerializeField] private int mDeckSortingOrder = 620;
    [SerializeField] private int mFlyingSortingOrder = 840;
    [SerializeField] private int mBaseSortingOrder = 560;

    [Header("Deck")]
    [SerializeField] private int mInitialCardCount = 20;
    [SerializeField] private bool mOnlyShowTopDeckCard = true;
    [SerializeField] private float mHorizontalSpacing = 0.035f;
    [SerializeField] private float mDeckRelayoutDuration = 0.03f;
    [SerializeField] private Ease mDeckRelayoutEase = Ease.OutQuad;
    [SerializeField] private bool mAlternateMonsterAndHelp = true;
    [SerializeField] private CardType mFirstSpawnType = CardType.Monster;
    [SerializeField] private string mMonsterCardId;
    [SerializeField] private string mHelpCardId;

    [Header("Deal")]
    [SerializeField] private float mPreDealAimDuration = 0.027f;
    [SerializeField] private Ease mPreDealAimEase = Ease.OutQuad;
    [SerializeField] private float mDealDuration = 0.107f;
    [SerializeField] private float mDealArcHeight = 0.55f;
    [SerializeField] private Ease mDealMoveEase = Ease.OutCubic;
    [SerializeField] private Ease mLandingRotationEase = Ease.OutBack;
    [SerializeField] private float mLandingRotationOvershoot = 1.55f;
    [SerializeField] private float mDealScaleMultiplier = 1.11f;
    [SerializeField] private Ease mDealScaleEase = Ease.OutQuad;

    [Header("Move")]
    [SerializeField] private float mMoveDuration = 0.28f;
    [SerializeField] private Ease mMoveEase = Ease.InOutSine;
    [SerializeField] private bool mEnableHopArc = true;
    [SerializeField] private float mHopArcHeight = 0.16f;
    [SerializeField] private bool mEnableHopScale = true;
    [SerializeField] private float mHopScaleMultiplier = 1.08f;
    [SerializeField] private Ease mHopScaleEase = Ease.OutQuad;

    [Header("Description")]
    [SerializeField] private TMP_Text mDescriptionText;
    [SerializeField] private string mDefaultHint;
    [SerializeField] private Vector3 mTopCardHoverBoundsPadding = new Vector3(0.08f, 0.08f, 1f);

    [Header("Board Hover")]
    [SerializeField] private float mBoardHoverScaleMultiplier = 1.06f;
    [SerializeField] private float mBoardHoverSpringStiffness = 150f;
    [SerializeField] private float mBoardHoverSpringDamping = 12f;
    [SerializeField] private int mBoardHoverSortingBoost = 80;

    [Header("Battle FX Preview")]
    [SerializeField, Range(0, 4)] private int mBattleFxStyleIndex;
    [SerializeField, Range(0f, 3f)] private float mBattleFxShakeAmplitudeMultiplier = 1f;
    [SerializeField, Range(0.2f, 3f)] private float mBattleFxShakeFrequencyMultiplier = 1f;
    [SerializeField] private bool mUseFallbackCameraShakeWhenNoCinemachineListener = true;

    [Header("Mouse")]
    [SerializeField] private Vector3 mCardHitBoundsPadding = new Vector3(0.08f, 0.08f, 1f);
    [SerializeField] private Vector3 mSlotHitBoundsPadding = new Vector3(0.08f, 0.08f, 1f);
    [SerializeField] private float mSlotFallbackHitRadius = 1.1f;

    [Header("Cleanup")]
    [SerializeField] private bool mSpawnDeckOnStart = true;
    [SerializeField] private bool mDestroySpawnedCardsOnDestroy = true;

    private readonly Dictionary<int, DemoCardEntry> mCardsBySlot = new Dictionary<int, DemoCardEntry>();
    private readonly List<DemoCardEntry> mDeckCards = new List<DemoCardEntry>();
    private readonly List<SlotInfo> mSlots = new List<SlotInfo>();
    private readonly List<CardDefinition> mMonsterPool = new List<CardDefinition>();
    private readonly List<CardDefinition> mHelpPool = new List<CardDefinition>();

    private BakedCardFaceComposer mComposer;
    private Camera mMainCamera;
    private bool mIsSequencing;
    private int mSpawnCount;
    private int mMonsterCursor;
    private int mHelpCursor;
    private bool mHoveringTopDeck;
    private int mHoveredBoardSlotNo = -1;
    private string mDefaultDescription;
    private bool mDescriptionOwned;
    private Vector3 mPlayerCardBaseScale = Vector3.one;
    private float mPlayerCardCurrentHoverScale = 1f;
    private float mPlayerCardHoverScaleVelocity;
    private bool mPlayerCardBaseScaleCached;
    private Coroutine mFallbackCameraShakeCoroutine;
    private Transform mFallbackShakeCamera;
    private Vector3 mFallbackShakeCameraBaseLocalPosition;

    private static readonly BattleFxStyle[] BattleFxStyles =
    {
        new BattleFxStyle("2 Snap", 0.18f, 0.045f, 0.20f, 0.58f, 1.12f, 1.08f, 10, 1.8f, 360f, 0.18f, 1.15f, 1.15f, "Bump"),
        new BattleFxStyle("3 Heavy", 0.24f, 0.06f, 0.28f, 0.68f, 1.22f, 1.16f, 18, 2.8f, 520f, 0.26f, 2.2f, 0.9f, "Explosion"),
        new BattleFxStyle("4 Slice", 0.16f, 0.035f, 0.22f, 0.52f, 1.08f, 1.03f, 14, 2.4f, 680f, 0.16f, 1.55f, 1.45f, "Recoil"),
        new BattleFxStyle("5 Bounce", 0.28f, 0.08f, 0.26f, 0.62f, 1.18f, 1.24f, 16, 2.1f, 420f, 0.22f, 1.6f, 1.25f, "Bump"),
        new BattleFxStyle("6 Burst", 0.20f, 0.04f, 0.34f, 0.72f, 1.28f, 1.12f, 22, 3.2f, 760f, 0.30f, 2.65f, 1.1f, "Explosion")
    };

    public IArchitecture GetArchitecture()
    {
        return TableNine.Interface;
    }

    private void Awake()
    {
        mCardPrefab = BakedCardPrefabRefs.ResolveStandardCard(mCardPrefab);
        mCardFaceTemplate = BakedCardPrefabRefs.ResolveCardExample(mCardFaceTemplate);
        mPlayerCardTemplate = BakedCardPrefabRefs.ResolvePlayerCard(mPlayerCardTemplate);
    }

    private void Start()
    {
        if (!EnsureReady())
        {
            enabled = false;
            return;
        }

        mComposer = new BakedCardFaceComposer(mCardFaceTemplate, mPlayerCardTemplate);
        InitializeDescriptionPanel();
        BuildPools();
        RebuildSlots();

        if (mSpawnDeckOnStart)
        {
            SpawnInitialDeck();
        }

        UpdateDeckLeftText();
    }

    private void Update()
    {
        if (!EnsureReady())
        {
            return;
        }

        var interactionBlocked = mIsSequencing || UIGameplayPanel.IsSidePanelHovered;
        if (interactionBlocked)
        {
            SuppressHoverInteraction();
            UpdateDescriptionPanel();
        }
        else
        {
            UpdateHoverState();
            UpdateDescriptionPanel();
        }

        UpdateBoardHoverVisuals(Time.deltaTime);

        if (interactionBlocked)
        {
            return;
        }

        if (mDealAllKey != KeyCode.None && Input.GetKeyDown(mDealAllKey))
        {
            DealAllOuterRingSlots();
            return;
        }

        UpdateBattleFxStyleHotkeys();

        if (Input.GetMouseButtonDown(1))
        {
            HandleRightClick();
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            HandleLeftClick();
        }
    }

    private void OnDestroy()
    {
        RestoreDescriptionIfOwned();
        ResetBoardHoverVisuals(true);
        CleanupEntries(mDeckCards);
        CleanupEntries(mCardsBySlot.Values);
        mDeckCards.Clear();
        mCardsBySlot.Clear();
        mComposer?.Dispose();
        mComposer = null;
    }

    [ContextMenu("Reset Demo Deck")]
    public void ResetDemoDeck()
    {
        CleanupEntries(mDeckCards);
        CleanupEntries(mCardsBySlot.Values);
        mDeckCards.Clear();
        mCardsBySlot.Clear();
        mIsSequencing = false;
        mSpawnCount = 0;
        mMonsterCursor = 0;
        mHelpCursor = 0;
        mHoveringTopDeck = false;
        mHoveredBoardSlotNo = -1;
        ResetBoardHoverVisuals(true);

        if (!EnsureReady())
        {
            return;
        }

        if (mComposer == null)
        {
            mComposer = new BakedCardFaceComposer(mCardFaceTemplate, mPlayerCardTemplate);
        }

        BuildPools();
        RebuildSlots();
        SpawnInitialDeck();
        UpdateDeckLeftText();
    }

    [ContextMenu("Deal All Outer Ring Slots")]
    public void DealAllOuterRingSlots()
    {
        if (mIsSequencing || !EnsureReady())
        {
            return;
        }

        var emptySlots = new List<int>();
        for (var i = 0; i < NineGridOuterRingUtility.Count; i++)
        {
            var slotNo = NineGridOuterRingUtility.GetSlotAt(i);
            if (!mCardsBySlot.ContainsKey(slotNo))
            {
                emptySlots.Add(slotNo);
            }
        }

        if (emptySlots.Count == 0 || mDeckCards.Count == 0)
        {
            return;
        }

        mIsSequencing = true;
        DealSlotsSequentially(emptySlots, 0, () => mIsSequencing = false);
    }

    [ContextMenu("Move Clockwise One Step")]
    public void MoveClockwiseOneStep()
    {
        if (mIsSequencing || !EnsureReady())
        {
            return;
        }

        mIsSequencing = true;
        MoveClockwiseOneStep(() => mIsSequencing = false);
    }

    private void HandleLeftClick()
    {
        if (TryFindHoveredCardSlot(out _))
        {
            mIsSequencing = true;
            MoveClockwiseOneStep(() => mIsSequencing = false);
            return;
        }

        if (mDeckCards.Count > 0)
        {
            return;
        }

        if (TryFindHoveredSlot(out var slot) &&
            NineGridOuterRingUtility.IsOuterRingSlot(slot.SlotNo) &&
            !mCardsBySlot.ContainsKey(slot.SlotNo))
        {
            mIsSequencing = true;
            MoveClockwiseOneStep(() => mIsSequencing = false);
        }
    }

    private void HandleRightClick()
    {
        if (!TryFindHoveredCardSlot(out var slotNo))
        {
            return;
        }

        mIsSequencing = true;
        StartCoroutine(PlayRightClickBattleFxThenRemove(slotNo));
    }

    private IEnumerator PlayRightClickBattleFxThenRemove(int slotNo)
    {
        if (!mCardsBySlot.TryGetValue(slotNo, out var entry) || entry?.Root == null || mPlayerCard == null)
        {
            FinishRightClickRemove(slotNo);
            yield break;
        }

        var style = BattleFxStyles[Mathf.Clamp(mBattleFxStyleIndex, 0, BattleFxStyles.Length - 1)];
        entry.Tween?.Kill();
        entry.Root.DOKill();
        ResetBoardHoverVisuals(true);

        var playerStartPosition = mPlayerCard.position;
        var targetStartPosition = entry.Root.position;
        var playerStartScale = mPlayerCard.localScale;
        var targetStartScale = entry.Root.localScale;
        var direction = targetStartPosition - playerStartPosition;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            direction = Vector3.right;
        }

        direction.Normalize();
        var hitPosition = Vector3.Lerp(playerStartPosition, targetStartPosition, style.ContactRatio);
        var targetHitPosition = targetStartPosition + direction * 0.08f;
        ApplySortingOrder(entry.Root, mFlyingSortingOrder + 90);

        var elapsed = 0f;
        while (elapsed < style.ApproachDuration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, style.ApproachDuration));
            var eased = EaseOutBackValue(t);
            mPlayerCard.position = Vector3.LerpUnclamped(playerStartPosition, hitPosition, eased);
            entry.Root.position = Vector3.Lerp(targetStartPosition, targetHitPosition, EaseOutQuadValue(t));
            mPlayerCard.localScale = playerStartScale * Mathf.Lerp(1f, style.PlayerScale, EaseOutQuadValue(t));
            entry.Root.localScale = targetStartScale * Mathf.Lerp(1f, style.TargetScale, EaseOutQuadValue(t));
            yield return null;
        }

        TriggerDemoBattleShake(targetStartPosition, direction, style);
        yield return CardFakeShatterEffect.PlayOnTransform(
            entry.Root,
            CardFakeShatterEffect.ComputeImpactPoint(
                entry.Root.GetComponentInChildren<SpriteRenderer>(true),
                targetStartPosition,
                direction),
            direction,
            style.ImpactHold,
            style.ShardDuration,
            CardFakeShatterSettings.FromShardCount(style.ShardCount));

        entry.Root.localScale = Vector3.zero;

        elapsed = 0f;
        var playerRecoverStartPosition = mPlayerCard.position;
        var playerRecoverStartScale = mPlayerCard.localScale;
        while (elapsed < style.RecoverDuration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, style.RecoverDuration));
            var eased = EaseOutQuadValue(t);
            mPlayerCard.position = Vector3.Lerp(playerRecoverStartPosition, playerStartPosition, eased);
            mPlayerCard.localScale = Vector3.Lerp(playerRecoverStartScale, playerStartScale, eased);
            yield return null;
        }

        mPlayerCard.position = playerStartPosition;
        mPlayerCard.localScale = playerStartScale;
        if (entry.Root != null)
        {
            entry.Root.position = targetStartPosition;
            entry.Root.localScale = targetStartScale;
        }

        FinishRightClickRemove(slotNo);
    }

    private void FinishRightClickRemove(int slotNo)
    {
        RemoveCardAt(slotNo);

        if (mDeckCards.Count > 0)
        {
            DealNextCardToSlot(slotNo, () => MoveClockwiseOneStep(() => mIsSequencing = false));
        }
        else
        {
            MoveClockwiseOneStep(() => mIsSequencing = false);
        }
    }

    private void UpdateBattleFxStyleHotkeys()
    {
        if (Input.GetKeyDown(KeyCode.Alpha2)) SetBattleFxStyle(0);
        if (Input.GetKeyDown(KeyCode.Alpha3)) SetBattleFxStyle(1);
        if (Input.GetKeyDown(KeyCode.Alpha4)) SetBattleFxStyle(2);
        if (Input.GetKeyDown(KeyCode.Alpha5)) SetBattleFxStyle(3);
        if (Input.GetKeyDown(KeyCode.Alpha6)) SetBattleFxStyle(4);
    }

    private void SetBattleFxStyle(int styleIndex)
    {
        mBattleFxStyleIndex = Mathf.Clamp(styleIndex, 0, BattleFxStyles.Length - 1);
        Debug.Log($"NineGrid Battle FX {BattleFxStyles[mBattleFxStyleIndex].Name}");
    }

    private bool EnsureReady()
    {
        if (mDeckSlot == null)
        {
            mDeckSlot = GameObject.Find(string.IsNullOrWhiteSpace(mDeckSlotName)
                ? DefaultDeckSlotName
                : mDeckSlotName)?.transform;
        }

        if (mBoardRoot == null)
        {
            mBoardRoot = GameObject.Find(string.IsNullOrWhiteSpace(mBoardRootName)
                ? DefaultBoardRootName
                : mBoardRootName)?.transform;
        }

        if (mDeckLeftText == null)
        {
            var textObject = GameObject.Find(string.IsNullOrWhiteSpace(mDeckLeftTextName)
                ? DefaultDeckLeftTextName
                : mDeckLeftTextName);
            if (textObject != null)
            {
                mDeckLeftText = textObject.GetComponent<TextMeshProUGUI>();
            }
        }

        if (mDeckSlot == null)
        {
            Debug.LogError($"[NineGridCardMoveDemo] Could not find deck slot '{mDeckSlotName}'.");
            return false;
        }

        if (mBoardRoot == null)
        {
            Debug.LogError($"[NineGridCardMoveDemo] Could not find board root '{mBoardRootName}'.");
            return false;
        }

        EnsurePlayerCardResolved();

        if (!TableNine.IsInitialized)
        {
            TableNine.InitArchitecture();
        }

        return TableNine.IsInitialized;
    }

    private void EnsurePlayerCardResolved()
    {
        if (mPlayerCard != null)
        {
            CachePlayerCardBaseScale();
            return;
        }

        if (mBoardRoot == null)
        {
            return;
        }

        var playerSlot = FindChild(mBoardRoot, "CardSlot5ForPlayer");
        if (playerSlot == null)
        {
            return;
        }

        mPlayerCard = FindChild(playerSlot, string.IsNullOrWhiteSpace(mPlayerCardName)
            ? DefaultPlayerCardName
            : mPlayerCardName);
        if ((mPlayerCard == null || !mPlayerCard.gameObject.activeInHierarchy) && mBoardRoot != null)
        {
            var bakedPlayerView = FindChild(mBoardRoot, "BoardCardView5");
            if (bakedPlayerView != null)
            {
                mPlayerCard = bakedPlayerView;
            }
        }

        CachePlayerCardBaseScale();
    }

    private void CachePlayerCardBaseScale()
    {
        if (mPlayerCard == null || mPlayerCardBaseScaleCached)
        {
            return;
        }

        mPlayerCardBaseScale = mPlayerCard.localScale;
        mPlayerCardBaseScaleCached = true;
    }

    private void InitializeDescriptionPanel()
    {
        if (mDescriptionText == null)
        {
            mDescriptionText = FindDescriptionText();
        }

        if (mDescriptionText == null)
        {
            return;
        }

        mDescriptionText.enableWordWrapping = true;
        mDescriptionText.overflowMode = TextOverflowModes.Overflow;
        mDefaultDescription = mDescriptionText.text;
    }

    private void BuildPools()
    {
        mMonsterPool.Clear();
        mHelpPool.Clear();

        var config = this.GetModel<IConfigModel>();
        AddCards(config.GetCardsByType(CardType.Monster), mMonsterPool);
        AddCards(config.GetCardsByType(CardType.Help), mHelpPool);
    }

    private static void AddCards(IReadOnlyList<CardDefinition> source, List<CardDefinition> target)
    {
        if (source == null)
        {
            return;
        }

        for (var i = 0; i < source.Count; i++)
        {
            if (source[i] != null)
            {
                target.Add(source[i]);
            }
        }
    }

    private void RebuildSlots()
    {
        mSlots.Clear();
        if (mBoardRoot == null)
        {
            return;
        }

        for (var i = 0; i < mBoardRoot.childCount; i++)
        {
            var child = mBoardRoot.GetChild(i);
            if (!TryResolveSlotNo(child.name, out var slotNo))
            {
                continue;
            }

            mSlots.Add(new SlotInfo(slotNo, child, child.GetComponent<SpriteRenderer>()));
        }
    }

    private void SpawnInitialDeck()
    {
        var count = Mathf.Max(0, mInitialCardCount);
        for (var i = 0; i < count; i++)
        {
            var definition = ResolveNextDefinition();
            if (definition == null)
            {
                Debug.LogWarning("[NineGridCardMoveDemo] No monster/help card definition is available.");
                break;
            }

            var entry = CreateCardEntry(definition, ResolveDeckCenter(), Quaternion.Euler(0f, 0f, 90f), mDeckSortingOrder + mSpawnCount);
            if (entry != null)
            {
                mDeckCards.Add(entry);
                mSpawnCount++;
            }
        }

        LayoutDeckLine(true);
    }

    private CardDefinition ResolveNextDefinition()
    {
        var targetType = ResolveSpawnType();
        var config = this.GetModel<IConfigModel>();
        var configuredId = targetType == CardType.Monster ? mMonsterCardId : mHelpCardId;
        if (!string.IsNullOrWhiteSpace(configuredId) &&
            config.TryGetCardDefinition(configuredId, out var configuredDefinition) &&
            configuredDefinition.CardType == targetType)
        {
            return configuredDefinition;
        }

        return targetType == CardType.Monster
            ? NextFromPool(mMonsterPool, ref mMonsterCursor)
            : NextFromPool(mHelpPool, ref mHelpCursor);
    }

    private CardType ResolveSpawnType()
    {
        if (!mAlternateMonsterAndHelp)
        {
            return mFirstSpawnType == CardType.Help ? CardType.Help : CardType.Monster;
        }

        var firstIsHelp = mFirstSpawnType == CardType.Help;
        var useHelp = (mSpawnCount % 2 == 0) == firstIsHelp;
        return useHelp ? CardType.Help : CardType.Monster;
    }

    private static CardDefinition NextFromPool(IReadOnlyList<CardDefinition> pool, ref int cursor)
    {
        if (pool == null || pool.Count == 0)
        {
            return null;
        }

        var definition = pool[cursor % pool.Count];
        cursor = (cursor + 1) % pool.Count;
        return definition;
    }

    private DemoCardEntry CreateCardEntry(CardDefinition definition, Vector3 worldPosition, Quaternion rotation, int sortingOrder)
    {
        var rootObject = new GameObject($"NineGridCard_{mSpawnCount:00}_{definition.CardType}_{definition.CardId}");
        rootObject.transform.SetParent(transform, true);
        rootObject.transform.position = worldPosition;
        rootObject.transform.rotation = rotation;
        rootObject.transform.localScale = Vector3.one;

        var cardObject = mCardPrefab != null
            ? Instantiate(mCardPrefab, rootObject.transform)
            : new GameObject("Card", typeof(CardView));
        cardObject.name = "Card";
        cardObject.transform.SetParent(rootObject.transform, false);
        cardObject.transform.localPosition = Vector3.zero;
        cardObject.transform.localRotation = Quaternion.identity;
        cardObject.transform.localScale = Vector3.one;

        var cardView = cardObject.GetComponent<CardView>();
        if (cardView == null)
        {
            cardView = cardObject.AddComponent<CardView>();
        }

        cardView.Initialize();
        cardView.SetTargetWorldHeight(mWorldCardHeight);

        var renderData = BakedCardRenderDataFactory.CreateFromCardDefinition(this.GetModel<IConfigModel>(), definition);
        if (renderData == null || mComposer == null)
        {
            DestroyUnityObject(rootObject);
            return null;
        }

        var sprites = mComposer.ComposeSet(renderData, BakedCardRenderDataFactory.StandardPixelsPerUnit);
        if (!sprites.HasFace)
        {
            DestroyUnityObject(rootObject);
            return null;
        }

        cardView.ShowBaked(sprites);
        cardView.DisplayAdapter?.SetFaceVisible(true);
        ApplySortingOrder(cardView, sortingOrder);

        return new DemoCardEntry(rootObject.transform, definition, rootObject.transform.localScale);
    }

    private void LayoutDeckLine(bool instant)
    {
        var center = ResolveDeckCenter();
        var half = (mDeckCards.Count - 1) * 0.5f;
        for (var i = 0; i < mDeckCards.Count; i++)
        {
            var entry = mDeckCards[i];
            if (entry.Root == null)
            {
                continue;
            }

            var isTop = i == mDeckCards.Count - 1;
            var target = mOnlyShowTopDeckCard
                ? center + new Vector3(0f, 0f, -0.002f * i)
                : center + new Vector3((i - half) * mHorizontalSpacing, 0f, -0.002f * i);

            SetCardVisible(entry, !mOnlyShowTopDeckCard || isTop);
            entry.Tween?.Kill();
            entry.Root.DOKill();

            if (instant)
            {
                entry.Root.position = target;
                entry.Root.rotation = Quaternion.Euler(0f, 0f, 90f);
                entry.Root.localScale = entry.BaseScale;
            }
            else
            {
                entry.Tween = entry.Root.DOMove(target, Mathf.Max(0.01f, mDeckRelayoutDuration))
                    .SetEase(mDeckRelayoutEase);
                entry.Root.DORotate(new Vector3(0f, 0f, 90f), Mathf.Max(0.01f, mDeckRelayoutDuration))
                    .SetEase(mDeckRelayoutEase);
            }

            ApplySortingOrder(entry.Root, mDeckSortingOrder + i);
        }
    }

    private Vector3 ResolveDeckCenter()
    {
        return (mDeckSlot != null ? mDeckSlot.position : transform.position) + mDeckWorldOffset;
    }

    private void DealSlotsSequentially(IReadOnlyList<int> slotNos, int index, TweenCallback onComplete)
    {
        if (index >= slotNos.Count || mDeckCards.Count == 0)
        {
            onComplete?.Invoke();
            return;
        }

        DealNextCardToSlot(slotNos[index], () => DealSlotsSequentially(slotNos, index + 1, onComplete));
    }

    private void DealNextCardToSlot(int slotNo, TweenCallback onComplete)
    {
        if (!TryGetSlotTransform(slotNo, out var slotTransform))
        {
            Debug.LogWarning($"[NineGridCardMoveDemo] Missing CardSlot{slotNo} under {mBoardRootName}.");
            onComplete?.Invoke();
            return;
        }

        var top = TakeTopDeckCard();
        if (top == null || top.Root == null)
        {
            onComplete?.Invoke();
            return;
        }

        top.SlotNo = slotNo;
        mCardsBySlot[slotNo] = top;
        SetCardVisible(top, true);
        top.Tween?.Kill();
        top.Root.DOKill();
        ResetEntryHoverMotion(top);
        ApplySortingOrder(top.Root, mFlyingSortingOrder + mCardsBySlot.Count);
        LayoutDeckLine(false);
        UpdateDeckLeftText();

        var start = top.Root.position;
        var target = slotTransform.position + mCardWorldOffset;
        var aimDuration = Mathf.Max(0f, mPreDealAimDuration);
        var aimAngle = CalculatePointAtAngle(start, target);
        var control = (start + target) * 0.5f + Vector3.up * Mathf.Max(0f, mDealArcHeight);

        var sequence = DOTween.Sequence();
        if (aimDuration > 0f)
        {
            sequence.Append(top.Root
                .DORotate(new Vector3(0f, 0f, aimAngle), aimDuration)
                .SetEase(mPreDealAimEase));
        }

        sequence.Append(DOTween.To(
                () => 0f,
                progress => top.Root.position = QuadraticBezier(start, control, target, progress),
                1f,
                Mathf.Max(0.01f, mDealDuration))
            .SetEase(mDealMoveEase));

        sequence.Join(top.Root
            .DORotate(Vector3.zero, Mathf.Max(0.01f, mDealDuration))
            .SetEase(mLandingRotationEase, mLandingRotationOvershoot));

        sequence.Join(top.Root
            .DOScale(top.BaseScale * Mathf.Max(0.01f, mDealScaleMultiplier), Mathf.Max(0.01f, mDealDuration * 0.42f))
            .SetLoops(2, LoopType.Yoyo)
            .SetEase(mDealScaleEase));

        sequence.OnComplete(() =>
        {
            if (top.Root != null)
            {
                top.Root.position = target;
                top.Root.rotation = Quaternion.identity;
                top.Root.localScale = top.BaseScale;
                ApplySortingOrder(top.Root, mBaseSortingOrder + slotNo);
            }

            ResetEntryHoverMotion(top);
            top.Tween = null;
            onComplete?.Invoke();
        });

        top.Tween = sequence;
        sequence.Play();
    }

    private DemoCardEntry TakeTopDeckCard()
    {
        if (mDeckCards.Count == 0)
        {
            return null;
        }

        var top = mDeckCards[mDeckCards.Count - 1];
        mDeckCards.RemoveAt(mDeckCards.Count - 1);
        return top;
    }

    private void MoveClockwiseOneStep(TweenCallback onComplete)
    {
        if (!EnsureReady())
        {
            onComplete?.Invoke();
            return;
        }

        var movingEntries = new List<DemoCardMove>();
        var nextMap = new Dictionary<int, DemoCardEntry>();
        for (var i = 0; i < NineGridOuterRingUtility.Count; i++)
        {
            var fromSlot = NineGridOuterRingUtility.GetSlotAt(i);
            if (!mCardsBySlot.TryGetValue(fromSlot, out var entry))
            {
                continue;
            }

            var toSlot = NineGridOuterRingUtility.GetNextClockwiseSlot(fromSlot);
            if (!TryGetSlotTransform(toSlot, out var toTransform))
            {
                Debug.LogWarning($"[NineGridCardMoveDemo] Missing destination CardSlot{toSlot} under {mBoardRootName}.");
                onComplete?.Invoke();
                return;
            }

            movingEntries.Add(new DemoCardMove(entry, fromSlot, toSlot, toTransform.position + mCardWorldOffset));
            nextMap[toSlot] = entry;
        }

        mCardsBySlot.Clear();
        foreach (var pair in nextMap)
        {
            pair.Value.SlotNo = pair.Key;
            mCardsBySlot[pair.Key] = pair.Value;
        }

        PlayMoveTweens(movingEntries, onComplete);
    }

    private void PlayMoveTweens(IReadOnlyList<DemoCardMove> moves, TweenCallback onComplete)
    {
        var remaining = moves.Count;
        if (remaining == 0)
        {
            onComplete?.Invoke();
            return;
        }

        for (var i = 0; i < moves.Count; i++)
        {
            var move = moves[i];
            var entry = move.Entry;
            if (entry.Root == null)
            {
                remaining--;
                continue;
            }

            entry.Tween?.Kill();
            entry.Root.DOKill();
            ResetEntryHoverMotion(entry);
            var start = entry.Root.position;
            var target = move.TargetWorldPosition;
            var control = (start + target) * 0.5f;
            if (mEnableHopArc)
            {
                control += Vector3.up * Mathf.Max(0f, mHopArcHeight);
            }

            var sequence = DOTween.Sequence();
            sequence.Join(DOTween.To(
                    () => 0f,
                    progress => entry.Root.position = QuadraticBezier(start, control, target, progress),
                    1f,
                    Mathf.Max(0.01f, mMoveDuration))
                .SetEase(mMoveEase));

            if (mEnableHopScale)
            {
                var scaleTarget = entry.BaseScale * Mathf.Max(0.01f, mHopScaleMultiplier);
                sequence.Join(entry.Root
                    .DOScale(scaleTarget, Mathf.Max(0.01f, mMoveDuration * 0.5f))
                    .SetLoops(2, LoopType.Yoyo)
                    .SetEase(mHopScaleEase));
            }

            sequence.OnComplete(() =>
            {
                if (entry.Root != null)
                {
                    entry.Root.position = target;
                    entry.Root.localScale = entry.BaseScale;
                    ApplySortingOrder(entry.Root, mBaseSortingOrder + entry.SlotNo);
                }

                ResetEntryHoverMotion(entry);
                entry.Tween = null;
                remaining--;
                if (remaining <= 0)
                {
                    onComplete?.Invoke();
                }
            });

            entry.Tween = sequence;
            sequence.Play();
        }

        if (remaining <= 0)
        {
            onComplete?.Invoke();
        }
    }

    private void RemoveCardAt(int slotNo)
    {
        if (!mCardsBySlot.TryGetValue(slotNo, out var entry))
        {
            return;
        }

        mCardsBySlot.Remove(slotNo);
        entry.Tween?.Kill();
        if (entry.Root != null)
        {
            entry.Root.DOKill();
            DestroyUnityObject(entry.Root.gameObject);
        }
    }

    private bool TryFindHoveredCardSlot(out int slotNo)
    {
        slotNo = 0;
        if (!TryGetMouseWorldPoint(out var world))
        {
            return false;
        }

        return TryFindHoveredCardSlot(world, out slotNo);
    }

    private bool TryFindHoveredCardSlot(Vector3 world, out int slotNo)
    {
        slotNo = 0;
        var bestDistance = float.MaxValue;
        var found = false;

        foreach (var pair in mCardsBySlot)
        {
            var entry = pair.Value;
            if (entry?.Root == null || !TryGetWorldBounds(entry.Root, out var bounds))
            {
                continue;
            }

            bounds.Expand(mCardHitBoundsPadding);
            if (!bounds.Contains(world))
            {
                continue;
            }

            var distance = (entry.Root.position - world).sqrMagnitude;
            if (distance >= bestDistance)
            {
                continue;
            }

            bestDistance = distance;
            slotNo = pair.Key;
            found = true;
        }

        return found;
    }

    private void UpdateHoverState()
    {
        mHoveringTopDeck = false;
        mHoveredBoardSlotNo = -1;

        if (!TryGetMouseWorldPoint(out var world))
        {
            return;
        }

        if (TryFindHoveredCardSlot(world, out var slotNo))
        {
            mHoveredBoardSlotNo = slotNo;
            return;
        }

        mHoveringTopDeck = TryIsHoveringDeck(world);
    }

    private void SuppressHoverInteraction()
    {
        if (!mHoveringTopDeck && mHoveredBoardSlotNo < 0)
        {
            return;
        }

        mHoveringTopDeck = false;
        mHoveredBoardSlotNo = -1;
    }

    private bool IsHoverInteractionBlocked()
    {
        return mIsSequencing || UIGameplayPanel.IsSidePanelHovered;
    }

    private bool TryIsHoveringDeck(Vector3 world)
    {
        var top = GetTopDeckCard();
        if (top?.Root == null || !top.Root.gameObject.activeInHierarchy)
        {
            return false;
        }

        if (TryGetWorldBounds(top.Root, out var cardBounds))
        {
            cardBounds.Expand(mTopCardHoverBoundsPadding);
            if (cardBounds.Contains(world))
            {
                return true;
            }
        }

        if (mDeckSlot == null)
        {
            return false;
        }

        var deckRenderer = mDeckSlot.GetComponent<SpriteRenderer>();
        if (deckRenderer != null)
        {
            var deckBounds = deckRenderer.bounds;
            deckBounds.Expand(mSlotHitBoundsPadding);
            return deckBounds.Contains(world);
        }

        return Vector2.Distance(world, ResolveDeckCenter()) <= Mathf.Max(0.01f, mSlotFallbackHitRadius);
    }

    private static void ResetEntryHoverMotion(DemoCardEntry entry)
    {
        if (entry == null)
        {
            return;
        }

        entry.CurrentHoverScale = 1f;
        entry.HoverScaleVelocity = 0f;
    }

    private void UpdateBoardHoverVisuals(float deltaTime)
    {
        var interactionBlocked = IsHoverInteractionBlocked();

        foreach (var pair in mCardsBySlot)
        {
            var entry = pair.Value;
            if (entry?.Root == null)
            {
                continue;
            }

            var isHovered = !interactionBlocked &&
                            pair.Key == mHoveredBoardSlotNo &&
                            entry.Tween == null;
            var targetScale = isHovered
                ? Mathf.Max(1f, mBoardHoverScaleMultiplier)
                : 1f;

            entry.CurrentHoverScale = SpringMath.Step(
                ref entry.CurrentHoverScale,
                ref entry.HoverScaleVelocity,
                targetScale,
                Mathf.Max(1f, mBoardHoverSpringStiffness),
                Mathf.Max(0f, mBoardHoverSpringDamping),
                deltaTime);

            if (entry.Tween == null)
            {
                entry.Root.localScale = entry.BaseScale * entry.CurrentHoverScale;
            }

            var sortingOrder = mBaseSortingOrder + pair.Key;
            if (isHovered)
            {
                sortingOrder += mBoardHoverSortingBoost;
            }

            ApplySortingOrder(entry.Root, sortingOrder);
        }

        UpdatePlayerCardHoverVisual(deltaTime, interactionBlocked);
    }

    private void UpdatePlayerCardHoverVisual(float deltaTime, bool interactionBlocked)
    {
        if (mPlayerCard == null)
        {
            return;
        }

        var isBoardCardHovered = !interactionBlocked && mHoveredBoardSlotNo > 0;
        var targetScale = isBoardCardHovered
            ? Mathf.Max(1f, mBoardHoverScaleMultiplier)
            : 1f;

        mPlayerCardCurrentHoverScale = SpringMath.Step(
            ref mPlayerCardCurrentHoverScale,
            ref mPlayerCardHoverScaleVelocity,
            targetScale,
            Mathf.Max(1f, mBoardHoverSpringStiffness),
            Mathf.Max(0f, mBoardHoverSpringDamping),
            deltaTime);

        mPlayerCard.localScale = mPlayerCardBaseScale * mPlayerCardCurrentHoverScale;
    }

    private void ResetBoardHoverVisuals(bool instant)
    {
        foreach (var pair in mCardsBySlot)
        {
            var entry = pair.Value;
            if (entry?.Root == null)
            {
                continue;
            }

            entry.HoverScaleVelocity = 0f;
            entry.CurrentHoverScale = instant ? 1f : entry.CurrentHoverScale;
            if (instant)
            {
                if (entry.Tween == null)
                {
                    entry.Root.localScale = entry.BaseScale;
                }

                ApplySortingOrder(entry.Root, mBaseSortingOrder + pair.Key);
            }
        }

        ResetPlayerCardHoverVisual(instant);
    }

    private void ResetPlayerCardHoverVisual(bool instant)
    {
        if (mPlayerCard == null)
        {
            return;
        }

        mPlayerCardHoverScaleVelocity = 0f;
        if (!instant)
        {
            return;
        }

        mPlayerCardCurrentHoverScale = 1f;
        mPlayerCard.localScale = mPlayerCardBaseScale;
    }

    private void UpdateDescriptionPanel()
    {
        if (mDescriptionText == null || UIGameplayPanel.IsSidePanelHovered)
        {
            return;
        }

        if (mHoveredBoardSlotNo > 0 &&
            mCardsBySlot.TryGetValue(mHoveredBoardSlotNo, out var boardEntry) &&
            boardEntry?.Definition != null)
        {
            ShowDescriptionForDefinition(boardEntry.Definition);
            return;
        }

        var top = GetTopDeckCard();
        if (mHoveringTopDeck && top?.Definition != null)
        {
            ShowDescriptionForDefinition(top.Definition);
            return;
        }

        RestoreDescriptionIfOwned();
    }

    private void ShowDescriptionForDefinition(CardDefinition definition)
    {
        var text = CardPreviewDescriptionComposer.Compose(this.GetModel<IConfigModel>(), definition);
        if (string.IsNullOrWhiteSpace(text))
        {
            text = definition.DisplayName ?? string.Empty;
        }

        mDescriptionText.text = DescriptionPanelTextRules.Clamp(text);
        mDescriptionOwned = true;
    }

    private void RestoreDescriptionIfOwned()
    {
        if (!mDescriptionOwned || mDescriptionText == null)
        {
            return;
        }

        mDescriptionText.text = !string.IsNullOrWhiteSpace(mDefaultHint)
            ? DescriptionPanelTextRules.Clamp(mDefaultHint)
            : mDefaultDescription;
        mDescriptionOwned = false;
    }

    private DemoCardEntry GetTopDeckCard()
    {
        return mDeckCards.Count > 0 ? mDeckCards[mDeckCards.Count - 1] : null;
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

    private bool TryFindHoveredSlot(out SlotInfo hoveredSlot)
    {
        hoveredSlot = null;
        if (mSlots.Count == 0)
        {
            RebuildSlots();
        }

        if (!TryGetMouseWorldPoint(out var world))
        {
            return false;
        }

        for (var i = 0; i < mSlots.Count; i++)
        {
            var slot = mSlots[i];
            if (slot.Transform == null)
            {
                continue;
            }

            if (slot.Renderer != null)
            {
                var bounds = slot.Renderer.bounds;
                bounds.Expand(mSlotHitBoundsPadding);
                if (bounds.Contains(world))
                {
                    hoveredSlot = slot;
                    return true;
                }
            }
            else if (Vector2.Distance(world, slot.Transform.position) <= Mathf.Max(0.01f, mSlotFallbackHitRadius))
            {
                hoveredSlot = slot;
                return true;
            }
        }

        return false;
    }

    private bool TryGetMouseWorldPoint(out Vector3 world)
    {
        world = Vector3.zero;
        var camera = ResolveCamera();
        if (camera == null)
        {
            return false;
        }

        var mouse = Input.mousePosition;
        var distance = -camera.transform.position.z;
        world = camera.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, distance));
        world.z = 0f;
        return true;
    }

    private Camera ResolveCamera()
    {
        if (mMainCamera == null)
        {
            mMainCamera = Camera.main;
        }

        return mMainCamera;
    }

    private bool TryGetSlotTransform(int slotNo, out Transform slotTransform)
    {
        slotTransform = null;
        if (mBoardRoot == null)
        {
            return false;
        }

        var childName = slotNo == 5 ? "CardSlot5ForPlayer" : $"CardSlot{slotNo}";
        slotTransform = FindChild(mBoardRoot, childName);
        return slotTransform != null;
    }

    private static Transform FindChild(Transform root, string childName)
    {
        if (root == null)
        {
            return null;
        }

        for (var i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i);
            if (child.name == childName)
            {
                return child;
            }
        }

        return null;
    }

    private static bool TryResolveSlotNo(string slotName, out int slotNo)
    {
        slotNo = 0;
        if (slotName == "CardSlot5ForPlayer")
        {
            slotNo = 5;
            return true;
        }

        const string prefix = "CardSlot";
        if (!slotName.StartsWith(prefix))
        {
            return false;
        }

        return int.TryParse(slotName.Substring(prefix.Length), out slotNo) && slotNo >= 1 && slotNo <= 9;
    }

    private static float CalculatePointAtAngle(Vector3 from, Vector3 to)
    {
        var direction = to - from;
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return 0f;
        }

        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
    }

    private void UpdateDeckLeftText()
    {
        if (mDeckLeftText != null)
        {
            mDeckLeftText.text = mDeckCards.Count.ToString();
        }
    }

    private static void SetCardVisible(DemoCardEntry entry, bool visible)
    {
        if (entry?.Root == null || entry.Root.gameObject.activeSelf == visible)
        {
            return;
        }

        entry.Root.gameObject.SetActive(visible);
    }

    private void ApplySortingOrder(Component root, int sortingOrder)
    {
        var renderers = root.GetComponentsInChildren<SpriteRenderer>(true);
        for (var i = 0; i < renderers.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(mSortingLayerName))
            {
                renderers[i].sortingLayerName = mSortingLayerName;
            }

            renderers[i].sortingOrder = sortingOrder;
        }
    }

    private static bool TryGetWorldBounds(Transform root, out Bounds bounds)
    {
        bounds = default;
        if (root == null)
        {
            return false;
        }

        var renderers = root.GetComponentsInChildren<SpriteRenderer>(false);
        var hasBounds = false;
        for (var i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer == null || !renderer.enabled || renderer.sprite == null)
            {
                continue;
            }

            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(renderer.bounds);
            }
        }

        return hasBounds;
    }

    private void TriggerDemoBattleShake(Vector3 origin, Vector3 hitDirection, BattleFxStyle style)
    {
        var amplitude = style.ShakeAmplitude * mBattleFxShakeAmplitudeMultiplier;
        var frequency = style.ShakeFrequency * mBattleFxShakeFrequencyMultiplier;
        var hasCinemachineListener = GameplayBattleEffectPreviewController.TryTriggerCinemachineImpulse(
            origin,
            hitDirection,
            style.ImpulseShapeName,
            style.ShakeDuration,
            amplitude,
            frequency);

        if (!hasCinemachineListener && mUseFallbackCameraShakeWhenNoCinemachineListener)
        {
            StartFallbackCameraShake(style.ShakeDuration, amplitude, frequency);
        }
    }

    private void StartFallbackCameraShake(float duration, float amplitude, float frequency)
    {
        var targetCamera = mMainCamera != null ? mMainCamera : Camera.main;
        if (targetCamera == null)
        {
            return;
        }

        if (mFallbackCameraShakeCoroutine != null)
        {
            StopCoroutine(mFallbackCameraShakeCoroutine);
            if (mFallbackShakeCamera != null)
            {
                mFallbackShakeCamera.localPosition = mFallbackShakeCameraBaseLocalPosition;
            }
        }

        mFallbackCameraShakeCoroutine = StartCoroutine(FallbackCameraShake(
            targetCamera.transform,
            duration,
            amplitude * 0.055f,
            frequency * 24f));
    }

    private IEnumerator FallbackCameraShake(Transform cameraTransform, float duration, float amplitude, float frequency)
    {
        mFallbackShakeCamera = cameraTransform;
        mFallbackShakeCameraBaseLocalPosition = cameraTransform.localPosition;
        var elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration));
            var fade = 1f - t;
            var x = (Mathf.PerlinNoise(Time.time * frequency, 0.1f) - 0.5f) * 2f;
            var y = (Mathf.PerlinNoise(0.2f, Time.time * frequency) - 0.5f) * 2f;
            cameraTransform.localPosition = mFallbackShakeCameraBaseLocalPosition + new Vector3(x, y, 0f) * amplitude * fade;
            yield return null;
        }

        cameraTransform.localPosition = mFallbackShakeCameraBaseLocalPosition;
        mFallbackCameraShakeCoroutine = null;
    }

    private static float EaseOutQuadValue(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }

    private static float EaseOutBackValue(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private static Vector3 QuadraticBezier(Vector3 start, Vector3 control, Vector3 end, float t)
    {
        var u = 1f - t;
        return u * u * start + 2f * u * t * control + t * t * end;
    }

    private void CleanupEntries(IEnumerable<DemoCardEntry> entries)
    {
        foreach (var entry in entries)
        {
            entry?.Tween?.Kill();
            if (entry?.Root != null)
            {
                entry.Root.DOKill();
            }

            if (mDestroySpawnedCardsOnDestroy && entry?.Root != null)
            {
                DestroyUnityObject(entry.Root.gameObject);
            }
        }
    }

    private static void DestroyUnityObject(Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }

    private readonly struct BattleFxStyle
    {
        public BattleFxStyle(
            string name,
            float approachDuration,
            float impactHold,
            float shardDuration,
            float contactRatio,
            float playerScale,
            float targetScale,
            int shardCount,
            float shardSpeed,
            float shardSpin,
            float shakeDuration,
            float shakeAmplitude,
            float shakeFrequency,
            string impulseShapeName)
        {
            Name = name;
            ApproachDuration = approachDuration;
            ImpactHold = impactHold;
            ShardDuration = shardDuration;
            ContactRatio = contactRatio;
            PlayerScale = playerScale;
            TargetScale = targetScale;
            ShardCount = shardCount;
            ShardSpeed = shardSpeed;
            ShardSpin = shardSpin;
            ShakeDuration = shakeDuration;
            ShakeAmplitude = shakeAmplitude;
            ShakeFrequency = shakeFrequency;
            ImpulseShapeName = impulseShapeName;
            RecoverDuration = 0.12f;
            ForwardShardBias = 0.55f;
            CrackAngleOffset = approachDuration * 8f;
        }

        public readonly string Name;
        public readonly float ApproachDuration;
        public readonly float ImpactHold;
        public readonly float ShardDuration;
        public readonly float ContactRatio;
        public readonly float PlayerScale;
        public readonly float TargetScale;
        public readonly int ShardCount;
        public readonly float ShardSpeed;
        public readonly float ShardSpin;
        public readonly float ShakeDuration;
        public readonly float ShakeAmplitude;
        public readonly float ShakeFrequency;
        public readonly float RecoverDuration;
        public readonly float ForwardShardBias;
        public readonly float CrackAngleOffset;
        public readonly string ImpulseShapeName;
    }

    private sealed class DemoCardEntry
    {
        public DemoCardEntry(Transform root, CardDefinition definition, Vector3 baseScale)
        {
            Root = root;
            Definition = definition;
            BaseScale = baseScale;
        }

        public Transform Root { get; }
        public CardDefinition Definition { get; }
        public Vector3 BaseScale { get; }
        public int SlotNo { get; set; }
        public Tween Tween { get; set; }
        public float CurrentHoverScale = 1f;
        public float HoverScaleVelocity;
    }

    private readonly struct DemoCardMove
    {
        public DemoCardMove(DemoCardEntry entry, int fromSlot, int toSlot, Vector3 targetWorldPosition)
        {
            Entry = entry;
            FromSlot = fromSlot;
            ToSlot = toSlot;
            TargetWorldPosition = targetWorldPosition;
        }

        public DemoCardEntry Entry { get; }
        public int FromSlot { get; }
        public int ToSlot { get; }
        public Vector3 TargetWorldPosition { get; }
    }

    private sealed class SlotInfo
    {
        public SlotInfo(int slotNo, Transform transform, SpriteRenderer renderer)
        {
            SlotNo = slotNo;
            Transform = transform;
            Renderer = renderer;
        }

        public int SlotNo { get; }
        public Transform Transform { get; }
        public SpriteRenderer Renderer { get; }
    }
}
