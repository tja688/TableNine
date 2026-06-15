using System.Collections.Generic;
using DG.Tweening;
using QFramework;
using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 临时垂直切片：从 CardDeckSlot 附近生成一组正式烘焙卡，默认只露出横放顶牌；悬停槽位时顶牌弹簧式指向，点击后弹射入槽。
/// </summary>
[DefaultExecutionOrder(275)]
public sealed class AgileCardDealerWorldDemo : MonoBehaviour, IController
{
    private const string StandardCardPrefabPath = "Assets/Prefabs/Cards/Card.prefab";
    private const string DefaultDeckSlotName = "CardDeckSlot";
    private const string DefaultBoardRootName = "NineGrid Main CardSlots";
    private const string DefaultDeckLeftTextName = "DeckLeftText";

    [Header("Scene")]
    [SerializeField] private Transform mDeckSlot;
    [SerializeField] private string mDeckSlotName = DefaultDeckSlotName;
    [SerializeField] private Transform mBoardRoot;
    [SerializeField] private string mBoardRootName = DefaultBoardRootName;
    [SerializeField] private TextMeshProUGUI mDeckLeftText;
    [SerializeField] private string mDeckLeftTextName = DefaultDeckLeftTextName;
    [SerializeField] private Vector3 mDeckWorldOffset = new Vector3(0f, 0f, -0.08f);
    [SerializeField] private Vector3 mSlotWorldOffset = new Vector3(0f, 0f, -0.08f);

    [Header("Card Pipeline")]
    [SerializeField] private GameObject mCardPrefab;
    [SerializeField] private GameObject mCardFaceTemplate;
    [SerializeField] private GameObject mPlayerCardTemplate;
    [SerializeField] private float mWorldCardHeight = BakedCardRenderDataFactory.CanonicalWorldCardHeight;
    [SerializeField] private string mSortingLayerName = "Cards_Front";
    [SerializeField] private int mDeckSortingOrder = 620;
    [SerializeField] private int mFlyingSortingOrder = 840;

    [Header("Deck")]
    [SerializeField] private int mInitialCardCount = 20;
    [SerializeField] private bool mOnlyShowTopDeckCard = true;
    [SerializeField] private float mHorizontalSpacing = 0.035f;
    [SerializeField] private float mDeckRelayoutDuration = 0.16f;
    [SerializeField] private Ease mDeckRelayoutEase = Ease.OutQuad;
    [SerializeField] private bool mAlternateMonsterAndHelp = true;
    [SerializeField] private CardType mFirstSpawnType = CardType.Monster;

    [Header("Aim")]
    [SerializeField] private float mDefaultAimAngleZ = 90f;
    [SerializeField] private float mAimSpringStiffness = 92f;
    [SerializeField] private float mAimSpringDamping = 13.5f;
    [SerializeField] private float mMaxAimAngularSpeed = 980f;
    [SerializeField] private float mAimSettleEpsilon = 0.02f;

    [Header("Deal")]
    [SerializeField] private float mDealDuration = 0.54f;
    [SerializeField] private float mDealArcHeight = 0.8f;
    [SerializeField] private Ease mDealMoveEase = Ease.OutCubic;
    [SerializeField] private Ease mLandingRotationEase = Ease.OutBack;
    [SerializeField] private float mLandingRotationOvershoot = 1.35f;
    [SerializeField] private float mDealScaleMultiplier = 1.08f;
    [SerializeField] private Ease mDealScaleEase = Ease.OutQuad;

    [Header("Cleanup")]
    [SerializeField] private bool mSpawnOnStart = true;
    [SerializeField] private bool mDestroySpawnedCardsOnDestroy = true;

    private readonly List<DealerCardEntry> mDeckCards = new List<DealerCardEntry>();
    private readonly List<DealerCardEntry> mPlacedCards = new List<DealerCardEntry>();
    private readonly List<SlotInfo> mSlots = new List<SlotInfo>();
    private readonly List<CardDefinition> mMonsterPool = new List<CardDefinition>();
    private readonly List<CardDefinition> mHelpPool = new List<CardDefinition>();

    private BakedCardFaceComposer mComposer;
    private Camera mMainCamera;
    private float mAimAngleZ;
    private float mAimVelocityZ;
    private int mSpawnCount;
    private int mMonsterCursor;
    private int mHelpCursor;
    private SlotInfo mHoveredSlot;
    private bool mIsDealing;

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
#endif
    }

    private void Start()
    {
        if (!EnsureReady())
        {
            enabled = false;
            return;
        }

        mComposer = new BakedCardFaceComposer(mCardFaceTemplate, mPlayerCardTemplate);
        mAimAngleZ = mDefaultAimAngleZ;
        BuildPools();
        RebuildSlots();

        if (mSpawnOnStart)
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

        UpdateHoveredSlot();
        StepTopCardAim(Time.deltaTime);

        if (!mIsDealing && mHoveredSlot != null && Input.GetMouseButtonDown(0))
        {
            DealTopCardTo(mHoveredSlot);
        }
    }

    private void OnDestroy()
    {
        CleanupEntries(mDeckCards);
        CleanupEntries(mPlacedCards);
        mDeckCards.Clear();
        mPlacedCards.Clear();
        mComposer?.Dispose();
        mComposer = null;
    }

    [ContextMenu("Reset And Spawn 20")]
    public void ResetAndSpawnInitialDeck()
    {
        CleanupEntries(mDeckCards);
        CleanupEntries(mPlacedCards);
        mDeckCards.Clear();
        mPlacedCards.Clear();
        mIsDealing = false;
        mAimVelocityZ = 0f;
        mAimAngleZ = mDefaultAimAngleZ;
        mSpawnCount = 0;
        mMonsterCursor = 0;
        mHelpCursor = 0;

        if (EnsureReady())
        {
            if (mComposer == null)
            {
                mComposer = new BakedCardFaceComposer(mCardFaceTemplate, mPlayerCardTemplate);
            }

            BuildPools();
            RebuildSlots();
            SpawnInitialDeck();
            UpdateDeckLeftText();
        }
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
            Debug.LogError($"[AgileCardDealerWorldDemo] Could not find deck slot '{mDeckSlotName}'.");
            return false;
        }

        if (mBoardRoot == null)
        {
            Debug.LogError($"[AgileCardDealerWorldDemo] Could not find board root '{mBoardRootName}'.");
            return false;
        }

        if (!TableNine.IsInitialized)
        {
            TableNine.InitArchitecture();
        }

        return TableNine.IsInitialized;
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
                Debug.LogWarning("[AgileCardDealerWorldDemo] No monster/help card definition is available.");
                break;
            }

            var entry = CreateCardEntry(definition);
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

    private DealerCardEntry CreateCardEntry(CardDefinition definition)
    {
        var rootObject = new GameObject($"DealerCard_{mSpawnCount:00}_{definition.CardType}_{definition.CardId}");
        rootObject.transform.SetParent(transform, true);
        rootObject.transform.position = ResolveDeckCenter();
        rootObject.transform.rotation = Quaternion.Euler(0f, 0f, mDefaultAimAngleZ);
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
        ApplySortingOrder(cardView, mDeckSortingOrder + mSpawnCount);

        return new DealerCardEntry(rootObject.transform, definition, rootObject.transform.localScale);
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

            SetDeckCardVisible(entry, !mOnlyShowTopDeckCard || isTop);
            entry.HomePosition = target;
            entry.Tween?.Kill();
            entry.Root.DOKill();

            if (instant)
            {
                entry.Root.position = target;
                entry.Root.rotation = Quaternion.Euler(0f, 0f, isTop ? mAimAngleZ : mDefaultAimAngleZ);
                entry.Root.localScale = entry.BaseScale;
            }
            else
            {
                entry.Tween = entry.Root.DOMove(target, Mathf.Max(0.01f, mDeckRelayoutDuration))
                    .SetEase(mDeckRelayoutEase);
                if (!isTop)
                {
                    entry.Root.DORotate(new Vector3(0f, 0f, mDefaultAimAngleZ), Mathf.Max(0.01f, mDeckRelayoutDuration))
                        .SetEase(mDeckRelayoutEase);
                }
            }

            ApplySortingOrder(entry.Root, mDeckSortingOrder + i);
        }
    }

    private Vector3 ResolveDeckCenter()
    {
        return (mDeckSlot != null ? mDeckSlot.position : transform.position) + mDeckWorldOffset;
    }

    private void UpdateHoveredSlot()
    {
        mHoveredSlot = null;
        if (mSlots.Count == 0)
        {
            RebuildSlots();
        }

        var camera = ResolveCamera();
        if (camera == null)
        {
            return;
        }

        var mouse = Input.mousePosition;
        var distance = -camera.transform.position.z;
        var world = camera.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, distance));
        world.z = 0f;

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
                bounds.Expand(new Vector3(0.08f, 0.08f, 1f));
                if (bounds.Contains(world))
                {
                    mHoveredSlot = slot;
                    return;
                }
            }
            else if (Vector2.Distance(world, slot.Transform.position) <= 1.1f)
            {
                mHoveredSlot = slot;
                return;
            }
        }
    }

    private Camera ResolveCamera()
    {
        if (mMainCamera == null)
        {
            mMainCamera = Camera.main;
        }

        return mMainCamera;
    }

    private void StepTopCardAim(float deltaTime)
    {
        var top = GetTopDeckCard();
        if (top == null || top.Root == null || mIsDealing)
        {
            return;
        }

        var targetAngle = mHoveredSlot != null
            ? CalculatePointAtAngle(top.Root.position, mHoveredSlot.Transform.position)
            : mDefaultAimAngleZ;

        var dt = Mathf.Max(0.0001f, deltaTime);
        var delta = Mathf.DeltaAngle(mAimAngleZ, targetAngle);
        var acceleration = delta * Mathf.Max(0f, mAimSpringStiffness) - mAimVelocityZ * Mathf.Max(0f, mAimSpringDamping);
        mAimVelocityZ = Mathf.Clamp(
            mAimVelocityZ + acceleration * dt,
            -Mathf.Max(1f, mMaxAimAngularSpeed),
            Mathf.Max(1f, mMaxAimAngularSpeed));
        mAimAngleZ += mAimVelocityZ * dt;

        if (Mathf.Abs(delta) <= mAimSettleEpsilon && Mathf.Abs(mAimVelocityZ) <= mAimSettleEpsilon)
        {
            mAimAngleZ = targetAngle;
            mAimVelocityZ = 0f;
        }

        top.Root.rotation = Quaternion.Euler(0f, 0f, mAimAngleZ);
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

    private DealerCardEntry GetTopDeckCard()
    {
        return mDeckCards.Count > 0 ? mDeckCards[mDeckCards.Count - 1] : null;
    }

    private void DealTopCardTo(SlotInfo slot)
    {
        var top = GetTopDeckCard();
        if (top == null || top.Root == null || slot == null || slot.Transform == null)
        {
            return;
        }

        mIsDealing = true;
        mDeckCards.RemoveAt(mDeckCards.Count - 1);
        UpdateDeckLeftText();
        LayoutDeckLine(false);

        top.Tween?.Kill();
        ApplySortingOrder(top.Root, mFlyingSortingOrder + mPlacedCards.Count);

        var start = top.Root.position;
        var target = slot.Transform.position + mSlotWorldOffset;
        var control = (start + target) * 0.5f + Vector3.up * Mathf.Max(0f, mDealArcHeight);

        var sequence = DOTween.Sequence();
        sequence.Join(DOTween.To(
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
            }

            top.Tween = null;
            mPlacedCards.Add(top);
            mAimVelocityZ = 0f;
            mAimAngleZ = mDefaultAimAngleZ;
            mIsDealing = false;
            Debug.Log($"[AgileCardDealerWorldDemo] Dealt '{top.Definition.DisplayName}' ({top.Definition.CardId}) -> CardSlot{slot.SlotNo}. Remaining: {mDeckCards.Count}.");
        });

        top.Tween = sequence;
        sequence.Play();
    }

    private void UpdateDeckLeftText()
    {
        if (mDeckLeftText != null)
        {
            mDeckLeftText.text = mDeckCards.Count.ToString();
        }
    }

    private static void SetDeckCardVisible(DealerCardEntry entry, bool visible)
    {
        if (entry?.Root == null || entry.Root.gameObject.activeSelf == visible)
        {
            return;
        }

        entry.Root.gameObject.SetActive(visible);
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

    private static Vector3 QuadraticBezier(Vector3 start, Vector3 control, Vector3 end, float t)
    {
        var u = 1f - t;
        return u * u * start + 2f * u * t * control + t * t * end;
    }

    private void CleanupEntries(IReadOnlyList<DealerCardEntry> entries)
    {
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            entry.Tween?.Kill();
            if (entry.Root != null)
            {
                entry.Root.DOKill();
            }

            if (mDestroySpawnedCardsOnDestroy && entry.Root != null)
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

    private sealed class DealerCardEntry
    {
        public DealerCardEntry(Transform root, CardDefinition definition, Vector3 baseScale)
        {
            Root = root;
            Definition = definition;
            BaseScale = baseScale;
            HomePosition = root != null ? root.position : Vector3.zero;
        }

        public Transform Root { get; }
        public CardDefinition Definition { get; }
        public Vector3 BaseScale { get; }
        public Vector3 HomePosition { get; set; }
        public Tween Tween { get; set; }
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
