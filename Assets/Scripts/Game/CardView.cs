using QFramework;
using UnityEngine;

public class CardView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer mArtworkRenderer;
    [SerializeField] private TextMesh mTextMesh;

    public CardUid? BoundUid { get; private set; }
    public CardViewData Data { get; private set; }

    public virtual void Initialize()
    {
        if (mArtworkRenderer == null)
        {
            mArtworkRenderer = GetComponent<SpriteRenderer>();
        }

        if (mTextMesh == null)
        {
            mTextMesh = GetComponentInChildren<TextMesh>();
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
        mTextMesh.color = new Color(0.14f, 0.14f, 0.14f);

        var renderer = mTextMesh.GetComponent<MeshRenderer>();
        if (renderer != null && mArtworkRenderer != null)
        {
            renderer.sortingOrder = mArtworkRenderer.sortingOrder + 1;
        }
    }

    public void Bind(CardViewData data, bool itemSlot, bool pending, Sprite sprite)
    {
        Data = data;
        BoundUid = data != null ? data.Uid : (CardUid?)null;
        if (data == null)
        {
            Hide();
            return;
        }

        Show(CardViewDataFactory.FormatWorldCard(data, itemSlot, pending), data.Tint, sprite);
    }

    public void Show(string cardText, Color cardColor)
    {
        Show(cardText, cardColor, null);
    }

    public void Show(string cardText, Color cardColor, Sprite sprite)
    {
        gameObject.SetActive(true);
        if (mArtworkRenderer != null)
        {
            if (sprite != null)
            {
                mArtworkRenderer.sprite = sprite;
            }

            mArtworkRenderer.color = cardColor;
        }

        if (mTextMesh != null)
        {
            mTextMesh.text = cardText;
        }
    }

    public void Hide()
    {
        BoundUid = null;
        Data = null;
        gameObject.SetActive(false);
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

    public void Bind(CardViewData data, bool pending, Sprite sprite)
    {
        if (mCardView != null)
        {
            mCardView.Bind(data, IsItemSlot, pending, sprite);
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

    public void Refresh(CardView view, CardUid uid, bool itemSlot, bool pending)
    {
        var data = Create(uid);
        var sprite = data != null
            ? this.GetUtility<IResourceUtility>().LoadCardSprite(data.SpriteId)
            : null;
        view.Bind(data, itemSlot, pending, sprite);
    }
}
