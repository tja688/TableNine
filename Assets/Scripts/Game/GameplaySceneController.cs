using System.Collections.Generic;
using QFramework;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameplayWorldPresenter : MonoBehaviour, IController
{
    private const string StandardCardPrefabPath = "Assets/Prefabs/Cards/Card.prefab";

    private readonly Dictionary<int, GameplayCardVisual> mBoardCardViews = new Dictionary<int, GameplayCardVisual>();
    private readonly Dictionary<int, GameplayCardVisual> mItemCardViews = new Dictionary<int, GameplayCardVisual>();
    private readonly List<IUnRegister> mEventRegisters = new List<IUnRegister>();

    [SerializeField] private bool mEnableLegacyGreyboxPresentation = true;
    [SerializeField] private Transform mBoardRoot;
    [SerializeField] private Transform mItemRoot;
    [SerializeField] private GameObject mCardTemplate;
    [SerializeField] private GameObject mCardFaceTemplate;
    [SerializeField] private GameObject mPlayerCardTemplate;
    [SerializeField] private float mFallbackWorldCardHeight = 3.64f;

    private CardViewPresenter mCardViewPresenter;
    private BakedCardFaceComposer mCardComposer;
    private GameObject mReferenceCardTemplate;
    private float mReferenceWorldCardHeight;

    public IArchitecture GetArchitecture()
    {
        return TableNine.Interface;
    }

    private void Start()
    {
        if (!TableNine.IsInitialized)
        {
            TableNine.InitArchitecture();
        }

        mCardViewPresenter = new CardViewPresenter();
        CacheSceneReferences();
        mCardComposer = new BakedCardFaceComposer(mCardFaceTemplate, mPlayerCardTemplate);
        BuildSlotInputs();
        BuildCardVisuals();
        RegisterGameplayEvents();
        RefreshAllCardViews();
    }

    private void OnDestroy()
    {
        for (var i = 0; i < mEventRegisters.Count; i++)
        {
            mEventRegisters[i].UnRegister();
        }

        mEventRegisters.Clear();
        if (mCardComposer != null)
        {
            mCardComposer.Dispose();
            mCardComposer = null;
        }
    }

    private void CacheSceneReferences()
    {
        if (mEnableLegacyGreyboxPresentation && mBoardRoot == null)
        {
            mBoardRoot = GameObject.Find("NineGrid CardSlots")?.transform;
        }

        if (mEnableLegacyGreyboxPresentation && mItemRoot == null)
        {
            mItemRoot = GameObject.Find("Item CardSlots")?.transform;
        }

        if (mEnableLegacyGreyboxPresentation && mReferenceCardTemplate == null)
        {
            mReferenceCardTemplate = GameObject.Find("NineGrid Main CardSlots/CardSlot1/CardExample");
        }

        if (mEnableLegacyGreyboxPresentation && mReferenceCardTemplate == null)
        {
            mReferenceCardTemplate = GameObject.Find("CardExample");
        }

        if (mCardTemplate == null)
        {
#if UNITY_EDITOR
            mCardTemplate = AssetDatabase.LoadAssetAtPath<GameObject>(StandardCardPrefabPath);
#endif
        }

        if (mBoardRoot == null || mItemRoot == null || mCardTemplate == null)
        {
            Debug.LogError($"GameplayWorldPresenter could not find required scene objects. BoardRoot={mBoardRoot != null} ItemRoot={mItemRoot != null} CardTemplate={mCardTemplate != null}");
            enabled = false;
            return;
        }

        mReferenceWorldCardHeight = MeasureReferenceWorldHeight();
        if (IsSceneObject(mReferenceCardTemplate))
        {
            mReferenceCardTemplate.SetActive(false);
        }
    }

    private void BuildSlotInputs()
    {
        if (!enabled)
        {
            return;
        }

        for (var slot = 1; slot <= 9; slot++)
        {
            var slotObject = FindChild(mBoardRoot, slot == 5 ? "CardSlot5ForPlayer" : $"CardSlot{slot}");
            if (slotObject == null)
            {
                continue;
            }

            if (slotObject.GetComponent<Collider2D>() == null)
            {
                slotObject.gameObject.AddComponent<BoxCollider2D>();
            }

            var slotView = slotObject.gameObject.GetComponent<BoardSlotView>();
            if (slotView == null)
            {
                slotView = slotObject.gameObject.AddComponent<BoardSlotView>();
            }

            slotView.InitializeBoardSlot(slot);

            var clickProxy = slotObject.gameObject.GetComponent<BoardSlotClickProxy>();
            if (clickProxy == null)
            {
                clickProxy = slotObject.gameObject.AddComponent<BoardSlotClickProxy>();
            }

            clickProxy.Initialize(slot);
        }

        for (var slot = 0; slot < 5; slot++)
        {
            var slotObject = FindChild(mItemRoot, $"CardSlot{slot + 1}");
            if (slotObject == null)
            {
                continue;
            }

            if (slotObject.GetComponent<Collider2D>() == null)
            {
                slotObject.gameObject.AddComponent<BoxCollider2D>();
            }

            var slotView = slotObject.gameObject.GetComponent<BoardSlotView>();
            if (slotView == null)
            {
                slotView = slotObject.gameObject.AddComponent<BoardSlotView>();
            }

            slotView.InitializeItemSlot(slot);

            var clickProxy = slotObject.gameObject.GetComponent<ItemSlotClickProxy>();
            if (clickProxy == null)
            {
                clickProxy = slotObject.gameObject.AddComponent<ItemSlotClickProxy>();
            }

            clickProxy.Initialize(slot);
        }
    }

    private void BuildCardVisuals()
    {
        if (!enabled)
        {
            return;
        }

        for (var slot = 1; slot <= 9; slot++)
        {
            var slotObject = FindChild(mBoardRoot, slot == 5 ? "CardSlot5ForPlayer" : $"CardSlot{slot}");
            if (slotObject == null)
            {
                continue;
            }

            var cardView = CreateCardVisual($"BoardCardView{slot}", slotObject.position, mBoardRoot, 1f);
            mBoardCardViews[slot] = cardView;
            slotObject.GetComponent<BoardSlotView>()?.SetCardView(cardView);
        }

        for (var slot = 0; slot < 5; slot++)
        {
            var slotObject = FindChild(mItemRoot, $"CardSlot{slot + 1}");
            if (slotObject == null)
            {
                continue;
            }

            var cardView = CreateCardVisual($"ItemCardView{slot + 1}", slotObject.position, mItemRoot, 0.85f);
            mItemCardViews[slot] = cardView;
            slotObject.GetComponent<BoardSlotView>()?.SetCardView(cardView);
        }
    }

    private void RegisterGameplayEvents()
    {
        mEventRegisters.Add(this.RegisterEvent<CardPlacedEvent>(e => RefreshBoardSlot(e.Slot)));
        mEventRegisters.Add(this.RegisterEvent<CardMovedEvent>(RefreshForCardMove));
        mEventRegisters.Add(this.RegisterEvent<CardRemovedEvent>(e => RefreshBoardSlot(e.Slot)));
        mEventRegisters.Add(this.RegisterEvent<BoardRotatedEvent>(RefreshForBoardRotation));
        mEventRegisters.Add(this.RegisterEvent<BoardSlotChangedEvent>(e => RefreshBoardSlot(e.Slot)));
        mEventRegisters.Add(this.RegisterEvent<ItemSlotChangedEvent>(e => RefreshItemSlot(e.ItemSlotIndex)));
        mEventRegisters.Add(this.RegisterEvent<DamageAppliedEvent>(e => RefreshCardByUid(e.TargetUid)));
        mEventRegisters.Add(this.RegisterEvent<ArmorChangedEvent>(e => RefreshCardByUid(e.TargetUid)));
        mEventRegisters.Add(this.RegisterEvent<HealAppliedEvent>(e => RefreshCardByUid(e.TargetUid)));
        mEventRegisters.Add(this.RegisterEvent<StatsDirtyEvent>(e => RefreshCardByUid(e.TargetUid)));
        mEventRegisters.Add(this.RegisterEvent<BattleDeckChangedEvent>(_ => RefreshBoardCardViews()));
        mEventRegisters.Add(this.RegisterEvent<GameplayMessageEvent>(_ => RefreshAllCardViews()));
        mEventRegisters.Add(this.RegisterEvent<FlowPhaseChangedEvent>(_ => RefreshAllCardViews()));
    }

    private void RefreshAllCardViews()
    {
        RefreshBoardCardViews();
        RefreshItemCardViews();
    }

    private void RefreshBoardCardViews()
    {
        if (!TableNine.IsInitialized || !this.GetModel<IRunModel>().IsRunActive.Value)
        {
            HideBoardCardViews();
            return;
        }

        var boardModel = this.GetModel<IBoardModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var deckModel = this.GetModel<IDeckModel>();

        for (var slot = 1; slot <= 9; slot++)
        {
            if (!mBoardCardViews.TryGetValue(slot, out var view))
            {
                continue;
            }

            var uid = boardModel.GetCardAt(new BoardSlotNo(slot));
            if (!uid.HasValue || !collectionModel.TryGetCard(uid.Value, out _))
            {
                view.Hide();
                continue;
            }

            var isPending = deckModel.PendingHelpCardAction.IsActive && deckModel.PendingHelpCardAction.HelpCardUid.Equals(uid.Value);
            RefreshCardView(view, uid.Value, false, isPending);
        }
    }

    private void RefreshBoardSlot(BoardSlotNo slot)
    {
        if (!mBoardCardViews.TryGetValue(slot.Value, out var view))
        {
            return;
        }

        if (!TableNine.IsInitialized || !this.GetModel<IRunModel>().IsRunActive.Value)
        {
            view.Hide();
            return;
        }

        var boardModel = this.GetModel<IBoardModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var deckModel = this.GetModel<IDeckModel>();
        var uid = boardModel.GetCardAt(slot);
        if (!uid.HasValue || !collectionModel.TryGetCard(uid.Value, out _))
        {
            view.Hide();
            return;
        }

        var isPending = deckModel.PendingHelpCardAction.IsActive && deckModel.PendingHelpCardAction.HelpCardUid.Equals(uid.Value);
        RefreshCardView(view, uid.Value, false, isPending);
    }

    private void RefreshItemCardViews()
    {
        if (!TableNine.IsInitialized || !this.GetModel<IRunModel>().IsRunActive.Value)
        {
            HideItemCardViews();
            return;
        }

        var deckModel = this.GetModel<IDeckModel>();
        var collectionModel = this.GetModel<ICollectionModel>();

        for (var slot = 0; slot < deckModel.ItemSlots.Length; slot++)
        {
            if (!mItemCardViews.TryGetValue(slot, out var view))
            {
                continue;
            }

            var uid = deckModel.ItemSlots[slot];
            if (!uid.HasValue || !collectionModel.TryGetCard(uid.Value, out _))
            {
                view.Hide();
                continue;
            }

            var isPending = deckModel.PendingHelpCardAction.IsActive && deckModel.PendingHelpCardAction.HelpCardUid.Equals(uid.Value);
            RefreshCardView(view, uid.Value, true, isPending);
        }
    }

    private void RefreshItemSlot(int itemSlotIndex)
    {
        if (!mItemCardViews.TryGetValue(itemSlotIndex, out var view))
        {
            return;
        }

        if (!TableNine.IsInitialized || !this.GetModel<IRunModel>().IsRunActive.Value)
        {
            view.Hide();
            return;
        }

        var deckModel = this.GetModel<IDeckModel>();
        var collectionModel = this.GetModel<ICollectionModel>();
        var uid = deckModel.ItemSlots[itemSlotIndex];
        if (!uid.HasValue || !collectionModel.TryGetCard(uid.Value, out _))
        {
            view.Hide();
            return;
        }

        var isPending = deckModel.PendingHelpCardAction.IsActive && deckModel.PendingHelpCardAction.HelpCardUid.Equals(uid.Value);
        RefreshCardView(view, uid.Value, true, isPending);
    }

    private void RefreshCardByUid(CardUid uid)
    {
        if (!TableNine.IsInitialized)
        {
            return;
        }

        var collectionModel = this.GetModel<ICollectionModel>();
        if (!collectionModel.TryGetCard(uid, out var runtime))
        {
            return;
        }

        if (runtime.BoardSlot.HasValue)
        {
            RefreshBoardSlot(runtime.BoardSlot.Value);
        }

        if (runtime.ItemSlotIndex.HasValue)
        {
            RefreshItemSlot(runtime.ItemSlotIndex.Value);
        }
    }

    private void RefreshForCardMove(CardMovedEvent evt)
    {
        if (evt.PreviousSlot.HasValue)
        {
            RefreshBoardSlot(evt.PreviousSlot.Value);
        }

        RefreshBoardSlot(evt.NewSlot);
    }

    private void RefreshForBoardRotation(BoardRotatedEvent evt)
    {
        for (var i = 0; i < evt.MovedCards.Count; i++)
        {
            RefreshForCardMove(evt.MovedCards[i]);
        }
    }

    private GameplayCardVisual CreateCardVisual(string objectName, Vector3 worldPosition, Transform parent, float scaleMultiplier)
    {
        var instance = Instantiate(mCardTemplate, worldPosition, Quaternion.identity, parent);
        instance.name = objectName;
        instance.SetActive(true);
        instance.transform.position = worldPosition;
        instance.transform.localScale = Vector3.one;

        var collider = instance.GetComponent<Collider2D>();
        if (collider != null)
        {
            Destroy(collider);
        }

        var view = instance.GetComponent<GameplayCardVisual>();
        if (view == null)
        {
            view = instance.AddComponent<GameplayCardVisual>();
        }

        view.Initialize();
        view.SetTargetWorldHeight(mReferenceWorldCardHeight * scaleMultiplier);
        view.Hide();
        return view;
    }

    private void RefreshCardView(GameplayCardVisual view, CardUid uid, bool itemSlot, bool pending)
    {
        var data = mCardViewPresenter.Create(uid);
        var sprites = ComposeCardSprites(data);
        view.Bind(data, itemSlot, pending, sprites);
    }

    private BakedCardSpriteSet ComposeCardSprites(CardViewData data)
    {
        if (mCardComposer == null || data == null)
        {
            return default;
        }

        var renderData = BakedCardRenderDataFactory.CreateRuntime(this, data);
        if (renderData == null)
        {
            return default;
        }

        var faceSprite = mCardComposer.Compose(renderData, BakedCardRenderDataFactory.StandardPixelsPerUnit, out var backSprite);
        return new BakedCardSpriteSet(faceSprite, backSprite);
    }

    private void HideBoardCardViews()
    {
        foreach (var pair in mBoardCardViews)
        {
            pair.Value.Hide();
        }
    }

    private void HideItemCardViews()
    {
        foreach (var pair in mItemCardViews)
        {
            pair.Value.Hide();
        }
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

    private float MeasureReferenceWorldHeight()
    {
        if (mReferenceCardTemplate == null)
        {
            return mFallbackWorldCardHeight;
        }

        var collider = mReferenceCardTemplate.GetComponent<Collider2D>();
        if (collider != null)
        {
            var size = collider.bounds.size.y;
            if (size > 0f)
            {
                return size;
            }
        }

        var renderer = mReferenceCardTemplate.GetComponentInChildren<SpriteRenderer>(true);
        if (renderer != null)
        {
            var size = renderer.bounds.size.y;
            if (size > 0f)
            {
                return size;
            }
        }

        return mFallbackWorldCardHeight;
    }

    private static bool IsSceneObject(GameObject target)
    {
        return target != null && target.scene.IsValid() && target.scene.isLoaded;
    }
}

// Compatibility shim for the existing scene component. New scenes should use GameplayWorldPresenter.
public sealed class GameplaySceneController : GameplayWorldPresenter
{
}

public sealed class GameplayCardVisual : CardView
{
}

public sealed class BoardSlotClickProxy : MonoBehaviour, IController
{
    private int mSlotNo;

    public IArchitecture GetArchitecture()
    {
        return TableNine.Interface;
    }

    public void Initialize(int slotNo)
    {
        mSlotNo = slotNo;
    }

    private void OnMouseUpAsButton()
    {
        if (TableNine.IsInitialized)
        {
            this.GetUtility<IAudioUtility>().Play(TableNineAudioIds.Click);
        }

        this.SendCommand(new ClickBoardSlotCommand(new BoardSlotNo(mSlotNo)));
    }
}

public sealed class ItemSlotClickProxy : MonoBehaviour, IController
{
    private int mItemSlotIndex;

    public IArchitecture GetArchitecture()
    {
        return TableNine.Interface;
    }

    public void Initialize(int itemSlotIndex)
    {
        mItemSlotIndex = itemSlotIndex;
    }

    private void OnMouseUpAsButton()
    {
        if (TableNine.IsInitialized)
        {
            this.GetUtility<IAudioUtility>().Play(TableNineAudioIds.Click);
        }

        this.SendCommand(new ClickItemSlotCommand(mItemSlotIndex));
    }
}
