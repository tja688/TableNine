using System.Collections.Generic;
using QFramework;
using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 临时测试：复刻 React Bits Stack 卡牌堆叠交互（世界空间、无 UI）。
/// 挂载在场景任意对象上即可；不需要时整对象删除。
/// </summary>
[DefaultExecutionOrder(250)]
public sealed class CardStackWorldDemo : MonoBehaviour, IController
{
    private const string StandardCardPrefabPath = "Assets/Prefabs/Cards/Card.prefab";

    [Header("Stack (React Bits defaults)")]
    [SerializeField] private bool mRandomRotation = true;
    [SerializeField] private float mSensitivity = 180f;
    [SerializeField] private bool mSendToBackOnClick = true;
    [SerializeField] private float mSpringStiffness = 260f;
    [SerializeField] private float mSpringDamping = 20f;
    [SerializeField] private int mCardCount = 6;

    [Header("Assets")]
    [SerializeField] private GameObject mCardPrefab;
    [SerializeField] private GameObject mCardFaceTemplate;
    [SerializeField] private GameObject mPlayerCardTemplate;
    [SerializeField] private float mWorldCardHeight = BakedCardRenderDataFactory.CanonicalWorldCardHeight;
    [SerializeField] private int mBaseSortingOrder = 400;

    [Header("Placement")]
    [SerializeField] private Vector3 mStackLocalOffset = new Vector3(6.5f, 0.5f, 0f);
    [SerializeField] private float mDragElastic = 0.6f;
    [SerializeField] private Transform mLinkedSlot;
    [SerializeField] private int mLinkedBoardSlotNo = 3;
    [SerializeField] private float mLinkedSlotSnapDistance = 1.2f;
    [SerializeField] private bool mAutoStartRunForDebug = true;
    [SerializeField] private bool mTriggerBoardSlotCommandOnPlace = true;
    [SerializeField] private float mReturnToStackDuration = 0.22f;

    [Header("Description")]
    [SerializeField] private TMP_Text mDescriptionText;
    [SerializeField] private string mDefaultHint = "拖拽顶卡到关联槽位放置；离槽拖动带倾斜效果";

    private readonly List<CardStackWorldCard> mStack = new List<CardStackWorldCard>();
    private readonly List<CardStackWorldCard> mPlacedCards = new List<CardStackWorldCard>();
    private BakedCardFaceComposer mComposer;
    private Transform mStackRoot;
    private Transform mPlacedCardsRoot;
    private Camera mCamera;
    private CardStackWorldCard mDraggingCard;
    private Vector3 mDragPointerOffset;
    private bool mPointerOverLinkedSlot;
    private string mDefaultDescription;

    public float SpringStiffness => mSpringStiffness;
    public float SpringDamping => mSpringDamping;
    public float DragElastic => mDragElastic;
    public float DragSensitivity => mSensitivity;
    public bool SendToBackOnClick => mSendToBackOnClick;
    public float ReturnToStackDuration => mReturnToStackDuration;
    public Transform StackRoot => mStackRoot;
    public Transform PlacedCardsRoot => mPlacedCardsRoot;

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
            Debug.LogError("[CardStackWorldDemo] Card prefab is missing.");
            enabled = false;
            return;
        }

        mCamera = Camera.main;
        mStackRoot = new GameObject("CardStackRoot").transform;
        mStackRoot.SetParent(transform, false);
        mStackRoot.localPosition = mStackLocalOffset;

        mPlacedCardsRoot = new GameObject("PlacedCardsRoot").transform;
        mPlacedCardsRoot.SetParent(transform, false);

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
        ResolveLinkedSlot();
        BuildStack();
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
        if (mCamera == null || (mStack.Count == 0 && mPlacedCards.Count == 0))
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            TryBeginDrag();
        }

        if (mDraggingCard != null && Input.GetMouseButton(0))
        {
            UpdateDrag();
        }

        if (mDraggingCard != null && Input.GetMouseButtonUp(0))
        {
            EndDrag();
        }
    }

    private void LateUpdate()
    {
        var deltaTime = Time.deltaTime;
        TickCards(mStack, deltaTime);
        TickCards(mPlacedCards, deltaTime);

        UpdateDescriptionPanel();
    }

    private static void TickCards(List<CardStackWorldCard> cards, float deltaTime)
    {
        for (var i = 0; i < cards.Count; i++)
        {
            cards[i].Tick(deltaTime);
        }
    }

    public void SendToBack(CardStackWorldCard card)
    {
        if (card == null)
        {
            return;
        }

        var index = mStack.IndexOf(card);
        if (index < 0)
        {
            return;
        }

        mStack.RemoveAt(index);
        mStack.Insert(0, card);
        RefreshStackLayout();
    }

    private void BuildStack()
    {
        var cards = PickDemoCards(mCardCount);
        if (cards.Count == 0)
        {
            Debug.LogWarning("[CardStackWorldDemo] No cards available in config.");
            return;
        }

        for (var i = 0; i < cards.Count; i++)
        {
            var definition = cards[i];
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

            var wrapper = new GameObject($"StackCard_{definition.CardId}");
            wrapper.transform.SetParent(mStackRoot, false);
            wrapper.transform.localPosition = Vector3.zero;

            var dragLayer = new GameObject("DragLayer").transform;
            dragLayer.SetParent(wrapper.transform, false);

            var cardObject = Instantiate(mCardPrefab, dragLayer, false);
            cardObject.name = "Card";

            var cardView = cardObject.GetComponent<CardView>();
            if (cardView == null)
            {
                cardView = cardObject.AddComponent<CardView>();
            }

            cardView.Initialize();
            cardView.SetTargetWorldHeight(mWorldCardHeight);
            cardView.ShowBaked(sprites);

            var stackCard = wrapper.AddComponent<CardStackWorldCard>();
            stackCard.Initialize(this, definition, cardView, mRandomRotation ? Random.Range(-5f, 5f) : 0f, mBaseSortingOrder);
            mStack.Add(stackCard);
        }

        RefreshStackLayout();
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
            var index = i % pool.Count;
            picked.Add(pool[index]);
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

    private void RefreshStackLayout()
    {
        for (var i = 0; i < mStack.Count; i++)
        {
            mStack[i].SetStackIndex(i, mStack.Count);
        }
    }

    private CardStackWorldCard GetTopCard()
    {
        for (var i = mStack.Count - 1; i >= 0; i--)
        {
            if (!mStack[i].IsPlaced)
            {
                return mStack[i];
            }
        }

        return null;
    }

    private void UpdateDrag()
    {
        if (mDraggingCard == null || !mDraggingCard.TryGetDragPlanePoint(mCamera, out var pointerWorld))
        {
            return;
        }

        mPointerOverLinkedSlot = IsOverLinkedSlot(pointerWorld);
        mDraggingCard.SetDragTarget(pointerWorld + mDragPointerOffset, mPointerOverLinkedSlot);
    }

    private void EndDrag()
    {
        if (mDraggingCard == null)
        {
            return;
        }

        if (mDraggingCard.TryGetDragPlanePoint(mCamera, out var pointerWorld))
        {
            mPointerOverLinkedSlot = IsOverLinkedSlot(pointerWorld);
        }

        if (mPointerOverLinkedSlot && mLinkedSlot != null)
        {
            PlaceCardOnSlot(mDraggingCard);
        }
        else if (mDraggingCard.IsPlaced)
        {
            ReturnToStackTop(mDraggingCard);
        }
        else if (mDraggingCard.ShouldSendToBack() || mSendToBackOnClick)
        {
            SendToBack(mDraggingCard);
        }

        mDraggingCard.EndDrag();
        mDraggingCard = null;
        mPointerOverLinkedSlot = false;
    }

    private void PlaceCardOnSlot(CardStackWorldCard card)
    {
        if (card == null || mLinkedSlot == null)
        {
            return;
        }

        var wasInStack = mStack.Remove(card);
        if (wasInStack)
        {
            RefreshStackLayout();
        }

        if (!mPlacedCards.Contains(card))
        {
            mPlacedCards.Add(card);
        }

        card.PlaceOnSlot(mLinkedSlot, mPlacedCardsRoot);
        TriggerDebugPlacement(mLinkedBoardSlotNo, card.Definition);
    }

    private void ReturnToStackTop(CardStackWorldCard card)
    {
        if (card == null)
        {
            return;
        }

        mPlacedCards.Remove(card);
        mStack.Add(card);
        card.BeginReturnToStack(mStackRoot, mReturnToStackDuration);
        RefreshStackLayout();
    }

    private void TriggerDebugPlacement(int slotNo, CardDefinition definition)
    {
        if (definition == null)
        {
            return;
        }

        Debug.Log(
            $"[CardStackWorldDemo] Placed '{definition.DisplayName}' ({definition.CardId}) on Main CardSlot {slotNo}.");

        if (!TableNine.IsInitialized)
        {
            return;
        }

        if (mAutoStartRunForDebug && !this.GetModel<IRunModel>().IsRunActive.Value)
        {
            this.SendCommand(new StartNewRunCommand());
        }

        if (!this.GetModel<IRunModel>().IsRunActive.Value)
        {
            return;
        }

        var boardSlot = new BoardSlotNo(slotNo);
        var boardModel = this.GetModel<IBoardModel>();
        if (!boardModel.GetCardAt(boardSlot).HasValue)
        {
            var runtime = this.GetModel<ICollectionModel>().CreateCard(definition);
            this.GetSystem<IBoardSystem>().PlaceCard(runtime.Uid, boardSlot, CardPlacementSource.Refill);
        }

        if (mTriggerBoardSlotCommandOnPlace)
        {
            this.SendCommand(new ClickBoardSlotCommand(boardSlot));
        }
    }

    private void ResolveLinkedSlot()
    {
        if (mLinkedSlot != null)
        {
            return;
        }

        mLinkedSlot = GameObject.Find("NineGrid Main CardSlots/CardSlot3")?.transform;
        if (mLinkedSlot == null)
        {
            Debug.LogWarning("[CardStackWorldDemo] Linked Slot is not assigned.");
        }
    }

    private bool IsOverLinkedSlot(Vector3 worldPoint)
    {
        if (mLinkedSlot == null)
        {
            return false;
        }

        var renderer = mLinkedSlot.GetComponent<SpriteRenderer>();
        if (renderer != null && renderer.bounds.Contains(worldPoint))
        {
            return true;
        }

        return Vector2.Distance(mLinkedSlot.position, worldPoint) <= mLinkedSlotSnapDistance;
    }

    private CardStackWorldCard GetDescriptionCard()
    {
        if (mDraggingCard != null)
        {
            return mDraggingCard;
        }

        if (mCamera == null || !TryGetPointerWorld(out var pointerWorld))
        {
            return null;
        }

        for (var i = mPlacedCards.Count - 1; i >= 0; i--)
        {
            if (mPlacedCards[i].ContainsWorldPoint(pointerWorld))
            {
                return mPlacedCards[i];
            }
        }

        var topCard = GetTopCard();
        return topCard != null && topCard.ContainsWorldPoint(pointerWorld) ? topCard : null;
    }

    private void UpdateDescriptionPanel()
    {
        if (mDescriptionText == null || UIGameplayPanel.IsSidePanelHovered)
        {
            return;
        }

        var card = GetDescriptionCard();
        if (card != null && card.Definition != null)
        {
            mDescriptionText.text = CardPreviewDescriptionComposer.Compose(this.GetModel<IConfigModel>(), card.Definition);
            return;
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

    private void TryBeginDrag()
    {
        if (!TryGetPointerWorld(out var pointerWorld))
        {
            return;
        }

        var card = FindCardUnderPointer(pointerWorld);
        if (card == null)
        {
            return;
        }

        mDraggingCard = card;
        mDragPointerOffset = card.DragLayer.position - pointerWorld;
        card.BeginDrag();
    }

    private CardStackWorldCard FindCardUnderPointer(Vector3 pointerWorld)
    {
        for (var i = mPlacedCards.Count - 1; i >= 0; i--)
        {
            if (mPlacedCards[i].ContainsWorldPoint(pointerWorld))
            {
                return mPlacedCards[i];
            }
        }

        var topCard = GetTopCard();
        if (topCard != null && topCard.ContainsWorldPoint(pointerWorld))
        {
            return topCard;
        }

        return null;
    }

    private bool TryGetPointerWorld(out Vector3 pointerWorld)
    {
        pointerWorld = Vector3.zero;
        if (mCamera == null)
        {
            return false;
        }

        var screenPoint = Input.mousePosition;
        screenPoint.z = Mathf.Abs(mCamera.transform.position.z);
        pointerWorld = mCamera.ScreenToWorldPoint(screenPoint);
        pointerWorld.z = 0f;
        return true;
    }
}
