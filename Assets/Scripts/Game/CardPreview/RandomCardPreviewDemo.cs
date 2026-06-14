using System.Collections.Generic;
using QFramework;
using TMPro;
using UnityEngine;

[DefaultExecutionOrder(100)]
public sealed class RandomCardPreviewDemo : MonoBehaviour, IController
{
    [SerializeField] private Transform mCardRoot;
    [SerializeField] private TMP_Text mDescriptionText;
    [SerializeField] private Camera mCamera;
    [SerializeField] private string mDefaultHint = "按空格切换随机卡牌，鼠标指向卡牌查看描述";

    private readonly List<CardDefinition> mRandomPool = new List<CardDefinition>();
    private CardDefinition mCurrentCard;
    private Collider2D mHoverCollider;
    private string mDefaultDescription;
    private bool mHoveringCard;

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

        if (mCardRoot == null)
        {
            Debug.LogError("[RandomCardPreviewDemo] CardExample root is missing.");
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
            mDescriptionText.overflowMode = TMPro.TextOverflowModes.Overflow;
        }

        mHoverCollider = mCardRoot.GetComponent<Collider2D>();
        if (mHoverCollider == null)
        {
            mHoverCollider = mCardRoot.gameObject.AddComponent<BoxCollider2D>();
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

        mCardRoot.gameObject.SetActive(true);
        BuildRandomPool();
        ShowRandomCard();
    }

    private void Update()
    {
        if (!TableNine.IsInitialized || mRandomPool.Count == 0)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            ShowRandomCard();
        }

        mHoveringCard = IsPointerOverCard();
    }

    private void LateUpdate()
    {
        if (mDescriptionText == null)
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
        if (mRandomPool.Count == 0)
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
        CardExampleFaceBinder.Apply(mCardRoot, mCurrentCard, this.GetModel<IConfigModel>());
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
}
