using System.Collections.Generic;
using QFramework;
using UnityEngine;

public class GameplayWorldPresenter : MonoBehaviour, IController
{
    private const string DefaultScenePlayerCardName = "PlayerCard";
    private const string DefaultBoardRootName = "NineGrid Main CardSlots";
    private const string DefaultItemRootName = "Item CardSlots";

    private readonly Dictionary<int, CardView> mBoardCardViews = new Dictionary<int, CardView>();
    private readonly Dictionary<int, CardView> mItemCardViews = new Dictionary<int, CardView>();
    private readonly List<IUnRegister> mEventRegisters = new List<IUnRegister>();

    [SerializeField] private bool mEnableLegacyGreyboxPresentation = true;
    [SerializeField] private bool mBootstrapPlayerCardOnly = true;
    [SerializeField] private Transform mBoardRoot;
    [SerializeField] private string mBoardRootName = DefaultBoardRootName;
    [SerializeField] private Transform mItemRoot;
    [SerializeField] private string mItemRootName = DefaultItemRootName;
    [SerializeField] private GameObject mCardTemplate;
    [SerializeField] private GameObject mCardFaceTemplate;
    [SerializeField] private GameObject mPlayerCardTemplate;
    [SerializeField] private Transform mScenePlayerCardPlaceholder;
    [SerializeField] private string mScenePlayerCardName = DefaultScenePlayerCardName;
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
        mCardFaceTemplate = BakedCardPrefabRefs.ResolveCardExample(mCardFaceTemplate);
        mPlayerCardTemplate = BakedCardPrefabRefs.ResolvePlayerCard(mPlayerCardTemplate);
        mCardTemplate = BakedCardPrefabRefs.ResolveStandardCard(mCardTemplate);
        mCardComposer = new BakedCardFaceComposer(mCardFaceTemplate, mPlayerCardTemplate);

        if (mBootstrapPlayerCardOnly &&
            !this.GetModel<IRunModel>().IsRunActive.Value)
        {
            this.SendCommand(new StartNewRunCommand(skipOpeningDeal: true));
        }

        BuildSlotInputs();
        BuildCardVisuals();
        EnsureBattleEffectPreviewController();
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
            mBoardRoot = FindSceneRoot(string.IsNullOrWhiteSpace(mBoardRootName)
                ? DefaultBoardRootName
                : mBoardRootName);
        }

        if (mEnableLegacyGreyboxPresentation && mItemRoot == null)
        {
            mItemRoot = FindSceneRoot(string.IsNullOrWhiteSpace(mItemRootName)
                ? DefaultItemRootName
                : mItemRootName);
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
            mCardTemplate = BakedCardPrefabRefs.ResolveStandardCard(null);
        }

        if (mCardFaceTemplate == null)
        {
            mCardFaceTemplate = BakedCardPrefabRefs.ResolveCardExample(null);
        }

        if (mPlayerCardTemplate == null)
        {
            mPlayerCardTemplate = BakedCardPrefabRefs.ResolvePlayerCard(null);
        }

        if (mScenePlayerCardPlaceholder == null && mBoardRoot != null)
        {
            var playerSlot = FindChild(mBoardRoot, "CardSlot5ForPlayer");
            if (playerSlot != null)
            {
                var placeholderName = string.IsNullOrWhiteSpace(mScenePlayerCardName)
                    ? DefaultScenePlayerCardName
                    : mScenePlayerCardName;
                mScenePlayerCardPlaceholder = FindChild(playerSlot, placeholderName);
            }
        }

        if (mBoardRoot == null || mCardTemplate == null)
        {
            Debug.LogError($"GameplayWorldPresenter could not find required scene objects. BoardRoot={mBoardRoot != null} ItemRoot={mItemRoot != null} CardTemplate={mCardTemplate != null} CardFaceTemplate={mCardFaceTemplate != null} PlayerCardTemplate={mPlayerCardTemplate != null}");
            enabled = false;
            return;
        }

        if (mItemRoot == null)
        {
            Debug.LogWarning($"GameplayWorldPresenter item slots are unavailable. ItemRootName='{mItemRootName}'. Board cards will still render.");
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
            if (mItemRoot == null)
            {
                break;
            }

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
            if (mItemRoot == null)
            {
                break;
            }

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

    private void EnsureBattleEffectPreviewController()
    {
        if (GetComponent<GameplayBattleEffectPreviewController>() == null)
        {
            gameObject.AddComponent<GameplayBattleEffectPreviewController>();
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
            SyncPlayerCardPlaceholder(false);
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
            if (slot == 5)
            {
                SyncPlayerCardPlaceholder(view.gameObject.activeSelf && view.Data != null);
            }
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
            if (slot.Value == 5)
            {
                SyncPlayerCardPlaceholder(false);
            }
            return;
        }

        var isPending = deckModel.PendingHelpCardAction.IsActive && deckModel.PendingHelpCardAction.HelpCardUid.Equals(uid.Value);
        RefreshCardView(view, uid.Value, false, isPending);
        if (slot.Value == 5)
        {
            SyncPlayerCardPlaceholder(true);
        }
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

    private CardView CreateCardVisual(string objectName, Vector3 worldPosition, Transform parent, float scaleMultiplier)
    {
        var instance = Instantiate(mCardTemplate, worldPosition, Quaternion.identity, parent);
        instance.name = objectName;
        instance.SetActive(true);
        instance.transform.position = worldPosition;
        instance.transform.localScale = Vector3.one;

        var view = instance.GetComponent<CardView>();
        if (view == null)
        {
            view = instance.AddComponent<CardView>();
        }

        view.Initialize();
        view.SetTargetWorldHeight(mReferenceWorldCardHeight * scaleMultiplier);
        view.Hide();
        return view;
    }

    private void RefreshCardView(CardView view, CardUid uid, bool itemSlot, bool pending)
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

        return mCardComposer.ComposeSet(renderData, BakedCardRenderDataFactory.StandardPixelsPerUnit);
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

    private void SyncPlayerCardPlaceholder(bool hidePlaceholder)
    {
        if (mScenePlayerCardPlaceholder == null)
        {
            return;
        }

        mScenePlayerCardPlaceholder.gameObject.SetActive(!hidePlaceholder);
    }

    private static Transform FindSceneRoot(string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
        {
            return null;
        }

        var active = GameObject.Find(objectName);
        if (active != null)
        {
            return active.transform;
        }

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return null;
        }

        var roots = scene.GetRootGameObjects();
        for (var i = 0; i < roots.Length; i++)
        {
            if (roots[i] != null && roots[i].name == objectName)
            {
                return roots[i].transform;
            }
        }

        return null;
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
            return BakedCardRenderDataFactory.CanonicalWorldCardHeight;
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

        return BakedCardRenderDataFactory.CanonicalWorldCardHeight;
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
    private bool mSuppressNextMouseUp;

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
        if (mSuppressNextMouseUp || Input.GetMouseButtonUp(1))
        {
            mSuppressNextMouseUp = false;
            return;
        }

        if (TableNine.IsInitialized)
        {
            this.GetUtility<IAudioUtility>().Play(TableNineAudioIds.Click);
        }

        this.SendCommand(new ClickBoardSlotCommand(new BoardSlotNo(mSlotNo)));
    }

    private void OnMouseOver()
    {
        if (!Input.GetMouseButtonDown(1))
        {
            return;
        }

        var effectController = FindObjectOfType<GameplayBattleEffectPreviewController>();
        if (effectController != null && effectController.TryPlayRightClickBattleEffect(new BoardSlotNo(mSlotNo)))
        {
            mSuppressNextMouseUp = true;
            if (TableNine.IsInitialized)
            {
                this.GetUtility<IAudioUtility>().Play(TableNineAudioIds.Hit);
            }
        }
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
