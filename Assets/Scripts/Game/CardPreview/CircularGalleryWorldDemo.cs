using System.Collections.Generic;
using DG.Tweening;
using QFramework;
using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 临时测试：贝塞尔弧线牌列——双侧进场 → 静止展示 → 拖拽到 CardSlot2 入槽。
/// 三个控制点在 Scene 视图中可用手柄调整（配合 CircularGalleryEditor）。
/// 点击判定区外收起整个效果；未拖入槽则弹回。
/// 不需要时整对象删除。
/// </summary>
[DefaultExecutionOrder(245)]
public sealed class CircularGalleryWorldDemo : MonoBehaviour, IController
{
    private const string StandardCardPrefabPath = "Assets/Prefabs/Cards/Card.prefab";
    private const float SettlingPositionThreshold = 0.008f;
    private const float SettlingVelocityThreshold = 0.04f;
    private const float DragTiltMaxDegrees = 18f;
    private const float OverSlotTiltX = -8f;
    private const float HoverLiftHeight = 0.22f;
    private const float SlotMoveDuration = 0.45f;

    public enum GalleryPhase
    {
        Entering,
        Browsing,
        Collapsed,
        Complete
    }

    // ── Bezier Control Points (local space) ─────────────
    [Header("Bezier Arc (local space)")]
    [SerializeField] public Vector3 mControlPointStart = new Vector3(-6f, -0.5f, 0f);
    [SerializeField] public Vector3 mControlPointMid = new Vector3(0f, 1.5f, 0f);
    [SerializeField] public Vector3 mControlPointEnd = new Vector3(6f, -0.5f, 0f);

    // ── Cards ───────────────────────────────────────────
    [Header("Cards")]
    [SerializeField] public int mCardCount = 8;
    [SerializeField] public float mWorldCardHeight = BakedCardRenderDataFactory.CanonicalWorldCardHeight;
    [SerializeField] public int mBaseSortingOrder = 430;

    // ── Entry ───────────────────────────────────────────
    [Header("Entry")]
    [SerializeField] public float mEntryCardDuration = 0.6f;
    [SerializeField] public float mEntryStagger = 0.06f;
    [SerializeField] public Ease mEntryEase = Ease.OutCubic;
    [SerializeField] public float mEntrySideOffset = 5f;

    // ── Spring ──────────────────────────────────────────
    [Header("Spring")]
    [SerializeField] public float mSpringStiffness = 260f;
    [SerializeField] public float mSpringDamping = 20f;

    // ── Drag & Snap ─────────────────────────────────────
    [Header("Drag & Snap")]
    [SerializeField] public Transform mLinkedSlot;
    [SerializeField] public int mLinkedBoardSlotNo = 2;
    [SerializeField] public float mLinkedSlotSnapDistance = 1.5f;

    // ── Judgment Area (local space) ─────────────────────
    [Header("Judgment Area (local space)")]
    [SerializeField] public Vector2 mJudgmentCenter = new Vector2(0f, 0.5f);
    [SerializeField] public Vector2 mJudgmentSize = new Vector2(16f, 8f);

    // ── Collapse ────────────────────────────────────────
    [Header("Collapse")]
    [SerializeField] public float mCollapseDuration = 0.35f;
    [SerializeField] public float mCollapseStagger = 0.03f;

    // ── Assets ──────────────────────────────────────────
    [Header("Assets")]
    [SerializeField] private GameObject mCardPrefab;
    [SerializeField] private GameObject mCardFaceTemplate;
    [SerializeField] private GameObject mPlayerCardTemplate;

    // ── Description ─────────────────────────────────────
    [Header("Description")]
    [SerializeField] private TMP_Text mDescriptionText;
    [SerializeField] private Camera mCamera;
    [SerializeField] private string mDefaultHint = "拖拽卡牌到上方牌位槽中；点击区域外收起";

    // ── State ───────────────────────────────────────────
    private readonly List<GalleryCardEntry> mCards = new List<GalleryCardEntry>();

    private BakedCardFaceComposer mComposer;
    private GalleryPhase mPhase = GalleryPhase.Entering;

    private int mHoveredIndex = -1;
    private int mActiveLayoutCount;

    private GalleryCardEntry mDraggingEntry;
    private Vector3 mDragPointerOffset;
    private bool mPointerOverSlot;

    private bool mIsSettling;
    private string mDefaultDescription;
    private readonly List<Tween> mTweens = new List<Tween>();

    // 贝塞尔曲线上的卡牌 t 参数缓存
    private float[] mCardTValues;

    public IArchitecture GetArchitecture() => TableNine.Interface;

    // ════════════════════════════════════════════════════
    //  LIFECYCLE
    // ════════════════════════════════════════════════════

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
            Debug.LogError("[CircularGalleryWorldDemo] Card prefab is missing.");
            enabled = false;
            return;
        }

        if (mCamera == null) mCamera = Camera.main;
        if (mDescriptionText == null) mDescriptionText = FindDescriptionText();

        if (mDescriptionText != null)
        {
            mDescriptionText.enableWordWrapping = true;
            mDescriptionText.overflowMode = TextOverflowModes.Overflow;
            mDefaultDescription = mDescriptionText.text;
        }

        RebuildCardTValues();
    }

    private void Start()
    {
        if (!enabled) return;
        if (!TableNine.IsInitialized) TableNine.InitArchitecture();

        mComposer = new BakedCardFaceComposer(mCardFaceTemplate, mPlayerCardTemplate);
        ResolveLinkedSlot();
        BuildCards();
        PlayEntryAnimation();
    }

    private void OnDestroy()
    {
        KillAllTweens();
        mComposer?.Dispose();
        mComposer = null;
    }

    private void Update()
    {
        if (mCards.Count == 0) return;
        if (mPhase == GalleryPhase.Collapsed || mPhase == GalleryPhase.Complete) return;

        if (mPhase == GalleryPhase.Browsing)
        {
            HandleIdleInput();
            UpdateHoverIndex();
            ApplyLayout();

            if (mIsSettling) CheckSettlingComplete();

            TickReturnSpring();
            CheckAllDone();
        }
    }

    private void LateUpdate()
    {
        if (mDescriptionText == null || UIGameplayPanel.IsSidePanelHovered) return;

        if (mHoveredIndex >= 0 && mHoveredIndex < mCards.Count
            && mPhase == GalleryPhase.Browsing && mDraggingEntry == null)
        {
            var def = mCards[mHoveredIndex].Definition;
            if (def != null)
            {
                mDescriptionText.text = CardPreviewDescriptionComposer.Compose(
                    this.GetModel<IConfigModel>(), def);
                return;
            }
        }

        mDescriptionText.text = !string.IsNullOrWhiteSpace(mDefaultHint)
            ? DescriptionPanelTextRules.Clamp(mDefaultHint)
            : mDefaultDescription;
    }

    // ════════════════════════════════════════════════════
    //  BEZIER ARC — 世界空间坐标辅助
    // ════════════════════════════════════════════════════

    private Vector3 WorldP0 => transform.TransformPoint(mControlPointStart);
    private Vector3 WorldP1 => transform.TransformPoint(mControlPointMid);
    private Vector3 WorldP2 => transform.TransformPoint(mControlPointEnd);

    private void RebuildCardTValues()
    {
        var n = Mathf.Max(mCardCount, 1);
        mCardTValues = new float[n];
        for (var i = 0; i < n; i++)
        {
            mCardTValues[i] = n > 1 ? (float)i / (n - 1) : 0.5f;
        }
    }

    // ════════════════════════════════════════════════════
    //  INPUT — 拖拽 + 判定区
    // ════════════════════════════════════════════════════

    private void HandleIdleInput()
    {
        if (mIsSettling) return;

        if (Input.GetMouseButtonDown(0) && !UIGameplayPanel.IsSidePanelHovered)
        {
            if (!TryGetPointerWorld(out var pointerWorld)) return;

            // 判定区检测
            if (!IsInsideJudgmentArea(pointerWorld))
            {
                BeginCollapse();
                return;
            }

            var hovered = DetermineHoveredIndex();
            if (hovered >= 0)
            {
                BeginCardDrag(hovered);
            }
        }

        if (mDraggingEntry != null)
        {
            if (Input.GetMouseButton(0))
            {
                UpdateCardDrag();
            }
            else if (Input.GetMouseButtonUp(0))
            {
                EndCardDrag();
            }
        }
    }

    private bool IsInsideJudgmentArea(Vector3 worldPoint)
    {
        var center = transform.TransformPoint(
            new Vector3(mJudgmentCenter.x, mJudgmentCenter.y, 0f));
        var halfW = mJudgmentSize.x * 0.5f;
        var halfH = mJudgmentSize.y * 0.5f;

        // 用 transform 的右/上方向投影到本地轴
        var right = transform.right;
        var up = transform.up;
        var localX = Vector3.Dot(worldPoint - center, right);
        var localY = Vector3.Dot(worldPoint - center, up);

        return Mathf.Abs(localX) <= halfW && Mathf.Abs(localY) <= halfH;
    }

    private void BeginCardDrag(int index)
    {
        if (mPhase != GalleryPhase.Browsing) return;
        if (index < 0 || index >= mCards.Count) return;
        var entry = mCards[index];
        if (entry.Wrapper == null || !entry.Wrapper.gameObject.activeSelf) return;
        if (entry.IsPlaced || entry.IsReturning) return;

        if (!TryGetPointerWorld(out var pointerWorld)) return;

        mDraggingEntry = entry;
        entry.IsDragging = true;
        mDragPointerOffset = entry.Wrapper.position - pointerWorld;

        // 抬升 + 排序层级
        var pos = entry.Wrapper.position;
        pos.y += 0.2f;
        pos.z = 0f;
        entry.Wrapper.position = pos;
        ApplySortingOrder(entry.CardView, mBaseSortingOrder + mCards.Count + 50);
    }

    private void UpdateCardDrag()
    {
        if (mDraggingEntry == null) return;
        if (!TryGetPointerWorld(out var pointerWorld)) return;

        var target = pointerWorld + mDragPointerOffset;
        target.z = 0f;
        mDraggingEntry.Wrapper.position = target;

        mPointerOverSlot = IsPointerOverLinkedSlot(pointerWorld);
        ApplyDragTilt(mDraggingEntry, pointerWorld, mPointerOverSlot);
    }

    private void EndCardDrag()
    {
        if (mDraggingEntry == null) return;

        // 检查是否释放到判定区外
        if (TryGetPointerWorld(out var pointerWorld) && !IsInsideJudgmentArea(pointerWorld))
        {
            var entry = mDraggingEntry;
            mDraggingEntry = null;
            entry.IsDragging = false;
            SnapCardBack(entry);
            BeginCollapse();
            mPointerOverSlot = false;
            return;
        }

        var e = mDraggingEntry;
        mDraggingEntry = null;
        e.IsDragging = false;

        if (mPointerOverSlot && mLinkedSlot != null)
        {
            PlaceCardOnSlot(e);
        }
        else
        {
            SnapCardBack(e);
        }

        mPointerOverSlot = false;
    }

    private void ApplyDragTilt(GalleryCardEntry entry, Vector3 pointerWorld, bool overSlot)
    {
        if (entry == null || entry.Wrapper == null) return;

        if (overSlot)
        {
            var rot = Quaternion.Euler(OverSlotTiltX, 0f, 0f);
            entry.Wrapper.localRotation = Quaternion.Lerp(
                entry.Wrapper.localRotation, rot, Time.deltaTime * 12f);
            return;
        }

        // 计算弧线静止位置用于偏移参考
        var restPos = GetCardWorldPosition(entry.LayoutIndex);
        var offset = pointerWorld - restPos;
        var halfW = Mathf.Max(6f, 1f);

        var tiltZ = Mathf.Clamp(-offset.x / halfW, -1f, 1f) * DragTiltMaxDegrees;
        var tiltX = Mathf.Clamp(offset.y / halfW, -1f, 1f) * DragTiltMaxDegrees * 0.5f;

        entry.Wrapper.localRotation = Quaternion.Euler(tiltX, 0f, tiltZ);
    }

    // ════════════════════════════════════════════════════
    //  PLACEMENT — 入槽 + 弹簧挤占
    // ════════════════════════════════════════════════════

    private void PlaceCardOnSlot(GalleryCardEntry entry)
    {
        if (mLinkedSlot == null) return;

        entry.IsPlaced = true;
        entry.Wrapper.DOKill(false);
        mActiveLayoutCount--;

        var newIdx = 0;
        for (var i = 0; i < mCards.Count; i++)
        {
            if (mCards[i] == entry || mCards[i].IsPlaced) continue;
            mCards[i].LayoutIndex = newIdx++;
        }

        mIsSettling = true;
        RebuildActiveTValues();

        entry.Wrapper.SetParent(transform, true);
        ApplySortingOrder(entry.CardView, mBaseSortingOrder + mCards.Count + 60);
        entry.Wrapper.localScale = Vector3.one;

        entry.Wrapper
            .DOMove(mLinkedSlot.position, SlotMoveDuration)
            .SetEase(Ease.OutBack);
        entry.Wrapper
            .DOLocalRotate(Vector3.zero, SlotMoveDuration)
            .SetEase(Ease.OutCubic)
            .OnComplete(() =>
            {
                Debug.Log(
                    $"[CircularGalleryWorldDemo] Placed '{entry.Definition.DisplayName}' " +
                    $"({entry.Definition.CardId}) -> CardSlot{mLinkedBoardSlotNo}.");
            });
    }

    private void SnapCardBack(GalleryCardEntry entry)
    {
        entry.Wrapper.DOKill(false);

        // 当前在贝塞尔 t 参数空间的位置
        var targetT = mCardTValues != null && entry.LayoutIndex < mCardTValues.Length
            ? mCardTValues[entry.LayoutIndex]
            : 0.5f;
        var targetPos = BezierArc.Eval(targetT, WorldP0, WorldP1, WorldP2);
        var restPos = GetCardWorldPosition(entry.LayoutIndex);

        // 将当前位置投影到贝塞尔弧线的切线方向来得到 AnimatedX
        var tangent = BezierArc.Tangent(targetT, WorldP0, WorldP1, WorldP2).normalized;
        entry.AnimatedX = Vector3.Dot(entry.Wrapper.position - restPos, tangent);
        entry.AnimatedXVelocity = 0f;
        entry.CurrentRotZ = entry.Wrapper.localEulerAngles.z;
        if (entry.CurrentRotZ > 180f) entry.CurrentRotZ -= 360f;

        entry.IsReturning = true;
    }

    /// <summary>
    /// 入槽后重新计算活跃卡牌的 t 参数（跳过已入槽的牌）。
    /// </summary>
    private void RebuildActiveTValues()
    {
        if (mActiveLayoutCount <= 0) return;
        var tValues = new float[mActiveLayoutCount];
        for (var i = 0; i < mActiveLayoutCount; i++)
        {
            tValues[i] = mActiveLayoutCount > 1 ? (float)i / (mActiveLayoutCount - 1) : 0.5f;
        }

        var idx = 0;
        for (var i = 0; i < mCards.Count; i++)
        {
            if (mCards[i].IsPlaced) continue;
            if (idx < tValues.Length)
            {
                mCards[i].LayoutIndex = idx;
                mCards[i].ActiveT = tValues[idx];
            }
            idx++;
        }
    }

    // ════════════════════════════════════════════════════
    //  COLLAPSE — 收起
    // ════════════════════════════════════════════════════

    private void BeginCollapse()
    {
        if (mPhase == GalleryPhase.Collapsed || mPhase == GalleryPhase.Complete) return;
        mPhase = GalleryPhase.Collapsed;
        mHoveredIndex = -1;

        if (mDraggingEntry != null)
        {
            mDraggingEntry.IsDragging = false;
            mDraggingEntry = null;
        }

        KillAllTweens();
        var stagger = 0f;

        for (var i = 0; i < mCards.Count; i++)
        {
            var entry = mCards[i];
            if (entry.Wrapper == null || !entry.Wrapper.gameObject.activeSelf) continue;
            if (entry.IsPlaced) continue;

            entry.Wrapper.DOKill(false);

            var pos = entry.Wrapper.position;
            pos.y += 0.4f;
            entry.Wrapper.DOMove(pos, mCollapseDuration)
                .SetDelay(stagger).SetEase(Ease.InCubic);
            entry.Wrapper.DOScale(Vector3.zero, mCollapseDuration)
                .SetDelay(stagger).SetEase(Ease.InCubic)
                .OnComplete(() =>
                {
                    if (entry.Wrapper != null)
                        entry.Wrapper.gameObject.SetActive(false);
                });

            stagger += mCollapseStagger;
        }

        // 完成回调
        var total = stagger + mCollapseDuration + 0.1f;
        var seq = DOTween.Sequence()
            .SetDelay(total)
            .OnComplete(() =>
            {
                Debug.Log("[CircularGalleryWorldDemo] Gallery collapsed.");
            });
        mTweens.Add(seq);
    }

    // ════════════════════════════════════════════════════
    //  ENTRY — 双侧进场（沿贝塞尔弧线）
    // ════════════════════════════════════════════════════

    private void PlayEntryAnimation()
    {
        if (mCards.Count == 0)
        {
            mPhase = GalleryPhase.Browsing;
            return;
        }

        mPhase = GalleryPhase.Entering;
        KillAllTweens();

        var p0 = WorldP0;
        var p1 = WorldP1;
        var p2 = WorldP2;

        // 曲线中点用于判断左右
        var midPos = BezierArc.Eval(0.5f, p0, p1, p2);

        var maxDist = 0f;
        for (var i = 0; i < mCards.Count; i++)
        {
            var d = Mathf.Abs(mCards[i].LayoutIndex - (mCards.Count - 1) * 0.5f);
            if (d > maxDist) maxDist = d;
        }

        var totalEntryTime = 0f;

        for (var i = 0; i < mCards.Count; i++)
        {
            var entry = mCards[i];
            var t = mCardTValues != null && i < mCardTValues.Length ? mCardTValues[i] : 0.5f;
            var targetPos = BezierArc.Eval(t, p0, p1, p2);
            var tangent = BezierArc.Tangent(t, p0, p1, p2);
            var targetRotZ = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;

            // 判断从哪侧进场
            var isLeft = targetPos.x < midPos.x;
            var camHalfW = mCamera != null
                ? mCamera.orthographicSize * mCamera.aspect
                : 8f;
            var sideX = isLeft
                ? midPos.x - camHalfW - mEntrySideOffset
                : midPos.x + camHalfW + mEntrySideOffset;
            var startPos = new Vector3(sideX, targetPos.y, targetPos.z);

            // stagger：外侧先动
            var dist = Mathf.Abs(i - (mCards.Count - 1) * 0.5f);
            var stagger = (maxDist - dist) * mEntryStagger;
            var cardEndTime = stagger + mEntryCardDuration;
            if (cardEndTime > totalEntryTime) totalEntryTime = cardEndTime;

            entry.Wrapper.position = startPos;
            entry.Wrapper.localRotation = Quaternion.Euler(0f, 0f, isLeft ? 15f : -15f);
            entry.Wrapper.localScale = new Vector3(0.7f, 0.7f, 0.7f);

            var tw1 = entry.Wrapper.DOMove(targetPos, mEntryCardDuration)
                .SetDelay(stagger).SetEase(mEntryEase);
            var tw2 = entry.Wrapper.DORotate(
                new Vector3(0f, 0f, targetRotZ), mEntryCardDuration)
                .SetDelay(stagger).SetEase(mEntryEase);
            var tw3 = entry.Wrapper.DOScale(Vector3.one, mEntryCardDuration)
                .SetDelay(stagger).SetEase(mEntryEase);

            mTweens.Add(tw1);
            mTweens.Add(tw2);
            mTweens.Add(tw3);
        }

        // 进场完成后切到 Browsing
        var safetyBuffer = 0.15f;
        var completionSeq = DOTween.Sequence()
            .SetDelay(totalEntryTime + safetyBuffer)
            .OnComplete(() =>
            {
                SyncSpringState();
                mPhase = GalleryPhase.Browsing;
            });
        mTweens.Add(completionSeq);
    }

    private void SyncSpringState()
    {
        for (var i = 0; i < mCards.Count; i++)
        {
            var entry = mCards[i];
            if (entry.IsPlaced) continue;
            entry.AnimatedX = 0f;
            entry.AnimatedXVelocity = 0f;

            var t = entry.ActiveT;
            var tangent = BezierArc.Tangent(t, WorldP0, WorldP1, WorldP2);
            entry.CurrentRotZ = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
        }
    }

    // ════════════════════════════════════════════════════
    //  LAYOUT — 贝塞尔弧线 + 弹簧
    // ════════════════════════════════════════════════════

    private Vector3 GetCardWorldPosition(int layoutIndex)
    {
        var t = 0.5f;
        for (var i = 0; i < mCards.Count; i++)
        {
            if (mCards[i].LayoutIndex == layoutIndex && !mCards[i].IsPlaced)
            {
                t = mCards[i].ActiveT;
                break;
            }
        }
        return BezierArc.Eval(t, WorldP0, WorldP1, WorldP2);
    }

    private void ApplyLayout()
    {
        var p0 = WorldP0;
        var p1 = WorldP1;
        var p2 = WorldP2;

        for (var i = 0; i < mCards.Count; i++)
        {
            var entry = mCards[i];
            if (entry.Wrapper == null || !entry.Wrapper.gameObject.activeSelf) continue;
            if (entry.IsDragging || entry.IsPlaced || entry.IsReturning) continue;

            var t = entry.ActiveT;
            var targetPos = BezierArc.Eval(t, p0, p1, p2);
            var tangent = BezierArc.Tangent(t, p0, p1, p2);
            var targetRotZ = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;

            // 悬停抬升（沿法线方向）
            var hoverOffset = Vector3.zero;
            if (mHoveredIndex == i && mPhase == GalleryPhase.Browsing && mDraggingEntry == null)
            {
                var normal = BezierArc.Normal(t, p0, p1, p2);
                hoverOffset = normal * HoverLiftHeight;
            }

            if (mIsSettling)
            {
                var dt = Time.deltaTime;
                entry.AnimatedX = SpringMath.Step(
                    ref entry.AnimatedX, ref entry.AnimatedXVelocity,
                    0f, mSpringStiffness, mSpringDamping, dt);
                entry.AnimatedXVelocity = Mathf.Clamp(entry.AnimatedXVelocity, -60f, 60f);

                // 沿切线偏移
                var tangentNorm = tangent.normalized;
                var offset = tangentNorm * entry.AnimatedX;
                entry.Wrapper.position = targetPos + offset + hoverOffset;
                entry.CurrentRotZ = Mathf.LerpAngle(entry.CurrentRotZ, targetRotZ, Time.deltaTime * 16f);
                entry.Wrapper.rotation = Quaternion.Euler(0f, 0f, entry.CurrentRotZ);
            }
            else
            {
                entry.AnimatedX = 0f;
                entry.AnimatedXVelocity = 0f;
                entry.CurrentRotZ = targetRotZ;

                entry.Wrapper.position = targetPos + hoverOffset;
                entry.Wrapper.rotation = Quaternion.Euler(0f, 0f, targetRotZ);
            }

            entry.Wrapper.localScale = Vector3.one;
            ApplySortingOrder(entry.CardView, entry.SortingOrder);
        }
    }

    private void CheckSettlingComplete()
    {
        var allSettled = true;

        for (var i = 0; i < mCards.Count; i++)
        {
            var entry = mCards[i];
            if (entry.IsPlaced || entry.IsDragging || entry.IsReturning) continue;
            if (entry.Wrapper == null || !entry.Wrapper.gameObject.activeSelf) continue;

            if (Mathf.Abs(entry.AnimatedX) > SettlingPositionThreshold
                || Mathf.Abs(entry.AnimatedXVelocity) > SettlingVelocityThreshold)
            {
                allSettled = false;
                break;
            }
        }

        if (allSettled)
        {
            mIsSettling = false;
            for (var i = 0; i < mCards.Count; i++)
            {
                var e = mCards[i];
                if (e.IsPlaced || e.IsDragging || e.IsReturning) continue;
                e.AnimatedX = 0f;
                e.AnimatedXVelocity = 0f;
            }
        }
    }

    private void TickReturnSpring()
    {
        var p0 = WorldP0;
        var p1 = WorldP1;
        var p2 = WorldP2;
        var dt = Time.deltaTime;

        for (var i = 0; i < mCards.Count; i++)
        {
            var entry = mCards[i];
            if (!entry.IsReturning) continue;
            if (entry.Wrapper == null || !entry.Wrapper.gameObject.activeSelf) continue;

            entry.AnimatedX = SpringMath.Step(
                ref entry.AnimatedX, ref entry.AnimatedXVelocity,
                0f, mSpringStiffness, mSpringDamping, dt);
            entry.AnimatedXVelocity = Mathf.Clamp(entry.AnimatedXVelocity, -60f, 60f);

            var t = entry.ActiveT;
            var targetPos = BezierArc.Eval(t, p0, p1, p2);
            var tangent = BezierArc.Tangent(t, p0, p1, p2).normalized;
            var offset = tangent * entry.AnimatedX;
            var rotZ = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;

            entry.CurrentRotZ = Mathf.LerpAngle(entry.CurrentRotZ, rotZ, dt * 16f);
            entry.Wrapper.position = targetPos + offset;
            entry.Wrapper.rotation = Quaternion.Euler(0f, 0f, entry.CurrentRotZ);
            entry.Wrapper.localScale = Vector3.one;

            if (Mathf.Abs(entry.AnimatedX) < SettlingPositionThreshold
                && Mathf.Abs(entry.AnimatedXVelocity) < SettlingVelocityThreshold)
            {
                entry.IsReturning = false;
                entry.AnimatedX = 0f;
                entry.AnimatedXVelocity = 0f;
                entry.CurrentRotZ = rotZ;
                entry.Wrapper.position = targetPos;
                entry.Wrapper.rotation = Quaternion.Euler(0f, 0f, rotZ);
            }
        }
    }

    private void CheckAllDone()
    {
        var anyReturning = false;
        for (var i = 0; i < mCards.Count; i++)
        {
            if (mCards[i].IsReturning) { anyReturning = true; break; }
        }

        if (!mIsSettling && !anyReturning && mDraggingEntry == null)
        {
            var anyActive = false;
            for (var i = 0; i < mCards.Count; i++)
            {
                if (!mCards[i].IsPlaced) { anyActive = true; break; }
            }

            if (!anyActive)
            {
                mPhase = GalleryPhase.Complete;
                Debug.Log("[CircularGalleryWorldDemo] All cards placed. Demo complete.");
            }
        }
    }

    // ════════════════════════════════════════════════════
    //  HOVER
    // ════════════════════════════════════════════════════

    private void UpdateHoverIndex()
    {
        mHoveredIndex = mDraggingEntry != null ? -1 : DetermineHoveredIndex();
    }

    private int DetermineHoveredIndex()
    {
        if (mCamera == null || !TryGetPointerWorld(out var pointerWorld)) return -1;

        for (var i = mCards.Count - 1; i >= 0; i--)
        {
            var entry = mCards[i];
            if (entry.Wrapper == null || !entry.Wrapper.gameObject.activeSelf) continue;
            if (entry.IsPlaced || entry.IsDragging || entry.IsReturning) continue;
            if (entry.Collider != null && entry.Collider.OverlapPoint(pointerWorld))
                return i;
        }

        return -1;
    }

    // ════════════════════════════════════════════════════
    //  SLOT DETECTION
    // ════════════════════════════════════════════════════

    private bool IsPointerOverLinkedSlot(Vector3 worldPoint)
    {
        if (mLinkedSlot == null) return false;

        var renderer = mLinkedSlot.GetComponent<SpriteRenderer>();
        if (renderer != null && renderer.bounds.Contains(worldPoint))
            return true;

        return Vector2.Distance(mLinkedSlot.position, worldPoint) <= mLinkedSlotSnapDistance;
    }

    // ════════════════════════════════════════════════════
    //  BUILD CARDS
    // ════════════════════════════════════════════════════

    private void BuildCards()
    {
        mCards.Clear();
        var definitions = PickDemoCards(mCardCount);
        if (definitions.Count == 0)
        {
            Debug.LogWarning("[CircularGalleryWorldDemo] No cards available in config.");
            return;
        }

        RebuildCardTValues();

        for (var i = 0; i < definitions.Count; i++)
        {
            var definition = definitions[i];
            var renderData = BakedCardRenderDataFactory.CreateFromCardDefinition(
                this.GetModel<IConfigModel>(), definition);
            if (renderData == null) continue;

            var sprites = mComposer.ComposeSet(
                renderData, BakedCardRenderDataFactory.StandardPixelsPerUnit);
            if (!sprites.HasFace) continue;

            var wrapper = new GameObject($"GalleryCard_{definition.CardId}").transform;
            wrapper.SetParent(transform, false);

            var cardObject = Instantiate(mCardPrefab, wrapper, false);
            cardObject.name = "Card";

            var cardView = cardObject.GetComponent<CardView>();
            if (cardView == null) cardView = cardObject.AddComponent<CardView>();

            cardView.Initialize();
            cardView.SetTargetWorldHeight(mWorldCardHeight);
            cardView.ShowBaked(sprites);

            var collider = cardObject.GetComponent<Collider2D>();
            ApplySortingOrder(cardView, mBaseSortingOrder + i);

            var t = mCardTValues != null && i < mCardTValues.Length ? mCardTValues[i] : 0.5f;

            mCards.Add(new GalleryCardEntry
            {
                Wrapper = wrapper,
                CardView = cardView,
                Collider = collider,
                Definition = definition,
                LayoutIndex = i,
                SortingOrder = mBaseSortingOrder + i,
                ActiveT = t
            });
        }

        mActiveLayoutCount = mCards.Count;
    }

    // ════════════════════════════════════════════════════
    //  INFRASTRUCTURE
    // ════════════════════════════════════════════════════

    private void ResolveLinkedSlot()
    {
        if (mLinkedSlot != null) return;
        mLinkedSlot = GameObject.Find("NineGrid Main CardSlots/CardSlot2")?.transform;
        if (mLinkedSlot == null)
        {
            Debug.LogWarning("[CircularGalleryWorldDemo] Linked slot (CardSlot2) not found.");
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
            picked.Add(pool[i % pool.Count]);
        return picked;
    }

    private static void AddCards(List<CardDefinition> pool, IReadOnlyList<CardDefinition> cards)
    {
        if (cards == null) return;
        for (var i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            if (card == null || card.CardType is CardType.Player or CardType.Room) continue;
            pool.Add(card);
        }
    }

    private bool TryGetPointerWorld(out Vector3 worldPoint)
    {
        worldPoint = Vector3.zero;
        if (mCamera == null) return false;
        var screen = Input.mousePosition;
        worldPoint = mCamera.ScreenToWorldPoint(
            new Vector3(screen.x, screen.y, Mathf.Abs(mCamera.transform.position.z)));
        worldPoint.z = 0f;
        return true;
    }

    private static void ApplySortingOrder(CardView cardView, int order)
    {
        if (cardView?.DisplayAdapter == null) return;
        var pivot = cardView.DisplayAdapter.VisualPivot;
        if (pivot == null) return;
        var renderers = pivot.GetComponentsInChildren<SpriteRenderer>(true);
        for (var i = 0; i < renderers.Length; i++)
            renderers[i].sortingOrder = order;
    }

    private static TMP_Text FindDescriptionText()
    {
        var texts = Object.FindObjectsOfType<TMP_Text>(true);
        for (var i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].name == "DescriptionText")
                return texts[i];
        }
        return null;
    }

    private void KillAllTweens()
    {
        for (var i = 0; i < mTweens.Count; i++)
        {
            if (mTweens[i] != null && mTweens[i].IsActive())
                mTweens[i].Kill(false);
        }
        mTweens.Clear();

        for (var i = 0; i < mCards.Count; i++)
        {
            if (mCards[i].Wrapper != null)
                mCards[i].Wrapper.DOKill(false);
        }
    }

    // ════════════════════════════════════════════════════
    //  INNER TYPES
    // ════════════════════════════════════════════════════

    private sealed class GalleryCardEntry
    {
        public Transform Wrapper;
        public CardView CardView;
        public Collider2D Collider;
        public CardDefinition Definition;
        public int LayoutIndex;
        public int SortingOrder;

        /// <summary>此牌在贝塞尔曲线上当前活跃布局中的 t 参数。</summary>
        public float ActiveT = 0.5f;

        // 弹簧动画（沿切线方向的偏移量）
        public float AnimatedX;
        public float AnimatedXVelocity;
        public float CurrentRotZ;

        // 状态标记
        public bool IsDragging;
        public bool IsPlaced;
        public bool IsReturning;
    }
}

// ════════════════════════════════════════════════════════
//  贝塞尔弧线数学工具（public 以便 Editor 程序集访问）
// ════════════════════════════════════════════════════════

public static class BezierArc
{
    /// <summary>二次贝塞尔求值：B(t) = (1-t)²P0 + 2(1-t)tP1 + t²P2</summary>
    public static Vector3 Eval(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        var u = 1f - t;
        return u * u * p0 + 2f * u * t * p1 + t * t * p2;
    }

    /// <summary>切线向量（未归一化）：B'(t) = 2(1-t)(P1-P0) + 2t(P2-P1)</summary>
    public static Vector3 Tangent(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        return 2f * (1f - t) * (p1 - p0) + 2f * t * (p2 - p1);
    }

    /// <summary>法线向量（XY 平面内，指向曲线凹侧上方）。</summary>
    public static Vector3 Normal(float t, Vector3 p0, Vector3 p1, Vector3 p2)
    {
        var tan = Tangent(t, p0, p1, p2);
        // 旋转 90° 得到法线（取朝上的方向）
        var n = new Vector3(-tan.y, tan.x, 0f);
        if (n.sqrMagnitude > 0.0001f) n.Normalize();
        return n;
    }
}
