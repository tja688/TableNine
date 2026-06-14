using System.Collections.Generic;
using QFramework;
using TMPro;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DefaultExecutionOrder(100)]
public sealed class RandomCardPreviewDemo : MonoBehaviour, IController
{
    private const string StandardCardPrefabPath = "Assets/Prefabs/Cards/Card.prefab";

    [SerializeField] private Transform mCardRoot;
    [SerializeField] private GameObject mCardPrefab;
    [SerializeField] private GameObject mCardFaceTemplate;
    [SerializeField] private GameObject mPlayerCardTemplate;
    [SerializeField] private UIChoiceOverlayPanel mChoiceOverlay;
    [SerializeField] private int mChoicePreviewIndex;
    [SerializeField] private float mFallbackWorldCardHeight = 3.64f;
    [SerializeField] private TMP_Text mDescriptionText;
    [SerializeField] private Camera mCamera;
    [SerializeField] private string mDefaultHint = "按 Tab 切换随机卡牌，鼠标指向卡牌查看描述";

    private readonly List<CardDefinition> mRandomPool = new List<CardDefinition>();
    private CardDefinition mCurrentCard;
    private CardView mWorldCardView;
    private Collider2D mHoverCollider;
    private string mDefaultDescription;
    private bool mHoveringCard;
    private BakedCardFaceComposer mComposer;

    public IArchitecture GetArchitecture()
    {
        return TableNine.Interface;
    }

    private void Awake()
    {
        if (mCardRoot == null)
        {
            mCardRoot = CardExampleFaceBinder.FindChild(transform, "CardExample");
        }

        if (mCardRoot == null)
        {
            mCardRoot = GameObject.Find("CardExample")?.transform;
        }

#if UNITY_EDITOR
        if (mCardPrefab == null)
        {
            mCardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(StandardCardPrefabPath);
        }
#endif

        if (mCardPrefab == null)
        {
            Debug.LogError("[RandomCardPreviewDemo] Card prefab is missing.");
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

        if (mChoiceOverlay == null)
        {
            mChoiceOverlay = FindObjectOfType<UIChoiceOverlayPanel>(true);
        }

        if (mDescriptionText != null)
        {
            mDescriptionText.enableWordWrapping = true;
            mDescriptionText.overflowMode = TMPro.TextOverflowModes.Overflow;
        }

        mDefaultDescription = mDescriptionText != null
            ? mDescriptionText.text
            : DescriptionPanelTexts.Get(DescriptionPanelTextKeys.HudDefaultHint);
    }

    private void Start()
    {
        if (!enabled || mCardRoot == null)
        {
            return;
        }

        if (!TableNine.IsInitialized)
        {
            TableNine.InitArchitecture();
        }

        mComposer = new BakedCardFaceComposer(mCardFaceTemplate, mPlayerCardTemplate);
        if (mChoiceOverlay != null)
        {
            mChoiceOverlay.Show();
        }

        EnsureWorldCardView();
        BuildRandomPool();
        ShowRandomCard();
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
        if (!TableNine.IsInitialized || mRandomPool.Count == 0)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            ShowRandomCard();
        }

        mHoveringCard = IsPointerOverCard();
    }

    private void LateUpdate()
    {
        if (mDescriptionText == null || UIGameplayPanel.IsSidePanelHovered)
        {
            return;
        }

        if (mHoveringCard && mCurrentCard != null)
        {
            mDescriptionText.text = CardPreviewDescriptionComposer.Compose(this.GetModel<IConfigModel>(), mCurrentCard);
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

    private void BuildRandomPool()
    {
        mRandomPool.Clear();
        var config = this.GetModel<IConfigModel>();
        AddCards(config.GetCardsByType(CardType.Monster));
        AddCards(config.GetCardsByType(CardType.Help));
        AddCards(config.GetCardsByType(CardType.Tutor));
    }

    private void AddCards(IReadOnlyList<CardDefinition> cards)
    {
        if (cards == null)
        {
            return;
        }

        for (var i = 0; i < cards.Count; i++)
        {
            var card = cards[i];
            if (card == null)
            {
                continue;
            }

            if (card.CardType is CardType.Player or CardType.Room)
            {
                continue;
            }

            mRandomPool.Add(card);
        }
    }

    private void ShowRandomCard()
    {
        if (mRandomPool.Count == 0 || mWorldCardView == null || mComposer == null)
        {
            Debug.LogWarning("[RandomCardPreviewDemo] No random cards available in config.");
            return;
        }

        CardDefinition nextCard;
        if (mRandomPool.Count == 1)
        {
            nextCard = mRandomPool[0];
        }
        else
        {
            do
            {
                nextCard = mRandomPool[Random.Range(0, mRandomPool.Count)];
            } while (mCurrentCard != null && nextCard.CardId == mCurrentCard.CardId);
        }

        mCurrentCard = nextCard;
        var renderData = BakedCardRenderDataFactory.CreateFromCardDefinition(this.GetModel<IConfigModel>(), mCurrentCard);
        if (renderData == null)
        {
            return;
        }

        if (mCurrentCard.CardType == CardType.Monster)
        {
            renderData.Life = Mathf.Max(1, mCurrentCard.BaseHp + Random.Range(-3, 4));
            renderData.Attack = Mathf.Max(0, mCurrentCard.BaseAttack + Random.Range(-1, 3));
            renderData.Defense = Mathf.Max(0, mCurrentCard.BaseArmor + Random.Range(0, 3));
        }

        var sprites = mComposer.ComposeSet(renderData, BakedCardRenderDataFactory.StandardPixelsPerUnit);
        mWorldCardView.ShowBaked(sprites);

        if (mChoiceOverlay != null && sprites.HasFace)
        {
            mChoiceOverlay.SetChoiceCardPreview(mChoicePreviewIndex, sprites.FaceSprite, mCurrentCard.DisplayName);
        }
    }

    private bool IsPointerOverCard()
    {
        if (mHoverCollider == null || mCamera == null)
        {
            return false;
        }

        var worldPoint = mCamera.ScreenToWorldPoint(Input.mousePosition);
        return mHoverCollider.OverlapPoint(worldPoint);
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

    private void EnsureWorldCardView()
    {
        if (mWorldCardView != null)
        {
            return;
        }

        if (mCardRoot != null)
        {
            mCardRoot.gameObject.SetActive(false);
        }

        var cardObject = Instantiate(mCardPrefab, transform, false);
        cardObject.name = "Card";
        cardObject.transform.localPosition = Vector3.zero;
        cardObject.transform.localRotation = Quaternion.identity;
        cardObject.transform.localScale = Vector3.one;

        mWorldCardView = cardObject.GetComponent<CardView>();
        if (mWorldCardView == null)
        {
            mWorldCardView = cardObject.AddComponent<CardView>();
        }

        mWorldCardView.Initialize();
        mWorldCardView.SetTargetWorldHeight(MeasureReferenceWorldHeight());
        mWorldCardView.gameObject.SetActive(true);

        mHoverCollider = mWorldCardView.GetComponent<Collider2D>();
    }

    private float MeasureReferenceWorldHeight()
    {
        if (mCardRoot == null)
        {
            return BakedCardRenderDataFactory.CanonicalWorldCardHeight;
        }

        var collider = mCardRoot.GetComponent<Collider2D>();
        if (collider != null)
        {
            var size = collider.bounds.size.y;
            if (size > 0f)
            {
                return size;
            }
        }

        var renderer = mCardRoot.GetComponentInChildren<SpriteRenderer>(true);
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
}
