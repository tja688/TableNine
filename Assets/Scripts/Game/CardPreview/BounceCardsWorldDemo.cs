using System.Collections.Generic;
using DG.Tweening;
using QFramework;
using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 临时测试：世界空间复刻 BounceCards_V3（悬停推挤 + 入场弹性 + 选卡落屏）。
/// 使用标准 Card.prefab 与真实卡牌 SO 烘焙链路；不需要时整对象删除。
/// </summary>
[DefaultExecutionOrder(240)]
public sealed class BounceCardsWorldDemo : MonoBehaviour, IController
{
    private const string StandardCardPrefabPath = "Assets/Prefabs/Cards/Card.prefab";

    [Header("Layout")]
    [SerializeField] private Vector3 mContainerLocalOffset = new Vector3(6.5f, 0.5f, 0f);
    [SerializeField] private int mCardCount = 5;
    [SerializeField] private float mWorldCardHeight = BakedCardRenderDataFactory.CanonicalWorldCardHeight;
    [SerializeField] private int mBaseSortingOrder = 420;

    [Header("Fan Layout (BounceCards defaults)")]
    [SerializeField] private float[] mBaseRotations = { 10f, 5f, -3f, -10f, 2f };
    [SerializeField] private float[] mBaseOffsetsX = { -2.2f, -1.1f, 0f, 1.1f, 2.2f };

    [Header("Hover")]
    [SerializeField] private float mHoverPushOffset = 1.6f;
    [SerializeField] private float mHoverLiftY = 0.28f;
    [SerializeField] private float mHoverDuration = 0.4f;
    [SerializeField] private float mHoverSiblingDelayStep = 0.03f;
    [SerializeField] private float mHoverOvershoot = 1.4f;

    [Header("Entry")]
    [SerializeField] private float mEntryDelay = 0.42f;
    [SerializeField] private float mEntryStagger = 0.08f;
    [SerializeField] private float mEntryDuration = 0.7f;

    [Header("Selection")]
    [SerializeField] private Transform mTargetSlot;
    [SerializeField] private float mFallLaunchUpward = 3.4f;
    [SerializeField] private float mFallGravity = 24f;
    [SerializeField] private float mFallHorizontalReach = 4.2f;
    [SerializeField] private float mFallHorizontalReachStep = 0.9f;
    [SerializeField] private float mFallSpinMin = 55f;
    [SerializeField] private float mFallSpinMax = 145f;
    [SerializeField] private float mFallStaggerStep = 0.035f;
    [SerializeField] private float mFallBelowScreenPadding = 1.6f;
    [SerializeField] private float mMoveToSlotDuration = 0.5f;
    [SerializeField] private float mMoveToSlotDelay = 0.12f;

    [Header("Assets")]
    [SerializeField] private GameObject mCardPrefab;
    [SerializeField] private GameObject mCardFaceTemplate;
    [SerializeField] private GameObject mPlayerCardTemplate;

    [Header("Description")]
    [SerializeField] private TMP_Text mDescriptionText;
    [SerializeField] private Camera mCamera;
    [SerializeField] private string mDefaultHint = "悬停卡牌查看描述；点击选择后其余卡牌掉落，选中卡移至 CardSlot1";

    private readonly List<BounceCardEntry> mCards = new List<BounceCardEntry>();
    private readonly List<Tween> mHoverTweens = new List<Tween>();

    private Transform mCardsContainer;
    private BakedCardFaceComposer mComposer;
    private int mHoveredIndex = -1;
    private float mEntryBlockRemaining;
    private bool mSelectionLocked;
    private string mDefaultDescription;

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

        if (mCardPrefab == null)
        {
            Debug.LogError("[BounceCardsWorldDemo] Card prefab is missing.");
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

        mCardsContainer = new GameObject("BounceCardsContainer").transform;
        mCardsContainer.SetParent(transform, false);
        mCardsContainer.localPosition = mContainerLocalOffset;
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

        mComposer = new BakedCardFaceComposer(mCardFaceTemplate, mPlayerCardTemplate);
        ResolveTargetSlot();
        BuildCards();
        PlayEntryAnimation();
    }

    private void OnDestroy()
    {
        KillHoverTweens();
        if (mComposer != null)
        {
            mComposer.Dispose();
            mComposer = null;
        }
    }

    private void Update()
    {
        if (mSelectionLocked || mCards.Count == 0)
        {
            return;
        }

        if (mEntryBlockRemaining > 0f)
        {
            mEntryBlockRemaining = Mathf.Max(0f, mEntryBlockRemaining - Time.unscaledDeltaTime);
            return;
        }

        var hovered = DetermineHoveredIndex();
        if (hovered == mHoveredIndex)
        {
            if (hovered >= 0 && Input.GetMouseButtonDown(0))
            {
                BeginSelection(hovered);
            }

            return;
        }

        mHoveredIndex = hovered;
        if (hovered < 0)
        {
            AnimateReset();
        }
        else
        {
            AnimateHover(hovered);
        }
    }

    private void LateUpdate()
    {
        if (mDescriptionText == null || UIGameplayPanel.IsSidePanelHovered)
        {
            return;
        }

        if (mHoveredIndex >= 0 && mHoveredIndex < mCards.Count)
        {
            var definition = mCards[mHoveredIndex].Definition;
            if (definition != null)
            {
                mDescriptionText.text = CardPreviewDescriptionComposer.Compose(this.GetModel<IConfigModel>(), definition);
                return;
            }
        }

        if (!string.IsNullOrWhiteSpace(mDefaultHint))
        {
            mDescriptionText.text = DescriptionPanelTextRules.Clamp(mDefaultHint);
        }
        else
        {
            mDescriptionText.text = mDefaultDescription;
        }
    }

    private void BuildCards()
    {
        mCards.Clear();
        var definitions = PickDemoCards(mCardCount);
        if (definitions.Count == 0)
        {
            Debug.LogWarning("[BounceCardsWorldDemo] No cards available in config.");
            return;
        }

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

            var wrapper = new GameObject($"BounceCard_{definition.CardId}");
            wrapper.transform.SetParent(mCardsContainer, false);
            wrapper.transform.localPosition = new Vector3(GetBaseOffsetX(i), 0f, 0f);
            wrapper.transform.localRotation = Quaternion.Euler(0f, 0f, GetBaseRotation(i));
            wrapper.transform.localScale = Vector3.zero;

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

            var collider = cardObject.GetComponent<Collider2D>();
            ApplySortingOrder(cardView, mBaseSortingOrder + i);

            mCards.Add(new BounceCardEntry
            {
                Wrapper = wrapper.transform,
                CardView = cardView,
                Collider = collider,
                Definition = definition,
                BaseLocalPosition = wrapper.transform.localPosition,
                BaseLocalRotationZ = GetBaseRotation(i),
                SortingOrder = mBaseSortingOrder + i
            });
        }
    }

    private void PlayEntryAnimation()
    {
        var count = mCards.Count;
        var safeEntryDelay = Mathf.Max(0f, mEntryDelay);
        var safeEntryStagger = Mathf.Max(0f, mEntryStagger);
        var safeEntryDuration = Mathf.Max(0.1f, mEntryDuration);
        mEntryBlockRemaining = count > 0
            ? safeEntryDelay + ((count - 1) * safeEntryStagger) + safeEntryDuration
            : 0f;

        for (var i = 0; i < count; i++)
        {
            var entry = mCards[i];
            if (entry.Wrapper == null)
            {
                continue;
            }

            entry.Wrapper.localScale = Vector3.zero;
            entry.Wrapper
                .DOScale(Vector3.one, safeEntryDuration)
                .SetDelay(safeEntryDelay + (i * safeEntryStagger))
                .SetEase(Ease.OutElastic);
        }
    }

    private int DetermineHoveredIndex()
    {
        if (mCamera == null || !TryGetPointerWorld(out var pointerWorld))
        {
            return -1;
        }

        for (var i = mCards.Count - 1; i >= 0; i--)
        {
            var entry = mCards[i];
            if (entry.Collider != null && entry.Collider.OverlapPoint(pointerWorld))
            {
                return i;
            }
        }

        return -1;
    }

    private void AnimateHover(int hoveredIndex)
    {
        KillHoverTweens();

        var safeDuration = Mathf.Max(0.05f, mHoverDuration);
        for (var i = 0; i < mCards.Count; i++)
        {
            var entry = mCards[i];
            if (entry.Wrapper == null)
            {
                continue;
            }

            if (i == hoveredIndex)
            {
                ApplySortingOrder(entry.CardView, mBaseSortingOrder + mCards.Count + 20);
                TrackHoverTween(entry.Wrapper
                    .DOLocalMove(entry.BaseLocalPosition + new Vector3(0f, mHoverLiftY, 0f), safeDuration)
                    .SetEase(Ease.OutBack, Mathf.Max(0f, mHoverOvershoot)));
                TrackHoverTween(entry.Wrapper
                    .DOLocalRotate(Vector3.zero, safeDuration)
                    .SetEase(Ease.OutBack, Mathf.Max(0f, mHoverOvershoot)));
                continue;
            }

            ApplySortingOrder(entry.CardView, entry.SortingOrder);
            var direction = i < hoveredIndex ? -1f : 1f;
            var distance = Mathf.Abs(hoveredIndex - i);
            var targetPos = entry.BaseLocalPosition + new Vector3(direction * mHoverPushOffset, 0f, 0f);
            var delay = Mathf.Max(0f, mHoverSiblingDelayStep) * distance;

            TrackHoverTween(entry.Wrapper
                .DOLocalMove(targetPos, safeDuration)
                .SetDelay(delay)
                .SetEase(Ease.OutBack, Mathf.Max(0f, mHoverOvershoot)));
            TrackHoverTween(entry.Wrapper
                .DOLocalRotate(new Vector3(0f, 0f, entry.BaseLocalRotationZ), safeDuration)
                .SetDelay(delay)
                .SetEase(Ease.OutBack, Mathf.Max(0f, mHoverOvershoot)));
        }
    }

    private void AnimateReset()
    {
        KillHoverTweens();

        var safeDuration = Mathf.Max(0.05f, mHoverDuration);
        for (var i = 0; i < mCards.Count; i++)
        {
            var entry = mCards[i];
            if (entry.Wrapper == null)
            {
                continue;
            }

            ApplySortingOrder(entry.CardView, entry.SortingOrder);
            TrackHoverTween(entry.Wrapper
                .DOLocalMove(entry.BaseLocalPosition, safeDuration)
                .SetEase(Ease.OutBack, Mathf.Max(0f, mHoverOvershoot)));
            TrackHoverTween(entry.Wrapper
                .DOLocalRotate(new Vector3(0f, 0f, entry.BaseLocalRotationZ), safeDuration)
                .SetEase(Ease.OutBack, Mathf.Max(0f, mHoverOvershoot)));
        }
    }

    private void BeginSelection(int selectedIndex)
    {
        if (selectedIndex < 0 || selectedIndex >= mCards.Count)
        {
            return;
        }

        mSelectionLocked = true;
        mHoveredIndex = -1;
        KillHoverTweens();

        var selected = mCards[selectedIndex];
        var fallCount = 0;

        for (var i = 0; i < mCards.Count; i++)
        {
            if (i == selectedIndex)
            {
                continue;
            }

            fallCount++;
            AnimateFallOff(i, selectedIndex, () =>
            {
                fallCount--;
            });
        }

        if (mTargetSlot == null)
        {
            Debug.LogWarning("[BounceCardsWorldDemo] Target slot missing; selected card stays in place.");
            return;
        }

        var targetWorld = mTargetSlot.position;
        var selectedWrapper = selected.Wrapper;
        if (selectedWrapper == null)
        {
            return;
        }

        ApplySortingOrder(selected.CardView, mBaseSortingOrder + mCards.Count + 40);
        selectedWrapper
            .DOLocalRotate(Vector3.zero, mMoveToSlotDuration)
            .SetDelay(mMoveToSlotDelay)
            .SetEase(Ease.OutBack, mHoverOvershoot);
        selectedWrapper
            .DOMove(targetWorld, mMoveToSlotDuration)
            .SetDelay(mMoveToSlotDelay)
            .SetEase(Ease.OutBack, mHoverOvershoot)
            .OnComplete(() =>
            {
                Debug.Log(
                    $"[BounceCardsWorldDemo] Selected '{selected.Definition.DisplayName}' ({selected.Definition.CardId}) -> CardSlot1.");
            });
    }

    private void AnimateFallOff(int index, int selectedIndex, System.Action onComplete)
    {
        var entry = mCards[index];
        if (entry.Wrapper == null)
        {
            onComplete?.Invoke();
            return;
        }

        var wrapper = entry.Wrapper;
        var start = wrapper.position;
        var awayDirection = index < selectedIndex ? -1f : 1f;
        var distanceFromSelected = Mathf.Abs(index - selectedIndex);
        var horizontalReach = awayDirection
            * (mFallHorizontalReach + distanceFromSelected * mFallHorizontalReachStep);
        var end = GetFallOffScreenTarget(start);
        end.x = start.x + horizontalReach;

        var drop = Mathf.Max(0.1f, start.y - end.y);
        var gravity = Mathf.Max(0.1f, mFallGravity);
        var launchUp = mFallLaunchUpward;
        var duration = (launchUp + Mathf.Sqrt(launchUp * launchUp + 2f * gravity * drop)) / gravity;
        var velocityX = horizontalReach / duration;
        var startRotZ = wrapper.localEulerAngles.z;
        var spinZ = entry.BaseLocalRotationZ + awayDirection * Random.Range(mFallSpinMin, mFallSpinMax);
        var stagger = Mathf.Max(0f, mFallStaggerStep) * distanceFromSelected;

        DOTween.To(() => 0f, progress =>
            {
                if (wrapper == null)
                {
                    return;
                }

                var elapsed = progress * duration;
                wrapper.position = new Vector3(
                    start.x + velocityX * elapsed,
                    start.y + launchUp * elapsed - 0.5f * gravity * elapsed * elapsed,
                    start.z);
                wrapper.localRotation = Quaternion.Euler(
                    0f,
                    0f,
                    Mathf.LerpAngle(startRotZ, spinZ, progress));
            }, 1f, duration)
            .SetDelay(stagger)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                if (wrapper != null)
                {
                    wrapper.gameObject.SetActive(false);
                }

                onComplete?.Invoke();
            });
    }

    private Vector3 GetFallOffScreenTarget(Vector3 start)
    {
        if (mCamera == null)
        {
            return start + new Vector3(0f, -12f, 0f);
        }

        var depth = Mathf.Abs(start.z - mCamera.transform.position.z);
        var belowScreen = mCamera.ViewportToWorldPoint(new Vector3(0.5f, -0.2f, depth));
        var targetY = belowScreen.y - mFallBelowScreenPadding - mWorldCardHeight * 0.5f;
        return new Vector3(start.x, targetY, start.z);
    }

    private void ResolveTargetSlot()
    {
        if (mTargetSlot != null)
        {
            return;
        }

        mTargetSlot = GameObject.Find("NineGrid Main CardSlots/CardSlot1")?.transform;
        if (mTargetSlot == null)
        {
            Debug.LogWarning("[BounceCardsWorldDemo] Target slot is not assigned.");
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

    private float GetBaseOffsetX(int index)
    {
        return index >= 0 && index < mBaseOffsetsX.Length
            ? mBaseOffsetsX[index]
            : (index - (mCardCount - 1) * 0.5f) * 1.1f;
    }

    private float GetBaseRotation(int index)
    {
        return index >= 0 && index < mBaseRotations.Length
            ? mBaseRotations[index]
            : 0f;
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

    private static void ApplySortingOrder(CardView cardView, int order)
    {
        if (cardView == null || cardView.DisplayAdapter == null)
        {
            return;
        }

        var pivot = cardView.DisplayAdapter.VisualPivot;
        if (pivot == null)
        {
            return;
        }

        var renderers = pivot.GetComponentsInChildren<SpriteRenderer>(true);
        for (var i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingOrder = order;
        }
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

    private void TrackHoverTween(Tween tween)
    {
        if (tween == null)
        {
            return;
        }

        mHoverTweens.Add(tween);
    }

    private void KillHoverTweens()
    {
        for (var i = mHoverTweens.Count - 1; i >= 0; i--)
        {
            var tween = mHoverTweens[i];
            if (tween != null && tween.IsActive())
            {
                tween.Kill(false);
            }
        }

        mHoverTweens.Clear();
    }

    private sealed class BounceCardEntry
    {
        public Transform Wrapper;
        public CardView CardView;
        public Collider2D Collider;
        public CardDefinition Definition;
        public Vector3 BaseLocalPosition;
        public float BaseLocalRotationZ;
        public int SortingOrder;
    }
}
