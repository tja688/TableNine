using QFramework;
using UnityEngine;

public class CardView : MonoBehaviour
{
    [SerializeField] private CardDisplayAdapter mDisplayAdapter;
    [SerializeField] private TextMesh mTextMesh;

    public CardUid? BoundUid { get; private set; }
    public CardViewData Data { get; private set; }
    public CardDisplayAdapter DisplayAdapter => mDisplayAdapter;

    public virtual void Initialize()
    {
        if (mDisplayAdapter == null)
        {
            mDisplayAdapter = GetComponent<CardDisplayAdapter>();
        }

        if (mDisplayAdapter == null)
        {
            mDisplayAdapter = gameObject.AddComponent<CardDisplayAdapter>();
        }

        mDisplayAdapter.Initialize();
    }

    public void SetTargetWorldHeight(float targetWorldHeight)
    {
        if (mDisplayAdapter != null)
        {
            mDisplayAdapter.SetTargetWorldHeight(targetWorldHeight);
        }
    }

    public void Bind(CardViewData data, bool itemSlot, bool pending, BakedCardSpriteSet sprites)
    {
        Data = data;
        BoundUid = data != null ? data.Uid : (CardUid?)null;
        if (data == null)
        {
            Hide();
            return;
        }

        if (sprites.HasFace)
        {
            ShowBaked(sprites, pending);
            return;
        }

        Show(CardViewDataFactory.FormatWorldCard(data, itemSlot, pending), data.Tint, null);
    }

    public void Show(string cardText, Color cardColor)
    {
        Show(cardText, cardColor, null);
    }

    public void Show(string cardText, Color cardColor, Sprite sprite)
    {
        gameObject.SetActive(true);
        if (mDisplayAdapter != null)
        {
            mDisplayAdapter.Hide();
            gameObject.SetActive(true);
        }

        EnsureTextMesh();
        if (mTextMesh != null)
        {
            mTextMesh.gameObject.SetActive(true);
            mTextMesh.text = cardText;
            mTextMesh.color = cardColor;
        }
    }

    public void ShowBaked(BakedCardSpriteSet sprites, bool pending = false)
    {
        if (mDisplayAdapter == null)
        {
            return;
        }

        if (mTextMesh != null)
        {
            mTextMesh.gameObject.SetActive(false);
        }

        mDisplayAdapter.ApplySprites(sprites, pending);
    }

    public void ShowBaked(Sprite faceSprite, Sprite backSprite, bool pending = false)
    {
        ShowBaked(new BakedCardSpriteSet(faceSprite, backSprite), pending);
    }

    public void Hide()
    {
        BoundUid = null;
        Data = null;
        if (mDisplayAdapter != null)
        {
            mDisplayAdapter.Hide();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void EnsureTextMesh()
    {
        if (mTextMesh == null)
        {
            mTextMesh = GetComponentInChildren<TextMesh>(true);
        }

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
        if (mTextMesh.color.a <= 0f)
        {
            mTextMesh.color = new Color(0.14f, 0.14f, 0.14f);
        }
    }
}

public sealed class BoardSlotView : MonoBehaviour
{
    [SerializeField] private int mBoardSlotNo;
    [SerializeField] private int mItemSlotIndex = -1;
    [SerializeField] private CardView mCardView;

    public int BoardSlotNo => mBoardSlotNo;
    public int ItemSlotIndex => mItemSlotIndex;
    public bool IsItemSlot => mItemSlotIndex >= 0;
    public CardView CardView => mCardView;

    public void InitializeBoardSlot(int slotNo)
    {
        mBoardSlotNo = slotNo;
        mItemSlotIndex = -1;
    }

    public void InitializeItemSlot(int itemSlotIndex)
    {
        mBoardSlotNo = 0;
        mItemSlotIndex = itemSlotIndex;
    }

    public void SetCardView(CardView cardView)
    {
        mCardView = cardView;
    }

    public void Bind(CardViewData data, bool pending, BakedCardSpriteSet sprites)
    {
        if (mCardView != null)
        {
            mCardView.Bind(data, IsItemSlot, pending, sprites);
        }
    }

    public void Clear()
    {
        if (mCardView != null)
        {
            mCardView.Hide();
        }
    }
}

public sealed class CardViewPresenter : IController
{
    public IArchitecture GetArchitecture()
    {
        return TableNine.Interface;
    }

    public CardViewData Create(CardUid uid)
    {
        return CardViewDataFactory.Create(this, uid);
    }
}
