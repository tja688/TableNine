using System.Collections.Generic;
using DG.Tweening;
using QFramework;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 临时测试：按 1 在九宫格外圈补一张正式烘焙卡，按 2 让已生成卡牌顺时针跳一格。
/// </summary>
[DefaultExecutionOrder(260)]
public sealed class NineGridCardMoveDemo : MonoBehaviour, IController
{
    private const string StandardCardPrefabPath = "Assets/Prefabs/Cards/Card.prefab";
    private const string DefaultBoardRootName = "NineGrid Main CardSlots";

    [Header("Input")]
    [SerializeField] private KeyCode mSpawnKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode mMoveKey = KeyCode.Alpha2;
    [SerializeField] private KeyCode mClearKey = KeyCode.None;

    [Header("Slots")]
    [SerializeField] private Transform mBoardRoot;
    [SerializeField] private string mBoardRootName = DefaultBoardRootName;
    [SerializeField] private Vector3 mCardWorldOffset = new Vector3(0f, 0f, -0.02f);

    [Header("Card Pipeline")]
    [SerializeField] private GameObject mCardPrefab;
    [SerializeField] private GameObject mCardFaceTemplate;
    [SerializeField] private GameObject mPlayerCardTemplate;
    [SerializeField] private float mWorldCardHeight = BakedCardRenderDataFactory.CanonicalWorldCardHeight;
    [SerializeField] private string mSortingLayerName = "Cards_Front";
    [SerializeField] private int mBaseSortingOrder = 560;

    [Header("Spawn")]
    [SerializeField] private bool mAlternateMonsterAndHelp = true;
    [SerializeField] private CardType mFirstSpawnType = CardType.Monster;
    [SerializeField] private string mMonsterCardId;
    [SerializeField] private string mHelpCardId;

    [Header("Move")]
    [SerializeField] private float mMoveDuration = 0.34f;
    [SerializeField] private Ease mMoveEase = Ease.InOutSine;
    [SerializeField] private bool mEnableHopArc = true;
    [SerializeField] private float mHopArcHeight = 0.18f;
    [SerializeField] private bool mEnableHopScale = true;
    [SerializeField] private float mHopScaleMultiplier = 1.08f;
    [SerializeField] private Ease mHopScaleEase = Ease.OutQuad;

    [Header("Cleanup")]
    [SerializeField] private bool mDestroySpawnedCardsOnDestroy = true;

    private readonly Dictionary<int, DemoCardEntry> mCardsBySlot = new Dictionary<int, DemoCardEntry>();
    private readonly List<CardDefinition> mMonsterPool = new List<CardDefinition>();
    private readonly List<CardDefinition> mHelpPool = new List<CardDefinition>();

    private BakedCardFaceComposer mComposer;
    private bool mIsMoving;
    private int mSpawnCount;
    private int mMonsterCursor;
    private int mHelpCursor;

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
        BuildPools();
    }

    private void Update()
    {
        if (mSpawnKey != KeyCode.None && Input.GetKeyDown(mSpawnKey))
        {
            SpawnNextCard();
        }

        if (mMoveKey != KeyCode.None && Input.GetKeyDown(mMoveKey))
        {
            MoveClockwiseOneStep();
        }

        if (mClearKey != KeyCode.None && Input.GetKeyDown(mClearKey))
        {
            ClearSpawnedCards();
        }
    }

    private void OnDestroy()
    {
        foreach (var pair in mCardsBySlot)
        {
            pair.Value.Tween?.Kill();
            if (mDestroySpawnedCardsOnDestroy && pair.Value.Root != null)
            {
                DestroyUnityObject(pair.Value.Root.gameObject);
            }
        }

        mCardsBySlot.Clear();
        mComposer?.Dispose();
        mComposer = null;
    }

    [ContextMenu("Spawn Next Card")]
    public void SpawnNextCard()
    {
        if (mIsMoving || !EnsureReady())
        {
            return;
        }

        if (!NineGridOuterRingUtility.TryGetFirstEmptySlot(slotNo => mCardsBySlot.ContainsKey(slotNo), out var slotNo))
        {
            Debug.Log("[NineGridCardMoveDemo] Outer ring is full. Press 2 to move, or clear demo cards before spawning more.");
            return;
        }

        var definition = ResolveNextDefinition();
        if (definition == null)
        {
            Debug.LogWarning("[NineGridCardMoveDemo] No monster/help card definition is available.");
            return;
        }

        if (!TryGetSlotTransform(slotNo, out var slotTransform))
        {
            Debug.LogWarning($"[NineGridCardMoveDemo] Missing CardSlot{slotNo} under {mBoardRootName}.");
            return;
        }

        var entry = CreateCardEntry(definition, slotNo, slotTransform.position + mCardWorldOffset);
        if (entry == null)
        {
            return;
        }

        mCardsBySlot[slotNo] = entry;
        mSpawnCount++;
    }

    [ContextMenu("Move Clockwise One Step")]
    public void MoveClockwiseOneStep()
    {
        if (mIsMoving || mCardsBySlot.Count == 0 || !EnsureReady())
        {
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

        PlayMoveTweens(movingEntries);
    }

    [ContextMenu("Clear Spawned Cards")]
    public void ClearSpawnedCards()
    {
        foreach (var pair in mCardsBySlot)
        {
            pair.Value.Tween?.Kill();
            if (pair.Value.Root != null)
            {
                DestroyUnityObject(pair.Value.Root.gameObject);
            }
        }

        mCardsBySlot.Clear();
        mIsMoving = false;
    }

    private bool EnsureReady()
    {
        if (mBoardRoot == null)
        {
            mBoardRoot = GameObject.Find(string.IsNullOrWhiteSpace(mBoardRootName)
                ? DefaultBoardRootName
                : mBoardRootName)?.transform;
        }

        if (mBoardRoot == null)
        {
            Debug.LogError($"[NineGridCardMoveDemo] Could not find board root '{mBoardRootName}'.");
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

    private DemoCardEntry CreateCardEntry(CardDefinition definition, int slotNo, Vector3 worldPosition)
    {
        var rootObject = new GameObject($"MoveDemo_{slotNo}_{definition.CardType}_{definition.CardId}");
        rootObject.transform.SetParent(mBoardRoot, true);
        rootObject.transform.position = worldPosition;
        rootObject.transform.rotation = Quaternion.identity;
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
        ApplySortingOrder(cardView, mBaseSortingOrder + mSpawnCount);

        return new DemoCardEntry(rootObject.transform, slotNo, rootObject.transform.localScale);
    }

    private void PlayMoveTweens(IReadOnlyList<DemoCardMove> moves)
    {
        mIsMoving = true;
        var remaining = moves.Count;
        if (remaining == 0)
        {
            mIsMoving = false;
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
                }

                remaining--;
                if (remaining <= 0)
                {
                    mIsMoving = false;
                }
            });

            entry.Tween = sequence;
            sequence.Play();
        }

        if (remaining <= 0)
        {
            mIsMoving = false;
        }
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

    private sealed class DemoCardEntry
    {
        public DemoCardEntry(Transform root, int slotNo, Vector3 baseScale)
        {
            Root = root;
            SlotNo = slotNo;
            BaseScale = baseScale;
        }

        public Transform Root { get; }
        public Vector3 BaseScale { get; }
        public int SlotNo { get; set; }
        public Tween Tween { get; set; }
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
}
