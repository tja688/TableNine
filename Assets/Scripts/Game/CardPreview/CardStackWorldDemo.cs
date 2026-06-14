using System.Collections.Generic;
using QFramework;
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

    private readonly List<CardStackWorldCard> mStack = new List<CardStackWorldCard>();
    private BakedCardFaceComposer mComposer;
    private Transform mStackRoot;
    private Camera mCamera;
    private CardStackWorldCard mDraggingCard;
    private Vector3 mDragPointerOffset;

    public float SpringStiffness => mSpringStiffness;
    public float SpringDamping => mSpringDamping;
    public float DragElastic => mDragElastic;
    public float DragSensitivity => mSensitivity;
    public bool SendToBackOnClick => mSendToBackOnClick;

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
        if (mStack.Count == 0 || mCamera == null)
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
        for (var i = 0; i < mStack.Count; i++)
        {
            mStack[i].Tick(deltaTime);
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
            stackCard.Initialize(this, cardView, mRandomRotation ? Random.Range(-5f, 5f) : 0f, mBaseSortingOrder);
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
        return mStack.Count > 0 ? mStack[mStack.Count - 1] : null;
    }

    private void TryBeginDrag()
    {
        var topCard = GetTopCard();
        if (topCard == null || !topCard.TryGetDragPlanePoint(mCamera, out var pointerWorld))
        {
            return;
        }

        if (!topCard.ContainsWorldPoint(pointerWorld))
        {
            return;
        }

        mDraggingCard = topCard;
        mDragPointerOffset = topCard.DragLayer.position - pointerWorld;
        topCard.BeginDrag();
    }

    private void UpdateDrag()
    {
        if (mDraggingCard == null || !mDraggingCard.TryGetDragPlanePoint(mCamera, out var pointerWorld))
        {
            return;
        }

        mDraggingCard.SetDragTarget(pointerWorld + mDragPointerOffset);
    }

    private void EndDrag()
    {
        if (mDraggingCard == null)
        {
            return;
        }

        if (mDraggingCard.ShouldSendToBack() || mSendToBackOnClick)
        {
            SendToBack(mDraggingCard);
        }

        mDraggingCard.EndDrag();
        mDraggingCard = null;
    }
}
