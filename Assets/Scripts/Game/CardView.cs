using QFramework;
using UnityEngine;

public class CardView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer mArtworkRenderer;
    [SerializeField] private SpriteRenderer mBackRenderer;
    [SerializeField] private TextMesh mTextMesh;
    [SerializeField] private BoxCollider2D mCollider;
    [SerializeField] private float mTargetWorldHeight = 3.6f;

    public CardUid? BoundUid { get; private set; }
    public CardViewData Data { get; private set; }

    private Vector3 mInitialLocalScale = Vector3.one;
    private Sprite mRuntimeFaceSprite;
    private Sprite mRuntimeBackSprite;

    public virtual void Initialize()
    {
        if (mArtworkRenderer == null)
        {
            mArtworkRenderer = GetComponent<SpriteRenderer>();
        }

        if (mArtworkRenderer == null)
        {
            mArtworkRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        if (mCollider == null)
        {
            mCollider = GetComponent<BoxCollider2D>();
        }

        if (mCollider == null)
        {
            mCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        mCollider.isTrigger = true;
        mBackRenderer = EnsureBackRenderer();
        mInitialLocalScale = transform.localScale;
    }

    public void SetTargetWorldHeight(float targetWorldHeight)
    {
        mTargetWorldHeight = Mathf.Max(0.1f, targetWorldHeight);
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
            ShowBaked(sprites.FaceSprite, sprites.BackSprite, pending);
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
        ReleaseRuntimeSprites();
        transform.localScale = mInitialLocalScale;

        if (mArtworkRenderer != null)
        {
            if (sprite != null)
            {
                mArtworkRenderer.sprite = sprite;
            }

            mArtworkRenderer.color = cardColor;
            mArtworkRenderer.enabled = mArtworkRenderer.sprite != null;
        }

        if (mBackRenderer != null)
        {
            mBackRenderer.enabled = false;
        }

        EnsureTextMesh();
        if (mTextMesh != null)
        {
            mTextMesh.gameObject.SetActive(true);
            mTextMesh.text = cardText;
        }

        SyncCollider(mArtworkRenderer != null ? mArtworkRenderer.sprite : null);
    }

    public void ShowBaked(Sprite faceSprite, Sprite backSprite, bool pending = false)
    {
        gameObject.SetActive(true);
        ReleaseRuntimeSprites();

        mRuntimeFaceSprite = faceSprite;
        mRuntimeBackSprite = backSprite;

        if (mArtworkRenderer != null)
        {
            mArtworkRenderer.sprite = faceSprite;
            mArtworkRenderer.color = pending ? new Color(1f, 0.97f, 0.88f) : Color.white;
            mArtworkRenderer.enabled = faceSprite != null;
        }

        if (mBackRenderer != null)
        {
            mBackRenderer.sprite = backSprite;
            mBackRenderer.enabled = false;
            mBackRenderer.sortingLayerName = mArtworkRenderer != null ? mArtworkRenderer.sortingLayerName : mBackRenderer.sortingLayerName;
            mBackRenderer.sortingOrder = mArtworkRenderer != null ? mArtworkRenderer.sortingOrder : mBackRenderer.sortingOrder;
        }

        if (mTextMesh != null)
        {
            mTextMesh.gameObject.SetActive(false);
        }

        FitToHeight(faceSprite);
        SyncCollider(faceSprite);
    }

    public void Hide()
    {
        BoundUid = null;
        Data = null;
        ReleaseRuntimeSprites();
        gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        ReleaseRuntimeSprites();
    }

    private SpriteRenderer EnsureBackRenderer()
    {
        var existing = transform.Find("CardBack");
        var backTransform = existing;
        if (backTransform == null)
        {
            var backObject = new GameObject("CardBack");
            backTransform = backObject.transform;
            backTransform.SetParent(transform, false);
            backTransform.localPosition = new Vector3(0f, 0f, 0.01f);
            backTransform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        }

        var renderer = backTransform.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = backTransform.gameObject.AddComponent<SpriteRenderer>();
        }

        renderer.enabled = false;
        return renderer;
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
        mTextMesh.color = new Color(0.14f, 0.14f, 0.14f);

        var renderer = mTextMesh.GetComponent<MeshRenderer>();
        if (renderer != null && mArtworkRenderer != null)
        {
            renderer.sortingOrder = mArtworkRenderer.sortingOrder + 1;
        }
    }

    private void FitToHeight(Sprite faceSprite)
    {
        if (faceSprite == null)
        {
            transform.localScale = mInitialLocalScale;
            return;
        }

        var localHeight = faceSprite.bounds.size.y;
        if (localHeight <= 0f)
        {
            return;
        }

        var parentScale = transform.parent != null ? transform.parent.lossyScale.y : 1f;
        if (Mathf.Approximately(parentScale, 0f))
        {
            parentScale = 1f;
        }

        var scale = mTargetWorldHeight / (localHeight * parentScale);
        transform.localScale = mInitialLocalScale * scale;
    }

    private void SyncCollider(Sprite sprite)
    {
        if (mCollider == null)
        {
            return;
        }

        if (sprite == null)
        {
            mCollider.size = new Vector2(1.5f, 2f);
            mCollider.offset = Vector2.zero;
            return;
        }

        mCollider.size = sprite.bounds.size;
        mCollider.offset = sprite.bounds.center;
    }

    private void ReleaseRuntimeSprites()
    {
        ReleaseRuntimeSprite(ref mRuntimeFaceSprite);
        ReleaseRuntimeSprite(ref mRuntimeBackSprite);
    }

    private static void ReleaseRuntimeSprite(ref Sprite sprite)
    {
        if (sprite == null)
        {
            return;
        }

        var texture = sprite.texture;
        if (!IsRuntimeGeneratedFace(sprite, texture))
        {
            sprite = null;
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(sprite);
            if (texture != null)
            {
                Destroy(texture);
            }
        }
        else
        {
            DestroyImmediate(sprite);
            if (texture != null)
            {
                DestroyImmediate(texture);
            }
        }

        sprite = null;
    }

    private static bool IsRuntimeGeneratedFace(Sprite sprite, Texture texture)
    {
        return sprite != null
               && sprite.name.StartsWith("BakedCardFace_")
               && texture != null
               && texture.name.StartsWith("BakedCardFace_");
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
