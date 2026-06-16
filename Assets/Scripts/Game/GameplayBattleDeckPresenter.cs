using System.Collections.Generic;
using QFramework;
using TMPro;
using UnityEngine;

[DefaultExecutionOrder(130)]
public sealed class GameplayBattleDeckPresenter : MonoBehaviour, IController
{
    private const string DefaultDeckSlotName = "CardDeckSlot";
    private const string DefaultDeckLeftTextName = "DeckLeftText";
    private const string RuntimeDeckViewName = "RuntimeBattleDeckView";
    private const int DeckSortingOrder = 740;

    private readonly List<IUnRegister> mEventRegisters = new List<IUnRegister>();

    [SerializeField] private Transform mDeckSlot;
    [SerializeField] private string mDeckSlotName = DefaultDeckSlotName;
    [SerializeField] private TextMeshProUGUI mDeckLeftText;
    [SerializeField] private string mDeckLeftTextName = DefaultDeckLeftTextName;
    [SerializeField] private GameObject mCardPrefab;
    [SerializeField] private GameObject mCardFaceTemplate;
    [SerializeField] private GameObject mPlayerCardTemplate;
    [SerializeField] private float mWorldCardHeight = BakedCardRenderDataFactory.CanonicalWorldCardHeight * 0.86f;
    [SerializeField] private Vector3 mDeckWorldOffset = new Vector3(0f, 0f, -0.08f);

    private CardView mDeckView;
    private BakedCardFaceComposer mComposer;
    private TMP_Text mDescriptionText;
    private CardPreview mPreview = CardPreview.Empty;
    private int mRemainingCount;
    private bool mPointerOverDeck;

    public IArchitecture GetArchitecture()
    {
        return TableNine.Interface;
    }

    private void Awake()
    {
        mCardPrefab = BakedCardPrefabRefs.ResolveStandardCard(mCardPrefab);
        mCardFaceTemplate = BakedCardPrefabRefs.ResolveCardExample(mCardFaceTemplate);
        mPlayerCardTemplate = BakedCardPrefabRefs.ResolvePlayerCard(mPlayerCardTemplate);
        mComposer = new BakedCardFaceComposer(mCardFaceTemplate, mPlayerCardTemplate);
    }

    private void OnEnable()
    {
        EnsureSceneReferences();
        EnsureDeckView();
        RegisterEvents();
        RefreshFromModel();
    }

    private void OnDisable()
    {
        for (var i = 0; i < mEventRegisters.Count; i++)
        {
            mEventRegisters[i].UnRegister();
        }

        mEventRegisters.Clear();
        mPointerOverDeck = false;
    }

    private void OnDestroy()
    {
        mComposer?.Dispose();
        mComposer = null;
    }

    private void LateUpdate()
    {
        if (!mPointerOverDeck || mDescriptionText == null || !TableNine.IsInitialized || UIGameplayPanel.IsSidePanelHovered)
        {
            return;
        }

        mDescriptionText.text = ComposeDeckDescription();
    }

    private void RegisterEvents()
    {
        if (!TableNine.IsInitialized || mEventRegisters.Count > 0)
        {
            return;
        }

        mEventRegisters.Add(this.RegisterEvent<BattleDeckChangedEvent>(evt =>
        {
            mRemainingCount = evt.RemainingCount;
            mPreview = evt.Preview;
            RefreshDeckView();
        }));
        mEventRegisters.Add(this.RegisterEvent<FlowPhaseChangedEvent>(_ => RefreshFromModel()));
    }

    private void RefreshFromModel()
    {
        if (!TableNine.IsInitialized || !this.GetModel<IRunModel>().IsRunActive.Value)
        {
            mRemainingCount = 0;
            mPreview = CardPreview.Empty;
            RefreshDeckView();
            return;
        }

        var deckModel = this.GetModel<IDeckModel>();
        mRemainingCount = deckModel.BattleDrawPile.Count;
        mPreview = deckModel.NextBattleCardPreview.Value;
        RefreshDeckView();
    }

    private void RefreshDeckView()
    {
        if (mDeckLeftText != null)
        {
            mDeckLeftText.text = mRemainingCount.ToString();
        }

        if (mDeckView == null)
        {
            return;
        }

        mDeckView.transform.position = ResolveDeckPosition();
        mDeckView.transform.rotation = Quaternion.identity;

        if (!TableNine.IsInitialized || mRemainingCount <= 0 || mPreview.IsEmpty)
        {
            mDeckView.Hide();
            return;
        }

        var data = CardViewDataFactory.Create(this, mPreview.Uid);
        var sprites = ComposeCardSprites(data);
        mDeckView.Bind(data, false, false, sprites);
        mDeckView.DisplayAdapter?.SetFaceVisible(true);
        mDeckView.gameObject.SetActive(true);
        ApplySortingOrder(mDeckView, DeckSortingOrder);
    }

    private string ComposeDeckDescription()
    {
        if (mRemainingCount <= 0 || mPreview.IsEmpty)
        {
            return "战斗牌组\n剩余 0 张";
        }

        var description = string.Empty;
        var configModel = this.GetModel<IConfigModel>();
        if (configModel.TryGetCardDefinition(mPreview.DefinitionId, out var definition))
        {
            description = DescriptionPanelTexts.Sanitize(definition.Description);
        }

        return string.IsNullOrWhiteSpace(description)
            ? $"战斗牌组\n剩余 {mRemainingCount} 张\n下一张：{mPreview.DisplayName}"
            : $"战斗牌组\n剩余 {mRemainingCount} 张\n下一张：{mPreview.DisplayName}\n{description}";
    }

    private void EnsureSceneReferences()
    {
        if (mDeckSlot == null)
        {
            var slotObject = GameObject.Find(string.IsNullOrWhiteSpace(mDeckSlotName)
                ? DefaultDeckSlotName
                : mDeckSlotName);
            if (slotObject != null)
            {
                mDeckSlot = slotObject.transform;
            }
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

        if (mDescriptionText == null)
        {
            var texts = Object.FindObjectsOfType<TMP_Text>(true);
            for (var i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i].name == "DescriptionText")
                {
                    mDescriptionText = texts[i];
                    break;
                }
            }
        }
    }

    private void EnsureDeckView()
    {
        if (mDeckView != null || mCardPrefab == null)
        {
            return;
        }

        var instance = Instantiate(mCardPrefab, ResolveDeckPosition(), Quaternion.identity, transform);
        instance.name = RuntimeDeckViewName;
        instance.transform.rotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        mDeckView = instance.GetComponent<CardView>() ?? instance.AddComponent<CardView>();
        mDeckView.Initialize();
        mDeckView.SetTargetWorldHeight(mWorldCardHeight);

        var hover = instance.GetComponent<BattleDeckHoverProxy>() ?? instance.AddComponent<BattleDeckHoverProxy>();
        hover.Initialize(this);
    }

    private Vector3 ResolveDeckPosition()
    {
        var basePosition = mDeckSlot != null ? mDeckSlot.position : transform.position;
        return basePosition + mDeckWorldOffset;
    }

    private BakedCardSpriteSet ComposeCardSprites(CardViewData data)
    {
        if (mComposer == null || data == null)
        {
            return default;
        }

        var renderData = BakedCardRenderDataFactory.CreateRuntime(this, data);
        if (renderData == null)
        {
            return default;
        }

        return mComposer.ComposeSet(renderData, BakedCardRenderDataFactory.StandardPixelsPerUnit);
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
            renderers[i].sortingLayerName = "Cards_Front";
            renderers[i].sortingOrder = order;
        }
    }

    private void SetDeckHovered(bool hovered)
    {
        mPointerOverDeck = hovered;
    }

    private sealed class BattleDeckHoverProxy : MonoBehaviour
    {
        private GameplayBattleDeckPresenter mOwner;

        public void Initialize(GameplayBattleDeckPresenter owner)
        {
            mOwner = owner;
        }

        private void OnMouseEnter()
        {
            mOwner?.SetDeckHovered(true);
        }

        private void OnMouseOver()
        {
            mOwner?.SetDeckHovered(true);
        }

        private void OnMouseExit()
        {
            mOwner?.SetDeckHovered(false);
        }
    }
}
