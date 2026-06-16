using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using QFramework;
using UnityEngine;

[DefaultExecutionOrder(-20)]
public sealed class GameplayWorldSequencePresenter : MonoBehaviour, IController, ISequenceUtility
{
    private const string CardsSortingLayerName = "Cards_Front";

    [Header("Deal")]
    [SerializeField] private float mDealDuration = 0.22f;
    [SerializeField] private float mDealArcHeight = 0.55f;
    [SerializeField] private float mDealScaleMultiplier = 1.11f;
    [SerializeField] private Ease mDealMoveEase = Ease.OutCubic;
    [SerializeField] private Ease mDealScaleEase = Ease.OutQuad;

    [Header("Move")]
    [SerializeField] private float mMoveDuration = 0.28f;
    [SerializeField] private float mHopArcHeight = 0.16f;
    [SerializeField] private float mHopScaleMultiplier = 1.08f;
    [SerializeField] private Ease mMoveEase = Ease.InOutSine;
    [SerializeField] private Ease mHopScaleEase = Ease.OutQuad;

    [Header("Combat")]
    [SerializeField] private float mCombatApproachDuration = 0.18f;
    [SerializeField] private float mCombatRecoverDuration = 0.2f;
    [SerializeField] private float mCombatContactRatio = 0.58f;
    [SerializeField] private float mCombatTargetBumpDistance = 0.08f;
    [SerializeField] private float mCombatPlayerScale = 1.12f;
    [SerializeField] private float mCombatTargetScale = 1.08f;
    [SerializeField] private float mCombatImpactHold = 0.045f;
    [SerializeField] private float mCombatShardDuration = 0.2f;
    [SerializeField] private int mCombatShardCount = 10;

    [Header("Assets")]
    [SerializeField] private GameObject mCardPrefab;
    [SerializeField] private GameObject mCardFaceTemplate;
    [SerializeField] private GameObject mPlayerCardTemplate;
    [SerializeField] private float mWorldCardHeight = BakedCardRenderDataFactory.CanonicalWorldCardHeight;

    private readonly List<IUnRegister> mEventRegisters = new List<IUnRegister>();
    private readonly List<CardPlacedEvent> mRecentPlacements = new List<CardPlacedEvent>();
    private readonly HashSet<int> mPendingDealSlotNos = new HashSet<int>();
    private readonly HashSet<int> mDeferredCombatRefreshUids = new HashSet<int>();
    private readonly HashSet<int> mDeferredCombatBoardSlots = new HashSet<int>();
    private readonly Dictionary<int, RemovedCardSnapshot> mRemovedMonsterSnapshots = new Dictionary<int, RemovedCardSnapshot>();
    private readonly List<Tween> mActiveTweens = new List<Tween>();
    private BoardRotatedEvent? mLastBoardRotation;
    private CombatStartedEvent? mLastCombat;
    private BoardSlotNo? mCombatMonsterSlot;
    private bool mDeferCombatCardRefresh;
    private bool mBoardRotationVisualPending;
    private GameplayWorldPresenter mPresenter;
    private BakedCardFaceComposer mComposer;
    private bool mInitialized;

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
        EnsureInitialized();
    }

    public void EnsureInitialized()
    {
        if (!TableNine.IsInitialized)
        {
            TableNine.InitArchitecture();
        }

        if (mInitialized)
        {
            return;
        }

        mPresenter = GetComponent<GameplayWorldPresenter>() ?? FindObjectOfType<GameplayWorldPresenter>();
        mComposer = new BakedCardFaceComposer(mCardFaceTemplate, mPlayerCardTemplate);
        TableNine.Interface.RegisterUtility<ISequenceUtility>(this);
        RegisterEvents();
        mInitialized = true;
    }

    public bool IsBoardRotationVisualPending => mBoardRotationVisualPending;

    public bool ShouldHoldCardAtDeck(BoardSlotNo slot)
    {
        return mPendingDealSlotNos.Contains(slot.Value);
    }

    public bool ShouldDeferOuterRingRefresh(BoardSlotNo slot)
    {
        return mBoardRotationVisualPending && NineGridOuterRingUtility.IsOuterRingSlot(slot.Value);
    }

    public void NotifyBoardRotationStarted()
    {
        mBoardRotationVisualPending = true;
    }

    public void NotifyBoardRotationEnded()
    {
        mBoardRotationVisualPending = false;
    }

    public void HoldCardAtPreviousSlot(CardView view, BoardSlotNo previousSlot)
    {
        if (view == null ||
            mPresenter == null ||
            !mPresenter.TryGetBoardSlotWorldPosition(previousSlot, out var position))
        {
            return;
        }

        view.transform.position = position;
    }

    public bool ShouldDeferCombatCardRefresh(CardUid uid)
    {
        return mDeferCombatCardRefresh && mLastCombat.HasValue &&
               (uid.Value == mLastCombat.Value.PlayerUid.Value ||
                uid.Value == mLastCombat.Value.MonsterUid.Value);
    }

    public bool ShouldDeferCombatBoardSlot(BoardSlotNo slot)
    {
        if (!mDeferCombatCardRefresh)
        {
            return false;
        }

        if (slot.Value == 5)
        {
            return true;
        }

        return mCombatMonsterSlot.HasValue && mCombatMonsterSlot.Value.Value == slot.Value;
    }

    public void EnqueueDeferredCombatCardRefresh(CardUid uid)
    {
        if (mDeferCombatCardRefresh)
        {
            mDeferredCombatRefreshUids.Add(uid.Value);
        }
    }

    public void EnqueueDeferredCombatBoardSlot(BoardSlotNo slot)
    {
        if (mDeferCombatCardRefresh)
        {
            mDeferredCombatBoardSlots.Add(slot.Value);
        }
    }

    public void FlushDeferredCombatRefreshes()
    {
        mDeferCombatCardRefresh = false;
        if (mPresenter == null)
        {
            mDeferredCombatRefreshUids.Clear();
            mDeferredCombatBoardSlots.Clear();
            return;
        }

        foreach (var uidValue in mDeferredCombatRefreshUids)
        {
            mPresenter.RefreshCardByUidForced(new CardUid(uidValue));
        }

        foreach (var slotValue in mDeferredCombatBoardSlots)
        {
            mPresenter.RefreshBoardSlotForced(new BoardSlotNo(slotValue));
        }

        mDeferredCombatRefreshUids.Clear();
        mDeferredCombatBoardSlots.Clear();
    }

    private void OnDestroy()
    {
        StopAllCoroutines();
        for (var i = 0; i < mActiveTweens.Count; i++)
        {
            mActiveTweens[i]?.Kill(false);
        }

        mActiveTweens.Clear();

        for (var i = 0; i < mEventRegisters.Count; i++)
        {
            mEventRegisters[i].UnRegister();
        }

        mEventRegisters.Clear();
        mComposer?.Dispose();
        mComposer = null;
    }

    public void Play(PresentationSequenceType sequenceType, Action onComplete)
    {
        if (!isActiveAndEnabled || mPresenter == null)
        {
            onComplete?.Invoke();
            return;
        }

        StartCoroutine(PlaySequence(sequenceType, onComplete));
    }

    private void RegisterEvents()
    {
        mEventRegisters.Add(this.RegisterEvent<CardPlacedEvent>(OnCardPlaced));
        mEventRegisters.Add(this.RegisterEvent<FlowPhaseChangedEvent>(OnFlowPhaseChanged));
        mEventRegisters.Add(this.RegisterEvent<BoardRotatedEvent>(OnBoardRotated));
        mEventRegisters.Add(this.RegisterEvent<CombatStartedEvent>(OnCombatStarted));
        mEventRegisters.Add(this.RegisterEvent<CardRemovedEvent>(OnCardRemoved));
    }

    private void OnCardPlaced(CardPlacedEvent evt)
    {
        if (!IsDealPlacementSource(evt.Source))
        {
            return;
        }

        mRecentPlacements.Add(evt);
        mPendingDealSlotNos.Add(evt.Slot.Value);
    }

    private void OnFlowPhaseChanged(FlowPhaseChangedEvent evt)
    {
        if (evt.NewPhase == FlowPhase.BoardMoving)
        {
            NotifyBoardRotationStarted();
        }
    }

    private void OnCombatStarted(CombatStartedEvent evt)
    {
        mLastCombat = evt;
        mDeferCombatCardRefresh = true;
        mDeferredCombatRefreshUids.Clear();
        mDeferredCombatBoardSlots.Clear();
        mRemovedMonsterSnapshots.Remove(evt.MonsterUid.Value);
        mCombatMonsterSlot = null;

        var collectionModel = this.GetModel<ICollectionModel>();
        if (collectionModel.TryGetCard(evt.MonsterUid, out var monsterRuntime) &&
            monsterRuntime.BoardSlot.HasValue)
        {
            mCombatMonsterSlot = monsterRuntime.BoardSlot.Value;
        }
    }

    private void OnBoardRotated(BoardRotatedEvent evt)
    {
        mLastBoardRotation = evt;
        PrePositionBoardRotationMoves(evt.MovedCards);
    }

    private void PrePositionBoardRotationMoves(IReadOnlyList<CardMovedEvent> moves)
    {
        if (mPresenter == null || moves == null)
        {
            return;
        }

        for (var i = 0; i < moves.Count; i++)
        {
            var move = moves[i];
            if (!move.PreviousSlot.HasValue ||
                !mPresenter.TryGetBoardCardView(move.NewSlot, out var view) ||
                view == null ||
                !mPresenter.TryGetBoardSlotWorldPosition(move.PreviousSlot.Value, out var from))
            {
                continue;
            }

            view.transform.position = from;
        }
    }

    private void OnCardRemoved(CardRemovedEvent evt)
    {
        if (evt.Reason != RemoveReason.Combat)
        {
            return;
        }

        TryCaptureMonsterSnapshot(evt.Uid, evt.Slot);
    }

    private void TryCaptureMonsterSnapshot(CardUid uid, BoardSlotNo slot)
    {
        var collectionModel = this.GetModel<ICollectionModel>();
        if (!collectionModel.TryGetCard(uid, out var runtime) || runtime.CardType != CardType.Monster)
        {
            return;
        }

        CardView view = null;
        if (mPresenter != null)
        {
            mPresenter.TryGetBoardCardView(slot, out view);
        }

        var cardData = CloneCardViewData(
            view?.Data != null ? view.Data : CardViewDataFactory.Create(this, uid));
        if (cardData == null)
        {
            return;
        }

        var worldPosition = view != null
            ? view.transform.position
            : Vector3.zero;
        var localScale = view != null
            ? view.transform.localScale
            : Vector3.one;
        if (view == null &&
            (mPresenter == null || !mPresenter.TryGetBoardSlotWorldPosition(slot, out worldPosition)))
        {
            return;
        }

        mRemovedMonsterSnapshots[uid.Value] = new RemovedCardSnapshot
        {
            Uid = uid,
            Slot = slot,
            WorldPosition = worldPosition,
            LocalScale = localScale,
            Data = cardData
        };
    }

    private IEnumerator PlaySequence(PresentationSequenceType sequenceType, Action onComplete)
    {
        switch (sequenceType)
        {
            case PresentationSequenceType.OpeningDeal:
                yield return PlayOpeningDealSequence();
                break;
            case PresentationSequenceType.CombatResolution:
                yield return PlayCombatSequence();
                break;
            case PresentationSequenceType.BoardRotation:
                yield return PlayBoardRotationSequence();
                break;
            case PresentationSequenceType.BoardRefill:
                yield return PlayBoardRefillSequence();
                break;
        }

        onComplete?.Invoke();
    }

    private IEnumerator PlayCombatSequence()
    {
        try
        {
            if (!mLastCombat.HasValue)
            {
                yield break;
            }

            var combat = mLastCombat.Value;
            if (!mPresenter.TryGetCardViewForUid(combat.PlayerUid, out var playerView) || playerView == null)
            {
                yield break;
            }

            var killed = mRemovedMonsterSnapshots.TryGetValue(combat.MonsterUid.Value, out var snapshot);
            CardView targetView = null;
            GameObject tempTargetObject = null;
            if (killed)
            {
                tempTargetObject = CreateTemporaryCardView(snapshot, out targetView);
            }
            else
            {
                mPresenter.TryGetCardViewForUid(combat.MonsterUid, out targetView);
            }

            if (targetView == null)
            {
                yield break;
            }

            var playerTransform = playerView.transform;
            var targetTransform = targetView.transform;
            var playerStartPosition = playerTransform.position;
            var targetStartPosition = targetTransform.position;
            var playerStartScale = playerTransform.localScale;
            var targetStartScale = targetTransform.localScale;
            var direction = targetStartPosition - playerStartPosition;
            if (direction.sqrMagnitude <= 0.0001f)
            {
                direction = Vector3.right;
            }

            direction.Normalize();
            var hitPosition = Vector3.Lerp(playerStartPosition, targetStartPosition, Mathf.Clamp01(mCombatContactRatio));
            var targetHitPosition = targetStartPosition + direction * mCombatTargetBumpDistance;

            yield return AnimateCombatApproach(
                playerTransform,
                targetTransform,
                playerStartPosition,
                hitPosition,
                targetStartPosition,
                targetHitPosition,
                playerStartScale,
                targetStartScale);

            FlushDeferredCombatRefreshes();

            if (killed && CardFakeShatterEffect.TryGetFaceRenderer(targetView, out var faceRenderer, out var backRenderer))
            {
                var shatterSettings = CardFakeShatterEffect.ResolveSettings(mCombatShardCount);
                shatterSettings.EnableFlash = false;
                shatterSettings.PostShatterBlockDuration = 0f;
                var impactPoint = CardFakeShatterEffect.ComputeImpactPoint(faceRenderer, targetStartPosition, direction);
                yield return CardFakeShatterEffect.Play(
                    faceRenderer,
                    backRenderer,
                    impactPoint,
                    direction,
                    0f,
                    mCombatShardDuration,
                    shatterSettings);

                if (tempTargetObject != null)
                {
                    tempTargetObject.SetActive(false);
                }
            }

            yield return AnimateCombatRecover(playerTransform, playerStartPosition, playerStartScale);

            playerTransform.position = playerStartPosition;
            playerTransform.localScale = playerStartScale;
            if (!killed)
            {
                targetTransform.position = targetStartPosition;
                targetTransform.localScale = targetStartScale;
            }

            if (tempTargetObject != null)
            {
                Destroy(tempTargetObject);
            }

            mRemovedMonsterSnapshots.Remove(combat.MonsterUid.Value);
        }
        finally
        {
            mCombatMonsterSlot = null;
            FlushDeferredCombatRefreshes();
        }
    }

    private IEnumerator PlayBoardRotationSequence()
    {
        try
        {
            if (!mLastBoardRotation.HasValue || mLastBoardRotation.Value.MovedCards == null)
            {
                yield break;
            }

            var moves = mLastBoardRotation.Value.MovedCards;
            var sequence = DOTween.Sequence();
            TrackTween(sequence);
            sequence.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            var finalized = new List<RotationMoveFinalize>();
            var hasMove = false;
            for (var i = 0; i < moves.Count; i++)
            {
                var move = moves[i];
                if (!move.PreviousSlot.HasValue ||
                    !mPresenter.TryGetBoardCardView(move.NewSlot, out var view) ||
                    view == null ||
                    !mPresenter.TryGetBoardSlotWorldPosition(move.PreviousSlot.Value, out var from) ||
                    !mPresenter.TryGetBoardSlotWorldPosition(move.NewSlot, out var to))
                {
                    continue;
                }

                hasMove = true;
                var transform = view.transform;
                transform.DOKill();
                HoldCardAtPreviousSlot(view, move.PreviousSlot.Value);
                var baseScale = transform.localScale;
                transform.position = from;
                var control = (from + to) * 0.5f + Vector3.up * Mathf.Max(0f, mHopArcHeight);
                var duration = Mathf.Max(0.01f, mMoveDuration);
                sequence.Join(DOTween.To(
                        () => 0f,
                        progress =>
                        {
                            if (transform != null)
                            {
                                transform.position = QuadraticBezier(from, control, to, progress);
                            }
                        },
                        1f,
                        duration)
                    .SetEase(mMoveEase));
                sequence.Join(transform.DOScale(baseScale * Mathf.Max(1f, mHopScaleMultiplier), duration * 0.5f)
                    .SetLoops(2, LoopType.Yoyo)
                    .SetEase(mHopScaleEase));
                finalized.Add(new RotationMoveFinalize(transform, to, baseScale));
            }

            mLastBoardRotation = null;
            if (hasMove)
            {
                yield return sequence.WaitForCompletion();
            }

            for (var i = 0; i < finalized.Count; i++)
            {
                var entry = finalized[i];
                if (entry.Transform == null)
                {
                    continue;
                }

                entry.Transform.position = entry.TargetPosition;
                entry.Transform.localScale = entry.BaseScale;
            }
        }
        finally
        {
            if (mLastBoardRotation.HasValue)
            {
                mLastBoardRotation = null;
            }

            NotifyBoardRotationEnded();
            mPresenter?.RefreshBoardCardViews();
        }
    }

    private IEnumerator PlayOpeningDealSequence()
    {
        var placements = CollectPlacementsInOuterRingOrder(IsOpeningPlacementSource);
        yield return PlaySequentialDealSequence(placements);
        RemoveProcessedPlacements(IsOpeningPlacementSource);
    }

    private IEnumerator PlayBoardRefillSequence()
    {
        var placements = CollectPlacementsInOuterRingOrder(source => source == CardPlacementSource.Refill);
        yield return PlaySequentialDealSequence(placements);
        RemoveProcessedPlacements(source => source == CardPlacementSource.Refill);
    }

    private IEnumerator PlaySequentialDealSequence(IReadOnlyList<CardPlacedEvent> placements)
    {
        for (var i = 0; i < placements.Count; i++)
        {
            yield return AnimateSingleDeal(placements[i]);
            mPendingDealSlotNos.Remove(placements[i].Slot.Value);
        }
    }

    private IEnumerator AnimateSingleDeal(CardPlacedEvent placed)
    {
        if (!mPresenter.TryGetBoardCardView(placed.Slot, out var view) ||
            view == null ||
            !mPresenter.TryGetBoardSlotWorldPosition(placed.Slot, out var target))
        {
            yield break;
        }

        var transform = view.transform;
        var baseScale = transform.localScale;
        var deckPosition = mPresenter.ResolveDeckWorldPosition();
        var start = new Vector3(deckPosition.x, deckPosition.y, target.z);
        transform.position = start;
        transform.rotation = Quaternion.identity;
        transform.localScale = baseScale * Mathf.Max(1f, mDealScaleMultiplier);

        var control = (start + target) * 0.5f + Vector3.up * Mathf.Max(0f, mDealArcHeight);
        var duration = Mathf.Max(0.01f, mDealDuration);
        var sequence = DOTween.Sequence();
        TrackTween(sequence);
        sequence.SetLink(view.gameObject, LinkBehaviour.KillOnDestroy);
        sequence.Append(DOTween.To(
                () => 0f,
                progress =>
                {
                    if (transform != null)
                    {
                        transform.position = QuadraticBezier(start, control, target, progress);
                    }
                },
                1f,
                duration)
            .SetEase(mDealMoveEase));
        sequence.Join(transform.DOScale(baseScale, duration)
            .SetEase(mDealScaleEase));

        yield return sequence.WaitForCompletion();
        UntrackTween(sequence);
        if (transform == null)
        {
            yield break;
        }

        transform.position = target;
        transform.rotation = Quaternion.identity;
        transform.localScale = baseScale;
    }

    private List<CardPlacedEvent> CollectPlacementsInOuterRingOrder(System.Func<CardPlacementSource, bool> sourceFilter)
    {
        var slotToPlacement = new Dictionary<int, CardPlacedEvent>();
        for (var i = 0; i < mRecentPlacements.Count; i++)
        {
            var placed = mRecentPlacements[i];
            if (sourceFilter(placed.Source))
            {
                slotToPlacement[placed.Slot.Value] = placed;
            }
        }

        var ordered = new List<CardPlacedEvent>();
        for (var i = 0; i < NineGridOuterRingUtility.Count; i++)
        {
            var slotNo = NineGridOuterRingUtility.GetSlotAt(i);
            if (slotToPlacement.TryGetValue(slotNo, out var placed))
            {
                ordered.Add(placed);
            }
        }

        return ordered;
    }

    private void RemoveProcessedPlacements(System.Func<CardPlacementSource, bool> sourceFilter)
    {
        for (var i = mRecentPlacements.Count - 1; i >= 0; i--)
        {
            if (sourceFilter(mRecentPlacements[i].Source))
            {
                mRecentPlacements.RemoveAt(i);
            }
        }
    }

    private static bool IsDealPlacementSource(CardPlacementSource source)
    {
        return source == CardPlacementSource.Refill ||
               source == CardPlacementSource.OpeningBattle ||
               source == CardPlacementSource.OpeningDemon ||
               source == CardPlacementSource.OpeningHelp;
    }

    private static bool IsOpeningPlacementSource(CardPlacementSource source)
    {
        return source == CardPlacementSource.OpeningBattle ||
               source == CardPlacementSource.OpeningDemon ||
               source == CardPlacementSource.OpeningHelp;
    }

    private IEnumerator AnimateCombatApproach(
        Transform playerTransform,
        Transform targetTransform,
        Vector3 playerStartPosition,
        Vector3 playerHitPosition,
        Vector3 targetStartPosition,
        Vector3 targetHitPosition,
        Vector3 playerStartScale,
        Vector3 targetStartScale)
    {
        var elapsed = 0f;
        var duration = Mathf.Max(0.01f, mCombatApproachDuration);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            var eased = EaseOutBack(t);
            playerTransform.position = Vector3.LerpUnclamped(playerStartPosition, playerHitPosition, eased);
            targetTransform.position = Vector3.Lerp(targetStartPosition, targetHitPosition, EaseOutQuad(t));
            playerTransform.localScale = playerStartScale * Mathf.Lerp(1f, Mathf.Max(1f, mCombatPlayerScale), EaseOutQuad(t));
            targetTransform.localScale = targetStartScale * Mathf.Lerp(1f, Mathf.Max(1f, mCombatTargetScale), EaseOutQuad(t));
            yield return null;
        }
    }

    private IEnumerator AnimateCombatRecover(Transform playerTransform, Vector3 startPosition, Vector3 startScale)
    {
        var fromPosition = playerTransform.position;
        var fromScale = playerTransform.localScale;
        var elapsed = 0f;
        var duration = Mathf.Max(0.01f, mCombatRecoverDuration);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            var t = Mathf.Clamp01(elapsed / duration);
            var eased = EaseOutQuad(t);
            playerTransform.position = Vector3.Lerp(fromPosition, startPosition, eased);
            playerTransform.localScale = Vector3.Lerp(fromScale, startScale, eased);
            yield return null;
        }
    }

    private GameObject CreateTemporaryCardView(RemovedCardSnapshot snapshot, out CardView cardView)
    {
        cardView = null;
        if (mCardPrefab == null || snapshot.Data == null || mComposer == null)
        {
            return null;
        }

        var renderData = BakedCardRenderDataFactory.CreateRuntime(this, snapshot.Data);
        if (renderData == null)
        {
            return null;
        }

        var sprites = mComposer.ComposeSet(renderData, BakedCardRenderDataFactory.StandardPixelsPerUnit);
        if (!sprites.HasFace)
        {
            return null;
        }

        var instance = Instantiate(mCardPrefab, snapshot.WorldPosition, Quaternion.identity);
        instance.name = $"CombatShatterSnapshot_{snapshot.Uid.Value}";
        cardView = instance.GetComponent<CardView>() ?? instance.AddComponent<CardView>();
        cardView.Initialize();
        cardView.SetTargetWorldHeight(mWorldCardHeight);
        cardView.ShowBaked(sprites);
        instance.transform.localScale = snapshot.LocalScale;
        ApplySortingOrder(cardView, 900);
        return instance;
    }

    private static CardViewData CloneCardViewData(CardViewData source)
    {
        if (source == null)
        {
            return null;
        }

        return new CardViewData
        {
            Uid = source.Uid,
            DefinitionId = source.DefinitionId,
            DisplayName = source.DisplayName,
            Type = source.Type,
            Quality = source.Quality,
            CurrentHp = source.CurrentHp,
            MaxHp = source.MaxHp,
            CurrentArmor = source.CurrentArmor,
            Attack = source.Attack,
            DamageReduction = source.DamageReduction,
            HasFirstStrike = source.HasFirstStrike,
            Description = source.Description,
            SystemTagText = source.SystemTagText,
            SpriteId = source.SpriteId,
            StatusIconIds = source.StatusIconIds != null ? new List<string>(source.StatusIconIds) : new List<string>(),
            Tint = source.Tint
        };
    }

    private static void ApplySortingOrder(CardView cardView, int order)
    {
        var pivot = cardView?.DisplayAdapter?.VisualPivot;
        if (pivot == null)
        {
            return;
        }

        var renderers = pivot.GetComponentsInChildren<SpriteRenderer>(true);
        for (var i = 0; i < renderers.Length; i++)
        {
            renderers[i].sortingLayerName = CardsSortingLayerName;
            renderers[i].sortingOrder = order;
        }
    }

    private static Vector3 QuadraticBezier(Vector3 start, Vector3 control, Vector3 end, float t)
    {
        var oneMinusT = 1f - t;
        return oneMinusT * oneMinusT * start + 2f * oneMinusT * t * control + t * t * end;
    }

    private static float EaseOutQuad(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }

    private static float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        var inv = t - 1f;
        return 1f + c3 * inv * inv * inv + c1 * inv * inv;
    }

    private void TrackTween(Tween tween)
    {
        if (tween != null && !mActiveTweens.Contains(tween))
        {
            mActiveTweens.Add(tween);
        }
    }

    private void UntrackTween(Tween tween)
    {
        if (tween != null)
        {
            mActiveTweens.Remove(tween);
        }
    }

    private sealed class RemovedCardSnapshot
    {
        public CardUid Uid;
        public BoardSlotNo Slot;
        public Vector3 WorldPosition;
        public Vector3 LocalScale;
        public CardViewData Data;
    }

    private readonly struct RotationMoveFinalize
    {
        public RotationMoveFinalize(Transform transform, Vector3 targetPosition, Vector3 baseScale)
        {
            Transform = transform;
            TargetPosition = targetPosition;
            BaseScale = baseScale;
        }

        public Transform Transform { get; }
        public Vector3 TargetPosition { get; }
        public Vector3 BaseScale { get; }
    }
}
