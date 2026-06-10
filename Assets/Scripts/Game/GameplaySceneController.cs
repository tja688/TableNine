using System.Collections.Generic;
using QFramework;
using UnityEngine;

public class GameplayWorldPresenter : MonoBehaviour, IController
{
    private readonly Dictionary<int, GameplayCardVisual> mBoardCardViews = new Dictionary<int, GameplayCardVisual>();
    private readonly Dictionary<int, GameplayCardVisual> mItemCardViews = new Dictionary<int, GameplayCardVisual>();
    private readonly List<IUnRegister> mEventRegisters = new List<IUnRegister>();

    [SerializeField] private Transform mBoardRoot;
    [SerializeField] private Transform mItemRoot;
    [SerializeField] private GameObject mCardTemplate;

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

        CacheSceneReferences();
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
    }

    private void LateUpdate()
    {
        if (!TableNine.IsInitialized)
        {
            return;
        }

        RefreshAllCardViews();
    }

    private void CacheSceneReferences()
    {
        if (mBoardRoot == null)
        {
            mBoardRoot = GameObject.Find("NineGrid CardSlots")?.transform;
        }

        if (mItemRoot == null)
        {
            mItemRoot = GameObject.Find("Item CardSlots")?.transform;
        }

        if (mCardTemplate == null)
        {
            mCardTemplate = GameObject.Find("NineGrid CardSlots/CardExample");
        }

        if (mCardTemplate == null)
        {
            mCardTemplate = GameObject.Find("CardExample");
        }

        if (mBoardRoot == null || mItemRoot == null || mCardTemplate == null)
        {
            Debug.LogError($"GameplayWorldPresenter could not find required scene objects. BoardRoot={mBoardRoot != null} ItemRoot={mItemRoot != null} CardTemplate={mCardTemplate != null}");
            enabled = false;
            return;
        }

        mCardTemplate.SetActive(false);
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

            mBoardCardViews[slot] = CreateCardVisual($"BoardCardView{slot}", slotObject.position, mBoardRoot, 1f);
        }

        for (var slot = 0; slot < 5; slot++)
        {
            var slotObject = FindChild(mItemRoot, $"CardSlot{slot + 1}");
            if (slotObject == null)
            {
                continue;
            }

            mItemCardViews[slot] = CreateCardVisual($"ItemCardView{slot + 1}", slotObject.position, mItemRoot, 0.85f);
        }
    }

    private void RegisterGameplayEvents()
    {
        mEventRegisters.Add(this.RegisterEvent<BoardSlotChangedEvent>(_ => RefreshBoardCardViews()));
        mEventRegisters.Add(this.RegisterEvent<ItemSlotChangedEvent>(_ => RefreshItemCardViews()));
        mEventRegisters.Add(this.RegisterEvent<DamageAppliedEvent>(_ => RefreshAllCardViews()));
        mEventRegisters.Add(this.RegisterEvent<MonsterKilledEvent>(_ => RefreshAllCardViews()));
        mEventRegisters.Add(this.RegisterEvent<BattleDeckChangedEvent>(_ => RefreshAllCardViews()));
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

            var data = CardViewDataFactory.Create(this, uid.Value);
            var isPending = deckModel.PendingHelpCardAction.IsActive && deckModel.PendingHelpCardAction.HelpCardUid.Equals(uid.Value);
            view.Show(FormatCardText(data, false, isPending), data.Tint);
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

            var data = CardViewDataFactory.Create(this, uid.Value);
            var isPending = deckModel.PendingHelpCardAction.IsActive && deckModel.PendingHelpCardAction.HelpCardUid.Equals(uid.Value);
            view.Show(FormatCardText(data, true, isPending), data.Tint);
        }
    }

    private static string FormatCardText(CardViewData data, bool itemSlot, bool pending)
    {
        var text = CardViewDataFactory.FormatWorldCard(data, itemSlot);
        return pending ? $"{text}\n等待操作" : text;
    }

    private GameplayCardVisual CreateCardVisual(string objectName, Vector3 worldPosition, Transform parent, float scaleMultiplier)
    {
        var instance = Instantiate(mCardTemplate, worldPosition, Quaternion.identity, parent);
        instance.name = objectName;
        instance.SetActive(true);
        instance.transform.position = worldPosition;
        instance.transform.localScale = mCardTemplate.transform.localScale * scaleMultiplier;

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
        view.Hide();
        return view;
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
}

// Compatibility shim for the existing scene component. New scenes should use GameplayWorldPresenter.
public sealed class GameplaySceneController : GameplayWorldPresenter
{
}

public sealed class GameplayCardVisual : MonoBehaviour
{
    private SpriteRenderer mSpriteRenderer;
    private TextMesh mTextMesh;

    public void Initialize()
    {
        mSpriteRenderer = GetComponent<SpriteRenderer>();
        mTextMesh = GetComponentInChildren<TextMesh>();
        if (mTextMesh == null)
        {
            var textObject = new GameObject("CardText", typeof(TextMesh));
            textObject.transform.SetParent(transform, false);
            textObject.transform.localPosition = new Vector3(0f, 0f, -0.1f);
            mTextMesh = textObject.GetComponent<TextMesh>();
        }

        mTextMesh.anchor = TextAnchor.MiddleCenter;
        mTextMesh.alignment = TextAlignment.Center;
        mTextMesh.fontSize = 40;
        mTextMesh.characterSize = 0.08f;
        mTextMesh.color = new Color(0.14f, 0.14f, 0.14f);

        var renderer = mTextMesh.GetComponent<MeshRenderer>();
        if (renderer != null && mSpriteRenderer != null)
        {
            renderer.sortingOrder = mSpriteRenderer.sortingOrder + 1;
        }
    }

    public void Show(string cardText, Color cardColor)
    {
        gameObject.SetActive(true);
        if (mSpriteRenderer != null)
        {
            mSpriteRenderer.color = cardColor;
        }

        if (mTextMesh != null)
        {
            mTextMesh.text = cardText;
        }
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
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
        this.SendCommand(new ClickItemSlotCommand(mItemSlotIndex));
    }
}
